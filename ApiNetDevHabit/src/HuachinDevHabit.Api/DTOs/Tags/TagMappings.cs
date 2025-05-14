using HuachinDevHabit.Api.DTOs.Habits;
using HuachinDevHabit.Api.Entities;

namespace HuachinDevHabit.Api.DTOs.Tags
{
	internal static class TagMappings
	{
		public static TagDto ToDto(this Tag tag)
		{
			TagDto tagDto = new()
			{
				Id = tag.Id,
				Name = tag.Name,
				Description = tag.Description,
				CreatedAtUtc = tag.CreatedAtUtc,
				UpdatedAtUtc = tag.UpdatedAtUtc
			};
			return tagDto;
		}
		public static Tag ToEntity(this CreateTagDto createTagDto, string userId)
		{
			Tag tag = new()
			{
				Id = $"t_{Guid.NewGuid()}",
				UserId = userId,
				Name = createTagDto.Name,
				Description = createTagDto.Description,
				CreatedAtUtc = DateTime.UtcNow
			};
			return tag;
		}

		public static void UpdateFromDto(this Tag habit, UpdateTagDto updateTagDto)
		{
			habit.Name = updateTagDto.Name;
			habit.Description = updateTagDto.Description;
			habit.UpdatedAtUtc = DateTime.UtcNow;
		}
	}
}
