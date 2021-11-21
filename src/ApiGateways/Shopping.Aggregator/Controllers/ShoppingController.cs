using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Shopping.Aggregator.Interfaces;
using Shopping.Aggregator.Models;

namespace Shopping.Aggregator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShoppingController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IBasketService _basketService;
        private readonly ICatalogService _catalogService;
        public ShoppingController(ICatalogService catalogService, IBasketService basketService, IOrderService orderService)
        {
            _catalogService = catalogService;
            _basketService = basketService;
            _orderService = orderService;
        }

        [HttpGet("{userName}", Name = "GetShopping")]
        [ProducesResponseType(type: typeof(ShoppingModel), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ShoppingModel>> GetShopping(string userName)
        {
            // get basket with username
            var basket = await _basketService.GetBasket(userName);

            // iterate basket items and consume products with basket item productId member
            foreach (var item in basket.Items)
            {
                var product = await _catalogService.GetCatalog(item.ProductId);

                // map product related members into basketitemdto with extended columns
                item.ProductName = product.Name;
                item.Category = product.Category;
                item.Summary = product.Summary;
                item.Description = product.Description;
                item.ImageFile = product.ImageFile;
            }

            // consume ordering microservices in order to retrieve order list
            var orders = await _orderService.GetOrdersByUserName(userName);

            // return root shoopingmodeldto class which includes all responses
            var shoopingModel = new ShoppingModel
            {
                UserName = userName,
                BasketWithProducts = basket,
                Orders = orders
            };

            return Ok(shoopingModel);
        }
    }
}