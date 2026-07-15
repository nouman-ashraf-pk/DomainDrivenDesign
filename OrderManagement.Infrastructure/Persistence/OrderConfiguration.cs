using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Infrastructure.Persistence;

// This class is the "seam" where a rich domain model meets a relational
// database. It's normal for it to look more awkward than the domain code —
// that's the price of keeping Order.cs free of [Column]/[Table] attributes
// and EF-specific compromises.
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        // Strongly-typed id: tell EF how to convert OrderId <-> Guid.
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasConversion(id => id.Value, value => OrderId.Of(value))
            .ValueGeneratedNever();

        builder.Property(o => o.CustomerId).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().IsRequired();
        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.CancellationReason).HasMaxLength(500);

        // Address value object: stored as columns on the Orders table.
        builder.OwnsOne(o => o.ShippingAddress, a =>
        {
            a.Property(p => p.Street).HasColumnName("ShippingStreet").HasMaxLength(200).IsRequired();
            a.Property(p => p.City).HasColumnName("ShippingCity").HasMaxLength(100).IsRequired();
            a.Property(p => p.PostalCode).HasColumnName("ShippingPostalCode").HasMaxLength(20).IsRequired();
            a.Property(p => p.Country).HasColumnName("ShippingCountry").HasMaxLength(100).IsRequired();
        });

        // OrderItem: owned collection in its own table, keyed by (OrderId, Id).
        // Access via the private backing field so EF doesn't need a public setter.
        builder.OwnsMany<OrderItem>(o => o.Items, items =>
        {
            items.ToTable("OrderItems");
            items.WithOwner().HasForeignKey("OrderId");
            items.HasKey(i => i.Id);

            items.Property(i => i.ProductId).IsRequired();
            items.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
            items.Property(i => i.Quantity).IsRequired();

            items.OwnsOne(i => i.UnitPrice, m =>
            {
                m.Property(p => p.Amount).HasColumnName("UnitPriceAmount").HasColumnType("decimal(18,2)");
                m.Property(p => p.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(3);
            });
        });

        builder.Metadata.FindNavigation(nameof(Order.Items))?.SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
        builder.Ignore(o => o.DomainEvents);
        builder.Ignore(o => o.Total);
    }
}
