using Microsoft.Extensions.Caching.Memory;

namespace rm.ThreadingTest
{
	/// <summary>
	/// Uses sync-over-async anti-pattern, "task.GetAwaiter().GetResult()".
	/// <para></para>
	/// Not recommended. Blocks the current thread.
	/// </summary>
	public class CacheWithBlockingThread : ICachedValueProvider, IMemoryCacheImpl, IDisposable
	{
		private readonly ICacheSettings cacheSettings;
		private readonly IValueProvider valueProvider;
		private readonly MemoryCache cache;
		private readonly object @lock;
		private readonly DiagHelper diagHelper;

		public CacheWithBlockingThread(
			ICacheSettings cacheSettings,
			IValueProvider valueProvider,
			DiagHelper diagHelper,
			MemoryCacheOptions memoryCacheOptions = null!)
		{
			this.cacheSettings = cacheSettings;
			this.valueProvider = valueProvider;
			cache = new MemoryCache(memoryCacheOptions ?? new());
			@lock = new object();
			this.diagHelper = diagHelper;
		}

		public Task<T> GetValueAsync<T>(object key)
		{
			if (cache.TryGetValue(key, out T value))
			{
				diagHelper.CacheHit(key);
				return Task.FromResult(value);
			}

			lock (@lock)
			{
				diagHelper.CacheMiss(key);
				value = valueProvider.GetValueAsync<T>(key).GetAwaiter().GetResult();
				cache.Set(key, value,
					new MemoryCacheEntryOptions()
					{
						AbsoluteExpirationRelativeToNow = cacheSettings.Ttl,
					}.RegisterPostEvictionCallback(PostEvictionCallback));
				return Task.FromResult(value);
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
				}

				disposed = true;
			}
		}
	}
}
