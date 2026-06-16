using LeavePortal.Core.Interfaces;
using LeavePortal.Infrastructure.Data;
using LeavePortal.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Azure.Messaging.ServiceBus;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers(); 

// OpenAPI + Swagger UI
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen();

// DbContext — reads connection string from appsettings
builder.Services.AddDbContext<LeavePortalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application services — plain service-layer classes the controllers call directly.
// (Replaced MediatR/CQRS handlers + FluentValidation: simpler to read and step through.)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();

// JWT Service
builder.Services.AddScoped<IJwtService, JwtService>();

// Azure Service Bus — ONE ServiceBusClient for the whole app (heavyweight + thread-safe → singleton).
// Connection string comes from user-secrets (ServiceBus:ConnectionString), never from committed config.
builder.Services.AddSingleton(_ =>
    new ServiceBusClient(builder.Configuration["ServiceBus:ConnectionString"]));

// Real Service Bus publisher (Day 5) — sends each notification message to the queue.
builder.Services.AddScoped<IServiceBusPublisher, ServiceBusPublisher>();

// JWT Authentication — reads token from HttpOnly Cookie
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };

        // Read JWT from HttpOnly Cookie instead of Authorization header
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["jwt"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
