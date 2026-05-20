using System;
using System.Collections.Generic;

namespace energy_backend.Application.Models.SignalR
{
    /// <summary>
    /// Pushed over SignalR whenever an aggregate bucket is updated.
    /// Contains both org-level totals and per-device breakdowns so the
    /// frontend can render a "Total" line and individual device series
    /// on the same chart.
    /// </summary>
    public class RealTimeChartDataDto
    {
        public string Range { get; set; } = string.Empty;

        /// <summary>
        /// Org-level total buckets (summed/averaged across all devices).
        /// The "Total" series on the chart.
        /// </summary>
        public List<RealTimeChartBucketDto> Buckets { get; set; } = new();

        /// <summary>
        /// Per-device buckets for the same time window.
        /// Each DeviceBucketGroup contains the device metadata and its series.
        /// Null / empty means per-device breakdown was not requested or not available.
        /// </summary>
        public List<DeviceBucketsDto> DeviceSeries { get; set; } = new();
    }

    /// <summary>
    /// One time bucket aggregated across all devices in the org.
    /// </summary>
    public class RealTimeChartBucketDto
    {
        public DateTime Timestamp { get; set; }

        // ── Energy ────────────────────────────────────────────────────────────
        public float TotalEnergy { get; set; }         // kWh

        // ── Active Power ──────────────────────────────────────────────────────
        public float AverageWatts { get; set; }        // W (org total)
        public float MinWatts { get; set; }
        public float MaxWatts { get; set; }

        // ── Electrical quality (org averages) ─────────────────────────────────
        /// <summary>Average line voltage across all devices, in Volts.</summary>
        public float AverageVoltageVolts { get; set; }

        /// <summary>Total current drawn by all devices, in Amperes (sum, not average).</summary>
        public float TotalCurrentAmps { get; set; }

        /// <summary>Power-weighted average power factor across all devices.</summary>
        public float AveragePowerFactor { get; set; }

        // ── Cost (computed from kWh × user rate) ──────────────────────────────
        /// <summary>Estimated cost for this bucket at the user's configured rate.</summary>
        public float EstimatedCost { get; set; }

        public int DataPointsCount { get; set; }
    }

    /// <summary>
    /// Per-device time series for a specific device within the org.
    /// </summary>
    public class DeviceBucketsDto
    {
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public List<DeviceBucketDto> Buckets { get; set; } = new();
    }

    /// <summary>
    /// One time bucket for a single device.
    /// </summary>
    public class DeviceBucketDto
    {
        public DateTime Timestamp { get; set; }
        public float TotalEnergy { get; set; }         // kWh
        public float AverageWatts { get; set; }        // W
        public float AverageVoltageVolts { get; set; } // V
        public float AverageCurrentAmps { get; set; }  // A
        public float AveragePowerFactor { get; set; }  // 0–1
        public float EstimatedCost { get; set; }       // currency
    }
}