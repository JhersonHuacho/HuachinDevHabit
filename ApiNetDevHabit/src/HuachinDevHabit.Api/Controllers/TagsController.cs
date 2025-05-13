using FluentValidation;
using FluentValidation.Results;
using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.DTOs.Common;
using HuachinDevHabit.Api.DTOs.Tags;
using HuachinDevHabit.Api.Entities;
using HuachinDevHabit.Api.Services.ContentNegotiation;
using HuachinDevHabit.Api.Services.Hateos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;

namespace HuachinDevHabit.Api.Controllers;

[ApiController]
[Route("tags")]
[Produces(
	MediaTypeNames.Application.Json,
	CustomMediaTypeNames.Application.JsonV1,
	CustomMediaTypeNames.Application.HateoasJson,
	CustomMediaTypeNames.Application.HateoasJsonV1)]
public sealed class TagsController : ControllerBase
{
	private readonly ApplicationDbContext _dbContext;
	private readonly LinkService _linkService;

	public TagsController(ApplicationDbContext dbContext, LinkService linkService)
	{
		_dbContext = dbContext;
		_linkService = linkService;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<TagsCollectionDto>>> GetTags(
		[FromHeader] AcceptHeaderDto acceptHeader)
	{
		List<TagDto> tags = await _dbContext.Tags
			.Select(TagQueries.ProjectToDto())
			.ToListAsync();

		var tagsCollectionDto = new TagsCollectionDto 
		{ 
			Items = tags 
		};

		if (acceptHeader.IncludeLinks)
		{
			tagsCollectionDto.Links = CreateLinksForTags();
		}

		return Ok(tagsCollectionDto);
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<TagDto>> GetTag(
		string id,
		[FromHeader] AcceptHeaderDto acceptHeader)
	{
		TagDto? tag = await _dbContext.Tags
			.Where(t => t.Id == id)
			.Select(TagQueries.ProjectToDto())
			.FirstOrDefaultAsync();

		if (tag == null)
		{
			return NotFound();
		}

		if (acceptHeader.IncludeLinks)
		{
			tag.Links = CreateLinksForTag(id);
		}

		return Ok(tag);
	}

	[HttpPost]
	public async Task<ActionResult<TagDto>> CreateTag(
		[FromBody] CreateTagDto createTagDto,
		[FromHeader] AcceptHeaderDto acceptHeader,
		IValidator<CreateTagDto> validator,
		ProblemDetailsFactory problemDetailsFactory)
	{
		ValidationResult validationResult = await validator.ValidateAsync(createTagDto);

		if (!validationResult.IsValid)
		{
			//return BadRequest(validationResult.ToDictionary());
			//return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));

			ProblemDetails problem = problemDetailsFactory.CreateProblemDetails(
				HttpContext,
				StatusCodes.Status400BadRequest);
			problem.Extensions.Add("errors", validationResult.ToDictionary());

			return BadRequest(problem);
		}

		Tag tag = createTagDto.ToEntity();

		if (await _dbContext.Tags.AnyAsync(t => t.Name == tag.Name))
		{
			//return Conflict($"The tag '{tag.Name}' already exists.");
			return Problem(
				detail: $"The tag '{tag.Name}' already exists.",
				statusCode: StatusCodes.Status409Conflict);
		}

		_dbContext.Tags.Add(tag);
		await _dbContext.SaveChangesAsync();

		TagDto tagDto = tag.ToDto();

		if (acceptHeader.IncludeLinks)
		{
			tagDto.Links = CreateLinksForTag(tag.Id);
		}

		return CreatedAtAction(nameof(GetTag), new { id = tagDto.Id }, tagDto);
	}

	[HttpPut("{id}")]
	public async Task<ActionResult> UpdateTag(string id, [FromBody] UpdateTagDto updateTagDto)
	{
		Tag? tag = await _dbContext.Tags.FirstOrDefaultAsync(h => h.Id == id);

		if (tag == null)
		{
			return NotFound();
		}

		tag.UpdateFromDto(updateTagDto);

		await _dbContext.SaveChangesAsync();

		return NoContent();
	}

	[HttpDelete("{id}")]
	public async Task<ActionResult> DeleteTag(string id)
	{
		Tag? tag = await _dbContext.Tags.FirstOrDefaultAsync(h => h.Id == id);

		if (tag == null)
		{
			return NotFound();
		}
		
		_dbContext.Tags.Remove(tag);
		await _dbContext.SaveChangesAsync();

		return NoContent();
	}

	private List<LinkDto> CreateLinksForTags()
	{
		List<LinkDto> links =
		[
			_linkService.Create(nameof(GetTags), "self", HttpMethods.Get),
			_linkService.Create(nameof(CreateTag), "create", HttpMethods.Post)
		];

		return links;
	}

	private List<LinkDto> CreateLinksForTag(string id)
	{
		List<LinkDto> links =
		[
			_linkService.Create(nameof(GetTag), "self", HttpMethods.Get, new { id }),
			_linkService.Create(nameof(UpdateTag), "update", HttpMethods.Put, new { id }),
			_linkService.Create(nameof(DeleteTag), "delete", HttpMethods.Delete, new { id })
		];

		return links;
	}
}
