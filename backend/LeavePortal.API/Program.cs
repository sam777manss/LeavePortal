using FluentValidation;
using LeavePortal.Core.Behaviors;
using LeavePortal.Core.Interfaces;
using LeavePortal.Core.Validators.Auth;
using LeavePortal.Infrastructure.Data;
using LeavePortal.Infrastructure.Handlers.Auth;
using LeavePortal.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

// MediatR — scans Infrastructure assembly for all Handlers
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(RegisterCommandHandler).Assembly));

// ValidationBehavior — runs FluentValidation before every Handler automatically
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// FluentValidation — scans Core assembly for all Validators
builder.Services.AddValidatorsFromAssemblyContaining<RegisterCommandValidator>();

// JWT Service
builder.Services.AddScoped<IJwtService, JwtService>();

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
