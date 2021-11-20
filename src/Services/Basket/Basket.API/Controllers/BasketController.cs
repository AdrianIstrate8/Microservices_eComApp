using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Basket.API.Entities;
using Basket.API.GrpcServices;
using Basket.API.Interfaces;
using EventBus.Messages.Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Basket.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BasketController : ControllerBase
    {
        private readonly IBasketRepository _repository;
        private readonly DiscountGrpcService _discountGrpcService;
        private readonly IMapper _mapper;
        private readonly IPublishEndpoint _publishEndpoint;
        public BasketController(IBasketRepository repository, DiscountGrpcService discountGrpcService, IMapper mapper, IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
            _mapper = mapper;
            _discountGrpcService = discountGrpcService;
            _repository = repository;
        }

        [HttpGet("{userName}", Name = "GetBasket")]
        [ProducesResponseType(type: typeof(ShoppingCart), statusCode: (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ShoppingCart>> GetBasket(string userName)
        {
            return Ok(await _repository.GetBasket(userName) ?? new ShoppingCart(userName));
        }

        [HttpPost]
        [ProducesResponseType(type: typeof(ShoppingCart), statusCode: (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ShoppingCart>> UpdateBasket([FromBody] ShoppingCart basket)
        {
            // DONE: Calculate latest prices of product into shopping cart
            foreach (var item in basket.Items)
            {
                var coupon = await _discountGrpcService.GetDiscount(item.ProductName);
                item.Price -= coupon.Amount;
            }

            return Ok(await _repository.UpdateBasket(basket));
        }

        [HttpDelete("{userName}", Name = "DeleteBasket")]
        [ProducesResponseType(type: typeof(void), statusCode: (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteBasket(string userName)
        {
            await _repository.DeleteBasket(userName);
            return Ok();
        }

        [HttpPost]
        [Route("[action]")]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Checkout([FromBody] BasketCheckout basketCheckout)
        {
            // get existing basket with total price
            var basket = await _repository.GetBasket(basketCheckout.UserName);

            if (basket == null) return BadRequest();

            // create basketCheckoutEvent
            var eventMessage = _mapper.Map<BasketCheckoutEvent>(basketCheckout);

            // set total price on basket checkout eventMessage
            eventMessage.TotalPrice = basket.TotalPrice;

            // send checkout event to rabbitmq
            await _publishEndpoint.Publish(eventMessage);

            // remove the basket
            await _repository.DeleteBasket(basket.UserName);

            return Accepted();
        }
    }
}