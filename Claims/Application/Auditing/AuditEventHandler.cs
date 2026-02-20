using Claims.Application.Common.Events;
using Claims.Domain.Events;

namespace Claims.Application.Auditing
{
    public class AuditEventHandler: IEventHandler<AuditEvent>
    {
        private readonly IAuditer _auditer;

        public AuditEventHandler(IAuditer auditer)
        {
            _auditer = auditer;
        }

        /// <summary>
        /// Processes an audit event by invoking the appropriate auditing operation based on the entity type specified
        /// in the event.
        /// </summary>
        /// <remarks>This method supports audit events for 'Claim' and 'Cover' entity types. The
        /// appropriate auditing method is called depending on the entity type provided in the event. Ensure that the
        /// audit event contains all required information before calling this method.</remarks>
        /// <param name="auditEvent">The audit event to process. Must not be null and should specify a valid entity type and action to audit.</param>
        /// <returns>A task that represents the asynchronous operation of handling the audit event.</returns>
        public async Task HandleAsync(AuditEvent auditEvent)
        {
            switch (auditEvent.EntityType)
            {
                case AuditEntityType.Claim:
                    await _auditer.AuditClaimAsync(
                        auditEvent.EntityId,
                        auditEvent.Action.ToString());
                    break;

                case AuditEntityType.Cover:
                    await _auditer.AuditCoverAsync(
                        auditEvent.EntityId,
                        auditEvent.Action.ToString());
                    break;
            }
        }
    }
}
