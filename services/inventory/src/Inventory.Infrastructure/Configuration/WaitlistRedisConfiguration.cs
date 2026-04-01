using StackExchange.Redis;

namespace Inventory.Infrastructure.Configuration;

public class WaitlistRedisConfiguration
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _queuePrefix = "waitlist";

    public WaitlistRedisConfiguration(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public IDatabase GetDatabase() => _redis.GetDatabase();

    public string GetWaitlistKey(Guid eventId, string section) => $"{_queuePrefix}:{eventId}:{section}";

    public string GetIdempotencyKey(string key) => $"waitlist:processed:{key}";

    public string GetOpportunityTokenKey(string token) => $"waitlist:opportunity:{token}";

    public async Task<bool> AddToQueueAsync(Guid eventId, string section, Guid userId, DateTime joinedAt)
    {
        var db = GetDatabase();
        var key = GetWaitlistKey(eventId, section);
        var score = joinedAt.Ticks;
        return await db.SortedSetAddAsync(key, userId.ToString(), score);
    }

    public async Task<RedisValue?> GetNextUserAsync(Guid eventId, string section)
    {
        var db = GetDatabase();
        var key = GetWaitlistKey(eventId, section);
        var results = await db.SortedSetPopAsync(key, 1, Order.Descending);
        var entries = results.ToArray();
        if (entries.Length > 0)
        {
            await db.SortedSetRemoveAsync(key, entries[0].Element);
            return entries[0].Element;
        }
        return null;
    }

    public async Task<long> GetQueuePositionAsync(Guid eventId, string section, Guid userId)
    {
        var db = GetDatabase();
        var key = GetWaitlistKey(eventId, section);
        var rank = await db.SortedSetRankAsync(key, userId.ToString(), Order.Ascending);
        return rank.HasValue ? (long)(rank.Value + 1) : -1;
    }

    public async Task<long> GetQueueLengthAsync(Guid eventId, string section)
    {
        var db = GetDatabase();
        var key = GetWaitlistKey(eventId, section);
        return await db.SortedSetLengthAsync(key);
    }

    public async Task SetIdempotencyAsync(string key, TimeSpan expiry)
    {
        var db = GetDatabase();
        await db.StringSetAsync($"waitlist:processed:{key}", "1", expiry);
    }

    public async Task<bool> CheckIdempotencyAsync(string key)
    {
        var db = GetDatabase();
        return await db.KeyExistsAsync($"waitlist:processed:{key}");
    }
}
