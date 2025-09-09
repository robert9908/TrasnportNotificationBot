using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportBot.Core.Entities
{
    public class Route
    {
        public int Id { get; set; }
        public string ExternalId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
