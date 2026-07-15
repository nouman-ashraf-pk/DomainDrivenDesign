namespace OrderManagement.Domain.Orders;

public enum OrderStatus
{
    Draft = 0,      // being built, items can still change
    Placed = 1,     // customer confirmed it, awaiting payment
    Paid = 2,       // payment captured, ready to ship
    Shipped = 3,    // handed to carrier
    Cancelled = 4,   // terminal state
    ManagerReview = 5
}
