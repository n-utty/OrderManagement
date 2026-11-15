using System.Text.Json.Serialization;
using FluentValidation;
using OrderManagement.Api.Configuration;
using OrderManagement.Api.Endpoints;
using OrderManagement.Api.Middleware;
using OrderManagement.Domain.PurchaseOrders;
using OrderManagement.Domain.PurchaseOrders.Models;
using OrderManagement.Domain.PurchaseOrders.Validators;
using OrderManagement.Infrastructure;
using Scalar.AspNetCore;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

var connectionString = SqliteConnectionString.ResolveRelativeTo(
    builder.Configuration.GetConnectionString("OrderManagement")
        ?? throw new InvalidOperationException("Connection string 'OrderManagement' is not configured."),
    builder.Environment.ContentRootPath);

builder.Services.AddInfrastructure(connectionString);

builder.Services.AddScoped<IValidator<CreatePurchaseOrderRequest>, CreatePurchaseOrderRequestValidator>();
builder.Services.AddScoped<IValidator<ChangeOrderStatusRequest>, ChangeOrderStatusRequestValidator>();
builder.Services.AddScoped<PurchaseOrderManager>();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    // Lets the browser client read where a newly created order lives.
    .WithExposedHeaders("Location")));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);

app.MapPurchaseOrderEndpoints();

await app.Services.ApplyMigrationsAsync();

await app.RunAsync();
