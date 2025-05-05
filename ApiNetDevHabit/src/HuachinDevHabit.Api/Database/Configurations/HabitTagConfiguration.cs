using HuachinDevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuachinDevHabit.Api.Database.Configurations
{
	public sealed class HabitTagConfiguration : IEntityTypeConfiguration<HabitTag>
	{
		public void Configure(EntityTypeBuilder<HabitTag> builder)
		{
			builder.HasKey(x => new { x.HabitId, x.TagId });

			builder.HasOne<Tag>()
				.WithMany()
				.HasForeignKey(ht => ht.TagId);

			builder.HasOne<Habit>()
				.WithMany()
				.HasForeignKey(ht => ht.HabitId);
		}
	}
}
