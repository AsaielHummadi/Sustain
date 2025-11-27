using System.Security.Claims;

namespace Sustain.Utilities.Helpers
{
    public static class AuthHelper
    {
        public static bool HasRole(this ClaimsPrincipal user, string roleName)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return false;

            // Super admin check (user ID = 1)
            if (int.TryParse(userIdClaim.Value, out int userId) && userId == 1 && roleName != "normal")
                return true;

            var roleClaim = user.FindFirst(ClaimTypes.Role);
            return roleClaim?.Value == roleName;
        }
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }
        public static int? GetOrganizationId(this ClaimsPrincipal user)
        {
            var orgClaim = user?.FindFirst("OrganizationId");
            if (orgClaim != null && int.TryParse(orgClaim.Value, out int orgId))
            {
                return orgId;
            }
            return null;
        }
        public static bool IsAuthenticated(this ClaimsPrincipal user)
        {
            return user?.Identity?.IsAuthenticated == true;
        }
    }
    
}
