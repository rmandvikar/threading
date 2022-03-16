using Microsoft.Extensions.Caching.Memory;

namespace rm.ThreadingTest
{
	public class CacheWithBlockingThread : IValueProvider, IDisposable
	{
		private readonly ICacheSettings cacheSettings;
		private readonly IValueProvider valueProvider;
		private readonly MemoryCache cache;
		private readonly object @lock;

		public CacheWithBlockingThread(
			ICacheSettings cacheSettings,
			IValueProvider valueProvider,
			MemoryCacheOptions memoryCacheOptions = null!)
		{
			this.cacheSettings = cacheSettings;
			this.valueProvider = valueProvider;
			cache = new MemoryCache(memoryCacheOptions ?? new MemoryCacheOptions());
			@lock = new object();
		}

		public Task<T> GetValueAsync<T>(object key)
		{
			if (cache.TryGetValue(key, out T value))
			{
				DiagHelper.CacheHit(key);
				return Task.FromResult(value);
			}

			lock (@lock)
			{
				DiagHelper.CacheMiss(key);
				value = valueProvider.GetValueAsync<T>(key).GetAwaiter().GetResult();
				cache.Set(key, value,
					new MemoryCacheEntryOptions()
					{
						AbsoluteExpirationRelativeToNow = cacheSettings.Ttl,
					});
				return Task.FromResult(value);
			}
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
