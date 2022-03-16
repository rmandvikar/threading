using Microsoft.Extensions.Caching.Memory;

namespace rm.ThreadingTest
{
	/// <summary>
	/// Uses an <i>uber</i> lock.
	/// <para></para>
	/// Not recommended. Blocks factory operations on other keys when performing one for a key.
	/// It's ok for cache size of 1 (only 1 cache entry).
	/// </summary>
	public class CacheWithLock : ICachedValueProvider, IMemoryCacheImpl, IDisposable
	{
		private readonly ICacheSettings cacheSettings;
		private readonly IValueProvider valueProvider;
		private readonly MemoryCache cache;
		private readonly SemaphoreSlim @lock;
		private readonly DiagHelper diagHelper;

		public CacheWithLock(
			ICacheSettings cacheSettings,
			IValueProvider valueProvider,
			DiagHelper diagHelper,
			MemoryCacheOptions memoryCacheOptions = null!)
		{
			this.cacheSettings = cacheSettings;
			this.valueProvider = valueProvider;
			cache = new MemoryCache(memoryCacheOptions ?? new());
			@lock = new SemaphoreSlim(1, 1);
			this.diagHelper = diagHelper;
		}

		public async Task<T> GetValueAsync<T>(object key)
		{
			if (cache.TryGetValue(key, out T value))
			{
				diagHelper.CacheHit(key);
				return value;
			}

			await @lock.WaitAsync()
				.ConfigureAwait(false);
			try
			{
				if (cache.TryGetValue(key, out value))
				{
					diagHelper.CacheHit(key);
					return value;
				}

				diagHelper.CacheMiss(key);
				value = await valueProvider.GetValueAsync<T>(key)
					.ConfigureAwait(false);
				cache.Set(key, value,
					new MemoryCacheEntryOptions()
					{
						AbsoluteExpirationRelativeToNow = cacheSettings.Ttl,
					}.RegisterPostEvictionCallback(PostEvictionCallback));

				return value;
			}
			finally
			{
				@lock.Release();
			}
		}

		private void PostEvictionCallback(object key, object value, EvictionReason reason, object state)
		{
			diagHelper.CacheEvicted(key);
		}

		public IMemoryCache Value => cache;

		private bool disposed = false;

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					cache?.Dispose();
					@lock?.Dispose();
				}

				disposed = true;
			}
		}
	}
}
