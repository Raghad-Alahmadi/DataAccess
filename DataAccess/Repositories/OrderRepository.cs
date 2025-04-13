using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Repositories.Interfaces;
using DataAccess.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;
        private readonly IPaymentService _paymentService;

        public OrderRepository(AppDbContext context, IPaymentService paymentService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders.ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int id)
        {
            return await _context.Orders.FindAsync(id);
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId)
        {
            if (!await _context.Users.AnyAsync(u => u.Id == userId))
                throw new InvalidOperationException($"User with ID {userId} not found.");

            return await _context.Orders
                .Where(o => o.UserId == userId)
                .ToListAsync();
        }

        public async Task<Order> AddOrderAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (!await _context.Users.AnyAsync(u => u.Id == order.UserId))
                throw new InvalidOperationException($"User with ID {order.UserId} not found.");

            // Process payment before adding order
            if (!await ProcessPaymentAsync(order))
                throw new InvalidOperationException("Payment processing failed.");

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order> UpdateOrderAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var existingOrder = await _context.Orders.FindAsync(order.OrderId);
            if (existingOrder == null)
                throw new InvalidOperationException($"Order with ID {order.OrderId} not found.");

            _context.Entry(existingOrder).State = EntityState.Detached;
            _context.Entry(order).State = EntityState.Modified;

            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return false;

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> OrderExistsAsync(int id)
        {
            return await _context.Orders.AnyAsync(o => o.OrderId == id);
        }

        public async Task<bool> ProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            return await _paymentService.ProcessPaymentAsync(order);
        }
    }
}
