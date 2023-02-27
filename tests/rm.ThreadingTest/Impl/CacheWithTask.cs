using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace rm.ThreadingTest;

/// <summary>
/// Uses a technique to cache the <i>task itself</i>, instead of the value, which is then awaited
/// to get the value. To not cache errors, an unsuccessful task is evicted using an <see cref="IChangeToken"/>.
/// <para></para>
/// Caveat: Calls for a key are <i>slightly</i> reentrant. The technique is quite clever, but tricky to follow.
/// </summary>
public class CacheWithTask : ICachedValueProvider, IMemoryCacheImpl, IDisposable
{
	private readonly ICacheSettings cacheSettings;
	private readonly IValueProvider valueProvider;
	private readonly MemoryCache cache;
	private readonly DiagHelper diagHelper;

	public CacheWithTask(
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
		var valueFactoryTask = cache.GetOrCreate(key, (cacheEntry) =>
		{
			diagHelper.CacheMiss(key);
			valueFactoryCalled = true;

			var task = valueProvider.GetValueAsync<T>(key);

			cacheEntry.AbsoluteExpirationRelativeToNow = cacheSettings.Ttl;
			cacheEntry.RegisterPostEvictionCallback(PostEvictionCallback);
			cacheEntry.AddExpirationToken(new EvictErroredCachedTaskChangeToken(task));

			return task;
		});
		if (!valueFactoryCalled)
		{
			diagHelper.CacheHit(key);
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

/// <summary>
/// Evict a task that completed unsuccessfully.
/// </summary>
public class EvictErroredCachedTaskChangeToken : IChangeToken
{
	private bool hasChanged = false;

	// note: some unsuccessful tasks are held on to, but not handed out
	public EvictErroredCachedTaskChangeToken(Task task)
	{
		// remove task if already errored
		hasChanged = IsEvictable(task);
		// setup to remove task if it errors later
		task.ContinueWith(t =>
			hasChanged = IsEvictable(t));
	}

	private bool IsEvictable(Task task) => task.IsFaulted || task.IsCanceled;

	public bool HasChanged => hasChanged;

	public bool ActiveChangeCallbacks => false;

	public IDisposable RegisterChangeCallback(Action<object> callback, object state)
	{
		throw new NotImplementedException();
	}
}
