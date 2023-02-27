using Microsoft.Extensions.Caching.Memory;
using rm.Threading;

namespace rm.ThreadingTest;

/// <summary>
/// Uses a lock pool, <see cref="SemaphoreSlimPool"/>.
/// <para></para>
/// Caveat: It loses <i>some</i> parallelism when the valueFactory calls for a key error out
/// as they get synchronized.
/// </summary>
public class CacheWithLockPool : ICachedValueProvider, IMemoryCacheImpl, IDisposable
{
	private readonly ICacheSettings cacheSettings;
	private readonly IValueProvider valueProvider;
	private readonly MemoryCache cache;
	private readonly SemaphoreSlimPool lockPool;
	private readonly DiagHelper diagHelper;

	public CacheWithLockPool(
		ICacheSettings cacheSettings,
		IValueProvider valueProvider,
		DiagHelper diagHelper,
		MemoryCacheOptions memoryCacheOptions = null!)
	{
		this.cacheSettings = cacheSettings;
		this.valueProvider = valueProvider;
		cache = new MemoryCache(memoryCacheOptions ?? new());
		lockPool = new SemaphoreSlimPool(1, 1);
		this.diagHelper = diagHelper;
	}

	public async Task<T> GetValueAsync<T>(object key)
	{
		if (cache.TryGetValue(key, out T value))
		{
			diagHelper.CacheHit(key);
			return value;
		}

		var lockForKey = lockPool[key];
		await lockForKey.WaitAsync()
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
			lockForKey.Release();
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
				lockPool?.Dispose();
			}

			disposed = true;
		}
	}
}
