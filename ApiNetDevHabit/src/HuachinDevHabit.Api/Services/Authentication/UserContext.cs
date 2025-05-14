using HuachinDevHabit.Api.Database;
using HuachinDevHabit.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HuachinDevHabit.Api.Services.Authentication
{
	public sealed class UserContext
	{
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly ApplicationDbContext _applicationDbContext;
		private readonly IMemoryCache _memoryCache;

		public UserContext(IHttpContextAccessor httpContextAccessor, ApplicationDbContext applicationDbContext, IMemoryCache memoryCache)
		{
			_httpContextAccessor = httpContextAccessor;
			_applicationDbContext = applicationDbContext;
			_memoryCache = memoryCache;
		}

		private const string CacheKeyPrefix = "user:id:";
		private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

		public async Task<string?> GetUserIdAsync(CancellationToken cancellationToken = default)
		{
			string? identityId = _httpContextAccessor.HttpContext?.User.GetIdentityId();
			if (string.IsNullOrWhiteSpace(identityId))
			{
				return null;
			}

			string cacheKey = $"{CacheKeyPrefix}{identityId}";

			string? userId = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = CacheDuration;

				string? userId = await _applicationDbContext.Users
					.Where(user => user.IdentityId == identityId)
					.Select(user => user.Id)
					.FirstOrDefaultAsync(cancellationToken);
				
				return userId;
			});

			return userId;
		}
	}
}
