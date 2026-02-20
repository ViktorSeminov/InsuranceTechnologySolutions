using Claims.Domain.Claims;
using Claims.Domain.Covers;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Claims.Infrastructure.Claims
{
    public class ClaimsContext : DbContext
    {

        public DbSet<Claim> Claims { get; init; }
        public DbSet<Cover> Covers { get; init; }

        public ClaimsContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Claim>().ToCollection("claims");
            modelBuilder.Entity<Cover>().ToCollection("covers");
        }

        //public async Task<IEnumerable<Claim>> GetClaimsAsync()
        //{
        //    return await Claims.ToListAsync();
        //}

        //public async Task<Claim> GetClaimByIdAsync(string id)
        //{
        //    return await Claims
        //        .Where(claim => claim.Id == id)
        //        .SingleOrDefaultAsync();
        //}

        //public async Task AddItemAsync(Claim item)
        //{
        //    Claims.Add(item);
        //    await SaveChangesAsync();
        //}

        //public async Task DeleteItemAsync(string id)
        //{
        //    var claim = await GetClaimByIdAsync(id);
        //    if (claim is not null)
        //    {
        //        Claims.Remove(claim);
        //        await SaveChangesAsync();
        //    }
        //}
    }
}
