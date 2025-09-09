using Microsoft.EntityFrameworkCore;
using TransportBot.Core.Entities;
using TransportBot.Core.Interfaces;
using TransportBot.Infrastructure.Data;

namespace TransportBot.Infrastructure.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly TransportDbContext _context;

        public SubscriptionRepository(TransportDbContext context)
        {
            _context = context;
        }

        public async Task<Subscription?> GetByIdAsync(int id)
        {
            return await _context.Subscriptions
                .Include(s => s.User)
                .Include(s => s.Stop)
                .Include(s => s.Route)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Subscription>> GetByUserIdAsync(int userId)
        {
            return await _context.Subscriptions
                .Include(s => s.Stop)
                .Include(s => s.Route)
                .Where(s => s.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subscription>> GetActiveSubscriptionsAsync()
        {
            return await _context.Subscriptions
                .Include(s => s.User)
                .Include(s => s.Stop)
                .Include(s => s.Route)
                .Where(s => s.IsActive)
                .ToListAsync();
        }

        public async Task AddAsync(Subscription subscription)
        {
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Subscription subscription)
        {
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription != null)
            {
                _context.Subscriptions.Remove(subscription);
                await _context.SaveChangesAsync();
            }
        }
    }
}
