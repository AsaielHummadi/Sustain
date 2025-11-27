namespace Sustain.Utilities.Constants
{
    public static class Roles
    {
        public const int ADMINISTRATOR = 3;
        public const int SUSTAINABILITY_OFFICER = 1;
        public const int FACTORY_OPERATOR = 2;

        private static readonly Dictionary<int, string> RoleNames = new()
        {
            { ADMINISTRATOR, "Administrator" },
            { SUSTAINABILITY_OFFICER, "Sustainability Officer" },
            { FACTORY_OPERATOR, "Factory Operator" }
        };
       
        public static string GetName(int roleId)
        {
            return RoleNames.TryGetValue(roleId, out var name) ? name : "Unknown";
        }

        public static IEnumerable<int> GetRoleIds()
        {
            return RoleNames.Keys;
        }
        public static Dictionary<int, string> GetAll()
        {
            return new Dictionary<int, string>(RoleNames);
        }

        public static bool IsValid(int roleId)
        {
            return RoleNames.ContainsKey(roleId);
        }
    }
}
