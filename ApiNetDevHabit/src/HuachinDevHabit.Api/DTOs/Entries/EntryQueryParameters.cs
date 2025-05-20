using HuachinDevHabit.Api.DTOs.Common;

namespace HuachinDevHabit.Api.DTOs.Entries
{
	public sealed record EntryQueryParameters : AcceptHeaderDto
	{
		public string? Fields { get; init; }
	}
}
