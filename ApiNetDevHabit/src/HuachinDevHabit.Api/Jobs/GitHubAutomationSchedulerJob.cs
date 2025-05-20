using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace HuachinDevHabit.Api.Jobs
{
	[DisallowConcurrentExecution]
	public sealed class GitHubAutomationSchedulerJob : IJob
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly ILogger<GitHubAutomationSchedulerJob> _logger;

		public GitHubAutomationSchedulerJob(
			ApplicationDbContext dbContext, 
			ILogger<GitHubAutomationSchedulerJob> logger)
		{
			_dbContext = dbContext;
			_logger = logger;
		}

		public async Task Execute(IJobExecutionContext context)
		{
			try
			{
				_logger.LogInformation("Starting GitHub automation scheduler job");

				List<Habit> habitsToProcess = await _dbContext.Habits
					.Where(h => h.AutomationSource == AutomationSource.GitHub && !h.IsArchived)
					.ToListAsync(context.CancellationToken);

				_logger.LogInformation("Found {Count} habits with GitHub automation", habitsToProcess.Count);

				foreach (Habit habit in habitsToProcess)
				{
					// Create a trigger for immediate execution
					ITrigger trigger = TriggerBuilder.Create()
						.WithIdentity($"github-habit-{habit.Id}", "github-habits")
						.StartNow()
						.Build();

					// Create the job with habit data
					IJobDetail jobDetail = JobBuilder.Create<GitHubHabitProcessorJob>()
						.WithIdentity($"github-habit-{habit.Id}", "github-habits")
						.UsingJobData("habitId", habit.Id)
						.Build();

					// Schedule the job
					await context.Scheduler.ScheduleJob(jobDetail, trigger);

					_logger.LogInformation("Scheduled processor job for habit {HabitId}", habit.Id);
				}

				_logger.LogInformation("Completed GitHub automation scheduler job");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error executing GitHub automation scheduler job");
				throw;
			}
		}
	}
}
