using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.DTOs.GitHub;
using HuachinDevHabit.Api.Entities;
using HuachinDevHabit.Api.Services.GitHub;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace HuachinDevHabit.Api.Jobs
{
	[DisallowConcurrentExecution]
	public sealed class GitHubHabitProcessorJob : IJob
	{
		private readonly ApplicationDbContext _applicationDbContext;
		private readonly GitHubAccessTokenService _gitHubAccessTokenService;
		private readonly RefitGitHubService _gitHubService;
		private readonly ILogger<GitHubHabitProcessorJob> _logger;

		private const string PushEventType = "PushEvent";

		public GitHubHabitProcessorJob(
			ApplicationDbContext applicationDbContext, 
			GitHubAccessTokenService gitHubAccessTokenService, 
			RefitGitHubService gitHubService, 
			ILogger<GitHubHabitProcessorJob> logger)
		{
			_applicationDbContext = applicationDbContext;
			_gitHubAccessTokenService = gitHubAccessTokenService;
			_gitHubService = gitHubService;
			_logger = logger;
		}

		public async Task Execute(IJobExecutionContext context)
		{
			string habitId = context.JobDetail.JobDataMap.GetString("habitId")
				?? throw new InvalidOperationException("HabitId not found in job data");

			try
			{
				_logger.LogInformation("Processing GitHub events for habit {HabitId}", habitId);

				// Get the habit and ensure it still exists and is configured for GitHub automation
				Habit? habit = await _applicationDbContext.Habits
					.FirstOrDefaultAsync(h => h.Id == habitId &&
						h.AutomationSource == AutomationSource.GitHub &&
						!h.IsArchived,
						context.CancellationToken);

				if (habit is null)
				{
					_logger.LogWarning("Habit {HabitId} not found or no longer configured for GitHub automation", habitId);
					return;
				}

				// Get the user's GitHub access token
				string? accessToken = await _gitHubAccessTokenService.GetAsync(habit.UserId, context.CancellationToken);

				if (string.IsNullOrWhiteSpace(accessToken))
				{
					_logger.LogWarning("No GitHub access token found for user {UserId}", habit.UserId);
					return;
				}

				// Get GitHub profile
				GitHubUserProfileDto? profile = await _gitHubService.GetUserProfileAsync(
					accessToken,
					context.CancellationToken);

				if (profile is null)
				{
					_logger.LogWarning("Couldn't retrieve GitHub profile for user {UserId}", habit.UserId);
					return;
				}

				// Get GitHub events
				List<GitHubEventDto> gitHubEvents = [];
				const int perPage = 100;
				const int pagesToFetch = 10;

				for (int page = 1; page <= pagesToFetch; page++)
				{
					IReadOnlyList<GitHubEventDto>? pageEvents = await _gitHubService.GetUserEventsAsync(
						profile.Login,
						accessToken,
						page,
						perPage,
						context.CancellationToken);

					if (pageEvents is null || !pageEvents.Any())
					{
						break;
					}

					gitHubEvents.AddRange(pageEvents);
				}

				if (!gitHubEvents.Any())
				{
					_logger.LogWarning("Couldn't retrieve GitHub events for user {UserId}", habit.UserId);
					return;
				}

				// Filter to push events
				var pushEvents = gitHubEvents
					.Where(a => a.Type == PushEventType)
					.ToList();

				_logger.LogInformation("Found {Count} push events for habit {HabitId}", pushEvents.Count, habitId);

				foreach (GitHubEventDto gitHubEventDto in pushEvents)
				{
					// Check if we already have an entry for this event
					bool exists = await _applicationDbContext.Entries.AnyAsync(
						e => e.HabitId == habitId &&
							 e.ExternalId == gitHubEventDto.Id,
						context.CancellationToken);

					if (exists)
					{
						_logger.LogDebug("Entry already exists for event {EventId}", gitHubEventDto.Id);
						continue;
					}

					// Create a new entry
					var entry = new Entry
					{
						Id = $"e_{Guid.NewGuid()}",
						HabitId = habit.Id,
						UserId = habit.UserId,
						Value = 1, // Each push counts as 1
						Notes =
							$"""
							{gitHubEventDto.Actor.Login} pushed:

							{string.Join(
								 Environment.NewLine,
								 gitHubEventDto.Payload.Commits?.Select(c => $"- {c.Message}") ?? [])}
							""",
						Date = DateOnly.FromDateTime(gitHubEventDto.CreatedAt),
						Source = EntrySource.Automation,
						ExternalId = gitHubEventDto.Id,
						CreatedAtUtc = DateTime.UtcNow
					};

					_applicationDbContext.Entries.Add(entry);
					_logger.LogInformation(
						"Created entry for event {EventId} on habit {HabitId}",
						gitHubEventDto.Id,
						habitId);
				}

				await _applicationDbContext.SaveChangesAsync(context.CancellationToken);

				_logger.LogInformation("Completed processing GitHub events for habit {HabitId}", habitId);
			}
			catch (Exception ex)
			{
				_logger.LogError(
					ex,
					"Error processing GitHub events for habit {HabitId}",
					habitId);
				throw;
			}
		}
	}
}
