using MemmoApi.Data;
using MemmoApi.Middleware;
using Microsoft.EntityFrameworkCore;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlServer(connectionString, builder =>
{
    builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
})
);
builder.Services.AddControllers();
var secretKey = builder.Configuration["secretKey"];
var issuer = builder.Configuration["issuer"];
var audience = builder.Configuration["audience"];
JwtAuthentication.Configure(builder.Services, secretKey,issuer,audience);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyCorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;

            return origin.EndsWith(".vercel.app") ||
                   origin.Contains("napatsai.com") ||
                   origin.Contains("github.dev") ||
                   origin.Contains("localhost");
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials(); // ต้องมีตัวนี้เพราะคุณใช้ JWT/Auth
    });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserService, UserService>();
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();

app.UseCors("MyCorsPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();
