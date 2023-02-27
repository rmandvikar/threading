namespace rm.ThreadingTest;

/// <summary>
/// Helps to diag race conditions by outputting cache hit, miss, evict messages.
/// <para></para>
/// Diag messages can be enabled by <see cref="enabled"/> flag.
/// </summary>
public class DiagHelper
{
	private readonly bool enabled;

	public DiagHelper(bool enabled)
	{
		this.enabled = enabled;
	}

	public void CacheHit(object key)
	{
		if (!enabled)
		{
			return;
		}
		CacheAction(key, "     cache hit");
	}

	public void CacheMiss(object key)
	{
		if (!enabled)
		{
			return;
		}
		CacheAction(key, "factory called");
	}

	public void CacheEvicted(object key)
	{
		if (!enabled)
		{
			return;
		}
		CacheAction(key, "       evicted");
	}

	private void CacheAction(object key, string action)
	{
		Console.WriteLine($"{DateTime.UtcNow:mm:ss.fff}: {action} for key {key}  thread {Environment.CurrentManagedThreadId,3}  processor {Thread.GetCurrentProcessorId(),3}");
	}
}
