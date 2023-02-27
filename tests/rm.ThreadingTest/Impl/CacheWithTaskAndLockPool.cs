using Microsoft.Extensions.Caching.Memory;
using rm.Threading;

namespace rm.ThreadingTest;

/// <summary>
/// Uses a combination of two techniques, cache the <i>task itself</i> instead of the value,
/// and also use the lock pool to create the task.
/// </summary>
public class CacheWithTaskAndLockPool : ICachedValueProvider, IMemoryCacheImpl, IDisposable
{
	private readonly ICacheSettings cacheSettings;
	private readonly IValueProvider valueProvider;
	private readonly MemoryCache cache;
	private readonly LockPool lockPool;
	private readonly DiagHelper diagHelper;

	public CacheWithTaskAndLockPool(
		ICacheSettings cacheSettings,
		IValueProvider valueProvider,
		DiagHelper diagHelper,
		MemoryCacheOptions memoryCacheOptions = null!)
	{
		this.cacheSettings = cacheSettings;
		this.valueProvider = valueProvider;
		cache = new MemoryCache(memoryCacheOptions ?? new());
		lockPool = new LockPool();
		this.diagHelper = diagHelper;
	}

	public async Task<T> GetValueAsync<T>(object key)
	{
		if (cache.TryGetValue(key, out Task<T> valueFactoryTask))
		{
			diagHelper.CacheHit(key);
		}
		else
		{
			var lockForKey = lockPool[key];
			lock (lockForKey)
			{
				if (cache.TryGetValue(key, out valueFactoryTask))
				{
					diagHelper.CacheHit(key);
				}
				else
				{
					diagHelper.CacheMiss(key);
					valueFactoryTask = cache.GetOrCreate(key, (cacheEntry) =>
					{
						var task = valueProvider.GetValueAsync<T>(key);

						cacheEntry.AbsoluteExpirationRelativeToNow = cacheSettings.Ttl;
						cacheEntry.RegisterPostEvictionCallback(PostEvictionCallback);
						cacheEntry.AddExpirationToken(new EvictErroredCachedTaskChangeToken(task));

						return task;
					});
				}
			}
		}

		var value = await valueFactoryTask
			.ConfigureAwait(false);

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
