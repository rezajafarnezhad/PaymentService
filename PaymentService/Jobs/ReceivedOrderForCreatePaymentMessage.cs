using PaymentService.Service;

namespace PaymentService.Jobs
{
    public class ReceivedOrderForCreatePaymentMessage : BackgroundService
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        public ReceivedOrderForCreatePaymentMessage(IServiceScopeFactory serviceScopeFactory)
        {
            this.serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
            while (
                !stoppingToken.IsCancellationRequested &&
                await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = serviceScopeFactory.CreateScope();
                var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                await paymentService.ReceivedOrderMessage();
            }
        }
    }
}
