using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Lab07Variant1;

public sealed class DomainRuleViolationException : Exception
{
    public DomainRuleViolationException(string message) : base(message) { }
}

public sealed class Product
{
    public string Name { get; }
    public decimal Price { get; }

    public Product(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty.", nameof(name));
        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");

        Name = name.Trim();
        Price = price;
    }
}

public sealed class OrderItem
{
    public Product Product { get; }
    public int Quantity { get; }

    public OrderItem(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        Product = product;
        Quantity = quantity;
    }
}

public enum OrderStatus { Created, Paid, Cancelled }

public sealed class Order
{
    public Guid Id { get; } = Guid.NewGuid();
    public string CustomerName { get; }
    private readonly List<OrderItem> _items = new();
    public IReadOnlyList<OrderItem> Items => _items;
    public OrderStatus Status { get; private set; } = OrderStatus.Created;
    public event EventHandler? OrderPaid;

    public Order(string customerName)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Customer name cannot be empty.", nameof(customerName));
        CustomerName = customerName.Trim();
    }

    public void AddItem(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        EnsureCreated("Items can be changed only in a created order.");
        _items.Add(new OrderItem(product, quantity));
    }

    public void Cancel()
    {
        EnsureCreated("Only a created order can be cancelled.");
        Status = OrderStatus.Cancelled;
    }

    public void Pay()
    {
        EnsureCreated("Only a created order can be paid.");
        if (_items.Count == 0)
            throw new DomainRuleViolationException("An order must contain at least one item.");

        Status = OrderStatus.Paid;
        OrderPaid?.Invoke(this, EventArgs.Empty);
    }

    private void EnsureCreated(string message)
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException(message);
    }
}

public sealed record OrderReport(
    Guid OrderId,
    string CustomerName,
    decimal Total,
    string Status,
    int ItemsCount);

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("LAB 7. Variant 1 - JSON, files and configuration\n");

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var relativePath = configuration["Storage:FilePath"]
                ?? throw new InvalidOperationException("Storage:FilePath is not configured.");
            var writeIndented = bool.TryParse(configuration["Storage:WriteIndented"], out var indented) && indented;
            var filePath = Path.Combine(AppContext.BaseDirectory, relativePath);

            var orders = CreateOrders();
            var reports = orders.Select(ToReport).ToList();
            var options = new JsonSerializerOptions { WriteIndented = writeIndented };
            var json = JsonSerializer.Serialize(reports, options);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(filePath, json);
            Console.WriteLine($"JSON saved to: {filePath}");

            var savedJson = File.ReadAllText(filePath);
            var restored = JsonSerializer.Deserialize<List<OrderReport>>(savedJson)
                ?? throw new JsonException("The JSON file contains no report records.");

            Console.WriteLine("\nImported orders:");
            foreach (var report in restored)
                Console.WriteLine($"{report.CustomerName}: {report.Total:C} | {report.Status} | items: {report.ItemsCount}");
            Console.WriteLine($"\nOrders count: {restored.Count}");
            Console.WriteLine($"Total amount: {restored.Sum(x => x.Total):C}");
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Error: the configuration or report file was not found.");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON error: {ex.Message}");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Error: access to the file was denied.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"File error: {ex.Message}");
        }
    }

    private static List<Order> CreateOrders()
    {
        var data = new[]
        {
            ("Anna Kowalska", "Keyboard", 2499m, 2),
            ("Piotr Nowak", "Mouse", 399m, 1),
            ("Oleh Bondarenko", "Monitor", 1799m, 2),
            ("Maria Zielinska", "Headphones", 599m, 3),
            ("Jan Wozniak", "USB cable", 99m, 4)
        };
        var orders = new List<Order>();
        foreach (var (customer, productName, price, quantity) in data)
        {
            var order = new Order(customer);
            order.AddItem(new Product(productName, price), quantity);
            order.Pay();
            orders.Add(order);
        }
        return orders;
    }

    private static OrderReport ToReport(Order order) => new(
        order.Id,
        order.CustomerName,
        order.Items.Sum(item => item.Product.Price * item.Quantity),
        order.Status.ToString(),
        order.Items.Sum(item => item.Quantity));

    private static void Execute(string title, Action operation)
    {
        Console.WriteLine($"\n{title}");
        try
        {
            operation();
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Argument error: {ex.Message}");
        }
        catch (DomainRuleViolationException ex)
        {
            Console.WriteLine($"Domain error: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"State error: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("Operation finished.");
        }
    }
}
