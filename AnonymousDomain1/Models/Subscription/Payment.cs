using AnonymousDomain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousDomain.Models.Subscription
    {
    public class Payment
    {
        public Guid Id { get; set; }
        public double Total {  get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid SubscriptionId { get; set; }
        public PaymentStatus Status { get; set; }
        public Currency Currency { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
    }
}
