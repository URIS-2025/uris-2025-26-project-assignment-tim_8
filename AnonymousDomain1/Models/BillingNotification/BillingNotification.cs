using AnonymousDomain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnonymousDomain.Models.BillingNotification
{
    public class BillingNotification
    {
        public Guid Id { get; set; }

        public string Text { get; set; }

        public bool IsRead {  get; set; }

        public Guid OrganizationId { get; set; }

        public Guid PaymentId { get; set; }

        public TypeSubject Type {  get; set; }
        
    }
}
