using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HuachinDevHabit.Api.Extensions
{
	public static class DatabaseExtension
	{
		public static async Task ApplyMigrationsAsync(this WebApplication app)
		{
			using IServiceScope scope = app.Services.CreateScope();
			await using ApplicationDbContext applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			await using ApplicationIdentityDbContext identityDbContext = scope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>();

			try
			{
				await applicationDbContext.Database.MigrateAsync();

				app.Logger.LogInformation("Application Database migrations applied successfully.");

				await identityDbContext.Database.MigrateAsync();

				app.Logger.LogInformation("Identity Database migrations applied successfully.");
			}
			catch (Exception ex)
			{
				app.Logger.LogError(ex, "An error occurred while applying database migrations.");
				throw;
			}			
		}

		public static async Task SeedInitialDataAsync(this WebApplication app)
		{
			using IServiceScope scope = app.Services.CreateScope();
			RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

			try
			{
				if (!await roleManager.RoleExistsAsync(Roles.Member))
				{
					await roleManager.CreateAsync(new IdentityRole(Roles.Member));
				}
				if (!await roleManager.RoleExistsAsync(Roles.Admin))
				{
					await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
				}

				app.Logger.LogInformation("Initial data seeded successfully.");
			}
			catch (Exception ex)
			{
				app.Logger.LogError(ex, "An error occurred while seeding initial data.");
				throw;
			}
		}
	}
}
