using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Models.Review;
using Models.User;
using MySqlConnector;
using Service.UserService;
using Services.BorrowedBookService;
using Services.JwtService;
using Services.ReviewService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// add the SQL service
builder.Services.AddMySqlDataSource(
    builder.Configuration.GetConnectionString("DefaultConnection")!
);

// add book service
builder.Services.AddScoped<IBookService, BookService>();

// add user service
builder.Services.AddScoped<IUserService, UserService>();

// add borrowed book service
builder.Services.AddTransient<IBorrowedBookService, BorrowedBookService>();

// add review service
builder.Services.AddTransient<IReviewService, ReviewService>();

// add jwt service
builder.Services.AddTransient<IJwtService, JwtService>();

builder.Services.AddAuthorization();

builder.Services.AddControllers();

// add authentication and authorization using JWT
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    // here we configure what we want to validate for JWT
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});

builder.Services.AddAuthorization(options =>
{
    // add our custom policy for admins and customers
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CustomerOrAdmin", policy => policy.RequireRole("Customer", "Admin"));
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
