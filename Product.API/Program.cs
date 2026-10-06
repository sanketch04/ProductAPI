
using Microsoft.EntityFrameworkCore;
using Product.BLL.Implementation;
using Product.BLL.Interfaces;
using Product.DAL.Context;
using Product.DAL.Implementation;
using Product.DAL.Interfaces;
using System;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

// Add controllers
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SQL Server connection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository dependency injection
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// BLL dependency injection
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductImageService, ProductImageService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IQrService, QrService>();


// CORS for frontend development (if needed)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Content-Disposition");
    });
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    Console.WriteLine(
        $"HTTP START: {context.Request.Method} {context.Request.Path}");

    context.Response.OnStarting(() =>
    {
        Console.WriteLine(
            $"HTTP HEADERS: {context.Request.Path} | Status: {context.Response.StatusCode}");

        return Task.CompletedTask;
    });

    context.Response.OnCompleted(() =>
    {
        Console.WriteLine(
            $"HTTP COMPLETED: {context.Request.Path}");

        return Task.CompletedTask;
    });

    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"HTTP ERROR: {ex}");
        throw;
    }
});
// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Serve product images from wwwroot
app.UseStaticFiles();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
