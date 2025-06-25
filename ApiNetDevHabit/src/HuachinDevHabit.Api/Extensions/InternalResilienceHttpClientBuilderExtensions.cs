using Microsoft.Extensions.Http.Resilience;

namespace HuachinDevHabit.Api.Extensions
{
	public static class InternalResilienceHttpClientBuilderExtensions
	{
		public static IHttpClientBuilder RemoveAllResilienceHandlers(this IHttpClientBuilder builder)
		{
			builder.ConfigureAdditionalHttpMessageHandlers(static (handlers, _) =>
			{
				for (int i = handlers.Count - 1; i >= 0; i--)
				{
#pragma warning disable EXTEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
					if (handlers[i] is ResilienceHandler)
					{
						handlers.RemoveAt(i);
					}
#pragma warning restore EXTEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
				}
			});

			return builder;
		}
	}
}
