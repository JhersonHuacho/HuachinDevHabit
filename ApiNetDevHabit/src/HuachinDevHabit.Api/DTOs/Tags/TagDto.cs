using HuachinDevHabit.Api.DTOs.Common;
using Newtonsoft.Json;

namespace HuachinDevHabit.Api.DTOs.Tags
{
	public sealed record TagDto : ILinkResponse
	{
		public required string Id { get; init; }
		public required string Name { get; init; }
		public string? Description { get; init; }
		public required DateTime CreatedAtUtc { get; init; }
		public DateTime? UpdatedAtUtc { get; init; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public List<LinkDto> Links { get; set; }
	}
}
