using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportBot.Core.Entities;
using TransportBot.Core.Interfaces;



namespace TransportBot.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task<User?> GetAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<User?> GetUserByTelegramIdAsync(long telegramId)
        {
            return await _repository.GetByTelegramIdAsync(telegramId);
        }

        public async Task<User> RegisterAsync(long telegramId, string? userName)
        {
            var existing = await _repository.GetByTelegramIdAsync(telegramId);
            if (existing != null) 
            {
                return existing;
            }

            var user = new User
            {
                TelegramId = telegramId,
                UserName = userName,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(user);
            return user;
        }

        public async Task<User> UpdateAsync(int id, string? userName, double? lat, double? lng)
        {
            var user = await _repository.GetByIdAsync(id)
                        ?? throw new Exception("User not found");

            user.UserName = userName ?? user.UserName;
            user.LocationLatitude = lat ?? user.LocationLatitude;
            user.LocationLongitude = lng ?? user.LocationLongitude;

            await _repository.UpdateAsync(user);
            return user;
        }
    }
}
