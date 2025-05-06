using HuachinDevHabit.Api.DTOs.Common;

namespace HuachinDevHabit.Api.Services.Hateos
{
	public sealed class LinkService
	{
		private readonly LinkGenerator _linkGenerator;
		private readonly IHttpContextAccessor _httpContextAccessor;

		public LinkService(LinkGenerator linkGenerator, IHttpContextAccessor httpContextAccessor)
		{
			_linkGenerator = linkGenerator;
			_httpContextAccessor = httpContextAccessor;
		}

		public LinkDto Create(
			string endpointName,
			string rel,
			string method,
			object? values = null,
			string? controller = null)
		{
			string? href = _linkGenerator.GetUriByAction(
				_httpContextAccessor.HttpContext!,
				endpointName,
				controller,
				values);

			return new LinkDto
			{
				Href = href ?? throw new Exception("Invalid endpoint name provided"),
				Rel = rel,
				Method = method
			};
		}
	}
}
