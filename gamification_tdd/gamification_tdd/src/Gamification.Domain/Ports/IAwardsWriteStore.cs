using System;
using System.Threading.Tasks;
using Gamification.Domain.Model;

namespace Gamification.Domain.Ports
{
    public interface IAwardsWriteStore
    {
        /// <summary>
        /// Persist badge + xp + log atomically. If this method throws, nothing should be considered persisted.
        /// </summary>
        Task SaveAwardAtomic(Guid studentId, Guid themeId, Guid missionId, Badge badge, XpAmount xp, RewardLog log, string? requestId);
    }
}