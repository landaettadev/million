using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using RealEstate.Application;

namespace RealEstate.Infrastructure.Services;

public class RedisCacheService : ICacheService, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IConfiguration configuration, ILogger<RedisCacheService> logger)
    {
        _logger = logger;
        
        var connectionString = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        var options = ConfigurationOptions.Parse(connectionString);
        options.ConnectRetry = 3;
        
        _redis = ConnectionMultiplexer.Connect(options);
        _database = _redis.GetDatabase();
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        
        _logger.LogInformation("Redis cache service initialized with connection: {ConnectionString}", connectionString);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }

            var result = JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving value from cache for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            await _database.StringSetAsync(key, serializedValue, expiry);
            
            _logger.LogDebug("Value cached for key: {Key} with expiry: {Expiry}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var result = await _database.KeyDeleteAsync(key);
            if (result)
            {
                _logger.LogDebug("Cache key removed: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }

    public async Task RemovePatternAsync(string pattern, CancellationToken ct = default)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);
            
            foreach (var key in keys)
            {
                await _database.KeyDeleteAsync(key);
            }
            
            _logger.LogDebug("Cache pattern removed: {Pattern}, {Count} keys affected", pattern, keys.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of cache key: {Key}", key);
            return false;
        }
    }

    public async Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var result = await _database.StringIncrementAsync(key, value);
            
            // Set expiry if specified
            if (expiry.HasValue)
            {
                await _database.KeyExpireAsync(key, expiry.Value);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing cache key: {Key}", key);
            return 0;
        }
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }
}

// In-memory cache service as fallback
public class InMemoryCacheService : ICacheService
{
    private readonly ILogger<InMemoryCacheService> _logger;
    private readonly Dictionary<string, (object Value, DateTime Expiry)> _cache = new();
    private readonly object _lock = new();

    public InMemoryCacheService(ILogger<InMemoryCacheService> logger)
    {
        _logger = logger;
        _logger.LogInformation("In-memory cache service initialized");
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                if (cached.Expiry > DateTime.UtcNow)
                {
                    _logger.LogDebug("In-memory cache hit for key: {Key}", key);
                    return Task.FromResult<T?>((T)cached.Value);
                }
                else
                {
                    _cache.Remove(key);
                }
            }
            
            _logger.LogDebug("In-memory cache miss for key: {Key}", key);
            return Task.FromResult<T?>(default);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var expiryTime = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromMinutes(30));
            _cache[key] = (value!, expiryTime);
            
            _logger.LogDebug("Value cached in memory for key: {Key} with expiry: {Expiry}", key, expiry);
            return Task.CompletedTask;
        }
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var removed = _cache.Remove(key);
            if (removed)
            {
                _logger.LogDebug("In-memory cache key removed: {Key}", key);
            }
            return Task.CompletedTask;
        }
    }

    public Task RemovePatternAsync(string pattern, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var keysToRemove = _cache.Keys.Where(k => k.Contains(pattern)).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
            
            _logger.LogDebug("In-memory cache pattern removed: {Pattern}, {Count} keys affected", pattern, keysToRemove.Count);
            return Task.CompletedTask;
        }
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                return Task.FromResult(cached.Expiry > DateTime.UtcNow);
            }
            return Task.FromResult(false);
        }
    }

    public Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached) && cached.Value is long currentValue)
            {
                var newValue = currentValue + value;
                _cache[key] = (newValue, cached.Expiry);
                return Task.FromResult(newValue);
            }
            
            var defaultValue = value;
            _cache[key] = (defaultValue, DateTime.UtcNow.AddMinutes(30));
            return Task.FromResult(defaultValue);
        }
    }
}
