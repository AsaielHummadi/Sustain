using Sustain.Models;

namespace Sustain.ViewModels.EmissionRecordVMs
{
    public class EmissionRecordShowViewModel
    {
        public EmissionRecord EmissionRecord { get; set; }
        public double Emissions { get; set; }
        public List<EmissionRecord> SimilarRecords { get; set; } = new();
    }
}
