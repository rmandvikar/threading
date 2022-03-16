using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;
using rm.Random2;

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
			[TestCase(10, 25, 10)]
			[TestCase(100, 25, 10)]
			[TestCase(1_000, 25, 10)]
			[TestCase(10_000, 25, 10)]
			[TestCase(100_000, 25, 10)]
			public async Task Verify_Perf_And_Race(int keyCount, double ttlInMs, int valueFactoryDelayInMs)
			{
				await Verify_Perf_And_Race(keyCount, ttlInMs, valueFactoryDelayInMs,
					(fixture) => fixture.Create<CacheWithNoLock>());
			}
		}

		[TestFixture]
		public class CacheWithLockTests : CacheImplsPerfTests
		{
			[Explicit]
			[Test]
			[TestCase(10, 25, 10)]
			[TestCase(100, 25, 10)]
			[TestCase(1_000, 25, 10)]
			[TestCase(10_000, 25, 10)]
			[TestCase(100_000, 25, 10)]
			public async Task Verify_Perf_And_Race(int keyCount, double ttlInMs, int valueFactoryDelayInMs)
			{
				await Verify_Perf_And_Race(keyCount, ttlInMs, valueFactoryDelayInMs,
					(fixture) => fixture.Create<CacheWithLock>());
			}
		}

		[TestFixture]
		public class CacheWithBlockingThreadTests : CacheImplsPerfTests
		{
			[Explicit]
			[Test]
			[TestCase(10, 25, 10)]
			[TestCase(100, 25, 10)]
			[TestCase(1_000, 25, 10)]
			[TestCase(10_000, 25, 10)]
			[TestCase(100_000, 25, 10)]
			public async Task Verify_Perf_And_Race(int keyCount, double ttlInMs, int valueFactoryDelayInMs)
			{
				await Verify_Perf_And_Race(keyCount, ttlInMs, valueFactoryDelayInMs,
					(fixture) => fixture.Create<CacheWithBlockingThread>());
			}
		}

		[TestFixture]
		public class CacheWithTaskTests : CacheImplsPerfTests
		{
			[Explicit]
			[Test]
			[TestCase(10, 25, 10)]
			[TestCase(100, 25, 10)]
			[TestCase(1_000, 25, 10)]
			[TestCase(10_000, 25, 10)]
			[TestCase(100_000, 25, 10)]
			public async Task Verify_Perf_And_Race(int keyCount, double ttlInMs, int valueFactoryDelayInMs)
			{
				await Verify_Perf_And_Race(keyCount, ttlInMs, valueFactoryDelayInMs,
					(fixture) => fixture.Create<CacheWithTask>());
			}
		}

		[TestFixture]
		public class CacheWithLockPoolTests : CacheImplsPerfTests
		{
			[Explicit]
			[Test]
			[TestCase(10, 25, 10)]
			[TestCase(100, 25, 10)]
			[TestCase(1_000, 25, 10)]
			[TestCase(10_000, 25, 10)]
			[TestCase(100_000, 25, 10)]
			public async Task Verify_Perf_And_Race(int keyCount, double ttlInMs, int valueFactoryDelayInMs)
			{
				await Verify_Perf_And_Race(keyCount, ttlInMs, valueFactoryDelayInMs,
					(fixture) => fixture.Create<CacheWithLockPool>());
			}
		}

		private async Task Verify_Perf_And_Race(int keyCount, double ttlInMs, int valueFactoryDelayInMs,
			Func<IFixture, IValueProvider> cacheImplFactory)
		{
			const string value = "cachetastic!";

			var fixture = GetFixture();

			fixture.Register<ICacheSettings>(() =>
				new CacheSettings
				{
					Ttl = TimeSpan.FromMilliseconds(ttlInMs),
				});

			var valueProvider = fixture.Freeze<Mock<IValueProvider>>();
			valueProvider
				.Setup(vp => vp.GetValueAsync<string>(It.IsAny<object>()))
				.Returns(async () =>
				{
					await Task.Delay(TimeSpan.FromMilliseconds(valueFactoryDelayInMs));
					return value;
				});

			fixture.Register(() =>
				new MemoryCacheOptions
				{
					ExpirationScanFrequency = TimeSpan.FromMilliseconds(0),
				});

			var cacheImpl = cacheImplFactory(fixture);

			var width = keyCount.ToString().Length;
			var keys = Enumerable.Range(0, keyCount).Select(x => x.ToString().PadLeft(width)).ToArray();

			const int batches = 200;
			int batchSize = Environment.ProcessorCount;
			var rng = RandomFactory.GetThreadStaticRandom();

			var tasks = new List<Task<string>>();
			for (int i = 0; i < batches; i++)
			{
				for (int b = 0; b < batchSize; b++)
				{
					var key = keys[rng.Next(keys.Length)];
					tasks.Add(cacheImpl.GetValueAsync<string>(key));
				}
				await Task.WhenAll(tasks);
			}
		}

		private IFixture GetFixture()
		{
			return new Fixture().Customize(new AutoMoqCustomization());
		}
	}
}
