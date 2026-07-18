using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Common;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Outbox;

// Polls for unprocessed outbox rows and dispatches them through the exact
// same IDomainEventDispatcher the in-process path used to call directly.
// Handlers don't know or care that they're being invoked from here instead
// of from OrderService - IDomainEventHandler<T> is unchanged.
//
// Single-instance note: this uses a plain "read then update" poll, which is
// fine with one instance of the API running. If you ever scale this out to
// multiple instances, two processors could pick up the same row in the same
// poll window and dispatch it twice - at that point add row locking (e.g.
// `WITH (UPDLOCK, READPAST)` on SQL Server, `FOR UPDATE SKIP LOCKED` on
// Postgres) to the query below, or make handlers idempotent, or both.
public sealed class OutboxProcessor : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;
    private const int MaxAttempts = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failure here means the POLL failed (e.g. DB briefly
                // unreachable), not that a specific message failed - that
                // case is handled per-message inside ProcessBatchAsync and
                // must never take down this loop.
                _logger.LogError(ex, "Outbox processor poll failed; will retry next interval.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown - the while condition below ends the loop.
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();

        var batch = await db.OutboxMessages
            .Where(m => m.ProcessedOn == null && m.Attempts < MaxAttempts)
            .OrderBy(m => m.OccurredOn)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (batch.Count == 0) return;

        foreach (var message in batch)
        {
            try
            {
                var domainEvent = message.Deserialize();
                await dispatcher.DispatchAsync(new[] { domainEvent }, ct);
                message.MarkProcessed();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Outbox message {MessageId} ({Type}) failed on attempt {Attempt}",
                    message.Id, message.Type, message.Attempts + 1);
                message.MarkFailed(ex.Message);
                // Left unprocessed on purpose - picked up again next poll,
                // up to MaxAttempts. After that it's effectively dead-lettered:
                // still in the table, still visible via Error/Attempts, but no
                // longer retried automatically. Alerting on Attempts >= MaxAttempts
                // is the next thing worth adding once this is in production.
            }
        }

        // One SaveChanges for the whole batch: each message's ProcessedOn/Error
        // update is independent of the others, so a failure marking one
        // message doesn't roll back the successful ones already marked processed.
        await db.SaveChangesAsync(ct);
    }
}
