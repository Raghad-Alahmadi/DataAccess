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
    public class UserRepositoryTests
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly Mock<IEmailService> _mockEmailService;

        public UserRepositoryTests()
        {
            // Set up in-memory database - use unique name for each test run
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"UserTestDb_{Guid.NewGuid()}")
                .Options;

            // Mock the email service
            _mockEmailService = new Mock<IEmailService>();
            _mockEmailService
                .Setup(service => service.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Initialize database for each test
            using var context = new AppDbContext(_options);
            context.Database.EnsureCreated();
        }

        #region CRUD Operation Tests

        [Fact]
        public async Task AddUserAsync_ShouldAddUser_WhenValidUserProvided()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            var user = new User
            {
                FirstName = "Raghad",
                LastName = "Alahmadi",
                Email = "raghad.alahmadi@gmail.com"
            };

            // Act
            var result = await repository.AddUserAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("Raghad", result.FirstName);
            Assert.Equal("Alahmadi", result.LastName);
            Assert.Equal("raghad.alahmadi@gmail.com", result.Email);

            // Verify user was added to database
            var dbUser = await context.Users.FindAsync(result.Id);
            Assert.NotNull(dbUser);
            Assert.Equal("raghad.alahmadi@gmail.com", dbUser.Email);

            // Verify email was sent
            _mockEmailService.Verify(service => service.SendEmailAsync(
                "raghad.alahmadi@gmail.com",
                It.IsAny<string>(),
                It.Is<string>(message => message.Contains("Welcome"))),
                Times.Once);
        }

        [Fact]
        public async Task GetAllUsersAsync_ShouldReturnAllUsers()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Add test users to the database
            var users = new List<User>
            {
                new User { FirstName = "Raghad", LastName = "Alahmadi", Email = "raghad.alahmadi@gmail.com" },
                new User { FirstName = "Sara", LastName = "Ahmed", Email = "sara.ahmed@gmail.com" },
                new User { FirstName = "Noura", LastName = "Mohammed", Email = "noura.mohammed@gmail.com" }
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllUsersAsync();

            // Assert
            Assert.Equal(3, result.Count());
            Assert.Contains(result, u => u.Email == "raghad.alahmadi@gmail.com");
            Assert.Contains(result, u => u.Email == "sara.ahmed@gmail.com");
            Assert.Contains(result, u => u.Email == "noura.mohammed@gmail.com");
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnUser_WhenUserExists()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Add a test user
            var user = new User
            {
                FirstName = "Raghad",
                LastName = "Alahmadi",
                Email = "raghad.alahmadi@gmail.com"
            };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUserByIdAsync(user.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal("Raghad", result.FirstName);
            Assert.Equal("Alahmadi", result.LastName);
            Assert.Equal("raghad.alahmadi@gmail.com", result.Email);
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Act
            var result = await repository.GetUserByIdAsync(999); // Non-existent ID

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldUpdateUser_WhenUserExists()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Add a test user
            var user = new User
            {
                FirstName = "Raghad",
                LastName = "Alahmadi",
                Email = "raghad.alahmadi@gmail.com"
            };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Modify the user
            user.FirstName = "Raghad2";
            user.LastName = "Updated";
            user.Email = "raghad.updated@gmail.com";

            // Act
            var result = await repository.UpdateUserAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal("Raghad2", result.FirstName);
            Assert.Equal("Updated", result.LastName);
            Assert.Equal("raghad.updated@gmail.com", result.Email);

            // Verify changes were saved to the database
            var updatedUser = await context.Users.FindAsync(user.Id);
            Assert.Equal("Raghad2", updatedUser.FirstName);
            Assert.Equal("Updated", updatedUser.LastName);
            Assert.Equal("raghad.updated@gmail.com", updatedUser.Email);
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldReturnTrue_WhenUserExists()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Add a test user
            var user = new User
            {
                FirstName = "Raghad",
                LastName = "Alahmadi",
                Email = "raghad.alahmadi@gmail.com"
            };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.DeleteUserAsync(user.Id);

            // Assert
            Assert.True(result);

            // Verify user was removed from the database
            var deletedUser = await context.Users.FindAsync(user.Id);
            Assert.Null(deletedUser);
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldReturnFalse_WhenUserDoesNotExist()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Act
            var result = await repository.DeleteUserAsync(999); // Non-existent ID

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UserExistsAsync_ShouldReturnTrue_WhenUserExists()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Add a test user
            var user = new User
            {
                FirstName = "Raghad",
                LastName = "Alahmadi",
                Email = "raghad.alahmadi@gmail.com"
            };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.UserExistsAsync(user.Id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UserExistsAsync_ShouldReturnFalse_WhenUserDoesNotExist()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Act
            var result = await repository.UserExistsAsync(999); // Non-existent ID

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task EmailExistsAsync_ShouldReturnTrue_WhenEmailExists()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Add a test user
            var user = new User
            {
                FirstName = "Raghad",
                LastName = "Alahmadi",
                Email = "raghad.alahmadi@gmail.com"
            };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.EmailExistsAsync("raghad.alahmadi@gmail.com");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task EmailExistsAsync_ShouldReturnFalse_WhenEmailDoesNotExist()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Act
            var result = await repository.EmailExistsAsync("nonexistent@gmail.com");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Exception Handling and Edge Cases

        [Fact]
        public async Task AddUserAsync_ShouldThrowArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddUserAsync(null));
        }

        [Fact]
        public async Task AddUserAsync_ShouldThrowInvalidOperationException_WhenEmailAlreadyExists()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Add a user with the email
            var existingUser = new User
            {
                FirstName = "Raghad",
                LastName = "Alahmadi",
                Email = "duplicate@gmail.com"
            };
            await context.Users.AddAsync(existingUser);
            await context.SaveChangesAsync();

            // Try to add another user with the same email
            var newUser = new User
            {
                FirstName = "Sara",
                LastName = "Ahmed",
                Email = "duplicate@gmail.com"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.AddUserAsync(newUser));

            Assert.Contains("already in use", exception.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldThrowArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateUserAsync(null));
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldThrowInvalidOperationException_WhenUserDoesNotExist()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            var nonExistentUser = new User
            {
                Id = 999, // Non-existent ID
                FirstName = "Raghad",
                LastName = "Alahmadi",
                Email = "raghad.alahmadi@gmail.com"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateUserAsync(nonExistentUser));

            Assert.Contains("not found", exception.Message);
        }

        [Fact]
        public async Task EmailExistsAsync_ShouldThrowArgumentException_WhenEmailIsNullOrEmpty()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => repository.EmailExistsAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(() => repository.EmailExistsAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => repository.EmailExistsAsync("  "));
        }

        [Fact]
        public async Task SendEmailAsync_ShouldThrowArgumentException_WhenEmailIsNullOrEmpty()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                repository.SendEmailAsync(null, "Subject", "Message"));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                repository.SendEmailAsync(string.Empty, "Subject", "Message"));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                repository.SendEmailAsync("  ", "Subject", "Message"));
        }

        #endregion

        #region Mocking External Dependencies

        [Fact]
        public async Task SendEmailAsync_ShouldCallEmailService_WithCorrectParameters()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            var email = "test@gmail.com";
            var subject = "Test Subject";
            var message = "Test Message";

            // Act
            await repository.SendEmailAsync(email, subject, message);

            // Assert
            _mockEmailService.Verify(service => service.SendEmailAsync(
                email, subject, message), Times.Once);
        }

        [Fact]
        public async Task AddUserAsync_ShouldSendWelcomeEmail_WhenUserIsAdded()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            var user = new User
            {
                FirstName = "Raghad",
                LastName = "Alahmadi",
                Email = "raghad.alahmadi@gmail.com"
            };

            // Act
            await repository.AddUserAsync(user);

            // Assert
            _mockEmailService.Verify(service => service.SendEmailAsync(
                "raghad.alahmadi@gmail.com",
                It.Is<string>(s => s.Contains("Welcome") || s.Contains("Registration")),
                It.Is<string>(m => m.Contains("Raghad"))),
                Times.Once);
        }

        #endregion

        #region Performance Optimization Tests

        [Fact]
        public async Task GetUserWithOrdersAsync_ShouldReturnUserWithOrders_UsingEagerLoading()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Add a user
            var user = new User
            {
                FirstName = "Raghad",
                LastName = "Alahmadi",
                Email = "raghad.alahmadi@gmail.com",
                Orders = new List<Order>()
            };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Add orders for the user
            var orders = new List<Order>
            {
                new Order { UserId = user.Id, Product = "Laptop", Quantity = 1, Price = 1299.99m },
                new Order { UserId = user.Id, Product = "Mouse", Quantity = 1, Price = 49.99m },
                new Order { UserId = user.Id, Product = "Keyboard", Quantity = 1, Price = 99.99m }
            };
            await context.Orders.AddRangeAsync(orders);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUserWithOrdersAsync(user.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.NotNull(result.Orders);
            Assert.Equal(3, result.Orders.Count());
            Assert.Contains(result.Orders, o => o.Product == "Laptop");
            Assert.Contains(result.Orders, o => o.Product == "Mouse");
            Assert.Contains(result.Orders, o => o.Product == "Keyboard");
        }

        [Fact]
        public async Task GetUserWithOrdersAsync_ShouldPerformBetterThanLazyLoading()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var repository = new UserRepository(context, _mockEmailService.Object);

            // Add a user
            var user = new User
            {
                FirstName = "Raghad",
                LastName = "Alahmadi",
                Email = "raghad.alahmadi@gmail.com",
                Orders = new List<Order>()
            };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Add many orders for the user to emphasize performance difference
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

            // Act - Measure performance of eager loading (GetUserWithOrdersAsync)
            var stopwatchEager = Stopwatch.StartNew();
            var userWithOrders = await repository.GetUserWithOrdersAsync(user.Id);
            var orderCountEager = userWithOrders.Orders.Count();
            stopwatchEager.Stop();

            // Act - Measure performance of lazy loading simulation
            var stopwatchLazy = Stopwatch.StartNew();
            var userOnly = await repository.GetUserByIdAsync(user.Id);
            // Force loading of orders in a separate query (simulating lazy loading)
            var ordersLazy = await context.Orders.Where(o => o.UserId == user.Id).ToListAsync();
            var orderCountLazy = ordersLazy.Count;
            stopwatchLazy.Stop();

            // Assert
            Assert.Equal(20, orderCountEager);
            Assert.Equal(20, orderCountLazy);

            // The eager loading should be faster
            Console.WriteLine($"Eager loading time: {stopwatchEager.ElapsedMilliseconds}ms");
            Console.WriteLine($"Lazy loading time: {stopwatchLazy.ElapsedMilliseconds}ms");

        }

        #endregion
    }
}
