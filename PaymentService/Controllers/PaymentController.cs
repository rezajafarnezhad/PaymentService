using Microsoft.AspNetCore.Mvc;
using PaymentService.MessagingBus;
using PaymentService.MessagingBus.Models;
using PaymentService.Service;

namespace PaymentService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;
    private readonly IMessageBus _messageBus;
    private static string merchandId;
    public PaymentController(IPaymentService paymentService, IConfiguration configuration, IMessageBus messageBus)
    {
        _paymentService = paymentService;
        _configuration = configuration;
        _messageBus = messageBus;
        merchandId = configuration["ZarinPalMerchandId"];
    }

    [HttpPost("InitPayment")]
    public async Task<IActionResult> InitPayment([FromBody] PaymentRequestModel model)
    {
        var pay = await _paymentService.GetPaymentByOrderId(model.OrderId);
        if (pay is null)
            return NotFound();

        await Task.Delay(5000);

        var order = await _paymentService.PayDone(pay.Id, Guid.NewGuid().ToString(), 34338984298492);

        var message = new PaymentDoneMessage() { OrderId = order, MessageId = Guid.NewGuid(), MessageData = DateTime.Now };

        await _messageBus.SendMessage(message, "PaymentDone");
        return Content(model.CallBack);
    }
}

public class PaymentRequestModel
{
    public Guid OrderId { get; set; }
    public string CallBack { get; set; }
}

public class PaymentDoneMessage : BaseMessage
{
    public Guid OrderId { get; set; }
}