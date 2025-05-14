using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.DTOs.GitHub;
using HuachinDevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace HuachinDevHabit.Api.Services.GitHub;

public sealed class GitHubAccessTokenService
{
	private readonly ApplicationDbContext _applicationDbContext;

	public GitHubAccessTokenService(ApplicationDbContext applicationDbContext)
	{
		_applicationDbContext = applicationDbContext;
	}

	public async Task StoreAsync(
		string userId,
		StoreGitHubAccessTokenDto accessTokenDto,
		CancellationToken cancellationToken = default)
	{
		GitHubAccessToken? existingAccessToken = await GetAccessTokenAsync(userId, cancellationToken);

		if (existingAccessToken is not null)
		{
			existingAccessToken.Token = accessTokenDto.AccessToken;
			existingAccessToken.ExpiresAtUtc = DateTime.UtcNow.AddDays(accessTokenDto.ExpiresInDays);
		}
		else
		{
			_applicationDbContext.GitHubAccessTokens.Add(new GitHubAccessToken
			{
				Id = $"gh_{Guid.NewGuid()}",
				UserId = userId,
				Token = accessTokenDto.AccessToken,
				CreatedAtUtc = DateTime.UtcNow,
				ExpiresAtUtc = DateTime.UtcNow.AddDays(accessTokenDto.ExpiresInDays)
			});
		}

		await _applicationDbContext.SaveChangesAsync(cancellationToken);
	}

	public async Task<string?> GetAsync(string userId, CancellationToken cancellationToken = default)
	{
		GitHubAccessToken? gitHubAccessToken = await GetAccessTokenAsync(userId, cancellationToken);

		return gitHubAccessToken?.Token;
	}

	public async Task RevokeAsync(string userId, CancellationToken cancellationToken = default)
	{
		GitHubAccessToken? gitHubAccessToken = await GetAccessTokenAsync(userId, cancellationToken);

		if (gitHubAccessToken is null)
		{
			return;
		}

		_applicationDbContext.GitHubAccessTokens.Remove(gitHubAccessToken);

		await _applicationDbContext.SaveChangesAsync(cancellationToken);
	}

	private async Task<GitHubAccessToken?> GetAccessTokenAsync(string userId, CancellationToken cancellationToken)
	{
		return await _applicationDbContext.GitHubAccessTokens.SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);
	}
}
