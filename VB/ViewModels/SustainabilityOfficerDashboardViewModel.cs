using Sustain.Models;

namespace Sustain.ViewModels.DashboardVMs
{
    public class SustainabilityOfficerDashboardViewModel
    {
        public User User { get; set; }
        public Organization Organization { get; set; }
        public List<EmissionRecord> EmissionRecords { get; set; } = new();
        public double TotalEmissions { get; set; }
        public double Scope1Emissions { get; set; }
        public double Scope2Emissions { get; set; }
        public Dictionary<string, object> ChartData { get; set; } = new();
        public int? SelectedFactoryId { get; set; }
    }
}
