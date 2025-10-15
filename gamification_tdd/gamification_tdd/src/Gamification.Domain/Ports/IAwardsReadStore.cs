using System;
using System.Threading.Tasks;

namespace Gamification.Domain.Ports
{
    public interface IAwardsReadStore
    {
        Task<bool> MissaoConcluida(Guid studentId, Guid missionId);
        Task<bool> AlreadyAwarded(Guid studentId, Guid themeId, Guid missionId, string badgeSlug);
        Task<bool> RequestProcessed(string requestId);
        Task<ThemeBonusDates?> GetThemeBonusDates(Guid themeId);
    }
}