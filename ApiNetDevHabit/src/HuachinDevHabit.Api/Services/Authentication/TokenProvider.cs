using HuachinDevHabit.Api.DTOs.Auth;
using HuachinDevHabit.Api.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HuachinDevHabit.Api.Services.Authentication
{
	public sealed class TokenProvider
	{
		private readonly JwtAuthOptions _jwtAuthOptions;

		public TokenProvider(IOptions<JwtAuthOptions> jwtAuthOptions)
		{
			_jwtAuthOptions = jwtAuthOptions.Value;
		}

		public AccessTokensDto Create(TokenRequest tokenRequest)
		{
			return new AccessTokensDto(GenerateAccessToken(tokenRequest), GenerateRefreshToken());
		}

		private string GenerateAccessToken(TokenRequest tokenRequest)
		{
			var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtAuthOptions.Key));
			var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

			List<Claim> claims = new()
			{
				new Claim(JwtRegisteredClaimNames.Sub, tokenRequest.UserId),
				new Claim(JwtRegisteredClaimNames.Email, tokenRequest.Email)//,
				//..tokenRequest.Roles.Select(role => new Claim(ClaimTypes.Role, role))

			};
			claims.AddRange(tokenRequest.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(claims),
				Expires = DateTime.UtcNow.AddMinutes(_jwtAuthOptions.ExpirationInMinutes),
				SigningCredentials = credentials,
				Issuer = _jwtAuthOptions.Issuer,
				Audience = _jwtAuthOptions.Audience
			};

			var handler = new JsonWebTokenHandler();

			string accessToken = handler.CreateToken(tokenDescriptor);

			return accessToken;
		}

		private static string GenerateRefreshToken()
		{
			byte[] randomBytes = RandomNumberGenerator.GetBytes(32);
			return Convert.ToBase64String(randomBytes);
		}
	}
}
