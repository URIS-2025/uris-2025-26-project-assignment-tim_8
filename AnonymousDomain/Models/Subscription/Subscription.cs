using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousDomain.Models.Subscription
{
    public class Subscription
    {
        public Guid Id { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public Guid SubscriptionPlanId { get; set; }

        public Guid PaymentId { get; set; }


    }
}
