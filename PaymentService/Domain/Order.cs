namespace PaymentService.Domain;

public class Order
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public List<Payment> Payments { get; set; }

    public Order(Guid orderId, decimal amount)
    {
        OrderId = orderId;
        Amount = amount;
    }

    public Order()
    {

    }

}


public class Payment
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime PayDate { get; set; }
    public string Authority { get; set; }
    public long RefId { get; set; }
    public Guid OrderId { get; set; }

    public Payment(decimal amount, Guid orderId, Order order)
    {
        Amount = amount;
        OrderId = orderId;
        Order = order;
    }

    public void PaymentDone(long refId, string authority)
    {
        PayDate = DateTime.Now;
        RefId = refId;
        Authority = authority;
    }

    public Order Order { get; set; }

    public Payment()
    {

    }
}