using HuachinDevHabit.Api.Database.Configurations;
using HuachinDevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace HuachinDevHabit.Api.Database;

public sealed class ApplicationDbContext : DbContext
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
	{
		
	}

	public DbSet<Habit> Habits { get; set; }
	public DbSet<Tag> Tags { get; set; }
	public DbSet<HabitTag> HabitTags { get; set; }
	public DbSet<User> Users { get; set; }
	public DbSet<GitHubAccessToken> GitHubAccessTokens { get; set; }
	public DbSet<Entry> Entries { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.HasDefaultSchema(Schemas.Application);

		//modelBuilder.ApplyConfiguration(new HabitConfiguration());
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
	}
}
