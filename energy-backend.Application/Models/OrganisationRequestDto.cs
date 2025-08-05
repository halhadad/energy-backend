namespace energy_backend.Models
{
    // for post and put
    public class OrganisationRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public float EnergyBudget { get; set; }
    }
}
