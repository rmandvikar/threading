using Microsoft.Extensions.Caching.Memory;

namespace rm.ThreadingTest
{
	public class CacheWithTask : IValueProvider, IDisposable
	{
		private readonly ICacheSettings cacheSettings;
		private readonly IValueProvider valueProvider;
		private readonly MemoryCache cache;

		public CacheWithTask(
			ICacheSettings cacheSettings,
			IValueProvider valueProvider,
			MemoryCacheOptions memoryCacheOptions = null!)
		{
			this.cacheSettings = cacheSettings;
			this.valueProvider = valueProvider;
			cache = new MemoryCache(memoryCacheOptions ?? new MemoryCacheOptions());
		}

		public async Task<T> GetValueAsync<T>(object key)
		{
			var valueFactoryCalled = false;
			var valueFactoryTask = cache.GetOrCreateAsync(key, (cacheEntry) =>
			{
				valueFactoryCalled = true;
				DiagHelper.CacheMiss(key);
				return valueProvider.GetValueAsync<T>(key);
			});

			if (!valueFactoryCalled)
			{
				DiagHelper.CacheHit(key);
			}
			var value = await valueFactoryTask
				.ConfigureAwait(false);

			if (valueFactoryCalled)
			{
				cache.Set(key, value,
					new MemoryCacheEntryOptions()
					{
						AbsoluteExpirationRelativeToNow = cacheSettings.Ttl,
					});
			}

			return value;
		}

		private bool _disposed = false;

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					cache?.Dispose();
				}

				_disposed = true;
			}
		}
	}
}
