namespace Lab09Variant1.Domain;

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
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Product name is required.", nameof(name));
        if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        Name = name.Trim(); Price = price;
    }
}

public sealed class OrderItem
{
    public Product Product { get; }
    public int Quantity { get; }
    public OrderItem(Product product, int quantity)
    {
        Product = product ?? throw new ArgumentNullException(nameof(product));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        Quantity = quantity;
    }
    public decimal Total => Product.Price * Quantity;
}

public enum OrderStatus { Created, Paid }

public sealed class Order
{
    private readonly List<OrderItem> _items = new();
    public IReadOnlyList<OrderItem> Items => _items;
    public OrderStatus Status { get; private set; } = OrderStatus.Created;
    public event EventHandler? OrderPaid;

    public void AddItem(Product product, int quantity)
    {
        if (Status != OrderStatus.Created) throw new InvalidOperationException("Only a created order can be changed.");
        _items.Add(new OrderItem(product, quantity));
    }

    public decimal Total => _items.Sum(item => item.Total);

    public void Pay()
    {
        if (Status != OrderStatus.Created) throw new InvalidOperationException("Order has already been paid.");
        if (_items.Count == 0) throw new DomainRuleViolationException("An order must contain at least one item.");
        Status = OrderStatus.Paid;
        OrderPaid?.Invoke(this, EventArgs.Empty);
    }
}
