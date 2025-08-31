using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportBot.Core.Entities
{
    public class RouteStop
    {
        public int Id { get; set; }

        public int RouteId { get; set; }
        public Route Route { get; set; }

        public int StopId { get; set; }
        public TransportStop Stop { get; set; }

        public int Sequence { get; set; }
    }
}
