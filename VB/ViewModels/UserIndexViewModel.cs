using Sustain.Models;

namespace Sustain.ViewModels.UserVMs
{
    public class UserIndexViewModel
    {
        public List<User> Users { get; set; } = new();
        public List<Factory> Factories { get; set; } = new();
        public List<Invitation> PendingInvitations { get; set; } = new();
    }
}
