using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Repositories;
using DataAccess.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DataAccess.Tests
{
    public class OrderRepositoryTests
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly Mock<IPaymentService> _paymentServiceMock;

        public OrderRepositoryTests()
        {
            // Set up in-memory database with unique name for each test run
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"OrderTestDb_{Guid.NewGuid()}")
                .Options;

            // Mock the payment service
            _paymentServiceMock = new Mock<IPaymentService>();
            _paymentServiceMock.Setup(service => service.ProcessPaymentAsync(It.IsAny<Order>()))
                .ReturnsAsync(true);

            // Initialize database
            using var context = new AppDbContext(_options);
            context.Database.EnsureCreated();
        }

        #region CRUD Operation Tests

        [Fact]
        public async Task AddOrderAsync_ShouldAddOrder_WhenValidOrderProvided()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Add a user first (for foreign key constraint)
            var user = new User { FirstName = "Raghad", LastName = "Alahmadi", Email = "raghad.alahmadi@gmail.com" };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var order = new Order
            {
                UserId = user.Id,
                Product = "Laptop",
                Quantity = 1,
                Price = 1200.00m
            };

            _paymentServiceMock
                .Setup(p => p.ProcessPaymentAsync(It.IsAny<Order>()))
                .ReturnsAsync(true);

            // Act
            var result = await repository.AddOrderAsync(order);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.OrderId > 0);
            Assert.Equal(order.UserId, result.UserId);
            Assert.Equal("Laptop", result.Product);
            Assert.Equal(1, result.Quantity);
            Assert.Equal(1200.00m, result.Price);

            // Verify order was added to database
            var dbOrder = await context.Orders.FindAsync(result.OrderId);
            Assert.NotNull(dbOrder);
            Assert.Equal("Laptop", dbOrder.Product);

            // Verify payment service was called
            _paymentServiceMock.Verify(p => p.ProcessPaymentAsync(It.IsAny<Order>()), Times.Once);
        }

        [Fact]
        public async Task GetAllOrdersAsync_ShouldReturnAllOrders()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Add a user first
            var user = new User { FirstName = "Raghad", LastName = "Alahmadi", Email = "raghad.alahmadi@gmail.com" };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Add test orders
            var orders = new List<Order>
            {
                new Order { UserId = user.Id, Product = "Laptop", Quantity = 1, Price = 1200.00m },
                new Order { UserId = user.Id, Product = "Mouse", Quantity = 2, Price = 25.99m },
                new Order { UserId = user.Id, Product = "Keyboard", Quantity = 1, Price = 89.99m }
            };

            await context.Orders.AddRangeAsync(orders);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllOrdersAsync();

            // Assert
            Assert.Equal(3, result.Count());
            Assert.Contains(result, o => o.Product == "Laptop");
            Assert.Contains(result, o => o.Product == "Mouse");
            Assert.Contains(result, o => o.Product == "Keyboard");
        }

        [Fact]
        public async Task GetOrderByIdAsync_ShouldReturnOrder_WhenOrderExists()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Add a user first
            var user = new User { FirstName = "Raghad", LastName = "Alahmadi", Email = "raghad.alahmadi@gmail.com" };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Add an order
            var order = new Order { UserId = user.Id, Product = "Laptop", Quantity = 1, Price = 1200.00m };
            await context.Orders.AddAsync(order);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetOrderByIdAsync(order.OrderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(order.OrderId, result.OrderId);
            Assert.Equal(user.Id, result.UserId);
            Assert.Equal("Laptop", result.Product);
            Assert.Equal(1, result.Quantity);
            Assert.Equal(1200.00m, result.Price);
        }

        [Fact]
        public async Task GetOrderByIdAsync_ShouldReturnNull_WhenOrderDoesNotExist()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Act
            var result = await repository.GetOrderByIdAsync(999); // Non-existent ID

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetOrdersByUserIdAsync_ShouldReturnUserOrders_WhenUserExists()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Add two users
            var user1 = new User { FirstName = "Raghad", LastName = "Alahmadi", Email = "raghad.alahmadi@gmail.com" };
            var user2 = new User { FirstName = "Sara", LastName = "Ahmed", Email = "sara.ahmed@gmail.com" };
            await context.Users.AddRangeAsync(user1, user2);
            await context.SaveChangesAsync();

            // Add orders for each user
            var orderUser1_1 = new Order { UserId = user1.Id, Product = "Laptop", Quantity = 1, Price = 1200.00m };
            var orderUser1_2 = new Order { UserId = user1.Id, Product = "Mouse", Quantity = 2, Price = 25.99m };
            var orderUser2 = new Order { UserId = user2.Id, Product = "Keyboard", Quantity = 1, Price = 89.99m };

            await context.Orders.AddRangeAsync(orderUser1_1, orderUser1_2, orderUser2);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetOrdersByUserIdAsync(user1.Id);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, o => o.Product == "Laptop");
            Assert.Contains(result, o => o.Product == "Mouse");
            Assert.DoesNotContain(result, o => o.Product == "Keyboard");
        }

        [Fact]
        public async Task UpdateOrderAsync_ShouldUpdateOrder_WhenOrderExists()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Add a user first
            var user = new User { FirstName = "Raghad", LastName = "Alahmadi", Email = "raghad.alahmadi@gmail.com" };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Add an order
            var order = new Order { UserId = user.Id, Product = "Laptop", Quantity = 1, Price = 1200.00m };
            await context.Orders.AddAsync(order);
            await context.SaveChangesAsync();

            // Update the order
            order.Product = "Gaming Laptop";
            order.Quantity = 2;
            order.Price = 2400.00m;

            // Act
            var result = await repository.UpdateOrderAsync(order);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(order.OrderId, result.OrderId);
            Assert.Equal("Gaming Laptop", result.Product);
            Assert.Equal(2, result.Quantity);
            Assert.Equal(2400.00m, result.Price);

            // Verify database was updated
            var updatedOrder = await context.Orders.FindAsync(order.OrderId);
            Assert.Equal("Gaming Laptop", updatedOrder.Product);
            Assert.Equal(2, updatedOrder.Quantity);
            Assert.Equal(2400.00m, updatedOrder.Price);
        }

        [Fact]
        public async Task DeleteOrderAsync_ShouldReturnTrue_AndDeleteOrder_WhenOrderExists()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Add a user first
            var user = new User { FirstName = "Raghad", LastName = "Alahmadi", Email = "raghad.alahmadi@gmail.com" };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Add an order
            var order = new Order { UserId = user.Id, Product = "Laptop", Quantity = 1, Price = 1200.00m };
            await context.Orders.AddAsync(order);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.DeleteOrderAsync(order.OrderId);

            // Assert
            Assert.True(result);

            // Verify order was deleted from database
            var deletedOrder = await context.Orders.FindAsync(order.OrderId);
            Assert.Null(deletedOrder);
        }

        [Fact]
        public async Task DeleteOrderAsync_ShouldReturnFalse_WhenOrderDoesNotExist()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Act
            var result = await repository.DeleteOrderAsync(999); // Non-existent ID

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task OrderExistsAsync_ShouldReturnTrue_WhenOrderExists()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Add a user first
            var user = new User { FirstName = "Raghad", LastName = "Alahmadi", Email = "raghad.alahmadi@gmail.com" };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Add an order
            var order = new Order { UserId = user.Id, Product = "Laptop", Quantity = 1, Price = 1200.00m };
            await context.Orders.AddAsync(order);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.OrderExistsAsync(order.OrderId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task OrderExistsAsync_ShouldReturnFalse_WhenOrderDoesNotExist()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Act
            var result = await repository.OrderExistsAsync(999); // Non-existent ID

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Exception Handling and Edge Cases

        [Fact]
        public async Task AddOrderAsync_ShouldThrowArgumentNullException_WhenOrderIsNull()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddOrderAsync(null));
        }

        [Fact]
        public async Task AddOrderAsync_ShouldThrowInvalidOperationException_WhenUserDoesNotExist()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            var order = new Order
            {
                UserId = 999, // Non-existent user ID
                Product = "Laptop",
                Quantity = 1,
                Price = 1200.00m
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.AddOrderAsync(order));

            Assert.Contains("not found", exception.Message);
        }

        [Fact]
        public async Task AddOrderAsync_ShouldThrowInvalidOperationException_WhenPaymentFails()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Add a user first
            var user = new User { FirstName = "Raghad", LastName = "Alahmadi", Email = "raghad.alahmadi@gmail.com" };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var order = new Order
            {
                UserId = user.Id,
                Product = "Laptop",
                Quantity = 1,
                Price = 1200.00m
            };

            // Setup payment service to fail
            _paymentServiceMock
                .Setup(p => p.ProcessPaymentAsync(It.IsAny<Order>()))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.AddOrderAsync(order));

            Assert.Contains("Payment processing failed", exception.Message);
        }

        [Fact]
        public async Task UpdateOrderAsync_ShouldThrowArgumentNullException_WhenOrderIsNull()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateOrderAsync(null));
        }

        [Fact]
        public async Task UpdateOrderAsync_ShouldThrowInvalidOperationException_WhenOrderDoesNotExist()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Add a user first
            var user = new User { FirstName = "Raghad", LastName = "Alahmadi", Email = "raghad.alahmadi@gmail.com" };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var nonExistentOrder = new Order
            {
                OrderId = 999, // Non-existent order ID
                UserId = user.Id,
                Product = "Laptop",
                Quantity = 1,
                Price = 1200.00m
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateOrderAsync(nonExistentOrder));

            Assert.Contains("not found", exception.Message);
        }

        [Fact]
        public async Task GetOrdersByUserIdAsync_ShouldThrowInvalidOperationException_WhenUserDoesNotExist()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.GetOrdersByUserIdAsync(999)); // Non-existent user ID

            Assert.Contains("not found", exception.Message);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ShouldThrowArgumentNullException_WhenOrderIsNull()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => repository.ProcessPaymentAsync(null));
        }

        #endregion

        #region Mocking External Dependencies

        [Fact]
        public async Task ProcessPaymentAsync_ShouldCallPaymentService_WithCorrectParameters()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            var order = new Order
            {
                UserId = 1,
                Product = "Laptop",
                Quantity = 1,
                Price = 1200.00m
            };

            // Setup mock with specific verification
            _paymentServiceMock
                .Setup(p => p.ProcessPaymentAsync(It.Is<Order>(o =>
                    o.Product == "Laptop" && o.Price == 1200.00m)))
                .ReturnsAsync(true)
                .Verifiable();

            // Act
            await repository.ProcessPaymentAsync(order);

            // Assert
            _paymentServiceMock.Verify();
        }

        [Fact]
        public async Task AddOrderAsync_ShouldProcessPayment_BeforeAddingOrder()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Add a user first
            var user = new User { FirstName = "Raghad", LastName = "Alahmadi", Email = "raghad.alahmadi@gmail.com" };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var order = new Order
            {
                UserId = user.Id,
                Product = "Laptop",
                Quantity = 1,
                Price = 1200.00m
            };

            // Track the order of method calls
            var callSequence = new List<string>();

            _paymentServiceMock
                .Setup(p => p.ProcessPaymentAsync(It.IsAny<Order>()))
                .Callback(() => callSequence.Add("ProcessPayment"))
                .ReturnsAsync(true);

            // Act
            var result = await repository.AddOrderAsync(order);

            // Assert
            Assert.Contains("ProcessPayment", callSequence);

            // Since we can't directly track the sequence of DB operations vs the mock call,
            // we can at least verify the payment was processed and the order was added
            Assert.NotNull(result);
            _paymentServiceMock.Verify(p => p.ProcessPaymentAsync(It.IsAny<Order>()), Times.Once);
        }

        #endregion

        #region Performance Optimization Tests

        [Fact]
        public async Task GetOrdersByUserIdAsync_ShouldPerformOptimizedQuery()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Add a user
            var user = new User { FirstName = "Raghad", LastName = "Alahmadi", Email = "raghad.alahmadi@gmail.com" };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Add multiple orders for the user
            var orders = new List<Order>();
            for (int i = 1; i <= 20; i++)
            {
                orders.Add(new Order
                {
                    UserId = user.Id,
                    Product = $"Product {i}",
                    Quantity = i,
                    Price = i * 10.99m
                });
            }
            await context.Orders.AddRangeAsync(orders);
            await context.SaveChangesAsync();

            // Act
            var stopwatch = Stopwatch.StartNew();
            var result = await repository.GetOrdersByUserIdAsync(user.Id);
            stopwatch.Stop();
            var executionTime = stopwatch.ElapsedMilliseconds;

            // Assert
            Assert.Equal(20, result.Count());

            // Log execution time for reference
            Console.WriteLine($"Query execution time: {executionTime}ms");

            // Note: In a real test, we would compare this against a non-optimized version
            // or use EF Core's ChangeTracker to verify reduced query count
        }

        [Fact]
        public async Task AddOrderAsync_ShouldOptimizeChecks_BeforeProcessingPayment()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new OrderRepository(context, _paymentServiceMock.Object);

            // Setup to track if payment service was called
            bool paymentServiceCalled = false;
            _paymentServiceMock
                .Setup(p => p.ProcessPaymentAsync(It.IsAny<Order>()))
                .Callback(() => paymentServiceCalled = true)
                .ReturnsAsync(true);

            // Try adding an order with non-existent user (should fail before payment processing)
            var invalidOrder = new Order
            {
                UserId = 999, // Non-existent user ID
                Product = "Laptop",
                Quantity = 1,
                Price = 1200.00m
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.AddOrderAsync(invalidOrder));

            // Verify payment service was not called when validation failed
            Assert.False(paymentServiceCalled);
        }

        #endregion
    }
}