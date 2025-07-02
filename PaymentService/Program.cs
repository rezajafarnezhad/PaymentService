using Microsoft.EntityFrameworkCore;
using PaymentService.Infrastructure;
using PaymentService.Jobs;
using PaymentService.MessagingBus;
using PaymentService.MessagingBus.Models;
using PaymentService.Service;

var builder = WebApplication.CreateBuilder(args);
https://tfs.ephoenix.ir/DefaultCollection/IBShop/_backlogs/backlog/IBShop%20Team/Theme
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContextPool<PaymentContext>(op =>
    op.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

builder.Services.AddScoped<IPaymentService, PaymentService.Service.PaymentService>();
builder.Services.AddHostedService<ReceivedOrderForCreatePaymentMessage>();

builder.Services.Configure<RabbitMqConfiguration>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddScoped<IMessageBus, RabbitMqMessageBus>();
builder.Services.AddScoped<IRabbitMqMessageBusHelper, RabbitMqMessageBusHelper>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
