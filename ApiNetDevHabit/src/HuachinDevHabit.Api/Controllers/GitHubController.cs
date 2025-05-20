using HuachinDevHabit.Api.DTOs.Common;
using HuachinDevHabit.Api.DTOs.GitHub;
using HuachinDevHabit.Api.Entities;
using HuachinDevHabit.Api.Services.Authentication;
using HuachinDevHabit.Api.Services.ContentNegotiation;
using HuachinDevHabit.Api.Services.GitHub;
using HuachinDevHabit.Api.Services.Hateos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace HuachinDevHabit.Api.Controllers
{
	[Authorize(Roles = Roles.Member)]
	[ApiController]
	[Route("github")]
	[Produces(
		MediaTypeNames.Application.Json,
		CustomMediaTypeNames.Application.JsonV1,
		CustomMediaTypeNames.Application.HateoasJson,
		CustomMediaTypeNames.Application.HateoasJsonV1)]
	public sealed class GitHubController : ControllerBase
	{
		private readonly GitHubAccessTokenService _gitHubAccessTokenService;
		private readonly GitHubService _gitHubService;
		private readonly UserContext _userContext;
		private readonly LinkService _linkService;

		public GitHubController(GitHubAccessTokenService gitHubAccessTokenService, GitHubService gitHubService, UserContext userContext, LinkService linkService)
		{
			_gitHubAccessTokenService = gitHubAccessTokenService;
			_gitHubService = gitHubService;
			_userContext = userContext;
			_linkService = linkService;
		}

		[HttpPut("personal-access-token")]
		public async Task<IActionResult> StoreAccessToken(
			[FromBody] StoreGitHubAccessTokenDto storeGitHubAccessTokenDto,
			CancellationToken cancellationToken = default)
		{
			string? userId = await _userContext.GetUserIdAsync(cancellationToken);
			if (string.IsNullOrWhiteSpace(userId))
			{
				return Unauthorized();
			}

			await _gitHubAccessTokenService.StoreAsync(userId, storeGitHubAccessTokenDto, cancellationToken);

			return NoContent();
		}

		[HttpDelete("personal-access-token")]
		public async Task<IActionResult> RevokeAccessToken(
			CancellationToken cancellationToken = default)
		{
			string? userId = await _userContext.GetUserIdAsync(cancellationToken);
			if (string.IsNullOrWhiteSpace(userId))
			{
				return Unauthorized();
			}

			await _gitHubAccessTokenService.RevokeAsync(userId, cancellationToken);

			return NoContent();
		}

		[HttpGet("profile")]
		public async Task<ActionResult<GitHubUserProfileDto>> GetUserProfile([FromHeader] AcceptHeaderDto acceptHeaderDto,
			CancellationToken cancellationToken = default)
		{
			string? userId = await _userContext.GetUserIdAsync(cancellationToken);
			if (string.IsNullOrWhiteSpace(userId))
			{
				return Unauthorized();
			}

			string? accessToken = await _gitHubAccessTokenService.GetAsync(userId, cancellationToken);
			if (string.IsNullOrWhiteSpace(accessToken))
			{
				return NotFound();
			}

			GitHubUserProfileDto? userProfile = await _gitHubService.GetUserProfileAsync(accessToken, cancellationToken);
			if (userProfile is null)
			{
				return NotFound();
			}

			if (acceptHeaderDto.IncludeLinks)
			{
				userProfile.Links =
				[
					_linkService.Create(nameof(GetUserProfile), "self", HttpMethods.Get, null, "GitHub"),
					_linkService.Create(nameof(StoreAccessToken), "store-token", HttpMethods.Put, null, "GitHub"),
					_linkService.Create(nameof(RevokeAccessToken), "revoke-token", HttpMethods.Delete, null, "GitHub")
				];
			}			

			return Ok(userProfile);
		}
	}
}
