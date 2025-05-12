namespace HuachinDevHabit.Api.Services.Sorting
{
	public sealed class SortMappingDefinition<TSource, TDestination> : ISortMappingDefinition
	{
		public required SortMapping[] Mappings { get; init; }

		// Fix Sonar: Adding usage of TSource and TDestination to resolve the diagnostics
		public Type SourceType => typeof(TSource);
		public Type DestinationType => typeof(TDestination);
	}
}
