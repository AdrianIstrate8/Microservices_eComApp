using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Ordering.Application.Contracts.Infrastructure;
using Ordering.Application.Contracts.Persistence;
using Ordering.Application.Models;
using Ordering.Domain.Entities;

namespace Ordering.Application.Features.Orders.Commands.CheckoutOrder
{
    public class CheckoutOrderCommandHandler : IRequestHandler<CheckoutOrderCommand, int>
    {
        private readonly IOrderRepository _repository;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly ILogger<CheckoutOrderCommandHandler> _logger;
        public CheckoutOrderCommandHandler(IOrderRepository repository, IMapper mapper, IEmailService emailService, ILogger<CheckoutOrderCommandHandler> logger)
        {
            _logger = logger;
            _emailService = emailService;
            _mapper = mapper;
            _repository = repository;
        }

        public async Task<int> Handle(CheckoutOrderCommand request, CancellationToken cancellationToken)
        {
            var orderEntity = _mapper.Map<Order>(request);
            var newOrder = await _repository.AddAsync(orderEntity);

            _logger.LogInformation($"Order {newOrder.Id} is successfully created");

            await SendMail(newOrder);

            return newOrder.Id;
        }

        private async Task SendMail(Order newOrder)
        {
            var email = new Email()
            {
                To = "majunior24@gmail.com",
                Body = "Order was created.",
                Subject = "New Order"
            };

            try
            {
                await _emailService.SendEmail(email);
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Order {newOrder.Id} failed due to an error with the email service: {ex.Message}");
            }
        }
    }
}