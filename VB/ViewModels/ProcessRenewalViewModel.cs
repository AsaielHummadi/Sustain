using System.ComponentModel.DataAnnotations;

namespace Sustain.ViewModels.SubscriptionVMs
{
    public class ProcessRenewalViewModel
    {
        [Required]
        public int SubscriptionPlanId { get; set; }

        [Required]
        public int RenewSubscriptionId { get; set; }

        [Required]
        [StringLength(19)]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(5)]
        public string ExpiryDate { get; set; } = string.Empty;

        [Required]
        [StringLength(4)]
        public string Cvc { get; set; } = string.Empty;
    }
}
