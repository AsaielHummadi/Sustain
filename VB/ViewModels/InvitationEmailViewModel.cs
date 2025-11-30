using Sustain.Models;

namespace Sustain.ViewModels.EmailVMs
{
    public class InvitationEmailViewModel
    {
        public Organization Organization { get; set; }
        public InvitationInfo Invitation { get; set; }
        public string InvitationLink { get; set; } = string.Empty;

        public class InvitationInfo
        {
            public string InvitedEmail { get; set; } = string.Empty;
            public RoleInfo Role { get; set; }
            public FactoryInfo Factory { get; set; }
        }

        public class RoleInfo
        {
            public string RoleName { get; set; } = string.Empty;
        }

        public class FactoryInfo
        {
            public string FactoryName { get; set; } = string.Empty;
        }
    }
}
