using Microsoft.Extensions.Caching.Memory;

namespace rm.ThreadingTest
{
	public class CacheWithLock : IValueProvider, IDisposable
	{
		private readonly ICacheSettings cacheSettings;
		private readonly IValueProvider valueProvider;
		private readonly MemoryCache cache;
		private readonly SemaphoreSlim @lock;

		public CacheWithLock(
			ICacheSettings cacheSettings,
			IValueProvider valueProvider,
			MemoryCacheOptions memoryCacheOptions = null!)
		{
			this.cacheSettings = cacheSettings;
			this.valueProvider = valueProvider;
			cache = new MemoryCache(memoryCacheOptions ?? new MemoryCacheOptions());
			@lock = new SemaphoreSlim(1, 1);
		}

		public async Task<T> GetValueAsync<T>(object key)
		{
			if (cache.TryGetValue(key, out T value))
			{
				DiagHelper.CacheHit(key);
				return value;
			}

			await @lock.WaitAsync()
				.ConfigureAwait(false);
			try
			{
				if (cache.TryGetValue(key, out value))
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
			finally
			{
				@lock.Release();
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
					@lock?.Dispose();
				}

				_disposed = true;
			}
		}
	}
}
