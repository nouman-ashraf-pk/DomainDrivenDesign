using OrderManagement.Domain.Common;
using OrderManagement.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderManagement.Infrastructure.Outbox
{
    public sealed class OutboxIntegrationEventPublisher : IIntegrationEventPublisher
    {
        private readonly AppDbContext _db;
        public OutboxIntegrationEventPublisher(AppDbContext db) => _db = db;

        public void Publish(IIntegrationEvent integrationEvent) =>
            _db.OutboxMessages.Add(new OutboxMessage(integrationEvent));
    }
}
