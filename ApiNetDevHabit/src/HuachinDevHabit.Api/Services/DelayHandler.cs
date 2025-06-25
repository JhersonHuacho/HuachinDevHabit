namespace HuachinDevHabit.Api.Services
{
	public sealed class DelayHandler : DelegatingHandler
	{
		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request, 
			CancellationToken cancellationToken)
		{
			// Introduce a delay of 1 second before processing the request
			await Task.Delay(TimeSpan.FromSeconds(10000), cancellationToken);
			return await base.SendAsync(request, cancellationToken);
		}
	}
}
