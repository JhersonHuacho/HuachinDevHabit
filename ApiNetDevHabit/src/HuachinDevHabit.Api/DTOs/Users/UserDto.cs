﻿using HuachinDevHabit.Api.DTOs.Common;

namespace HuachinDevHabit.Api.DTOs.Users
{
	public sealed record UserDto : ILinkResponse
	{
		public required string Id { get; set; }
		public required string Email { get; set; }
		public required string Name { get; set; }
		public required DateTime CreatedAtUtc { get; set; }
		public DateTime? UpdatedAtUtc { get; set; }
		public List<LinkDto> Links { get; set; }
	}
}
