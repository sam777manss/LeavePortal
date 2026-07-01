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

// CORS — let the React dev server call this API, and allow cookies through.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")  // React (Vite) dev server address
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();   // REQUIRED — without this the JWT cookie is blocked
    });
});

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

// Blob storage — one reusable client for the app (thread-safe → singleton).
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

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

app.UseDefaultFiles();   // lets "/" serve index.html
app.UseStaticFiles();    // serves the built React files from wwwroot

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");   // must come before UseAuthentication
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Any route that ISN'T an API call falls back to index.html,
// so React Router can handle client-side routes like /dashboard, /manager.
app.MapFallbackToFile("/index.html");

app.Run();
