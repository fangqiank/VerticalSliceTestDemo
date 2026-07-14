using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using VerticalSliceDemo.Domains;
using VerticalSliceDemo.Features.Orders;
using VerticalSliceDemo.Features.Shipments;
using VerticalSliceDemo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddScoped<OrderPricingService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ShipmentService>();

// Expose order/shipment capabilities to MCP clients (e.g. Claude) over Streamable HTTP at /mcp.
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapOrderEndpoints();
app.MapShipmentEndpoints();
app.MapMcp("mcp");

app.Run();


//让测试项目能够引用和访问应用程序的入口点
public partial class Program { }
