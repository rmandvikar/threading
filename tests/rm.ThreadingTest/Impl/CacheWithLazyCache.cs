using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;

namespace rm.ThreadingTest
{
	/// <summary>
	/// Uses LazyCache.
	/// </summary>
	public class CacheWithLazyCache : ICachedValueProvider, IMemoryCacheImpl, IDisposable
	{
		private readonly ICacheSettings cacheSettings;
		private readonly IValueProvider valueProvider;
		private readonly IAppCache appCache;
		private readonly ICacheProvider cacheProvider;
		private readonly IMemoryCache cache;
		private readonly DiagHelper diagHelper;

		public CacheWithLazyCache(
			ICacheSettings cacheSettings,
			IValueProvider valueProvider,
			DiagHelper diagHelper,
			MemoryCacheOptions memoryCacheOptions = null!)
		{
			this.cacheSettings = cacheSettings;
			this.valueProvider = valueProvider;
			appCache =
				new CachingService(
					// hold references to dispose
					cacheProvider = new MemoryCacheProvider(
						cache = new MemoryCache(
							memoryCacheOptions ?? new MemoryCacheOptions())));
			this.diagHelper = diagHelper;
		}

		public async Task<T> GetValueAsync<T>(object key)
		{
			var valueFactoryCalled = false;
			var value = await appCache.GetOrAddAsync((string)key, (cacheEntry) =>
			{
				diagHelper.CacheMiss(key);
				valueFactoryCalled = true;

				cacheEntry.AbsoluteExpirationRelativeToNow = cacheSettings.Ttl;
				cacheEntry.RegisterPostEvictionCallback(PostEvictionCallback);

				return valueProvider.GetValueAsync<T>(key);
			});
			if (!valueFactoryCalled)
			{
				diagHelper.CacheHit(key);
			}

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
					cacheProvider?.Dispose();
					cache?.Dispose();
				}

				disposed = true;
			}
		}
	}
}
