using Sustain.Models;

namespace Sustain.ViewModels.SubscriptionVMs
{
    public class BillingViewModel
    {
        public Subscription? Subscription { get; set; }
        public List<Invoice> Invoices { get; set; } = new List<Invoice>();
        public Organization Organization { get; set; } = new Organization();
    }
}
