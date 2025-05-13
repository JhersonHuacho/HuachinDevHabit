using HuachinDevHabit.Api.DTOs.Common;
using Newtonsoft.Json;

namespace HuachinDevHabit.Api.DTOs.Tags
{
	public sealed record TagsCollectionDto : ICollectionResponse<TagDto>, ILinkResponse
	{
		public List<TagDto> Items { get; init; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public List<LinkDto> Links { get; set; }
	}
}
