using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuachinDevHabit.Api.Controllers;

[Authorize]
[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
	private readonly ApplicationDbContext _dbContext;

	public UsersController(ApplicationDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<UserDto>> GetUserById(string id)
	{
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
}
