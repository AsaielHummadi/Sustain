using Sustain.Models;

namespace Sustain.ViewModels.EmissionRecordVMs
{
    public class EmissionRecordIndexViewModel
    {
        public List<EmissionRecord> EmissionRecords { get; set; } = new();
        public List<EmissionSource> EmissionSources { get; set; } = new();
        public List<Factory> Factories { get; set; } = new();
        public double TotalEmissions { get; set; }
        public double Scope1Emissions { get; set; }
        public double Scope2Emissions { get; set; }
    }
}
