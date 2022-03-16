namespace rm.ThreadingTest
{
	public static class DiagHelper
	{
		public static void CacheHit(object key)
		{
			CacheAction(key, "     cache hit");
		}

		public static void CacheMiss(object key)
		{
			CacheAction(key, "factory called");
		}

		private static void CacheAction(object key, string action)
		{
			Console.WriteLine($"{DateTime.UtcNow:mm:ss.fff}: {action} for key {key}  thread {Thread.CurrentThread.ManagedThreadId,3}  processor {Thread.GetCurrentProcessorId(),3}");
		}
	}
}
