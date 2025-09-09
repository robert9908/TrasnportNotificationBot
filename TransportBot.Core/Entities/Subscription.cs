using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportBot.Core.Entities
{
    public class Subscription
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int? StopId { get; set; }
        public TransportStop? Stop { get; set; }

        public int? RouteId { get; set; }
        public Route? Route { get; set; }

        public string? ExternalStopCode { get; set; }
        public string? ExternalRouteNumber { get; set; }

        public int NotifyBeforeMinutes { get; set; }

        public bool IsActive { get; set; }

        public DateTime LastNotifiedAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
