# Threading

[![NuGet version (rm.Threading)](https://img.shields.io/nuget/v/rm.Threading.svg?style=flat-square)](https://www.nuget.org/packages/rm.Threading/)

## SemaphoreSlimPool

`MemoryCache` has an issue with reentrancy (see [this](https://github.com/dotnet/runtime/issues/36499)). To tackle this for async, coupled with `SemaphoreSlim` being `IDisposable`, a lock pool of `SemaphoreSlim`s can be used.

```python
# key-specific lock with async
memoryCache = new
lockPool = new semaphoreSlimPool(1, 1)
task<T> getAsync(key, valueFactoryAsync):
    if memoryCache.tryGetValue(key, out value):
        return value
    lockForKey = lockPool.get(key)
    await lockForKey.waitAsync()
    try:
        if memoryCache.tryGetValue(key, out value):
            return value
        value = await valueFactoryAsync()
        memoryCache.set(key, value, ttl)
        return value
    finally:
        lockForKey.release()
```

## LockPool

A lock pool of `Object`s for sync.

```python
# key-specific lock with sync (instead, can also use a concurrent map)
memoryCache = new
lockPool = new lockPool()
T get(key, valueFactory):
    if memoryCache.tryGetValue(key, out value):
        return value
    lockForKey = lockPool.get(key)
    lock lockForKey:
        if memoryCache.tryGetValue(key, out value):
            return value
        value = valueFactory()
        memoryCache.set(key, value, ttl)
        return value
```

### Cache Impls

Below are different cache impls to compare against.

 - `CacheWithBlockingThread`, `CacheWithLock` (uber lock), and `CacheWithNoLock` are not really good impls. `CacheWithBlockingThread` uses the sync-over-async anti-pattern, `CacheWithLock` uses an uber lock thereby blocking factory operations of other keys while performing one on a key, `CacheWithNoLock` has no critical section and blasts the downstream dep'y with multiple calls potentially causing cache stampede.
 - `CacheWithTextbookPattern` is how the guidance is currently, but it's reentrant.
 - `CacheWithLazyCache` uses [LazyCache](https://github.com/alastairtree/LazyCache) which solves the rentrancy issue using `Lazy<T>`, [`AsyncLazy<T>`](https://devblogs.microsoft.com/pfxteam/asynclazyt/), and lock pool.
 - `CacheWithFusionCache` uses [FusionCache](https://github.com/ZiggyCreatures/FusionCache). It has many different concurrency implementations.
 - `CacheWithTask` uses a technique to cache the _task itself_ instead of the value, and the task is then awaited to get the value. The technique is quite clever, but tricky to follow.
 - `CacheWithLockPool` uses a lock pool where a key maps to the _same lock_ using hashing, and multiple keys also map to a lock. It loses _some_ parallelism when the `valueFactory[Async]` calls for a key error out as they get synchronized. `poolSize` is `1024` (`processorCount` of `16` * `factor` of `64`), which is bit high to not cause a bottleneck.
 - `CacheWithTaskAndLockPool` reaps the best of both impls; cache the _task itself_ instead of the value, and also use a lock pool to create the task in a critical section. The task is awaited _outside_ of the `lock` (`monitor`) to get the value.

### Perf

`Verify_Perf` for 200 batches where each batch consists of `Environment.ProcessorCount` tasks (total 16,000 tasks), 1,000 keys (cache items), 25ms cache ttl, 10ms of valueFactory delay. The test result shows that when the cache is write-heavy, `CacheWithLockPool` is slower than `CacheWithLazyCache`, `CacheWithTask`, and `CacheWithTaskAndLockPool`.

| Impl (net6.0)            | Time (secs) | Fault %  | Tx/s | Issues                          |
| :-                       |          -: |       -: |   -: | :-                              |
| CacheWithBlockingThread  |        50.4 |        0 |  317 | rentrant, slow, sync-over-async |
| CacheWithLock            |        50.4 |        0 |  317 | slow                            |
| CacheWithNoLock          |         3.3 |        0 | 4848 | highly rentrant                 |
| CacheWithTextbookPattern |         3.4 |        0 | 4705 | highly rentrant                 |
| <br /> |
| CacheWithLazyCache       |         3.4 |        0 | 4705 |                                 |
| CacheWithFusionCache     |         3.4 |        0 | 4705 |                                 |
| CacheWithTask            |         3.3 |        0 | 4848 | slightly rentrant               |
| CacheWithLockPool        |         3.7 |        0 | 4324 | slightly slow                   |
| CacheWithTaskAndLockPool |         3.3 |        0 | 4848 |                                 |

`Verify_Perf` for 1,000 batches where each batch consists of `Environment.ProcessorCount` tasks (total 16,000 tasks), 1,000 keys (cache items), high cache ttl, 10ms of valueFactory delay. In contrast to above test, this test result showcases that all impls perf the same when the cache is read-heavy which is the typical case.

| Impl (net6.0)            | Time (secs) | Fault %  | Tx/s | Issues            |
| :-                       |          -: |       -: |   -: | :-                |
| CacheWithLazyCache       |         3.5 |        0 | 4571 |                   |
| CacheWithFusionCache     |         3.5 |        0 | 4571 |                   |
| CacheWithTask            |         3.5 |        0 | 4571 | slightly rentrant |
| CacheWithLockPool        |         3.5 |        0 | 4571 | slightly slow     |
| CacheWithTaskAndLockPool |         3.5 |        0 | 4571 |                   |

`Verify_Perf` for 1,000 batches where each batch consists of `Environment.ProcessorCount` tasks (total 16,000 tasks), 1,000 keys (cache items), high cache ttl, 10ms of valueFactory delay, and faults. With faults, `CacheWithLockPool` is slower than others, `CacheWithLazyCache`, `CacheWithTask`, `CacheWithTaskAndLockPool`. This is because others will hand out the same task for the concurrent calls on a key, whereas `CacheWithLockPool` will synchronize them taking more time due to backpressure. `CacheWithTask`, `CacheWithTaskAndLockPool` do evict the task if it eventually faults/cancels using an `IChangeToken` so as to not cache errors, same with `CacheWithLazyCache`. `CacheWithLazyCache`, `CacheWithTaskAndLockPool` perf the same as `CacheWithTask`, and don't have `CacheWithTask`'s reentrancy, and `CacheWithLockPool`'s backpressure issues. `CacheWithFusionCache` is negligibly slower as the faults increase, but perfs similarly.

| Impl (net6.0)            | Time (secs) | Fault %  | Tx/s | Issues            |
| :-                       |          -: |       -: |   -: | :-                |
| CacheWithLazyCache       |         3.5 |        0 | 4571 |                   |
| CacheWithFusionCache     |         3.6 |        0 | 4444 |                   |
| CacheWithTask            |         3.5 |        0 | 4571 | slightly rentrant |
| CacheWithLockPool        |         3.5 |        0 | 4571 | slightly slow     |
| CacheWithTaskAndLockPool |         3.5 |        0 | 4571 |                   |
| <br /> |
| CacheWithLazyCache       |         3.9 |       10 | 4102 |                   |
| CacheWithFusionCache     |         4.0 |       10 | 4000 |                   |
| CacheWithTask            |         3.9 |       10 | 4102 | slightly rentrant |
| CacheWithLockPool        |         4.0 |       10 | 4000 | slightly slow     |
| CacheWithTaskAndLockPool |         3.9 |       10 | 4102 |                   |
| <br /> |
| CacheWithLazyCache       |         7.0 |       50 | 2285 |                   |
| CacheWithFusionCache     |         7.1 |       50 | 2254 |                   |
| CacheWithTask            |         7.0 |       50 | 2285 | slightly rentrant |
| CacheWithLockPool        |         7.2 |       50 | 2222 | slightly slow     |
| CacheWithTaskAndLockPool |         7.0 |       50 | 2285 |                   |
| <br /> |
| CacheWithLazyCache       |        16.5 |      100 |  969 |                   |
| CacheWithFusionCache     |        17.1 |      100 |  936 |                   |
| CacheWithTask            |        16.5 |      100 |  969 | slightly rentrant |
| CacheWithLockPool        |        19.9 |      100 |  804 | slightly slow     |
| CacheWithTaskAndLockPool |        16.5 |      100 |  969 |                   |

## Verdict

Use `CacheWithLazyCache` (uses [LazyCache](https://github.com/alastairtree/LazyCache)), `CacheWithFusionCache` (uses [FusionCache](https://github.com/ZiggyCreatures/FusionCache)), or `CacheWithTaskAndLockPool`.

## `CacheWithTaskAndLockPool` impl

```python
# key-specific lock with async
lockForKeyPool = new lockPool()
task<T> getAsync(key, valueFactoryAsync):
    if !cache.tryGetValue(key, out valueTask):
        lockForKey = lockForKeyPool.get(key)
        lock lockForKey:
            if !cache.tryGetValue(key, out valueTask):
                valueTask = cache.getOrAdd(key, (cacheEntry) =>
                {
                    cacheEntry.ttl = ttl
                    var task = valueFactoryAsync<T>(...)
                    cacheEntry.addExpirationToken(new evictErroredCachedTaskChangeToken(task))
                })
    return await valueTask

# evict task that errors out
class evictErroredCachedTaskChangeToken : iChangeToken
    changed = false
    ctor(task):
        changed = isEvictable(task)
        task.continueWith(t =>
            changed = isEvictable(t))
    bool isEvictable(task) => task.canceled || task.faulted
    hasChange => changed
    # ...
```

