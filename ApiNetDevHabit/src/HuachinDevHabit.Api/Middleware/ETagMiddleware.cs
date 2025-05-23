using HuachinDevHabit.Api.Services.Cache;
using System.Security.Cryptography;
using System.Text;

namespace HuachinDevHabit.Api.Middleware
{
	public sealed class ETagMiddleware
	{
		private readonly RequestDelegate _next;

		public ETagMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task InvokeAsync(HttpContext context, InMemoryETagStore inMemoryETagStore)
		{
			if (CanSkipETag(context))
			{
				await _next(context);
				return;
			}

			string resourceUri = context.Request.Path.Value!;
			string? ifNoneMatch = context.Request.Headers.IfNoneMatch.FirstOrDefault()?.Replace("\"", "");

			Stream originalStream = context.Response.Body;
			using var memoryStream = new MemoryStream();
			context.Response.Body = memoryStream;

			await _next(context);

			if (IsEtaggableResponse(context))
			{
				memoryStream.Position = 0;
				byte[] responseBody = await GetResponseBody(memoryStream);
				string etag = GenerateETag(responseBody);

				inMemoryETagStore.SetETag(resourceUri, etag);
				context.Response.Headers.ETag = $"\"{etag}\"";
				context.Response.Body = originalStream;

				if (context.Request.Method == HttpMethods.Get && ifNoneMatch == etag)
				{
					context.Response.StatusCode = StatusCodes.Status304NotModified;
					context.Response.ContentLength = 0;
					return;
				}
			}

			memoryStream.Position = 0;
			await memoryStream.CopyToAsync(originalStream);
		}

		public static bool IsEtaggableResponse(HttpContext context)
		{
			return context.Response.StatusCode == StatusCodes.Status200OK &&
				   (context.Response.Headers.ContentType
						.FirstOrDefault()?
						.Contains("json", StringComparison.Ordinal) ?? false);
		}

		private static async Task<byte[]> GetResponseBody(Stream memoryStream)
		{
			using var reader = new StreamReader(memoryStream, leaveOpen: true);
			memoryStream.Position = 0;

			string content = await reader.ReadToEndAsync();

			return Encoding.UTF8.GetBytes(content);
		}

		private static string GenerateETag(byte[] content)
		{
			byte[] hash = SHA512.HashData(content);

			return Convert.ToHexString(hash);
		}

		private static bool CanSkipETag(HttpContext context)
		{
			return context.Request.Method == HttpMethods.Post ||
				  context.Request.Method == HttpMethods.Delete;
		}
	}
}
