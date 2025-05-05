using FluentValidation;
using FluentValidation.Results;
using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.DTOs.Tags;
using HuachinDevHabit.Api.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HuachinDevHabit.Api.Controllers
{
	[ApiController]
	[Route("tags")]
	public sealed class TagsController : ControllerBase
	{
		private readonly ApplicationDbContext _dbContext;

		public TagsController(ApplicationDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<TagsCollectionDto>>> GetTags()
		{
			List<TagDto> tags = await _dbContext.Tags
				.Select(TagQueries.ProjectToDto())
				.ToListAsync();

			var tagsCollectionDto = new TagsCollectionDto 
			{ 
				Data = tags 
			};

			return Ok(tagsCollectionDto);
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<TagDto>> GetTag(string id)
		{
			TagDto? tag = await _dbContext.Tags
				.Where(t => t.Id == id)
				.Select(TagQueries.ProjectToDto())
				.FirstOrDefaultAsync();

			if (tag == null)
			{
				return NotFound();
			}

			return Ok(tag);
		}

		[HttpPost]
		public async Task<ActionResult<TagDto>> CreateTag(
			[FromBody] CreateTagDto createTagDto, 
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
	}
}
