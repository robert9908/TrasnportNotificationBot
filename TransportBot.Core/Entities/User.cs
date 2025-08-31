using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportBot.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public long TelegramId { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public double? LocationLatitude { get; set; }
        public double? LocationLongitude { get; set;}
    }
}
