using DataAccess.Models;
using DataAccess.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public class PaymentService : IPaymentService
    {
        public async Task<bool> ProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));


            await Task.Delay(100); // Simulate network latency

            // Simple validation - reject orders with negative price or quantity
            if (order.Price <= 0 || order.Quantity <= 0)
                return false;

            Console.WriteLine($"Payment processed for Order: {order.OrderId}, Amount: {order.Price * order.Quantity:C}");

            return true;
        }
    }
}
