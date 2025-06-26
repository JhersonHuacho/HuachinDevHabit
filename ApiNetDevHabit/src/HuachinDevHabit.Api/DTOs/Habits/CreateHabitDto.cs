using HuachinDevHabit.Api.Entities;

namespace HuachinDevHabit.Api.DTOs.Habits
{
	public sealed class CreateHabitDto
	{
		public required string Name { get; init; }
		public string? Description { get; init; }
		public required HabitType Type { get; init; }
		public required FrequencyDto Frequency { get; init; }
		public required TargetDto Target { get; init; }
		public DateOnly? EndDate { get; init; }
		public CreateMilestoneDto? Milestone { get; init; }		
	}

	public sealed record CreateMilestoneDto
	{
		public required int Target { get; init; }
	}
}
