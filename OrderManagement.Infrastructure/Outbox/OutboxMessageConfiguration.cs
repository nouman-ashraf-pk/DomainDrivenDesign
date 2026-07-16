using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OrderManagement.Infrastructure.Outbox;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasMaxLength(500).IsRequired();
        builder.Property(m => m.Payload).IsRequired(); // nvarchar(max) by default
        builder.Property(m => m.OccurredOn).IsRequired();
        builder.Property(m => m.Error).HasMaxLength(2000);

        // The one query the processor actually runs: "unprocessed rows, oldest first."
        builder.HasIndex(m => new { m.ProcessedOn, m.OccurredOn });
    }
}
