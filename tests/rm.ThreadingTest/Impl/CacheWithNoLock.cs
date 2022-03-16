using Microsoft.Extensions.Caching.Memory;

namespace rm.ThreadingTest
{
	/// <summary>
	/// No lock(s).
	/// <para></para>
	/// Not recommended. Calls for a key are highly reentrant, and could cause a cache stampede issue.
	/// </summary>
	public class CacheWithNoLock : ICachedValueProvider, IMemoryCacheImpl, IDisposable
	{
		private readonly ICacheSettings cacheSettings;
		private readonly IValueProvider valueProvider;
		private readonly MemoryCache cache;
		private readonly DiagHelper diagHelper;

		public CacheWithNoLock(
			ICacheSettings cacheSettings,
			IValueProvider valueProvider,
			DiagHelper diagHelper,
			MemoryCacheOptions memoryCacheOptions = null!)
		{
			this.cacheSettings = cacheSettings;
			this.valueProvider = valueProvider;
			cache = new MemoryCache(memoryCacheOptions ?? new());
			this.diagHelper = diagHelper;
		}

		public async Task<T> GetValueAsync<T>(object key)
		{
			if (cache.TryGetValue(key, out T value))
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
				}

				disposed = true;
			}
		}
	}
}
