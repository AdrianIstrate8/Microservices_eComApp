using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Discount.API.Entities;
using Discount.API.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Discount.API.Repositories
{
    public class DiscountRepository : IDiscountRepository
    {
        private readonly IConfiguration _config;
        private readonly string ConnectionString;

        public DiscountRepository(IConfiguration config)
        {
            _config = config;
            ConnectionString = _config["DatabaseSettings:ConnectionString"];
        }

        public async Task<Coupon> GetDiscount(string productName)
        {
            using var connection = new NpgsqlConnection(ConnectionString);

            var coupon = await connection.QueryFirstOrDefaultAsync<Coupon>
            (
                sql: @"SELECT * FROM Coupon WHERE ProductName = @ProductName",
                param: new { ProductName = productName }
            );

            if (coupon == null) return new Coupon
            {
                ProductName = "No Discount",
                Description = "No Discount Description",
                Amount = 0
            };

            return coupon;
        }

        public async Task<bool> CreateDiscount(Coupon coupon)
        {
            using var connection = new NpgsqlConnection(ConnectionString);

            var affected = await connection.ExecuteAsync
            (
                sql: @"INSERT INTO Coupon (ProductName, Description, Amount) VALUES (@ProductName, @Description, @Amount)",
                param: new { ProductName = coupon.ProductName, Description = coupon.Description, @Amount = coupon.Amount }
            );

            return affected == 0 ? false : true;
        }

        public async Task<bool> UpdateDiscount(Coupon coupon)
        {
            using var connection = new NpgsqlConnection(ConnectionString);

            var affected = await connection.ExecuteAsync
            (
                sql: @"UPDATE Coupon SET ProductName=@ProductName, Description=@Description, Amount=@Amount WHERE Id = @Id",
                param: new { ProductName = coupon.ProductName, Description = coupon.Description, @Amount = coupon.Amount, @Id = coupon.Id }
            );

            return affected == 0 ? false : true;
        }

        public async Task<bool> DeleteDiscount(string productName)
        {
            using var connection = new NpgsqlConnection(ConnectionString);

            var affected = await connection.ExecuteAsync
            (
                sql: @"DELETE FROM Coupon WHERE ProductName = @ProductName",
                param: new { ProductName = productName }
            );

            return affected == 0 ? false : true;
        }
    }
}