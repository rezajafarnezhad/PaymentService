using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PaymentService.MessagingBus.Models;
using RabbitMQ.Client;

namespace PaymentService.MessagingBus;

public interface IMessageBus
{
    Task SendMessage(BaseMessage message, string queueName);
}


public class RabbitMqMessageBus : IMessageBus
{
    private readonly RabbitMqConfiguration _rabbitMqConfiguration;
    private readonly IRabbitMqMessageBusHelper _rabbitMqHelper;
    public RabbitMqMessageBus(IOptions<RabbitMqConfiguration> rabbitMqConfiguration, IRabbitMqMessageBusHelper rabbitMqHelper)
    {
        _rabbitMqHelper = rabbitMqHelper;
        _rabbitMqConfiguration = rabbitMqConfiguration.Value;
    }

    public async Task SendMessage(BaseMessage message, string queueName)
    {
        var connection = await _rabbitMqHelper.CheckCreateRabbitMqConnection(_rabbitMqConfiguration.HostName, _rabbitMqConfiguration.UserName, _rabbitMqConfiguration.Password);

        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: queueName, durable: true,
           exclusive: false, autoDelete: false, arguments: null);

        var body = _rabbitMqHelper.CreateBody(message);
        var prop = channel.CreateBasicProperties();
        prop.Persistent = true;
        channel.BasicPublish(exchange: "", routingKey: queueName, mandatory: false, prop, body);

    }
}


public interface IRabbitMqMessageBusHelper
{
    Task<IConnection> CreateRabbitMqConnection(string hostName, string userName, string password);
    ValueTask<IConnection> CheckCreateRabbitMqConnection(string hostName, string userName, string password);
    byte[] CreateBody(BaseMessage message);
}



public class RabbitMqMessageBusHelper : IRabbitMqMessageBusHelper
{
    private static IConnection _connection;
    public async Task<IConnection> CreateRabbitMqConnection(string hostName, string userName, string password)
    {
        try
        {
            var connectionFactory = new ConnectionFactory()
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
            };

            _connection = connectionFactory.CreateConnection();
            return _connection;
        }
        catch (Exception e)
        {
            Console.WriteLine($"can not Create connection: {e.Message}");
            throw;
        }
    }

    public async ValueTask<IConnection> CheckCreateRabbitMqConnection(string hostName, string userName, string password)
    {
        if (_connection is not null)
            return _connection;

        return await CreateRabbitMqConnection(hostName, userName, password);
    }

    public byte[] CreateBody(BaseMessage message)
    {
        var json = JsonConvert.SerializeObject(message);
        return Encoding.UTF8.GetBytes(json);
    }
}
