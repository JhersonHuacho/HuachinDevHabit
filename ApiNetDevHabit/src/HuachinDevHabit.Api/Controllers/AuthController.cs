using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.DTOs.Auth;
using HuachinDevHabit.Api.DTOs.Users;
using HuachinDevHabit.Api.Entities;
using HuachinDevHabit.Api.Services.Authentication;
using HuachinDevHabit.Api.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace HuachinDevHabit.Api.Controllers
{
	[ApiController]
	[Route("auth")]
	[AllowAnonymous]
	public sealed class AuthController : ControllerBase
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly ApplicationDbContext _applicationDbContext;
		private readonly ApplicationIdentityDbContext _identityDbContext;
		private readonly TokenProvider _tokenProvider;
		private readonly JwtAuthOptions _jwtAuthOptions;

		public AuthController(
			UserManager<IdentityUser> userManager,
			ApplicationDbContext applicationDbContext,
			ApplicationIdentityDbContext identityDbContext,
			TokenProvider tokenProvider,
			IOptions<JwtAuthOptions> jwtAuthOptions)
		{
			_userManager = userManager;
			_applicationDbContext = applicationDbContext;
			_identityDbContext = identityDbContext;
			_tokenProvider = tokenProvider;
			_jwtAuthOptions = jwtAuthOptions.Value;
		}

		[HttpPost("register")]
		public async Task<ActionResult<AccessTokensDto>> Register(RegisterUserDto registerUserDto)
		{
			// Begin transaction
			using var transaction = await _identityDbContext.Database.BeginTransactionAsync();
			_applicationDbContext.Database.SetDbConnection(_identityDbContext.Database.GetDbConnection());
			await _applicationDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction());
			// Begin transaction

			// Create identity user
			var identityUser = new IdentityUser
			{
				UserName = registerUserDto.Email,
				Email = registerUserDto.Email,
			};

			IdentityResult createUserResult = await _userManager.CreateAsync(identityUser, registerUserDto.Password);

			if (!createUserResult.Succeeded)
			{				
				//var extensions = new Dictionary<string, object?>
				//{
				//	{
				//		"errors",
				//		identityResult.Errors.ToDictionary(e => e.Code, e => e.Description)
				//	}
				//};

				var problemDetails = new ProblemDetails
				{
					Status = StatusCodes.Status400BadRequest,
					Title = "Unable to register user",
					Detail = "Unable to register user, please try again"//,
					//Extensions = extensions
				};

				// Puedes agregar un diccionario de errores así:
				problemDetails.Extensions["errors"] = createUserResult.Errors.ToDictionary(e => e.Code, e => e.Description);

				//return Problem(
				//	detail: "Unable to register user, please try againg",
				//	statusCode: StatusCodes.Status400BadRequest,
				//	extensions: extensions
				//);
				return BadRequest(problemDetails);
			}

			// Create app user

			IdentityResult addToRoleResult = await _userManager.AddToRoleAsync(identityUser, Roles.Member);

			if (!addToRoleResult.Succeeded)
			{

				var problemDetails = new ProblemDetails
				{
					Status = StatusCodes.Status400BadRequest,
					Title = "Unable to register user",
					Detail = "Unable to register user, please try again"
				};

				problemDetails.Extensions["errors"] = addToRoleResult.Errors.ToDictionary(e => e.Code, e => e.Description);

				return BadRequest(problemDetails);
			}

			User user = registerUserDto.ToEntity();
			user.IdentityId = identityUser.Id;

			_applicationDbContext.Users.Add(user);
			await _applicationDbContext.SaveChangesAsync();

			var tokenRequest = new TokenRequest(identityUser.Id, identityUser.Email, [Roles.Member]);
			AccessTokensDto accessTokens = _tokenProvider.Create(tokenRequest);

			var refreshToken = new RefreshToken
			{
				Id = Guid.NewGuid(),
				UserId = identityUser.Id,
				Token = accessTokens.RefreshToken,
				ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationInDays)
			};
			_identityDbContext.RefreshTokens.Add(refreshToken);
			await _identityDbContext.SaveChangesAsync();

			// Commit transaction
			await transaction.CommitAsync();
			// Commit transaction			

			return Ok(accessTokens);
		}

		[HttpPost("register/withtransaction")]
		public async Task<IActionResult> RegisterWithoutTransaction(RegisterUserDto registerUserDto)
		{
			// Create identity user
			var identityUser = new IdentityUser
			{
				UserName = registerUserDto.Email,
				Email = registerUserDto.Email,
			};

			IdentityResult identityResult = await _userManager.CreateAsync(identityUser, registerUserDto.Password);

			if (!identityResult.Succeeded)
			{
				//var extensions = new Dictionary<string, object?>
				//{
				//	{
				//		"errors",
				//		identityResult.Errors.ToDictionary(e => e.Code, e => e.Description)
				//	}
				//};

				return Problem(
					detail: "Unable to register user, please try againg",
					statusCode: StatusCodes.Status400BadRequest//,
					//extensions: extensions
				);
			}

			// Create app user

			User user = registerUserDto.ToEntity();
			user.IdentityId = identityUser.Id;

			_applicationDbContext.Users.Add(user);
			await _applicationDbContext.SaveChangesAsync();

			return Ok(user.Id);
		}

		[HttpPost("login")]
		public async Task<ActionResult<AccessTokensDto>> Login(LoginUserDto loginUserDto)
		{
			IdentityUser? identityUser = await _userManager.FindByEmailAsync(loginUserDto.Email);
			if (identityUser is null)
			{
				return NotFound("User not found");
			}

			bool isPasswordValid = await _userManager.CheckPasswordAsync(identityUser, loginUserDto.Password);
			if (!isPasswordValid)
			{
				return Unauthorized("Invalid password");
			}

			IList<string> roles = await _userManager.GetRolesAsync(identityUser);
			var tokenRequest = new TokenRequest(identityUser.Id, identityUser.Email!, roles);
			AccessTokensDto accessTokens = _tokenProvider.Create(tokenRequest);

			var refreshToken = new RefreshToken
			{
				Id = Guid.NewGuid(),
				UserId = identityUser.Id,
				Token = accessTokens.RefreshToken,
				ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationInDays)
			};
			_identityDbContext.RefreshTokens.Add(refreshToken);
			await _identityDbContext.SaveChangesAsync();

			return Ok(accessTokens);
		}

		[HttpPost("refresh")]
		public async Task<ActionResult<AccessTokensDto>> Refresh(RefreshTokenDto refreshTokenDto)
		{
			RefreshToken? refreshToken = await _identityDbContext.RefreshTokens
				.Include(rt => rt.User)
				.FirstOrDefaultAsync(x => x.Token == refreshTokenDto.RefreshToken);

			if (refreshToken is null)
			{
				return Unauthorized("Refresh token not found");
			}

			if (refreshToken.ExpiresAtUtc < DateTime.UtcNow)
			{
				return Unauthorized("Refresh token expired");
			}

			var identityUser = await _userManager.FindByIdAsync(refreshToken.UserId);
			if (identityUser is null)
			{
				return Unauthorized("User not found");
			}

			IList<string> roles = await _userManager.GetRolesAsync(refreshToken.User);
			var tokenRequest = new TokenRequest(refreshToken.User.Id, refreshToken.User.Email!, roles);
			AccessTokensDto accessTokens = _tokenProvider.Create(tokenRequest);

			refreshToken.Token = accessTokens.RefreshToken;
			refreshToken.ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationInDays);
			
			await _identityDbContext.SaveChangesAsync();
			
			return Ok(accessTokens);
		}
	}
}
