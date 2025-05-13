using Asp.Versioning;
using FluentValidation;
using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.DTOs.Common;
using HuachinDevHabit.Api.DTOs.Habits;
using HuachinDevHabit.Api.Entities;
using HuachinDevHabit.Api.Services.ContentNegotiation;
using HuachinDevHabit.Api.Services.DataShaping;
using HuachinDevHabit.Api.Services.Hateos;
using HuachinDevHabit.Api.Services.Sorting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Dynamic;
using System.Linq.Dynamic.Core;
using System.Net.Mime;

namespace HuachinDevHabit.Api.Controllers;

[Authorize]
[ApiController]
[Route("habits")]
[ApiVersion("1.0")]
[Produces(
	MediaTypeNames.Application.Json,
	CustomMediaTypeNames.Application.JsonV1,
	CustomMediaTypeNames.Application.JsonV2,
	CustomMediaTypeNames.Application.HateoasJson,
	CustomMediaTypeNames.Application.HateoasJsonV1,
	CustomMediaTypeNames.Application.HateoasJsonV2)]
public sealed class HabitsController : ControllerBase
{
	private readonly ApplicationDbContext _dbContext;
	private readonly LinkService _linkService;

	public HabitsController(ApplicationDbContext dbContext, LinkService linkService)
	{
		_dbContext = dbContext;
		_linkService = linkService;
	}

	[HttpGet]
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
			Items = dataShapingService.ShapeCollectionData(
				habits, 
				queryParameters.Fields,
				queryParameters.IncludeLinks ? h => CreateLinksForHabit(h.Id, queryParameters.Fields) : null),
			Page = queryParameters.Page,
			PageSize = queryParameters.PageSize,
			TotalCount = totalCount
		};

		if (queryParameters.IncludeLinks)
		{
			paginationResult.Links = CreateLinksForHabits(
				queryParameters,
				paginationResult.HasNextPage,
				paginationResult.HasPreviousPage);
		}			

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
	[MapToApiVersion(1.0)]
	public async Task<IActionResult> GetHabit(
		string id,
		[FromQuery] HabitQueryParameters queryParameters,
		DataShapingService dataShapingService)
	{
		if (!dataShapingService.Validate<HabitWithTagsDto>(queryParameters.Fields))
		{
			return Problem(
				statusCode: StatusCodes.Status400BadRequest,
				detail: $"The provided data shaping fields aren't valid: '{queryParameters.Fields}'");
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

		ExpandoObject shapedHabitDto = dataShapingService.ShapeData(habit, queryParameters.Fields);

		if (queryParameters.IncludeLinks)
		{
			List<LinkDto> links = CreateLinksForHabit(id, queryParameters.Fields);
			shapedHabitDto.TryAdd("links", links);
		}			

		return Ok(shapedHabitDto);
	}

	[HttpGet("{id}")]
	[ApiVersion(2.0)]
	public async Task<IActionResult> GetHabitV2(
		string id,
		string? fields,
		[FromHeader(Name = "Accept")] string? acceptHeader,
		DataShapingService dataShapingService)
	{
		if (!dataShapingService.Validate<HabitWithTagsDtoV2>(fields))
		{
			return Problem(
				statusCode: StatusCodes.Status400BadRequest,
				detail: $"The provided data shaping fields aren't valid: '{fields}'");
		}

		HabitWithTagsDtoV2? habit = await _dbContext
			.Habits
			.Where(h => h.Id == id)
			.Select(HabitQueries.ProjectToDtoWithTagsV2())
			.FirstOrDefaultAsync();

		if (habit == null)
		{
			return NotFound();
		}

		ExpandoObject shapedHabitDto = dataShapingService.ShapeData(habit, fields);

		if (acceptHeader == CustomMediaTypeNames.Application.HateosJson)
		{
			List<LinkDto> links = CreateLinksForHabit(id, fields);
			shapedHabitDto.TryAdd("links", links);
		}

		return Ok(shapedHabitDto);
	}

	[HttpPost]
	public async Task<ActionResult<HabitDto>> CreateHabit(
		[FromBody] CreateHabitDto createHabitDto,
		[FromHeader] AcceptHeaderDto acceptHeader,
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

		if (acceptHeader.IncludeLinks)
		{
			habitDto.Links = CreateLinksForHabit(habitDto.Id, null);
		}			

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
	private List<LinkDto> CreateLinksForHabits(
		HabitsQueryParameters queryParameters,
		bool hasNextPage,
		bool hasPreviousPage)
	{
		List<LinkDto> links =
		[
			_linkService.Create(nameof(GetHabits), "self", HttpMethods.Get, new 
			{
				page = queryParameters.Page,
				pageSize = queryParameters.PageSize,
				fields = queryParameters.Fields,
				q = queryParameters.Search,
				sort = queryParameters.Sort,
				type = queryParameters.Type,
				status = queryParameters.Status
			}),
			_linkService.Create(nameof(CreateHabit), "create", HttpMethods.Post)
		];

		if (hasNextPage)
		{
			links.Add(_linkService.Create(nameof(GetHabits), "next-page", HttpMethods.Get, new
			{
				page = queryParameters.Page + 1,
				pageSize = queryParameters.PageSize,
				fields = queryParameters.Fields,
				q = queryParameters.Search,
				sort = queryParameters.Sort,
				type = queryParameters.Type,
				status = queryParameters.Status
			}));
		}

		if (hasPreviousPage)
		{
			links.Add(_linkService.Create(nameof(GetHabits), "previous-page", HttpMethods.Get, new
			{
				page = queryParameters.Page - 1,
				pageSize = queryParameters.PageSize,
				fields = queryParameters.Fields,
				q = queryParameters.Search,
				sort = queryParameters.Sort,
				type = queryParameters.Type,
				status = queryParameters.Status
			}));
		}

		return links;
	}
	private List<LinkDto> CreateLinksForHabit(string id, string? fields)
	{
		List<LinkDto> links =
		[
			_linkService.Create(nameof(GetHabit), "self", HttpMethods.Get, new { id, fields }),
			_linkService.Create(nameof(UpdateHabit), "update", HttpMethods.Put, new { id }),
			_linkService.Create(nameof(PatchHabit), "partial-update", HttpMethods.Patch, new { id }),
			_linkService.Create(nameof(DeleteHabit), "delete", HttpMethods.Delete, new { id }),
			_linkService.Create(
				nameof(HabitTagsController.UpsertHabitTags), 
				"upsert-tags", 
				HttpMethods.Put, 
				new { habitId = id }, 
				HabitTagsController.Name),
		];

		return links;
	}
}
