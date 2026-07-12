using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using VerticalSliceDemo.Domains;
using VerticalSliceDemo.Features.Orders;
using VerticalSliceDemo.Features.Shipmemts;
using VerticalSliceDemo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddScoped<OrderPricingService>();

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

app.Run();


//让测试项目能够引用和访问应用程序的入口点
public partial class Program { }
