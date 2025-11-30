using Sustain.Models;

namespace Sustain.ViewModels.UserVMs
{
    public class PendingInvitationsViewModel
    {
        public List<Invitation> PendingInvitations { get; set; } = new();
        public List<Factory> Factories { get; set; } = new();
    }
}
