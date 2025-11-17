using System.Text;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// CORS setup
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueClient", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // cổng chạy Vite (Vue)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // 👈 thêm dòng này nếu dùng JWT hoặc cookie
    });
});

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//đảm bảo Status trả về là int, không phải enum string để hiện tên danh mục
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IRentalCheckoutService, RentalCheckoutService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<IShippingService, ShippingService>();
builder.Services.AddScoped<IDailyRentalService, DailyRentalService>();
// options
// Program.cs
builder.Services.Configure<Backend.Models.PayOSOptions>(builder.Configuration.GetSection("PayOS"));

// Dùng HttpClient vì service có gọi API PayOS
builder.Services.AddHttpClient<Backend.Services.IPayOSService, Backend.Services.PayOSService>();
// Thêm vào Program.cs
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddAuthentication(opts =>
{
    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opts =>
{
    var cfg = builder.Configuration;
    var secret = cfg["Jwt:Secret"] ?? "";
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

    opts.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidIssuer = cfg["Jwt:Issuer"],
        ValidAudience = cfg["Jwt:Audience"],
        ClockSkew = TimeSpan.Zero // không n?i gi? h?t h?n
    };
});

builder.Services.AddAuthorization();

//builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
//     .AllowAnyHeader().AllowAnyMethod().AllowCredentials()
//     .SetIsOriginAllowed(_ => true)));


var app = builder.Build();
//app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowVueClient"); // Đặt trước Authentication

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Cho phép truy cập file tĩnh (wwwroot/images)
app.UseStaticFiles();

app.Run();
