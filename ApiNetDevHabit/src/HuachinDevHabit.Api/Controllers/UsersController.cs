using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.DTOs.Users;
using HuachinDevHabit.Api.Entities;
using HuachinDevHabit.Api.Services.Authentication;
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

	public UsersController(ApplicationDbContext dbContext, UserContext userContext)
	{
		_dbContext = dbContext;
		_userContext = userContext;
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
	public async Task<ActionResult<UserDto>> GetCurrentUser()
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

		return Ok(user);
	}
}
