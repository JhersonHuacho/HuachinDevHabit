namespace HuachinDevHabit.Api.Settings
{
	public sealed class TagsOptions
	{
		public const string SectionName = "Tags";

		public required int MaxAllowedTags { get; init; }
	}
}
