using Claims.Domain.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Application.Claims
{
    public interface IClaimsService
    {
        Task<IEnumerable<Claim>> GetAllClaimsAsync();
        Task<Claim?> GetClaimByIdAsync(string id);
        Task<Claim> CreateClaimAsync(Claim claim);
        Task<bool> DeleteClaimAsync(string id);
    }
}
