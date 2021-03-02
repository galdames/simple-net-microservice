using Basket.API.Data.Interfaces;
using Basket.API.Entities;
using Basket.API.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Basket.API.Repositories
{
    public class BasketRepository : IBasketRepository
    {
        private readonly IBasketContext _basketContext;

        public BasketRepository(IBasketContext basketContext)
        {
            _basketContext = basketContext ?? throw new ArgumentNullException(nameof(basketContext));
        }

        public async Task<bool> DeleteBasket(string userName)
        {
            return await this._basketContext.RemoveByKey(userName);
        }

        public async Task<BasketCart> GetBasket(string userName)
        {
            return await this._basketContext.Get<BasketCart>(userName);
        }

        public async Task<BasketCart> UpdateBasket(BasketCart basket)
        {
            var updated = await this._basketContext.Add<BasketCart>(basket.UserName, basket);

            if (!updated)
            {
                return null;
            }

            return await GetBasket(basket.UserName);
        }
    }

}
