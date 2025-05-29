using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Services
{
   public static class DefaultAdminUserServiceExtensions
   {
      public static IServiceCollection AddDefaultAdminUser(this IServiceCollection services, string defaultPassword)
      {
         services.AddHostedService<DefaultAdminUserInitializer>(provider =>
            new DefaultAdminUserInitializer(
               provider.GetRequiredService<IServiceScopeFactory>(),
               defaultPassword
            ));
         return services;
      }
   }

   public class DefaultAdminUserInitializer : IHostedService
   {
      private readonly IServiceScopeFactory _scopeFactory;
      private readonly string _defaultPassword;

      public DefaultAdminUserInitializer(IServiceScopeFactory scopeFactory, string defaultPassword)
      {
         _scopeFactory = scopeFactory;
         _defaultPassword = defaultPassword;
      }

      public async Task StartAsync(CancellationToken cancellationToken)
      {
         using var scope = _scopeFactory.CreateScope();
         var context = scope.ServiceProvider.GetRequiredService<UserContext>();

         // Check if an admin user exists
         var adminExists = await context.Users.AnyAsync(u => u.Role == "Admin", cancellationToken);
         if (!adminExists)
         {
            var adminUser = new User
            {
               Username = "admin",
               EMail = "admin@example.com",
               Password = BCrypt.Net.BCrypt.HashPassword(_defaultPassword),
               Role = "Admin"
            };
            context.Users.Add(adminUser);
            await context.SaveChangesAsync(cancellationToken);
         }
      }

      public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
   }
}