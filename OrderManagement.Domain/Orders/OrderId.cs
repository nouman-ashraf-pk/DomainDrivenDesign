using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Orders;

public sealed class OrderId : ValueObject
{
    public Guid Value { get; }

    private OrderId(Guid value) => Value = value;

    public static OrderId New() => new(Guid.NewGuid());
    public static OrderId Of(Guid value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
