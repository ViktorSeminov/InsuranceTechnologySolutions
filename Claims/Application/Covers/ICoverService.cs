using Claims.Domain.Covers;

namespace Claims.Application.Covers
{
    public interface ICoverService
    {
        Task<IEnumerable<Cover>> GetAllCoversAsync();

        Task<Cover?> GetCoverByIdAsync(string id);

        Task<Cover> CreateCoverAsync(Cover cover);

        Task<bool> DeleteCoverAsync(string id);
    }
}
