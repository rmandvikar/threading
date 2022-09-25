using System.Security.Cryptography;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;
using rm.FeatureToggle;
using rm.Random2;
using ZiggyCreatures.Caching.Fusion;

namespace rm.ThreadingTest
{
	[TestFixture]
	public class CacheImplsPerfTests
	{
		[TestFixture]
		public class CacheWithNoLockTests : CacheImplsPerfTests
		{
			[Explicit]
			[Test]
			[TestCase(10, 25, 10, 200, 0)]
			[TestCase(100, 25, 10, 200, 0)]
			[TestCase(1_000, 25, 10, 200, 0)]
			[TestCase(10_000, 25, 10, 200, 0)]
			[TestCase(100_000, 25, 10, 200, 0)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 0)]
			public async Task Verify_Perf(int keyCount, double ttlInMs, int valueFactoryDelayInMs, int batches, double errorPercentage)
			{
				await Verify_Perf(keyCount, ttlInMs, valueFactoryDelayInMs, batches, errorPercentage,
					(fixture) => fixture.Create<CacheWithNoLock>());
			}
		}

		[TestFixture]
		public class CacheWithLockTests : CacheImplsPerfTests
		{
			[Explicit]
			[Test]
			[TestCase(10, 25, 10, 200, 0)]
			[TestCase(100, 25, 10, 200, 0)]
			[TestCase(1_000, 25, 10, 200, 0)]
			[TestCase(10_000, 25, 10, 200, 0)]
			[TestCase(100_000, 25, 10, 200, 0)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 0)]
			public async Task Verify_Perf(int keyCount, double ttlInMs, int valueFactoryDelayInMs, int batches, double errorPercentage)
			{
				await Verify_Perf(keyCount, ttlInMs, valueFactoryDelayInMs, batches, errorPercentage,
					(fixture) => fixture.Create<CacheWithLock>());
			}
		}

		[TestFixture]
		public class CacheWithTextbookPatternTests : CacheImplsPerfTests
		{
			[Explicit]
			[Test]
			[TestCase(10, 25, 10, 200, 0)]
			[TestCase(100, 25, 10, 200, 0)]
			[TestCase(1_000, 25, 10, 200, 0)]
			[TestCase(10_000, 25, 10, 200, 0)]
			[TestCase(100_000, 25, 10, 200, 0)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 0)]
			// inject fault
			[TestCase(1_000, int.MaxValue, 10, 1000, 10)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 50)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 100)]
			// inject fault with higher delay
			[TestCase(1_000, int.MaxValue, 20, 1000, 100)]
			public async Task Verify_Perf(int keyCount, double ttlInMs, int valueFactoryDelayInMs, int batches, double errorPercentage)
			{
				await Verify_Perf(keyCount, ttlInMs, valueFactoryDelayInMs, batches, errorPercentage,
					(fixture) => fixture.Create<CacheWithTextbookPattern>());
			}
		}

		[TestFixture]
		public class CacheWithBlockingThreadTests : CacheImplsPerfTests
		{
			[Explicit]
			[Test]
			[TestCase(10, 25, 10, 200, 0)]
			[TestCase(100, 25, 10, 200, 0)]
			[TestCase(1_000, 25, 10, 200, 0)]
			[TestCase(10_000, 25, 10, 200, 0)]
			[TestCase(100_000, 25, 10, 200, 0)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 0)]
			public async Task Verify_Perf(int keyCount, double ttlInMs, int valueFactoryDelayInMs, int batches, double errorPercentage)
			{
				await Verify_Perf(keyCount, ttlInMs, valueFactoryDelayInMs, batches, errorPercentage,
					(fixture) => fixture.Create<CacheWithBlockingThread>());
			}
		}

		[TestFixture]
		public class CacheWithLazyCacheTests : CacheImplsPerfTests
		{
			[Explicit]
			[Test]
			[TestCase(1, 5, 10, 200, 0)]
			[TestCase(10, 25, 10, 200, 0)]
			[TestCase(100, 25, 10, 200, 0)]
			[TestCase(1_000, 25, 10, 200, 0)]
			[TestCase(10_000, 25, 10, 200, 0)]
			[TestCase(100_000, 25, 10, 200, 0)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 0)]
			// inject fault
			[TestCase(1_000, int.MaxValue, 10, 1000, 10)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 50)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 100)]
			// inject fault with higher delay
			[TestCase(1_000, int.MaxValue, 20, 1000, 100)]
			public async Task Verify_Perf(int keyCount, double ttlInMs, int valueFactoryDelayInMs, int batches, double errorPercentage)
			{
				await Verify_Perf(keyCount, ttlInMs, valueFactoryDelayInMs, batches, errorPercentage,
					(fixture) => fixture.Create<CacheWithLazyCache>());
			}
		}

		[TestFixture]
		public class CacheWithFusionCacheTests : CacheImplsPerfTests
		{
			[Explicit]
			[Test]
			[TestCase(1, 5, 10, 200, 0)]
			[TestCase(10, 25, 10, 200, 0)]
			[TestCase(100, 25, 10, 200, 0)]
			[TestCase(1_000, 25, 10, 200, 0)]
			[TestCase(10_000, 25, 10, 200, 0)]
			[TestCase(100_000, 25, 10, 200, 0)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 0)]
			// inject fault
			[TestCase(1_000, int.MaxValue, 10, 1000, 10)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 50)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 100)]
			// inject fault with higher delay
			[TestCase(1_000, int.MaxValue, 20, 1000, 100)]
			public async Task Verify_Perf(int keyCount, double ttlInMs, int valueFactoryDelayInMs, int batches, double errorPercentage)
			{
				await Verify_Perf(keyCount, ttlInMs, valueFactoryDelayInMs, batches, errorPercentage,
					(fixture) =>
					{
						fixture.Register<FusionCacheOptions>(() => null!);
						return fixture.Create<CacheWithFusionCache>();
					});
			}
		}

		[TestFixture]
		public class CacheWithTaskTests : CacheImplsPerfTests
		{
			[Explicit]
			[Test]
			[TestCase(10, 25, 10, 200, 0)]
			[TestCase(100, 25, 10, 200, 0)]
			[TestCase(1_000, 25, 10, 200, 0)]
			[TestCase(10_000, 25, 10, 200, 0)]
			[TestCase(100_000, 25, 10, 200, 0)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 0)]
			// inject fault
			[TestCase(1_000, int.MaxValue, 10, 1000, 10)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 50)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 100)]
			// inject fault with higher delay
			[TestCase(1_000, int.MaxValue, 20, 1000, 100)]
			public async Task Verify_Perf(int keyCount, double ttlInMs, int valueFactoryDelayInMs, int batches, double errorPercentage)
			{
				await Verify_Perf(keyCount, ttlInMs, valueFactoryDelayInMs, batches, errorPercentage,
					(fixture) => fixture.Create<CacheWithTask>());
			}
		}

		[TestFixture]
		public class CacheWithLockPoolTests : CacheImplsPerfTests
		{
			[Explicit]
			[Test]
			[TestCase(10, 25, 10, 200, 0)]
			[TestCase(100, 25, 10, 200, 0)]
			[TestCase(1_000, 25, 10, 200, 0)]
			[TestCase(10_000, 25, 10, 200, 0)]
			[TestCase(100_000, 25, 10, 200, 0)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 0)]
			// inject fault
			[TestCase(1_000, int.MaxValue, 10, 1000, 10)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 50)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 100)]
			// inject fault with higher delay
			[TestCase(1_000, int.MaxValue, 20, 1000, 100)]
			public async Task Verify_Perf(int keyCount, double ttlInMs, int valueFactoryDelayInMs, int batches, double errorPercentage)
			{
				await Verify_Perf(keyCount, ttlInMs, valueFactoryDelayInMs, batches, errorPercentage,
					(fixture) => fixture.Create<CacheWithLockPool>());
			}
		}

		[TestFixture]
		public class CacheWithTaskAndLockPoolTests : CacheImplsPerfTests
		{
			[Explicit]
			[Test]
			[TestCase(10, 25, 10, 200, 0)]
			[TestCase(100, 25, 10, 200, 0)]
			[TestCase(1_000, 25, 10, 200, 0)]
			[TestCase(10_000, 25, 10, 200, 0)]
			[TestCase(100_000, 25, 10, 200, 0)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 0)]
			// inject fault
			[TestCase(1_000, int.MaxValue, 10, 1000, 10)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 50)]
			[TestCase(1_000, int.MaxValue, 10, 1000, 100)]
			// inject fault with higher delay
			[TestCase(1_000, int.MaxValue, 20, 1000, 100)]
			public async Task Verify_Perf(int keyCount, double ttlInMs, int valueFactoryDelayInMs, int batches, double errorPercentage)
			{
				await Verify_Perf(keyCount, ttlInMs, valueFactoryDelayInMs, batches, errorPercentage,
					(fixture) => fixture.Create<CacheWithTaskAndLockPool>());
			}
		}

		private async Task Verify_Perf(int keyCount, double ttlInMs, int valueFactoryDelayInMs, int batches, double errorPercentage,
			Func<IFixture, ICachedValueProvider> cacheImplFactory)
		{
			const string value = "cachetastic!";

			var fixture = GetFixture();

			fixture.Register<ICacheSettings>(() =>
				new CacheSettings
				{
					Ttl = TimeSpan.FromMilliseconds(ttlInMs),
				});

			var rng = RandomFactory.GetThreadStaticRandom();
			var probability = new Probability(rng);

			var valueFactoryCalledCount = 0;
			var valueProvider = fixture.Freeze<Mock<IValueProvider>>();
			valueProvider
				.Setup(vp => vp.GetValueAsync<string>(It.IsAny<object>()))
				.Returns(async (object key) =>
				{
					Interlocked.Increment(ref valueFactoryCalledCount);
					await Task.Delay(TimeSpan.FromMilliseconds(valueFactoryDelayInMs));
					if (probability.IsTrue(errorPercentage))
					{
						throw new TaskCanceledException("boom!");
					}
					return $"{value} {key}";
				});

			fixture.Register(() =>
				new MemoryCacheOptions
				{
					// default 1m
					ExpirationScanFrequency = TimeSpan.FromMilliseconds(0),
					// default 0.05
					//CompactionPercentage = 1.0d,
				});

			fixture.Register(() =>
				new DiagHelper(false));

			using var cacheImpl = cacheImplFactory(fixture);
			var width = keyCount.ToString().Length;
			var keys = Enumerable.Range(0, keyCount).Select(x => x.ToString().PadLeft(width)).ToArray();

			int batchSize = Environment.ProcessorCount;

			var tasks = new List<Task<string>>(batchSize);
			string[] results = null!;
			for (int i = 0; i < batches; i++)
			{
				for (int b = 0; b < batchSize; b++)
				{
					var key = keys[rng.Next(keys.Length)];
					tasks.Add(cacheImpl.GetValueAsync<string>(key));
				}
				try
				{
					results = await Task.WhenAll(tasks);
				}
				catch
				{
					// swallow
				}
				finally
				{
					tasks.Clear();
				}
			}

			// check to verify if errored tasks are handed out by cache impl that caches tasks
			VerifyCacheWithTaskImpls((IMemoryCacheImpl)cacheImpl, keys);

			Console.WriteLine($"valueFactoryCalledCount: {valueFactoryCalledCount}");
		}

		private static void VerifyCacheWithTaskImpls(IMemoryCacheImpl cacheImpl, string[] keys)
		{
			if (cacheImpl is not CacheWithTask
				&& cacheImpl is not CacheWithTaskAndLockPool)
			{
				return;
			}

			var erroredTasksCount = 0;
			var memoryCache = (MemoryCache)cacheImpl.Value;
			var cacheSizeBefore = memoryCache.Count;
			foreach (var key in keys)
			{
				if (memoryCache.Get(key) is Task<string> valueTask
					&& (valueTask.IsFaulted || valueTask.IsCanceled))
				{
					erroredTasksCount++;
				}
			}
			var cacheSizeAfter = memoryCache.Count;
			if (cacheSizeBefore != cacheSizeAfter)
			{
				// DEBUG only. if here, CacheWithTask impl does hang on to errored task(s)
				// that are not evicted yet
			}
			if (erroredTasksCount > 0)
			{
				// if here, CacheWithTask impl hands out errored task(s)
				// i.e. it has a bug (specifically in EvictErroredCachedTaskChangeToken)
				Assert.Fail();
			}
		}

		private IFixture GetFixture()
		{
			return new Fixture().Customize(new AutoMoqCustomization());
		}
	}
}
