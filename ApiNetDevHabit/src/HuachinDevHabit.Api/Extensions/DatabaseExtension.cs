using HuachinDevHabit.Api.Database;
using Microsoft.EntityFrameworkCore;

namespace HuachinDevHabit.Api.Extensions
{
	public static class DatabaseExtension
	{
		public static async Task ApplyMigrationsAsync(this WebApplication app)
		{
			using IServiceScope scope = app.Services.CreateScope();
			await using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			try
			{
				await dbContext.Database.MigrateAsync();

				app.Logger.LogInformation("Database migrations applied successfully.");
			}
			catch (Exception ex)
			{
				app.Logger.LogError(ex, "An error occurred while applying database migrations.");
				throw;
			}			
		}
	}
}
