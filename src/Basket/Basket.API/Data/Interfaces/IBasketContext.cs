using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Basket.API.Data.Interfaces
{
    public interface IBasketContext
    {
        Task<bool> Add<T>(string cacheItemKey, T cacheItem);
        Task<bool> Add<T>(string cacheItemKey, T cacheItem, int minutesToExpire);
        Task<bool> ContainsKey(string cacheItemKey);
        Task<T> Get<T>(string cacheItemKey);
        Task<bool> RemoveByKey(string cacheItemKey);
    }
}
