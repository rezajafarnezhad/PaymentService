using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PaymentService.Domain;
using PaymentService.Infrastructure;
using PaymentService.MessagingBus;
using PaymentService.MessagingBus.Models;
using RabbitMQ.Client.Events;
using System.Text;

namespace PaymentService.Service;

public interface IPaymentService
{
    Task<PaymentModel> GetPaymentByOrderId(Guid orderId);
    Task<PaymentModel> GetPaymentByPaymentId(Guid paymentId);
    Task<bool> CreatePayment(OrderMessageModel model);
    Task<Guid> PayDone(Guid paymentId, string authority, long refId);
    Task ReceivedOrderMessage();
}

public class PaymentService : IPaymentService
{
    private readonly PaymentContext _context;
    private readonly IRabbitMqMessageBusHelper _messageBusHelper;
    private readonly RabbitMqConfiguration _rabbitMqConfiguration;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    public PaymentService(PaymentContext context, IRabbitMqMessageBusHelper messageBusHelper, IOptions<RabbitMqConfiguration> rabbitMqConfiguration, IServiceScopeFactory serviceScopeFactory)
    {
        _context = context;
        _messageBusHelper = messageBusHelper;
        _serviceScopeFactory = serviceScopeFactory;
        _rabbitMqConfiguration = rabbitMqConfiguration.Value;
    }

    public async Task<PaymentModel> GetPaymentByOrderId(Guid orderId)
    {
        var payment = await _context.Payments.AsNoTracking().Include(c => c.Order)
            .SingleOrDefaultAsync(c => c.OrderId == orderId);
        return payment.Adapt(new PaymentModel());

    }

    public async Task<PaymentModel> GetPaymentByPaymentId(Guid paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        return payment.Adapt(new PaymentModel());
    }

    public async Task<bool> CreatePayment(OrderMessageModel model)
    {
        var payment = await _context.Payments.Include(c => c.Order)
            .SingleOrDefaultAsync(c => c.OrderId == model.OrderId);

        if (payment == null)
        {
            var createPayment = new Payment(model.Amount, model.OrderId, new Order(model.OrderId, model.Amount));
            _context.Payments.Add(createPayment);
            await _context.SaveChangesAsync();
            return true;
        }

        if (payment.Order.Amount != model.Amount)
        {
            payment.Order.Amount = model.Amount;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<Guid> PayDone(Guid paymentId, string authority, long refId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        payment.PaymentDone(refId, authority);
        await _context.SaveChangesAsync();
        return payment.OrderId;
    }

    public async Task ReceivedOrderMessage()
    {
        var connection = await _messageBusHelper.CreateRabbitMqConnection(_rabbitMqConfiguration.HostName,
            _rabbitMqConfiguration.UserName, _rabbitMqConfiguration.Password);

        var channel = connection.CreateModel();
        channel.QueueDeclare(_rabbitMqConfiguration.QueueName, true, false, false, null);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (sender, args) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(args.Body.ToArray());
                var message = JsonConvert.DeserializeObject<OrderMessageModel>(body);
                using var scope = _serviceScopeFactory.CreateScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                var result = await orderService.CreatePayment(message);
                if (result)
                {
                    channel.BasicAck(deliveryTag: args.DeliveryTag, multiple: false);
                }
                else
                {
                    channel.BasicNack(args.DeliveryTag, false, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }

        };
        channel.BasicConsume(
            queue: _rabbitMqConfiguration.QueueName,
            autoAck: false,
            consumerTag: string.Empty,
            noLocal: false,
            exclusive: false,
            arguments: null,
            consumer: consumer
        );
    }


}

public class OrderMessageModel
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public Guid MessageId { get; set; }
    public DateTime MessageData { get; set; }
}
public class PaymentModel
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime PayDate { get; set; }
    public string Authority { get; set; }
    public long RefId { get; set; }
    public Guid OrderId { get; set; }
}