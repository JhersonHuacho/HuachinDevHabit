using HuachinDevHabit.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HuachinDevHabit.Api.Database
{
	public sealed class ApplicationIdentityDbContext : IdentityDbContext
	{
		public ApplicationIdentityDbContext(DbContextOptions<ApplicationIdentityDbContext> options)
			: base(options)
		{
		}

		public DbSet<RefreshToken> RefreshTokens { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			// Este metodo base.OnModelCreating(builder); lo que hace es 
			// crear las tablas de Identity en la base de datos
			base.OnModelCreating(builder);
			// este metodo lo que hace es crear el esquema de la base de datos
			builder.HasDefaultSchema(Schemas.Identity);
			// si quiero personalizar los nombres de las tablas de Identity para que coincida con la convención de nombres SnakeCase,
			// deberá especificar el nombre de la tabla en el modelo de la siguiente manera:
			builder.Entity<IdentityUser>(b => b.ToTable("asp_net_users"));
			builder.Entity<IdentityRole>(b => b.ToTable("asp_net_roles"));
			builder.Entity<IdentityUserRole<string>>(b => b.ToTable("asp_net_user_roles"));
			builder.Entity<IdentityRoleClaim<string>>(b => b.ToTable("asp_net_role_claims"));
			builder.Entity<IdentityUserClaim<string>>(b => b.ToTable("asp_net_user_claims"));
			builder.Entity<IdentityUserLogin<string>>(b => b.ToTable("asp_net_user_logins"));
			builder.Entity<IdentityUserToken<string>>(b => b.ToTable("asp_net_user_tokens"));

			builder.Entity<RefreshToken>(entity =>
			{
				entity.HasKey(e => e.Id);

				entity.Property(e => e.UserId).HasMaxLength(300);
				entity.Property(e => e.Token).HasMaxLength(1000);

				entity.HasIndex(e => e.Token).IsUnique();

				entity.HasOne(e => e.User)
					.WithMany()
					.HasForeignKey(e => e.UserId)
					.OnDelete(DeleteBehavior.Cascade);
			});
		}
	}
}
