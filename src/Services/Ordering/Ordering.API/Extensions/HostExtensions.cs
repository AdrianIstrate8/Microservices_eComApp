using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ordering.API.Extensions
{
    public static class HostExtensions
    {
        public static IHost MigrateDatabase<T>(this IHost host, Action<T, IServiceProvider> seeder, int? retry = 0) where T : DbContext
        {
            int retryForAvailability = retry.Value;

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<T>>();
                var context = services.GetService<T>();

                try
                {
                    logger.LogInformation("Migrting database associated with context {DbContextName}", typeof(T).Name);

                    InvokeSeeder(seeder, context, services);

                    logger.LogInformation("Migrting database associated with context {DbContextName}", typeof(T).Name);

                }
                catch (SqlException ex)
                {
                    logger.LogError(ex, "An error occurred while migrating the database used on context {DbContextName}", typeof(T).Name);

                    if (retryForAvailability < 50)
                    {
                        retryForAvailability++;
                        Thread.Sleep(2000);
                        MigrateDatabase<T>(host, seeder, retryForAvailability);
                    }
                }
                return host;
            }
        }

        private static void InvokeSeeder<T>(Action<T, IServiceProvider> seeder, T context, IServiceProvider services) where T : DbContext
        {
            context.Database.Migrate();
            seeder(context, services);
        }
    }
}