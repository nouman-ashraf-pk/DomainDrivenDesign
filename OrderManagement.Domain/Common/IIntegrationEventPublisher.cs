using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderManagement.Domain.Common
{
    // Domain.Common
    public interface IIntegrationEventPublisher
    {
        void Publish(IIntegrationEvent integrationEvent); // just stages it — SaveChanges persists it
    }
}
