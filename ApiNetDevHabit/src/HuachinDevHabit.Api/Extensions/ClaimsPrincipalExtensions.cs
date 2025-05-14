using System.Security.Claims;

namespace HuachinDevHabit.Api.Extensions
{
	public static class ClaimsPrincipalExtensions
	{
		public static string? GetIdentityId(this ClaimsPrincipal? claimsPrincipal)
		{
			string? identityId = claimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier);
			
			return identityId;
		}
		//public static string? GetEmail(this ClaimsPrincipal claimsPrincipal)
		//{
		//	return claimsPrincipal.FindFirstValue(ClaimTypes.Email);
		//}
		//public static string? GetRole(this ClaimsPrincipal claimsPrincipal)
		//{
		//	return claimsPrincipal.FindFirstValue(ClaimTypes.Role);
		//}
	}
}
