using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace energy_backend.Core.Enums
{
    public enum DeviceType
    {

        HVAC,               // Heating, Ventilation, Air Conditioning
        Lighting,           // General lighting circuits
        EVCharger,          // Electric Vehicle Supply Equipment (EVSE)
        IndustrialLoad,     // Heavy machinery, motors, compressors
        Refrigeration,      // Cooling units, cold storage

        // Infrastructure & IT
        ITEquipment,        // Servers, networking gear, data center racks
        OfficeEquipment,    // Computers, monitors, printers

        // Renewable/Generation
        SolarPV,            // Solar Photovoltaic generation
        BatteryStorage,     // BESS (Battery Energy Storage Systems)

        // Monitoring
        EnergyMeter,        // Dedicated revenue-grade meter or sub-meter
        Sensor,             // Environmental, temp, humidity, occupancy

        // Catch-all
        Other
    }
}
