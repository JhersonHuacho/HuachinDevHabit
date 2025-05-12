using HuachinDevHabit.Api.DTOs.Common;

namespace HuachinDevHabit.Api.DTOs.Tags
{
	public sealed record TagsCollectionDto : ICollectionResponse<TagDto>
	{
		public List<TagDto> Items { get; init; }
	}
}
