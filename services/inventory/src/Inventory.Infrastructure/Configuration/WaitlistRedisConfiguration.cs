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

    public async Task<bool> AddToQueueAsync(Guid eventId, string section, Guid userId, DateTime joinedAt)
    {
        var db = GetDatabase();
        var key = GetWaitlistKey(eventId, section);
        var score = joinedAt.Ticks;
        return await db.SortedSetAddAsync(key, userId.ToString(), score);
    }

    public async Task<long> GetQueuePositionAsync(Guid eventId, string section, Guid userId)
    {
        var db = GetDatabase();
        var key = GetWaitlistKey(eventId, section);
        var rank = await db.SortedSetRankAsync(key, userId.ToString(), Order.Ascending);
        return rank.HasValue ? (long)(rank.Value + 1) : -1;
    }

    public async Task<Guid?> FifoPopAsync(Guid eventId, string section)
    {
        var db = GetDatabase();
        var key = GetWaitlistKey(eventId, section);
        
        // Get the first element (lowest score = earliest joined)
        var results = await db.SortedSetRangeByRankAsync(key, 0, 0, Order.Ascending);
        
        if (results.Length > 0)
        {
            var userIdStr = results[0].ToString();
            if (Guid.TryParse(userIdStr, out var userId))
            {
                // Remove from queue after getting
                await db.SortedSetRemoveAsync(key, userIdStr);
                return userId;
            }
        }
        return null;
    }

    public async Task RemoveFromQueueAsync(Guid eventId, string section, Guid userId)
    {
        var db = GetDatabase();
        var key = GetWaitlistKey(eventId, section);
        await db.SortedSetRemoveAsync(key, userId.ToString());
    }
}
