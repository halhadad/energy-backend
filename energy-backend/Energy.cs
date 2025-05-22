using Microsoft.AspNetCore.Authentication;

namespace energy_backend
{
    public class Energy
    {
        public int id { get; set; }
        public float CurrentConsumption { get; set; }
        public float TotalConsumption { get; set; }

    }
}
