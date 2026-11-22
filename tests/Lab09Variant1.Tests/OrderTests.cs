using Lab09Variant1.Domain;

namespace Lab09Variant1.Tests;

public sealed class OrderTests
{
    [Fact]
    public void Total_WithTwoItems_ReturnsCombinedAmount()
    {
        var order = new Order();
        order.AddItem(new Product("Keyboard", 2499m), 2);
        order.AddItem(new Product("Mouse", 399m), 1);

        Assert.Equal(5397m, order.Total);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_WithInvalidQuantity_ThrowsAndDoesNotChangeOrder(int quantity)
    {
        var order = new Order();
        var product = new Product("Mouse", 399m);

        Assert.Throws<ArgumentOutOfRangeException>(() => order.AddItem(product, quantity));
        Assert.Empty(order.Items);
        Assert.Equal(0m, order.Total);
    }

    [Fact]
    public void Pay_WithItems_ChangesStatusToPaid()
    {
        var order = new Order();
        order.AddItem(new Product("Monitor", 1799m), 1);

        order.Pay();

        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public void Pay_Twice_ThrowsAndKeepsPaidStatus()
    {
        var order = new Order();
        order.AddItem(new Product("Keyboard", 2499m), 1);
        order.Pay();

        Assert.Throws<InvalidOperationException>(() => order.Pay());
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public void OrderPaid_IsRaisedExactlyOnceAfterSuccessfulPayment()
    {
        var order = new Order();
        order.AddItem(new Product("Headphones", 599m), 1);
        var eventCount = 0;
        order.OrderPaid += (_, _) => eventCount++;

        order.Pay();

        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void Pay_WithoutItems_ThrowsAndDoesNotRaiseEvent()
    {
        var order = new Order();
        var eventCount = 0;
        order.OrderPaid += (_, _) => eventCount++;

        Assert.Throws<DomainRuleViolationException>(() => order.Pay());
        Assert.Equal(OrderStatus.Created, order.Status);
        Assert.Equal(0, eventCount);
    }
}
