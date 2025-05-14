using HuachinDevHabit.Api.DTOs.GitHub;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;

namespace HuachinDevHabit.Api.Services.GitHub;

public sealed class GitHubService
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly ILogger<GitHubService> _logger;

	public GitHubService(IHttpClientFactory httpClientFactory, ILogger<GitHubService> logger)
	{
		_httpClientFactory = httpClientFactory;
		_logger = logger;
	}

	public async Task<GitHubUserProfileDto?> GetUserProfileAsync(
		string accessToken,
		CancellationToken cancellationToken = default)
	{
		using HttpClient client = CreateGitHubClient(accessToken);

		HttpResponseMessage response = await client.GetAsync("user", cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			_logger.LogWarning("Failed to get GitHub user profile. Status code: {StatusCode}", response.StatusCode);
			return null;
		}

		string content = await response.Content.ReadAsStringAsync(cancellationToken);

		return JsonConvert.DeserializeObject<GitHubUserProfileDto>(content);
	}

	public async Task<IReadOnlyList<GitHubEventDto>?> GetUserEventsAsync(
		string username,
		string accessToken,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrEmpty(username);

		using HttpClient client = CreateGitHubClient(accessToken);

		HttpResponseMessage response = await client.GetAsync(
			$"users/{username}/events?per_page=100",
			cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			_logger.LogWarning("Failed to get GitHub user events. Status code: {StatusCode}", response.StatusCode);
			return null;
		}

		string content = await response.Content.ReadAsStringAsync(cancellationToken);

		return JsonConvert.DeserializeObject<List<GitHubEventDto>>(content);
	}

	private HttpClient CreateGitHubClient(string accessToken)
	{
		HttpClient client = _httpClientFactory.CreateClient("github");
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

		return client;
	}
}
