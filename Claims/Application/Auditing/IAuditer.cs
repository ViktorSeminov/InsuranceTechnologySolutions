namespace Claims.Application.Auditing
{
    public interface IAuditer
    {
        Task AuditClaimAsync(string id, string httpRequestType);
        Task AuditCoverAsync(string id, string httpRequestType);
    }
}
