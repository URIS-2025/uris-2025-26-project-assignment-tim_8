using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousDomain.Models.BillingNotification
{
    public class Payment
    {
        public Guid Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public double Total {  get; set; }

        public Guid SubscriptionId { get; set; }

    }
}
