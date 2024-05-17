using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Huna.Notifications.Contracts
{
    public class NotificationRequest
    {
        public string? ToUserEmail {get;set;}
        public string? Title {get;set;}
        public string? Message {get;set;}
        public string? IssuedAt {get;set;}
        public string? Severity {get;set;}
        public string[]? DeliveryTypes {get;set;}
    }
}