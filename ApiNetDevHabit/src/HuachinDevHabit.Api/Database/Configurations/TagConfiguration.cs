using HuachinDevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuachinDevHabit.Api.Database.Configurations
{
	public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
	{
		public void Configure(EntityTypeBuilder<Tag> builder)
		{
			builder.HasKey(x => x.Id);

			builder.Property(t => t.Id).HasMaxLength(500).IsRequired();

			builder.Property(t => t.Name).IsRequired().HasMaxLength(50);

			builder.Property(t => t.Description).HasMaxLength(500);

			builder.HasIndex(t => new { t.Name }).IsUnique();
		}
	}
}
