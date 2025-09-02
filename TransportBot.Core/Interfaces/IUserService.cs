using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportBot.Core.Entities;

namespace TransportBot.Core.Interfaces
{
    public interface IUserService
    {
        Task<User> RegisterAsync(long telegramId, string? userName);
        Task<User?> GetAsync(int id);
        Task<User> UpdateAsync(int id, string? userName, double? lat, double? lng);
        Task DeleteAsync(int id);
    }
}
