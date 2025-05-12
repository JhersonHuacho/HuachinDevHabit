using HuachinDevHabit.Api.DTOs.Habits;
using HuachinDevHabit.Api.Entities;
using System.Linq.Expressions;

namespace HuachinDevHabit.Api.DTOs.Tags
{
	internal static class TagQueries
	{
		public static Expression<Func<Tag, TagDto>> ProjectToDto()
		{
			return t => new TagDto
			{
				Id = t.Id,
				Name = t.Name,
				Description = t.Description,
				CreatedAtUtc = t.CreatedAtUtc,
				UpdatedAtUtc = t.UpdatedAtUtc
			};
		}
	}
}
