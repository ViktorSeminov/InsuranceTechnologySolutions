namespace Claims.Domain.Events
{
    public enum AuditAction
    {
        POST,
        DELETE
    }

    public enum AuditEntityType
    {
        Claim,
        Cover
    }

    public record AuditEvent(
        AuditEntityType EntityType,
        string EntityId,
        AuditAction Action,
        DateTime OccurredAt
    );
}
