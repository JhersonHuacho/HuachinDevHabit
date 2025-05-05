using FluentValidation;
using FluentValidation.Results;
using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.DTOs.Habits;
using HuachinDevHabit.Api.Entities;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HuachinDevHabit.Api.Controllers
{
	[ApiController]
	[Route("habits")]
	public sealed class HabitsController : ControllerBase
	{
		private readonly ApplicationDbContext _dbContext;

		public HabitsController(ApplicationDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		[HttpGet]
		public async Task<ActionResult<HabitsCollectionDto>> GetHabits() 
		{
			List<HabitDto> habits = await _dbContext
				.Habits
				.Select(HabitQueries.ProjectToDto())
				.ToListAsync();
			
			var habitsCollectionDto = new HabitsCollectionDto { Data = habits };

			return Ok(habitsCollectionDto);
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<HabitWithTagsDto>> GetHabit(string id)
		{
			HabitWithTagsDto? habit = await _dbContext
				.Habits
				.Where(h => h.Id == id)
				.Select(HabitQueries.ProjectToDtoWithTags())
				.FirstOrDefaultAsync();

			if (habit == null)
			{
				return NotFound();
			}

			return Ok(habit);
		}		

		[HttpPost]
		public async Task<ActionResult<HabitDto>> CreateHabit(
			[FromBody] CreateHabitDto createHabitDto,
			IValidator<CreateHabitDto> validator)
		{
			#region Fluent Validation
			//ValidationResult validationResult = await validator.ValidateAsync(createHabitDto);

			//if (!validationResult.IsValid)
			//{
			//	return BadRequest(validationResult.ToDictionary());
			//}

			await validator.ValidateAndThrowAsync(createHabitDto);
			#endregion

			Habit habit = createHabitDto.ToEntity();
			
			_dbContext.Habits.Add(habit);
			await _dbContext.SaveChangesAsync();

			HabitDto habitDto = habit.ToDto();

			return CreatedAtAction(nameof(GetHabit), new { id = habitDto.Id }, habitDto);
		}

		[HttpPut("{id}")]
		public async Task<ActionResult> UpdateHabit(string id, [FromBody] UpdateHabitDto updateHabitDto)
		{
			Habit? habit = await _dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);
			
			if (habit == null)
			{
				return NotFound();
			}

			habit.UpdateFromDto(updateHabitDto);			
			
			await _dbContext.SaveChangesAsync();
			
			return NoContent();
		}

		//// /habits/:id/tags = /tags
		//// /habits/:id/tags/:tagId = /tags/:tagId
		//[HttpPut("{id}/tags/{tagId}")]
		//public async Task<ActionResult> AddTagToHabit(string id, string tagId)
		//{			
		//	return Ok();
		//}

		//// /habits/:id/tags = /tags
		//// /habits/:id/tags/:tagId = /tags/:tagId
		//[HttpDelete("{id}/tags/{tagId}")]
		//public async Task<ActionResult> RemoveTagFromHabit(string id, string tagId)
		//{
		//	return Ok();
		//}

		[HttpPatch("{id}")]
		public async Task<ActionResult> PatchHabit(string id, JsonPatchDocument<HabitDto> patchDocument)
		{
			Habit? habit = await _dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

			if (habit == null)
			{
				return NotFound();
			}

			HabitDto habitDto = habit.ToDto();

			patchDocument.ApplyTo(habitDto, ModelState);

			//if (!ModelState.IsValid)
			if (!TryValidateModel(habitDto))
				{
				return ValidationProblem(ModelState);
			}

			habit.Name = habitDto.Name;
			habit.Description = habitDto.Description;
			habit.UpdatedAtUtc = DateTime.UtcNow;

			await _dbContext.SaveChangesAsync();

			return NoContent();
		}

		[HttpDelete("{id}")]
		public async Task<ActionResult> DeleteHabit(string id)
		{
			Habit? habit = await _dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);
			if (habit == null)
			{
				return NotFound();
			}

			_dbContext.Habits.Remove(habit);
			await _dbContext.SaveChangesAsync();
			
			return NoContent();
		}
	}
}
