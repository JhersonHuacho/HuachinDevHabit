using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.DTOs.HabitTags;
using HuachinDevHabit.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuachinDevHabit.Api.Controllers
{
	[Authorize]
	[ApiController]
	[Route("habits/{habitId}/tags")]
	public sealed class HabitTagsController : ControllerBase
	{
		public static readonly string Name = nameof(HabitTagsController).Replace("Controller", string.Empty);

		private readonly ApplicationDbContext _dbContext;

		public HabitTagsController(ApplicationDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		// /habits/:id/tags = /tags
		// /habits/:id/tags/:tagId = /tags/:tagId
		//[HttpPut("{tagId}")]
		[HttpPut]
		public async Task<ActionResult> UpsertHabitTags(string habitId, UpsertHabitTagsDto upsertHabitTagsDto)
		{
			Habit? habit = await _dbContext.Habits
				.Include(h => h.HabitTags)
				.FirstOrDefaultAsync(h => h.Id == habitId);

			if (habit == null)
			{
				return NotFound($"Habit with ID '{habitId}' not found.");
			}

			var currentTagsIds = habit.HabitTags.Select(ht => ht.TagId).ToHashSet();
			if (currentTagsIds.SetEquals(upsertHabitTagsDto.TagIds))
			{
				return NoContent();
			}

			List<string> existingTagIds = await _dbContext.Tags
				.Where(t => upsertHabitTagsDto.TagIds.Contains(t.Id))
				.Select(t => t.Id)
				.ToListAsync();

			if (existingTagIds.Count != upsertHabitTagsDto.TagIds.Count)
			{
				return BadRequest($"One or more tag IDs is invalid");
			}

			habit.HabitTags.RemoveAll(ht => !upsertHabitTagsDto.TagIds.Contains(ht.TagId));

			string[] tagIdsToAdd = upsertHabitTagsDto.TagIds
				.Except(currentTagsIds)
				.ToArray();

			habit.HabitTags.AddRange(tagIdsToAdd.Select(tagId => new HabitTag 
			{
				HabitId = habit.Id,
				TagId = tagId,
				CreatedAtUtc = DateTime.UtcNow
			}));

			await _dbContext.SaveChangesAsync();

			return NoContent();
		}

		// /habits/:id/tags = /tags
		// /habits/:id/tags/:tagId = /tags/:tagId
		[HttpDelete("{tagId}")]
		public async Task<ActionResult> DeleteHabitTag(string habitId, string tagId)
		{
			HabitTag? habitTag = await _dbContext.HabitTags
				.SingleOrDefaultAsync(ht => ht.HabitId == habitId && ht.TagId == tagId);

			if (habitTag == null)
			{
				return NotFound();
			}

			_dbContext.HabitTags.Remove(habitTag);

			await _dbContext.SaveChangesAsync();

			return NoContent();
		}
	}
}
