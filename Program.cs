using ChatAPI.Hubs;
using ChatAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<RoomManager>();
builder.Services.AddEndpointsApiExplorer();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // KHÔNG dùng AllowAnyOrigin() khi có AllowCredentials()
        // Thay vào đó chỉ định cụ thể origin
        policy.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Cho phép gửi credentials
    });
});


var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/chathub");

app.Run();