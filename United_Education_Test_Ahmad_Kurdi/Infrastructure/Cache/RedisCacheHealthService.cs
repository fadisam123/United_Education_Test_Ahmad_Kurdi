//using StackExchange.Redis;

//namespace United_Education_Test_Ahmad_Kurdi.Infrastructure.Cache
//{
//    // This class is registerd, and should, as SingleTon; so that only one instance is used
//    // (To preserve consistency, correct behavior, and state)
//    public class RedisCacheHealthService : ICacheHealthService
//    {
//        private readonly ILogger<RedisCacheHealthService> _logger;
//        private readonly IConnectionMultiplexer _connectionMultiplexer;

//        // marke as volatile to prevent one thread from seeing a stale value in multi-threaded scenarios
//        // we can make use of lock keyword but this is simpler (full locking here is undesirable for performance)
//        private volatile bool _isCacheHealthy = true; // simple thread safe flag

//        // A timestamp indicating last time a health check was performed
//        // We use the Interlocked class to read and write this value to prevent race conditions
//        private long _lastHealthCheckTicks = DateTime.MinValue.Ticks;

//        // Semaphore acts like a lock (in multi-threaded scenarios for thread safe operations),
//        // ensuring that only one thread can execute the actual health check logic at a given time.
//        // This prevents "thundering herd",
//        // a problem where many requests might trigger a flood of simultaneous health checks.
//        private readonly SemaphoreSlim _healthCheckSemaphore = new(1, 1);

//        // Configurable constants

//        // The minimum time that must pass before a new health check is performed.
//        // This is the throttling mechanism.
//        private const int HealthCheckIntervalMs = 30_000;

//        // The maximum time a thread will wait to acquire the semaphore.
//        // If it can't get the lock in this time (because another thread is already running a check)
//        private const int SemaphoreWaitMs = 500;

//        // The number of consecutive ping attempts that must fail before the cache is marked as unhealthy.
//        private const int FailureThreshold = 2;

//        // A counter that tracks the number of failures.
//        // We use the Interlocked class to read and write this value to prevent race conditions
//        private int _consecutiveFailures = 0;

//        public RedisCacheHealthService(
//            ILogger<RedisCacheHealthService> logger,
//            IConnectionMultiplexer connectionMultiplexer)
//        {
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//            _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
//        }

//        public bool IsCacheHealthy => _isCacheHealthy;

//        public async Task<bool> CheckCacheHealthAsync()
//        {
//            // We use the Interlocked class to read and write _lastHealthCheckTicks value to prevent race conditions
//            var last = Interlocked.Read(ref _lastHealthCheckTicks);
//            var nowTicks = DateTime.UtcNow.Ticks;
//            var elapsedTicks = nowTicks - last;
//            var intervalTicks = TimeSpan.FromMilliseconds(HealthCheckIntervalMs).Ticks;

//            // Checks if a health check was run recently (within the HealthCheckIntervalMs variable),
//            // just return current state
//            if (elapsedTicks > 0 && elapsedTicks < intervalTicks)
//                return _isCacheHealthy;

//            // Try to acquire the semaphore (i.e., lock) to perform a health check
//            // If successful, the thread proceeds to perform the actual health check, i.e., does not enter if.
//            if (!await _healthCheckSemaphore.WaitAsync(SemaphoreWaitMs))
//            {
//                // If unsuccessful (because another thread is already performing a check
//                // and didn't finish within SemaphoreWaitMs variable time),
//                // just log and returns the current cached health state
//                _logger.LogDebug("Health check semaphore contended; returning cached health state={Health}", _isCacheHealthy);
//                return _isCacheHealthy;
//            }

//            try
//            {
//                // Update last-check timestamp immediately so other callers won't also probe
//                Interlocked.Exchange(ref _lastHealthCheckTicks, DateTime.UtcNow.Ticks);

//                try
//                {
//                    _logger.LogInformation("Checking Redis server availability using Ping:");

//                    var db = _connectionMultiplexer.GetDatabase();
//                    var pong = await db.PingAsync();

//                    // success -> reset failure counter and mark healthy
//                    Interlocked.Exchange(ref _consecutiveFailures, 0);
//                    if (!_isCacheHealthy)
//                    {   // success indicating healthy (_isCacheHealthy state is false)
//                        _isCacheHealthy = true;
//                        _logger.LogInformation("Redis marked healthy (ping {Ms} ms)", pong.TotalMilliseconds);
//                    }
//                    else
//                    {   // success indicating healthy (_isCacheHealthy state is true)
//                        _logger.LogDebug("Redis health check passed in {Ms} ms", pong.TotalMilliseconds);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    var failures = Interlocked.Increment(ref _consecutiveFailures);

//                    _logger.LogWarning(ex, "Redis health check attempt failed (consecutiveFailures={Failures})", failures);

//                    // if _consecutiveFailures reached FailureThreshold,
//                    // mark as unhealty and log
//                    if (failures >= FailureThreshold)
//                    {
//                        if (_isCacheHealthy)
//                        {
//                            _isCacheHealthy = false;
//                            _logger.LogWarning("Redis marked as unhealthy after {Failures} consecutive failures", failures);
//                        }
//                    }
//                }

//                return _isCacheHealthy;
//            }
//            // Guarantee releasing the lock so other threads can perform checks later
//            finally
//            {
//                _healthCheckSemaphore.Release();
//            }
//        }

//        public void MarkCacheUnhealthy()
//        {
//            if (_isCacheHealthy)
//            {
//                _isCacheHealthy = false;
//                Interlocked.Exchange(ref _consecutiveFailures, FailureThreshold);
//                _logger.LogWarning("Redis marked as unhealthy (manually)");
//            }
//        }

//        public void MarkCacheHealthy()
//        {
//            if (!_isCacheHealthy)
//            {
//                _isCacheHealthy = true;
//                Interlocked.Exchange(ref _consecutiveFailures, 0);
//                _logger.LogInformation("Redis marked as healthy (manually)");
//            }
//        }
//    }
//}
