using Asp.Versioning;
using DevHabit.Api.Jobs;
using FluentValidation;
using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.DTOs.Common;
using HuachinDevHabit.Api.DTOs.EntryImports;
using HuachinDevHabit.Api.Entities;
using HuachinDevHabit.Api.Services.Authentication;
using HuachinDevHabit.Api.Services.ContentNegotiation;
using HuachinDevHabit.Api.Services.Hateos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System.Net.Mime;

namespace HuachinDevHabit.Api.Controllers
{
	[Authorize(Roles = Roles.Member)]
	[ApiController]
	[Route("entries/imports")]
	[ApiVersion(1.0)]
	[Produces(
		MediaTypeNames.Application.Json,
		CustomMediaTypeNames.Application.JsonV1,
		CustomMediaTypeNames.Application.HateoasJson,
		CustomMediaTypeNames.Application.HateoasJsonV1)]
	public sealed class EntryImportsController : ControllerBase
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly ISchedulerFactory _schedulerFactory;
		private readonly LinkService _linkService;
		private readonly UserContext _userContext;

		public EntryImportsController(
			ApplicationDbContext dbContext, 
			ISchedulerFactory schedulerFactory, 
			LinkService linkService, 
			UserContext user)
		{
			_dbContext = dbContext;
			_schedulerFactory = schedulerFactory;
			_linkService = linkService;
			_userContext = user;
		}

		[HttpPost]
		public async Task<ActionResult<EntryImportJobDto>> CreateImportJob(
			[FromForm] CreateEntryImportJobDto createEntryImportJobDto,
			[FromHeader] AcceptHeaderDto acceptHeaderDto,
			IValidator<CreateEntryImportJobDto> validator)
		{
			string? userId = await _userContext.GetUserIdAsync();
			if (string.IsNullOrWhiteSpace(userId))
			{
				return Unauthorized();
			}

			await validator.ValidateAsync(createEntryImportJobDto);

			// Create import job
			using var memoryStream = new MemoryStream();
			await createEntryImportJobDto.File.CopyToAsync(memoryStream);

			var importJob = new EntryImportJob
			{
				Id = $"ei_{Guid.NewGuid()}",
				UserId = userId,
				Status = EntryImportStatus.Pending,
				FileName = createEntryImportJobDto.File.FileName,
				FileContent = memoryStream.ToArray(),
				CreatedAtUtc = DateTime.UtcNow
			};

			_dbContext.EntryImportJobs.Add(importJob);
			await _dbContext.SaveChangesAsync();

			// Schedule processing job
			IScheduler scheduler = await _schedulerFactory.GetScheduler();

			IJobDetail jobDetail = JobBuilder.Create<ProcessEntryImportJob>()
				.WithIdentity($"process-entry-import-{importJob.Id}")
				.UsingJobData("importJobId", importJob.Id)
				.Build();

			ITrigger trigger = TriggerBuilder.Create()
				.WithIdentity($"process-entry-import-trigger-{importJob.Id}")
				.StartNow()
				.Build();

			await scheduler.ScheduleJob(jobDetail, trigger);

			EntryImportJobDto importJobDto = importJob.ToDto();

			if (acceptHeaderDto.IncludeLinks)
			{
				importJobDto.Links = CreateLinksForImportJob(importJob.Id);
			}

			return CreatedAtAction(nameof(GetImportJob), new { id = importJob.Id }, importJobDto);
		}

		[HttpGet]
		public async Task<ActionResult<PaginationResult<EntryImportJobDto>>> GetImportJobs(
		[FromHeader] AcceptHeaderDto acceptHeader,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 10)
		{
			string? userId = await _userContext.GetUserIdAsync();
			if (string.IsNullOrWhiteSpace(userId))
			{
				return Unauthorized();
			}

			IQueryable<EntryImportJob> query = _dbContext.EntryImportJobs
				.Where(j => j.UserId == userId)
				.OrderByDescending(j => j.CreatedAtUtc);

			int totalCount = await query.CountAsync();

			List<EntryImportJobDto> importJobDtos = await query
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(EntryImportQueries.ProjectToDto())
				.ToListAsync();

			if (acceptHeader.IncludeLinks)
			{
				foreach (EntryImportJobDto dto in importJobDtos)
				{
					dto.Links = CreateLinksForImportJob(dto.Id);
				}
			}

			var result = new PaginationResult<EntryImportJobDto>
			{
				Items = importJobDtos,
				Page = page,
				PageSize = pageSize,
				TotalCount = totalCount
			};

			if (acceptHeader.IncludeLinks)
			{
				result.Links = CreateLinksForImportJobs(page, pageSize, result.HasNextPage, result.HasPreviousPage);
			}

			return Ok(result);
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<EntryImportJobDto>> GetImportJob(
		string id,
		[FromHeader] AcceptHeaderDto acceptHeader)
		{
			string? userId = await _userContext.GetUserIdAsync();
			if (string.IsNullOrWhiteSpace(userId))
			{
				return Unauthorized();
			}

			EntryImportJobDto? importJob = await _dbContext.EntryImportJobs
				.Where(j => j.Id == id && j.UserId == userId)
				.Select(EntryImportQueries.ProjectToDto())
				.FirstOrDefaultAsync();

			if (importJob is null)
			{
				return NotFound();
			}

			if (acceptHeader.IncludeLinks)
			{
				importJob.Links = CreateLinksForImportJob(id);
			}

			return Ok(importJob);
		}

		private List<LinkDto> CreateLinksForImportJob(string id)
		{
			return
			[
				_linkService.Create(nameof(GetImportJob), "self", HttpMethods.Get, new { id })
			];
		}

		private List<LinkDto> CreateLinksForImportJobs(int page, int pageSize, bool hasNextPage, bool hasPreviousPage)
		{
			var links = new List<LinkDto>
		{
			_linkService.Create(nameof(GetImportJobs), "self", HttpMethods.Get, new { page, pageSize })
		};

			if (hasNextPage)
			{
				links.Add(_linkService.Create(nameof(GetImportJobs), "next-page", HttpMethods.Get, new
				{
					page = page + 1,
					pageSize
				}));
			}

			if (hasPreviousPage)
			{
				links.Add(_linkService.Create(nameof(GetImportJobs), "previous-page", HttpMethods.Get, new
				{
					page = page - 1,
					pageSize
				}));
			}

			return links;
		}
	}
}
