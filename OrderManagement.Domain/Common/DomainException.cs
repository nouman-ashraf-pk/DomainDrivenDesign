namespace OrderManagement.Domain.Common;

// Thrown when code tries to push an aggregate into an invalid state.
// This is different from a validation error (bad input shape) — it's a
// business-rule violation, e.g. "you can't ship an unpaid order."
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
