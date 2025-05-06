using FluentValidation;
using FluentValidation.Results;
using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.DTOs.Habits;
using HuachinDevHabit.Api.Entities;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using HuachinDevHabit.Api.Services.Sorting;
using HuachinDevHabit.Api.DTOs.Common;
using HuachinDevHabit.Api.Services.DataShaping;
using System.Dynamic;

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

		[HttpGet()]
		public async Task<IActionResult> GetHabits(
			[FromQuery] HabitsQueryParameters queryParameters,
			SortMappingProvider sortMappingProvider,
			DataShapingService dataShapingService)
		{
			if (!sortMappingProvider.ValidateMappings<HabitDto, Habit>(queryParameters.Sort))
			{
				return Problem(
					statusCode: StatusCodes.Status400BadRequest,
					detail: $"The provided sort parameter isn't valid: '{queryParameters.Sort}'");
			}

			if (!dataShapingService.Validate<HabitDto>(queryParameters.Fields))
			{
				return Problem(
					statusCode: StatusCodes.Status400BadRequest,
					detail: $"The provided data shaping fields aren't valid: '{queryParameters.Fields}'");
			}

			queryParameters.Search ??= queryParameters.Search?.Trim().ToLower();

			SortMapping[] sortMappings = sortMappingProvider.GetMappings<HabitDto, Habit>();

			IQueryable<HabitDto> habitsQuery = _dbContext.Habits
				.Where(h => queryParameters.Search == null ||
							h.Name.Contains(queryParameters.Search, StringComparison.CurrentCultureIgnoreCase) ||
							h.Description != null && h.Description.Contains(queryParameters.Search, StringComparison.CurrentCultureIgnoreCase))
				.Where(h => queryParameters.Type == null || h.Type == queryParameters.Type)
				.Where(h => queryParameters.Status == null || h.Status == queryParameters.Status)
				.ApplySort(queryParameters.Sort, sortMappings)
				.Select(HabitQueries.ProjectToDto());

			int totalCount = await habitsQuery.CountAsync();

			List<HabitDto> habits = await habitsQuery
				.Skip(queryParameters.PageSize * (queryParameters.Page - 1))
				.Take(queryParameters.PageSize)
				.ToListAsync();

			//var paginationResult = new PaginationResult<HabitDto>
			var paginationResult = new PaginationResult<ExpandoObject>
			{
				//Items = habits,
				Items = dataShapingService.ShapeCollectionData(habits, queryParameters.Fields),
				Page = queryParameters.Page,
				PageSize = queryParameters.PageSize,
				TotalCount = totalCount
			};

			return Ok(paginationResult);
		}

		//[HttpGet("GetHabitsV2/{id}")]
		////public async Task<ActionResult<HabitsCollectionDto>> GetHabits(
		////	[FromQuery(Name = "q")] string? search,
		////	HabitType? type,
		////	HabitStatus? status) 
		//public async Task<ActionResult<PaginationResult>> GetHabitsV2(int id, [FromQuery] HabitsQueryParameters queryParameters)
		//{
		//	queryParameters.Search ??= queryParameters.Search?.Trim().ToLower();

		//	IQueryable<Habit> query = _dbContext.Habits;

		//	if (!string.IsNullOrWhiteSpace(queryParameters.Search))
		//	{
		//		//query = query.Where(h => h.Name.ToLower().Contains(search) ||
		//		//						 h.Description != null && h.Description.ToLower().Contains(search));

		//		query = query.Where(h => h.Name.Contains(queryParameters.Search, StringComparison.CurrentCultureIgnoreCase) ||
		//								 h.Description != null && h.Description.Contains(queryParameters.Search, StringComparison.CurrentCultureIgnoreCase));
		//	}

		//	if (queryParameters.Type != null)
		//	{
		//		query = query.Where(h => h.Type == queryParameters.Type);
		//	}

		//	if (queryParameters.Status != null)
		//	{
		//		query = query.Where(h => h.Status == queryParameters.Status);
		//	}

		//	List<HabitDto> habits = await query
		//		.Select(HabitQueries.ProjectToDto())
		//		.ToListAsync();
			
		//	var habitsCollectionDto = new PaginationResult { Data = habits };

		//	return Ok(habitsCollectionDto);
		//}		

		[HttpGet("{id}")]
		public async Task<IActionResult> GetHabit(
			string id,
			string? fields,
			DataShapingService dataShapingService)
		{
			if (!dataShapingService.Validate<HabitWithTagsDto>(fields))
			{
				return Problem(
					statusCode: StatusCodes.Status400BadRequest,
					detail: $"The provided data shaping fields aren't valid: '{fields}'");
			}

			HabitWithTagsDto? habit = await _dbContext
				.Habits
				.Where(h => h.Id == id)
				.Select(HabitQueries.ProjectToDtoWithTags())
				.FirstOrDefaultAsync();

			if (habit == null)
			{
				return NotFound();
			}

			ExpandoObject shapedHabitDto = dataShapingService.ShapeData(habit, fields);

			return Ok(shapedHabitDto);
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
