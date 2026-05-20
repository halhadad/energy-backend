using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using energy_backend.Core.Common;
using energy_backend.Core.Entities;
using energy_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using energy_backend.Application.Services;

namespace energy_backend.Infrastructure.Services
{
    /// <summary>
    /// Simulates a real single-phase IoT power meter (e.g. Shelly 3EM) sending
    /// readings every 5 seconds per device.
    ///
    /// Per-reading simulated values:
    ///
    ///   VoltageVolts     — 230 V nominal (EU grid) with ±2% sag/swell noise.
    ///                      Realistic: EN 50160 allows ±10%, but typical is ±2%.
    ///
    ///   PowerFactor      — device-type-dependent base PF (0.75–0.99) with small
    ///                      jitter. Resistive loads (heaters) near 1.0; motors and
    ///                      SMPS devices 0.75–0.90.
    ///
    ///   ActivePowerWatts — time-of-day load curve × RatedPowerWatts × noise.
    ///                      P = V × I × PF, so we derive current from this.
    ///
    ///   CurrentAmps      — derived: I = P / (V × PF).
    ///                      We store it explicitly so the UI can show it without
    ///                      recomputing.
    /// </summary>
    public class EnergyReadingSimulator : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EnergyReadingSimulator> _logger;
        private readonly Random _rng = new Random();

        // Stable base watts per device (= RatedPowerWatts, assigned once).
        private readonly Dictionary<Guid, float> _deviceBaseWatts = new();

        // Per-device stable power factor base (assigned once based on device type).
        private readonly Dictionary<Guid, float> _deviceBasePf = new();

        // EU nominal voltage
        private const float NominalVoltage = 230f;

        public EnergyReadingSimulator(
            IServiceScopeFactory scopeFactory,
            ILogger<EnergyReadingSimulator> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await DelayUntilNextSlot(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();
                    var coordinator = scope.ServiceProvider.GetRequiredService<IAggregationCoordinatorService>();

                    var nowSlot = TimeSlots.FloorTo5sUtc(DateTime.UtcNow);

                    var devices = await db.Devices
                        .Include(d => d.Organisation)
                        .AsNoTracking()
                        .ToListAsync(stoppingToken);

                    if (devices.Count == 0)
                    {
                        await DelayUntilNextSlot(stoppingToken);
                        continue;
                    }

                    var existingIds = await db.EnergyReadings
                        .AsNoTracking()
                        .Where(r => r.Timestamp == nowSlot)
                        .Select(r => r.DeviceId)
                        .ToHashSetAsync(stoppingToken);

                    var newReadings = new List<EnergyReading>();

                    foreach (var device in devices)
                    {
                        if (existingIds.Contains(device.DeviceId)) continue;

                        // Stable rated watts (RatedPowerWatts or random default)
                        if (!_deviceBaseWatts.TryGetValue(device.DeviceId, out var baseW))
                        {
                            baseW = device.RatedPowerWatts > 0
                                ? device.RatedPowerWatts
                                : (float)(_rng.NextDouble() * 450.0 + 50.0); // 50–500 W fallback

                            if (device.RatedPowerWatts <= 0)
                                _logger.LogWarning(
                                    "Device '{Name}' ({Id}) has no RatedPowerWatts; using {W:F0} W",
                                    device.Name, device.DeviceId, baseW);

                            _deviceBaseWatts[device.DeviceId] = baseW;
                        }

                        // Stable power factor base per device (assigned by device type)
                        if (!_deviceBasePf.TryGetValue(device.DeviceId, out var basePf))
                        {
                            basePf = BasePowerFactorForDeviceType(device.Type);
                            _deviceBasePf[device.DeviceId] = basePf;
                        }

                        var (activePower, voltage, current, pf) =
                            SimulateReading(baseW, basePf, nowSlot);

                        newReadings.Add(new EnergyReading
                        {
                            EnergyReadingId = Guid.NewGuid(),
                            DeviceId = device.DeviceId,
                            Timestamp = nowSlot,
                            ActivePowerWatts = (float)Math.Round(activePower, 2),
                            VoltageVolts = (float)Math.Round(voltage, 1),
                            CurrentAmps = (float)Math.Round(current, 3),
                            PowerFactor = (float)Math.Round(pf, 3),
                        });
                    }

                    if (newReadings.Count == 0)
                    {
                        await DelayUntilNextSlot(stoppingToken);
                        continue;
                    }

                    await db.EnergyReadings.AddRangeAsync(newReadings, stoppingToken);
                    await db.SaveChangesAsync(stoppingToken);

                    _logger.LogDebug(
                        "Simulator: {Count} readings at {Slot} — watts: {Watts}",
                        newReadings.Count, nowSlot,
                        string.Join(", ", newReadings.Select(r =>
                            $"{r.ActivePowerWatts:F0}W/{r.VoltageVolts:F0}V/{r.CurrentAmps:F2}A/PF={r.PowerFactor:F2}")));

                    // Upsert all devices first, then fire ONE SignalR message per org
                    var byOrg = newReadings
                        .GroupBy(r => devices.First(d => d.DeviceId == r.DeviceId).OrganisationId);

                    foreach (var orgGroup in byOrg)
                    {
                        foreach (var reading in orgGroup)
                            await coordinator.UpsertBucketsAsync(reading);

                        await coordinator.NotifyOrgAsync(
                            orgGroup.First().DeviceId,
                            orgGroup.First().Timestamp);
                    }
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogWarning(ex, "Simulator: duplicate slot; skipping.");
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Simulator: unhandled error");
                }

                await DelayUntilNextSlot(stoppingToken);
            }
        }

        /// <summary>
        /// Simulate a complete instantaneous reading for one device at a given UTC time.
        ///
        /// Returns (activePowerWatts, voltageVolts, currentAmps, powerFactor).
        ///
        /// Simulation model:
        ///   1. Voltage:     230 V ± 2% uniform noise (realistic EU grid sag/swell).
        ///   2. Active power: ratedW × time-of-day load curve × ±20% noise.
        ///   3. Power factor: device-type base PF ± 3% jitter.
        ///   4. Current:     derived as I = P / (V × PF)   [avoids inconsistency].
        /// </summary>
        private (float activePower, float voltage, float current, float pf)
            SimulateReading(float ratedW, float basePf, DateTime utcTime)
        {
            // 1. Voltage: 230 V ± 2%
            var voltageNoise = (_rng.NextDouble() * 0.04) - 0.02; // ±2%
            var voltage = (float)(NominalVoltage * (1.0 + voltageNoise));

            // 2. Active power with time-of-day curve + ±20% noise
            var hour = utcTime.Hour;
            double loadFactor = hour switch
            {
                >= 0 and < 5 => 0.15,
                >= 5 and < 7 => 0.30 + (hour - 5) * 0.15,
                >= 7 and < 9 => 0.65 + (hour - 7) * 0.10,
                >= 9 and < 12 => 0.85,
                >= 12 and < 14 => 0.75,
                >= 14 and < 17 => 0.85,
                >= 17 and < 20 => 1.00,
                >= 20 and < 22 => 0.80,
                _ => 0.50
            };

            var powerNoise = (_rng.NextDouble() * 0.40) - 0.20; // ±20%
            var activePower = (float)Math.Max(0.0, ratedW * loadFactor * (1.0 + powerNoise));

            // 3. Power factor: base ± 3% jitter, clamped 0.5–1.0
            var pfNoise = (_rng.NextDouble() * 0.06) - 0.03;
            var pf = (float)Math.Clamp(basePf + pfNoise, 0.50, 1.00);

            // 4. Current: I = P / (V × PF), guarded against div-by-zero
            var current = (voltage > 0 && pf > 0)
                ? activePower / (voltage * pf)
                : 0f;

            return (activePower, voltage, current, pf);
        }

        /// <summary>
        /// Returns a realistic base power factor for a device type string.
        /// Resistive loads (heaters, incandescent) → ~1.0.
        /// Motors, compressors            → ~0.75–0.85.
        /// SMPS / LED drivers             → ~0.90–0.95.
        /// Unknown / default              → 0.92 (a reasonable SMPS assumption).
        /// </summary>
        private static float BasePowerFactorForDeviceType(string type)
        {
            return (type?.ToLowerInvariant() ?? "") switch
            {
                var t when t.Contains("heater") || t.Contains("resistive") || t.Contains("oven")
                    => 0.99f,
                var t when t.Contains("motor") || t.Contains("pump") || t.Contains("compressor") || t.Contains("hvac")
                    => 0.80f,
                var t when t.Contains("led") || t.Contains("light") || t.Contains("lamp")
                    => 0.90f,
                var t when t.Contains("server") || t.Contains("computer") || t.Contains("pc")
                    => 0.95f,
                var t when t.Contains("fridge") || t.Contains("freezer") || t.Contains("washing")
                    => 0.82f,
                _ => 0.92f
            };
        }

        private static async Task DelayUntilNextSlot(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var next = new DateTime(
                now.Ticks - (now.Ticks % TimeSpan.FromSeconds(5).Ticks) + TimeSpan.FromSeconds(5).Ticks,
                DateTimeKind.Utc);
            var delay = next - now;
            if (delay.TotalMilliseconds < 0) delay = TimeSpan.Zero;
            await Task.Delay(delay, ct);
        }
    }
}