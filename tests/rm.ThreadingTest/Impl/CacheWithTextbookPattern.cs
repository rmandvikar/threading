using Microsoft.Extensions.Caching.Memory;

namespace rm.ThreadingTest;

/// <summary>
/// Uses <i>recently added</i> async method <see cref="CacheExtensions.GetOrCreateAsync{TItem}(IMemoryCache, object, Func{ICacheEntry, Task{TItem}})"/>.
/// <para></para>
/// Not recommended. Calls for a key are reentrant.
/// </summary>
public class CacheWithTextbookPattern : ICachedValueProvider, IMemoryCacheImpl, IDisposable
{
	private readonly ICacheSettings cacheSettings;
	private readonly IValueProvider valueProvider;
	private readonly MemoryCache cache;
	private readonly DiagHelper diagHelper;

	public CacheWithTextbookPattern(
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
		var valueFactoryCalled = false;
		var value = await cache.GetOrCreateAsync(key, async (cacheEntry) =>
		{
			diagHelper.CacheMiss(key);
			valueFactoryCalled = true;
			var value = await valueProvider.GetValueAsync<T>(key).ConfigureAwait(false);

			cacheEntry.AbsoluteExpirationRelativeToNow = cacheSettings.Ttl;
			cacheEntry.RegisterPostEvictionCallback(PostEvictionCallback);

			return value;
		}).ConfigureAwait(false);
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
				cache?.Dispose();
			}

			disposed = true;
		}
	}
}
