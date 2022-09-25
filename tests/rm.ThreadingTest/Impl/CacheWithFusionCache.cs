using Microsoft.Extensions.Caching.Memory;
using ZiggyCreatures.Caching.Fusion;

namespace rm.ThreadingTest
{
	/// <summary>
	/// Uses FusionCache.
	/// </summary>
	public class CacheWithFusionCache : ICachedValueProvider, IMemoryCacheImpl, IDisposable
	{
		private readonly ICacheSettings cacheSettings;
		private readonly IValueProvider valueProvider;
		private readonly IFusionCache fusionCache;
		private readonly IMemoryCache cache;
		private readonly DiagHelper diagHelper;

		public CacheWithFusionCache(
			ICacheSettings cacheSettings,
			IValueProvider valueProvider,
			DiagHelper diagHelper,
			FusionCacheOptions fusionCacheOptions = null!,
			MemoryCacheOptions memoryCacheOptions = null!)
		{
			this.cacheSettings = cacheSettings;
			this.valueProvider = valueProvider;
			fusionCache = new FusionCache(
				optionsAccessor: fusionCacheOptions ?? new FusionCacheOptions()
				{
					DefaultEntryOptions = new FusionCacheEntryOptions
					{
						Duration = cacheSettings.Ttl,
					}
				},
				memoryCache: cache = new MemoryCache(memoryCacheOptions ?? new MemoryCacheOptions()));
			this.diagHelper = diagHelper;
		}

		public async Task<T> GetValueAsync<T>(object key)
		{
			var valueFactoryCalled = false;
			var value = await fusionCache.GetOrSetAsync((string)key, async ct =>
			{
				diagHelper.CacheMiss(key);
				valueFactoryCalled = true;

				return await valueProvider.GetValueAsync<T>(key);
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
					fusionCache?.Dispose();
				}

				disposed = true;
			}
		}
	}
}
