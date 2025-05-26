using HuachinDevHabit.Api.DTOs.GitHub;
using Refit;

namespace HuachinDevHabit.Api.Services.Refit
{
	[Headers("User-Agent: DevHabit/1.0", "Accept: application/vnd.github+json")]
	public interface IGitHubApi
	{
		[Get("/user")]
		Task<ApiResponse<GitHubUserProfileDto>> GetUserProfileAsync(
			[Authorize(scheme: "Bearer")] string accessToken,
			CancellationToken cancellationToken = default);

		[Get("/users/{username}/events")]
		Task<ApiResponse<List<GitHubEventDto>>> GetUserEventsAsync(
			string username,
			[Authorize(scheme: "Bearer")] string accessToken,
			int page = 1,
			[AliasAs("per_page")] int perPage = 100,
			CancellationToken cancellationToken = default);
	}
}
