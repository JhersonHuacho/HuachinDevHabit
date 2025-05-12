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

			// Already applied by the FK definition (Habit, Tag)
			builder.Property(h => h.HabitId).HasMaxLength(500);
			builder.Property(h => h.TagId).HasMaxLength(500);

			builder.HasOne<Tag>()
				.WithMany()
				.HasForeignKey(ht => ht.TagId);

			builder.HasOne<Habit>()
				.WithMany(h => h.HabitTags)
				.HasForeignKey(ht => ht.HabitId);
		}
	}
}
