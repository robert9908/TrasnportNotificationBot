using Microsoft.EntityFrameworkCore;
using TransportBot.Core.Entities;
using TransportBot.Core.Interfaces;
using TransportBot.Infrastructure.Data;

namespace TransportBot.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly TransportDbContext _context;

        public UserRepository(TransportDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null) 
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByTelegramIdAsync(long telegramId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId);
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
