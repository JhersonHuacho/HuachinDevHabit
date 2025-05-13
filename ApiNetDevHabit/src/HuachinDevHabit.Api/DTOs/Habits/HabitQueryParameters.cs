using HuachinDevHabit.Api.DTOs.Common;

namespace HuachinDevHabit.Api.DTOs.Habits
{
	public sealed record HabitQueryParameters : AcceptHeaderDto
	{
		public string? Fields { get; init; }
	}
}
