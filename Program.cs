using DACN.Data;
using DACN.Models;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using DACN.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Database Configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Hangfire Configuration
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();
// Add services to the container.
builder.Services.AddTransient<HangfireJobService>();


// === BẮT ĐẦU CẤU HÌNH AUTH ===
// 1. Đọc cấu hình JWT từ appsettings.json
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"]; 
// 2. Thêm dịch vụ Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => // 3. Cấu hình JWT
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Nó sẽ check 3 cái này khi valid
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        // Cung cấp thông tin (lấy từ appsettings)
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization(); // Thêm dịch vụ Authorization (nếu chưa có)
// === HẾT CẤU HÌNH AUTH ===


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // đổi Enum thành Chuỗi
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // 1. Thêm định nghĩa "Nút Khóa"
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http, // Dùng Http (chuẩn cho Bearer)
        Scheme = "bearer", // Phải là chữ thường
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Chỉ cần dán Token lấy từ API /login vào đây. Swagger sẽ tự động thêm 'Bearer ' cho bạn."
    });

    // Yêu cầu TẤT CẢ các API phải đính kèm token
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer" // Phải khớp với cái tên "Bearer" ở trên
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});



var app = builder.Build();

// --- BẮT ĐẦU CẤU HÌNH HELPER ---
// Lấy dịch vụ IConfiguration 
var config = app.Services.GetRequiredService<IConfiguration>();
UrlHelper.Initialize(config);
// --- HẾT CẤU HÌNH HELPER ---

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();



//Đăng ký các Job
app.UseHangfireDashboard(); // Bật Dashboard
// Cảnh báo: Cron.Hourly() rất NẶNG!
RecurringJob.AddOrUpdate<HangfireJobService>("bxh-luot-doc",
    job => job.UpdateReadRankings(),
    Cron.Hourly(0)); // Chạy vào phút 00 của mỗi giờ (hoặc Cron.Daily(2) cho an toàn)
RecurringJob.AddOrUpdate<HangfireJobService>("bxh-de-cu",
    job => job.UpdateTicketRankings(),
    Cron.Daily(3, 0)); // 3h sáng (giờ server)
RecurringJob.AddOrUpdate<HangfireJobService>("het-han-vip",
    job => job.CheckVipChapterExpiry(),
    Cron.Daily(1, 0)); // 1h sáng
RecurringJob.AddOrUpdate<HangfireJobService>("phat-ve-hang-tuan",
    job => job.GrantWeeklyTickets(),
    Cron.Weekly(DayOfWeek.Monday, 4, 0)); // 4h sáng Thứ Hai
RecurringJob.AddOrUpdate<HangfireJobService>("bxh-global",
    job => job.UpdateGlobalRankings(),
    Cron.Daily(3, 0)); // 4h sáng

app.MapControllers();
app.Run();
