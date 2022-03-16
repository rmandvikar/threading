using Microsoft.Extensions.Caching.Memory;
using rm.Threading;

namespace rm.ThreadingTest
{
	public class CacheWithLockPool : IValueProvider, IDisposable
	{
		private readonly ICacheSettings cacheSettings;
		private readonly IValueProvider valueProvider;
		private readonly MemoryCache cache;
		private readonly SemaphoreSlimPool lockPool;

		public CacheWithLockPool(
			ICacheSettings cacheSettings,
			IValueProvider valueProvider,
			MemoryCacheOptions memoryCacheOptions = null!)
		{
			this.cacheSettings = cacheSettings;
			this.valueProvider = valueProvider;
			cache = new MemoryCache(memoryCacheOptions ?? new MemoryCacheOptions());
			lockPool = new SemaphoreSlimPool(1, 1);
		}

		public async Task<T> GetValueAsync<T>(object key)
		{
			if (cache.TryGetValue(key, out T value))
			{
				DiagHelper.CacheHit(key);
				return value;
			}

			var lockForKey = lockPool[key];
			await lockForKey.WaitAsync()
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
				lockForKey.Release();
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
					lockPool?.Dispose();
				}

				_disposed = true;
			}
		}
	}
}
