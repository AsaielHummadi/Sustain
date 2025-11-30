using Sustain.Models;

namespace Sustain.ViewModels.DashboardVMs
{
    public class AdminDashboardViewModel
    {
        public User User { get; set; }
        public Organization Organization { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalFactories { get; set; }
        public int FactoriesWithRecords { get; set; }
        public Subscription? CurrentSubscription { get; set; }
        public double DataCompliance { get; set; }
        public List<double> FactoryPerformanceData { get; set; } = new();
        public List<string> FactoryPerformanceLabels { get; set; } = new();
        public List<int> UserDistributionData { get; set; } = new();
        public List<string> UserDistributionLabels { get; set; } = new();
        public List<RecentActivityItem> RecentActivity { get; set; } = new();
    }

}
