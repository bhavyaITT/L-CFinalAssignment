using System.Security.Claims;

namespace PRM.API
{
    /// <summary>
    /// Extension methods on ClaimsPrincipal to extract typed claim values.
    /// Keeps controllers clean — no raw claim string parsing scattered everywhere (DRY).
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>Extracts the UserId from the "sub" JWT claim.</summary>
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? user.FindFirstValue("sub");

            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}
