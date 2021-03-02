using Basket.API.Data.Interfaces;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Basket.API.Data
{
    public class BasketContext : IBasketContext
    {
        private readonly ConnectionMultiplexer _redisConnection;
        private IDatabase _Redis { get; }

        public BasketContext(ConnectionMultiplexer redisConnection)
        {
            _redisConnection = redisConnection ?? throw new ArgumentNullException(nameof(redisConnection));
            _Redis = _redisConnection.GetDatabase();
        }

        public async Task<bool> Add<T>(string cacheItemKey, T cacheItem)
        {
            var json = JsonConvert.SerializeObject(cacheItem);

            var added = await _Redis.StringSetAsync(cacheItemKey, json);

            return added;
        }

        public async Task<bool> Add<T>(string cacheItemKey, T cacheItem, int minutesToExpire)
        {
            var json = JsonConvert.SerializeObject(cacheItem);

            var added = await _Redis.StringSetAsync(cacheItemKey, json, TimeSpan.FromMinutes(minutesToExpire));

            return added;
        }

        public async Task<bool> ContainsKey(string cacheItemKey)
        { 
            return await _Redis.KeyExistsAsync(cacheItemKey);
        }

        public async Task<T> Get<T>(string cacheItemKey)
        {
            if (!_Redis.KeyExists(cacheItemKey))
                return default(T);

            var json = await _Redis.StringGetAsync(cacheItemKey);

            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task<bool> RemoveByKey(string cacheItemKey)
        {
            var deleted = await _Redis.KeyDeleteAsync(cacheItemKey);

            return deleted;
        }
    }
}
