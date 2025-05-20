using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.DTOs.Common;
using HuachinDevHabit.Api.DTOs.Users;
using HuachinDevHabit.Api.Entities;
using HuachinDevHabit.Api.Services.Authentication;
using HuachinDevHabit.Api.Services.Hateos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HuachinDevHabit.Api.Controllers;

//[Authorize(Roles = $"{Roles.Admin},{Roles.Member}")]
[Authorize(Roles = $"{Roles.Member}")]
[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
	private readonly ApplicationDbContext _dbContext;
	private readonly UserContext _userContext;
	private readonly LinkService _linkService;

	public UsersController(ApplicationDbContext dbContext, UserContext userContext, LinkService linkService)
	{
		_dbContext = dbContext;
		_userContext = userContext;
		_linkService = linkService;
	}

	[HttpGet("{id}")]
	[Authorize(Roles = $"{Roles.Admin}")]
	public async Task<ActionResult<UserDto>> GetUserById(string id)
	{
		string? userId = await _userContext.GetUserIdAsync();
		if (string.IsNullOrWhiteSpace(userId))
		{
			return Unauthorized();
		}

		if (id != userId)
		{
			return Forbid();
		}

		UserDto? user = await _dbContext.Users
			.Where(user => user.Id == id)
			.Select(UserQueries.ProjectToDto())
			.FirstOrDefaultAsync();

		if (user == null)
		{
			return NotFound();
		}

		return Ok(user);
	}

	[HttpGet("me")]
	public async Task<ActionResult<UserDto>> GetCurrentUser([FromHeader] AcceptHeaderDto acceptHeaderDto)
	{
		string? userId = await _userContext.GetUserIdAsync();
		if (string.IsNullOrWhiteSpace(userId))
		{
			return Unauthorized();
		}

		UserDto? user = await _dbContext.Users
			.Where(user => user.Id == userId)
			.Select(UserQueries.ProjectToDto())
			.FirstOrDefaultAsync();

		if (user == null)
		{
			return NotFound();
		}

		if (acceptHeaderDto.IncludeLinks)
		{
			user.Links = CreateLinksForUser();
		}

		return Ok(user);
	}

	[HttpPut("me/profile")]
	public async Task<ActionResult> UpdateProfile(UpdateUserProfileDto updateUserProfileDto)
	{
		string? userId = await _userContext.GetUserIdAsync();
		if (string.IsNullOrWhiteSpace(userId))
		{
			return Unauthorized();
		}

		User? user = await _dbContext.Users
			.Where(user => user.Id == userId)
			.FirstOrDefaultAsync();
		if (user == null)
		{
			return NotFound();
		}

		user.Name = updateUserProfileDto.Name;
		user.UpdatedAtUtc = DateTime.UtcNow;

		await _dbContext.SaveChangesAsync();

		return NoContent();
	}

	private List<LinkDto> CreateLinksForUser()
	{
		List<LinkDto> links = new List<LinkDto>
		{
			_linkService.Create(nameof(GetCurrentUser), "self", HttpMethods.Get),
			_linkService.Create(nameof(UpdateProfile), "update_profile", HttpMethods.Put)
		};

		return links;
	}
}
