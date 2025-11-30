namespace Sustain.ViewModels.InvitationVMs
{
    public class AcceptInvitationViewModel
    {
        public string Token { get; set; } = string.Empty;
        public string InvitedEmail { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string? FactoryName { get; set; }

        // Add these missing properties
        public string UserFname { get; set; } = string.Empty;
        public string UserLname { get; set; } = string.Empty;
        public string? UserPhone { get; set; }
        public string UserPassword { get; set; } = string.Empty;
        public string UserPasswordConfirmation { get; set; } = string.Empty;
    }
}
