using Microsoft.Extensions.Caching.Memory;

namespace rm.ThreadingTest
{
	public class CacheWithNoLock : IValueProvider, IDisposable
	{
		private readonly ICacheSettings cacheSettings;
		private readonly IValueProvider valueProvider;
		private readonly MemoryCache cache;

		public CacheWithNoLock(
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
			if (cache.TryGetValue(key, out T value))
			{
				DiagHelper.CacheHit(key);
				return value;
			}

			DiagHelper.CacheMiss(key);
			value = await valueProvider.GetValueAsync<T>(key)
				.ConfigureAwait(false);
			cache.Set(key, value,
				new MemoryCacheEntryOptions()
				{
					AbsoluteExpirationRelativeToNow = cacheSettings.Ttl,
				});

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
