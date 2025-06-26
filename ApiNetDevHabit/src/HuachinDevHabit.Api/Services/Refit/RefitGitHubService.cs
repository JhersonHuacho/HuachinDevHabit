using HuachinDevHabit.Api.DTOs.GitHub;
using Refit;

namespace HuachinDevHabit.Api.Services.Refit;

public sealed class RefitGitHubService
{
	private readonly IGitHubApi _gitHubApi;
	private readonly ILogger<RefitGitHubService> _logger;

	public RefitGitHubService(IGitHubApi gitHubApi, ILogger<RefitGitHubService> logger)
	{
		_gitHubApi = gitHubApi;
		_logger = logger;
	}

	public async Task<GitHubUserProfileDto?> GetUserProfileAsync(
		string accessToken,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrEmpty(accessToken);		

		ApiResponse<GitHubUserProfileDto> response = await _gitHubApi.GetUserProfileAsync(accessToken, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			_logger.LogWarning("Failed to get GitHub user profile. Status code: {StatusCode}", response.StatusCode);
			return null;
		}		

		return response.Content;
	}

	public async Task<IReadOnlyList<GitHubEventDto>?> GetUserEventsAsync(
		string username,
		string accessToken,
		int page = 1,
		int perPage = 100,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrEmpty(accessToken);
		ArgumentException.ThrowIfNullOrEmpty(username);

		ApiResponse<List<GitHubEventDto>> response = await _gitHubApi.GetUserEventsAsync(
			username,
			accessToken,
			page,
			perPage,
			cancellationToken: cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			_logger.LogWarning("Failed to get GitHub user events. Status code: {StatusCode}", response.StatusCode);
			return null;
		}		

		return response.Content;
	}
}
