using HuachinDevHabit.Api.DTOs.Auth;
using HuachinDevHabit.Api.Entities;

namespace HuachinDevHabit.Api.DTOs.Users
{
	public static class UserMapping
	{
		public static User ToEntity(this RegisterUserDto registerUserDto)
		{
			return new User
			{
				Id = $"u_{Guid.NewGuid()}",
				Name = registerUserDto.Name,
				Email = registerUserDto.Email,
				CreatedAtUtc = DateTime.UtcNow,
			};
		}
	}
}
