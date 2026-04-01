using StackExchange.Redis;

namespace Inventory.Infrastructure.Configuration;

public class WaitlistRedisConfiguration
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _queuePrefix = "waitlist";
    private readonly LuaScript _atomicSelectScript;
    private readonly LuaScript _lockScript;

    public WaitlistRedisConfiguration(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _atomicSelectScript = LuaScript.Prepare(
            @"local key = @key
            local result = redis.call('ZRANGE', key, 0, 0, 'WITHSCORES')
            if #result > 0 then
                redis.call('ZREM', key, result[1])
                return result
            end
            return nil");

        _lockScript = LuaScript.Prepare(
            @"local lockKey = @lockKey
            local lockValue = @lockValue
            local ttl = @ttl
            if redis.call('SET', lockKey, lockValue, 'NX', 'EX', ttl) then
                return 1
            end
            return 0");
    }

    public IDatabase GetDatabase() => _redis.GetDatabase();

    public string GetWaitlistKey(Guid eventId, string section) => $"{_queuePrefix}:{eventId}:{section}";

    public string GetIdempotencyKey(string key) => $"waitlist:processed:{key}";

    public string GetOpportunityTokenKey(string token) => $"waitlist:opportunity:{token}";

    public string GetLockKey(Guid eventId, string section) => $"waitlist:lock:{eventId}:{section}";

    public async Task<bool> AddToQueueAsync(Guid eventId, string section, Guid userId, DateTime joinedAt)
    {
        var db = GetDatabase();
        var key = GetWaitlistKey(eventId, section);
        var score = joinedAt.Ticks;
        return await db.SortedSetAddAsync(key, userId.ToString(), score);
    }

    public async Task<(Guid? UserId, long Score)?> AtomicFifoSelectAsync(Guid eventId, string section)
    {
        var db = GetDatabase();
        var key = GetWaitlistKey(eventId, section);
        
        var result = await db.SortedSetPopAsync(key, 1, Order.Ascending);
        var entries = result.ToArray();
        
        if (entries.Length > 0)
        {
            var userIdStr = entries[0].Element.ToString();
            var score = (long)entries[0].Score;
            if (Guid.TryParse(userIdStr, out var userId))
            {
                return (userId, score);
            }
        }
        return null;
    }

    public async Task<bool> AcquireLockAsync(Guid eventId, string section, string lockValue, TimeSpan ttl)
    {
        var db = GetDatabase();
        var lockKey = GetLockKey(eventId, section);
        return await db.StringSetAsync(lockKey, lockValue, ttl, When.NotExists);
    }

    public async Task ReleaseLockAsync(Guid eventId, string section, string lockValue)
    {
        var db = GetDatabase();
        var lockKey = GetLockKey(eventId, section);
        var currentValue = await db.StringGetAsync(lockKey);
        if (currentValue == lockValue)
        {
            await db.KeyDeleteAsync(lockKey);
        }
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

    public async Task RemoveFromQueueAsync(Guid eventId, string section, Guid userId)
    {
        var db = GetDatabase();
        var key = GetWaitlistKey(eventId, section);
        await db.SortedSetRemoveAsync(key, userId.ToString());
    }
}
