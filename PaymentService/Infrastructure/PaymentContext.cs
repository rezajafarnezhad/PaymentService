using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;

namespace PaymentService.Infrastructure;

public class PaymentContext(DbContextOptions<PaymentContext> options) : DbContext(options)
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<Payment> Payments { get; set; }
}