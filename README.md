# Data Access Layer with Unit Testing

This project implements a Data Access Layer (DAL) for managing user and order data, along with comprehensive unit tests to ensure functionality, reliability, and performance. The project is built using `.NET 8`, Entity Framework Core, and xUnit for testing.

## Features

- **Data Models**:
  - `User`: Represents a user with properties like `Id`, `FirstName`, `LastName`, and `Email`.
  - `Order`: Represents an order with properties like `OrderId`, `UserId`, `Product`, `Quantity`, and `Price`.

- **Repositories**:
  - `UserRepository`: Handles CRUD operations for users.
  - `OrderRepository`: Handles CRUD operations for orders and integrates with a mocked payment service.

- **Unit Testing**:
  - Comprehensive tests for CRUD operations, exception handling, and edge cases.
  - Mocking of external dependencies (e.g., email service, payment service) using Moq.
  - Performance tests for database interaction (eager vs. lazy loading).

## Technologies Used

- **.NET 8**
- **Entity Framework Core** (In-Memory Database for testing)
- **xUnit** (Unit testing framework)
- **Moq** (Mocking framework for external dependencies)

## Setup Instructions

1. **Clone the Repository**:


2. **Install Dependencies**:
   Ensure you have the following installed:
   - .NET 8 SDK
   - Visual Studio 2022 (or any IDE supporting .NET 8)

3. **Build the Project**:
   Open the solution in Visual Studio and build the project using:
   - Menu: __Build > Build Solution__ or
   - Keyboard shortcut: __Ctrl+Shift+B__

4. **Run Unit Tests**:
   - Open the Test Explorer in Visual Studio:
     - Menu: __Test > Test Explorer__ or
     - Keyboard shortcut: __Ctrl+E, T__
   - Run all tests:
     - Click "Run All" in Test Explorer or use __Ctrl+R, A__

## Usage

### UserRepository
- Add a new user:
  ```
  var user = new User { FirstName = "Raghad", LastName = "Alahmadi", Email = "raghad.alahmadi@gmail.com" }; await userRepository.AddUserAsync(user);

- Get a user by ID:
  ```
  var user = await userRepository.GetUserByIdAsync(userId);

- Update a user:
  ```
  user.FirstName = "UpdatedName"; await userRepository.UpdateUserAsync(user);

- Delete a user:
  ```
  await userRepository.DeleteUserAsync(userId);

### OrderRepository
- Add a new order:
  ```
  var order = new Order { UserId = userId, Product = "Laptop", Quantity = 1, Price = 1200.00m }; await orderRepository.AddOrderAsync(order);

- Get all orders for a user:
  ```
  var orders = await orderRepository.GetOrdersByUserIdAsync(userId);

- Update an order:
  ```
  order.Product = "UpdatedProduct"; await orderRepository.UpdateOrderAsync(order);

- Delete an order:
  ```
  await orderRepository.DeleteOrderAsync(orderId);

## Testing

- **CRUD Operation Tests**:
  - Verify that users and orders can be created, read, updated, and deleted.
- **Mocking External Dependencies**:
  - Mock email service for `UserRepository`.
  - Mock payment service for `OrderRepository`.
- **Exception Handling**:
  - Test for invalid inputs, non-existent records, and other edge cases.
- **Performance Tests**:
  - Compare eager vs. lazy loading for database queries.

## Contributing

1. Fork the repository.
2. Create a new branch for your feature or bug fix.
3. Commit your changes and push the branch.
4. Open a pull request.


