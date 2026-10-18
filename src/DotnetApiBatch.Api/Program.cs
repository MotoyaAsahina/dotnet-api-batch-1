using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetApiBatch.Api.Errors;
using DotnetApiBatch.Api.Features.Catalog;
using DotnetApiBatch.Api.Features.Customers;
using DotnetApiBatch.Api.Features.Orders;
using DotnetApiBatch.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)));
// エラーレスポンスを ErrorResponse の形にそろえる
builder.Services.AddErrorHandling();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// 共通設定（DbContext）
builder.Services.AddData(builder.Configuration);
// Catalog
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
// Customers
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
// Orders
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

var app = builder.Build();

app.UseErrorHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
