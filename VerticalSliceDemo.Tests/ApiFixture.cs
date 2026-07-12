using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using VerticalSliceDemo.Domains;
using VerticalSliceDemo.Infrastructure;

namespace VerticalSliceDemo.Tests
{
    public class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _dbContainer;

        public ApiFixture()
        {
            _dbContainer = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("verticalslice_test")
                .WithUsername("test")
                .WithPassword("test")
                .WithCleanUp(true)  // 自动清理
                .Build();
        }

        public override async ValueTask DisposeAsync()
        {
            await _dbContainer.DisposeAsync();
            await base.DisposeAsync();
        }

        // xunit's IAsyncLifetime.DisposeAsync() returns Task; delegate to the real
        // (IAsyncDisposable) disposal so both call sites tear down the container.
        Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

        public async Task ResetAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await db.Shipments.ExecuteDeleteAsync();
            await db.Set<OrderItem>().ExecuteDeleteAsync();
            await db.Orders.ExecuteDeleteAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Database", _dbContainer.GetConnectionString());

            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if(descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseNpgsql(_dbContainer.GetConnectionString());
                });
            });
        }

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
        }
    }
}
