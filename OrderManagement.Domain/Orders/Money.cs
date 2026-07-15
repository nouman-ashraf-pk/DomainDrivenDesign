using OrderManagement.Domain.Common;

namespace OrderManagement.Domain.Orders;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Of(decimal amount, string currency = "USD")
    {
        if (amount < 0) throw new DomainException("Money amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency)) throw new DomainException("Currency is required.");
        return new Money(amount, currency.ToUpperInvariant());
    }

    public static Money Zero(string currency = "USD") => new(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int factor) => new(Amount * factor, Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot combine {Currency} with {other.Currency}.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
