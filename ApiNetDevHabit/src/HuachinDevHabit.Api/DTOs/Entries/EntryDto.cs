using HuachinDevHabit.Api.DTOs.Common;
using HuachinDevHabit.Api.Entities;

namespace HuachinDevHabit.Api.DTOs.Entries
{
	public sealed record EntryDto : ILinkResponse
	{
		public required string Id { get; init; }
		public required EntryHabitDto Habit { get; init; }
		public required int Value { get; init; }
		public string? Notes { get; init; }
		public required EntrySource Source { get; init; }
		public string? ExternalId { get; init; }
		public required bool IsArchived { get; init; }
		public required DateOnly Date { get; init; }
		public required DateTime CreatedAtUtc { get; init; }
		public DateTime? UpdatedAtUtc { get; init; }
		public List<LinkDto> Links { get; set; }
	}
}
