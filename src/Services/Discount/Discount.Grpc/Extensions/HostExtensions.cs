using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Discount.Grpc.Extensions
{
    public static class HostExtensions
    {
        public static IHost MigrateDatabase<T>(this IHost host, int? retry = 0)
        {
            int retryForAvailability = retry.Value;

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var configuration = services.GetRequiredService<IConfiguration>();
                var logger = services.GetRequiredService<ILogger<T>>();

                try
                {
                    logger.LogInformation("Migrating postgresql starting...");

                    var conne = configuration["DatabaseSettings:ConnectionString"];

                    using var connection = new NpgsqlConnection(conne);
                    connection.Open();

                    using var command = new NpgsqlCommand
                    {
                        Connection = connection
                    };

                    command.CommandText = "DROP TABLE IF EXISTS Coupon";
                    command.ExecuteNonQuery();

                    command.CommandText = @"create table Coupon (
                                                ID 			SERIAL 		PRIMARY KEY		NOT NULL,
                                                ProductName VARCHAR(24)	NOT NULL,
                                                Description	TEXT,
                                                Amount		INT)";
                    command.ExecuteNonQuery();

                    command.CommandText = @"insert into coupon (ProductName, description, amount)
                                            values ('IPhone X', 'IPhone Discount', 150)";
                    command.ExecuteNonQuery();

                    command.CommandText = @"insert into coupon (ProductName, description, amount)
                                            values ('Samsung 10', 'Samsung Discount', 100)";
                    command.ExecuteNonQuery();

                    logger.LogInformation("Migration to postgresql FINISHED.");

                }
                catch (NpgsqlException ex)
                {
                    logger.LogError(ex, "An error occured while migration the postgresql database");

                    if (retryForAvailability < 50)
                    {
                        retryForAvailability++;
                        Thread.Sleep(2000);
                        MigrateDatabase<T>(host, retryForAvailability);
                    }
                }
            }

            return host;
        }
    }
}