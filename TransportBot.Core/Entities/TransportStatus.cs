using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportBot.Core.Entities
{
    public class TransportStatus
    {
        public int Id { get; set; }

        public int RouteId { get; set; }
        public Route Route { get; set; }

        public int StopId { get; set; }
        public TransportStop Stop { get; set; }

        public string Status { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
