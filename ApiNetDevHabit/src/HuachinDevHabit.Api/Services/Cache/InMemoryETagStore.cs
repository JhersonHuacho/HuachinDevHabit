using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace HuachinDevHabit.Api.Services.Cache
{
	public sealed class InMemoryETagStore
	{
		private static readonly ConcurrentDictionary<string, string> ETags = new();

		public string GetETag(string resourceUri)
		{
			return ETags.GetOrAdd(resourceUri, _ => string.Empty);
		}

		public void SetETag(string resourceUri, object resource)
		{
			ETags.AddOrUpdate(resourceUri, GenerateETag(resource), (_, _) => GenerateETag(resource));
		}

		public void RemoveETag(string resourceUri)
		{
			ETags.TryRemove(resourceUri, out _);
		}

		private static string GenerateETag(object resource)
		{
			byte[] content = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resource));
			byte[] hash = SHA512.HashData(content);

			return Convert.ToHexString(hash);
		}
	}
}
