using System;
using System.Collections.Generic;

namespace Lab06Variant1;

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
    private readonly List<OrderItem> _items = new();
    public IReadOnlyList<OrderItem> Items => _items;
    public OrderStatus Status { get; private set; } = OrderStatus.Created;
    public event EventHandler? OrderPaid;

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

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var order = new Order();
        order.OrderPaid += (_, _) => Console.WriteLine("EVENT: OrderPaid");

        Console.WriteLine("LAB 6. Variant 1 - Internet shop\n");

        Execute("Correct operation: AddItem", () =>
        {
            order.AddItem(new Product("Keyboard", 2499.00m), 2);
            Console.WriteLine("Item added successfully.");
        });

        Execute("Invalid input: empty product name", () => _ = new Product("  ", 100m));
        Execute("Invalid input: negative price", () => _ = new Product("Mouse", -1m));
        Execute("Invalid input: zero quantity", () => order.AddItem(new Product("Monitor", 5000m), 0));

        Execute("Domain rule: pay empty order", () => new Order().Pay());

        Execute("Correct operation: Pay", order.Pay);
        Execute("Invalid state: add item after payment", () =>
            order.AddItem(new Product("USB cable", 199m), 1));
        Execute("Invalid state: repeat payment", order.Pay);

        Console.WriteLine($"\nFinal status: {order.Status}");
        Console.WriteLine($"Items count: {order.Items.Count}");

        Console.Write("\nEnter an integer quantity (TryParse demonstration): ");
        if (int.TryParse(Console.ReadLine(), out var quantity) && quantity > 0)
            Console.WriteLine($"Accepted quantity: {quantity}");
        else
            Console.WriteLine("Error: enter a positive integer.");
    }

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
