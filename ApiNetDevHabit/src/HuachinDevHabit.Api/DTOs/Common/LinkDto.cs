namespace HuachinDevHabit.Api.DTOs.Common
{
	public sealed class LinkDto
	{
		public required string Href { get; init; }
		public required string Rel { get; set; }
		public required string Method { get; set; }
	}
}
