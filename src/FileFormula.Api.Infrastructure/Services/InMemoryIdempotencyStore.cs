using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace FileFormula.Api.Infrastructure.Services;

/// <summary>
/// Stores idempotent request state in memory.
/// </summary>
public class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _inFlight = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryIdempotencyStore"/> class.
    /// </summary>
    /// <param name="cache">The memory cache used for completed responses.</param>
    public InMemoryIdempotencyStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public IdempotencyLookupResult Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (_cache.TryGetValue(key, out IdempotencyStoredResponse? response) && response is not null)
        {
            return new IdempotencyLookupResult
            {
                HasStoredResponse = true,
                Response = response
            };
        }

        return new IdempotencyLookupResult
        {
            IsInProgress = _inFlight.ContainsKey(key)
        };
    }

    /// <inheritdoc />
    public bool TryBegin(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _inFlight.TryAdd(key, 0);
    }

    /// <inheritdoc />
    public void Complete(string key, IdempotencyStoredResponse response, TimeSpan expiration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(response);

        _cache.Set(key, response, expiration);
        _inFlight.TryRemove(key, out _);
    }

    /// <inheritdoc />
    public void Abandon(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _inFlight.TryRemove(key, out _);
    }
}