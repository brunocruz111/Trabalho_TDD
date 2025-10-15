using System;
using System.Threading.Tasks;
using Gamification.Domain.Ports;
using Gamification.Domain.Model;
using Gamification.Domain.Policies;
using Gamification.Domain.Exceptions;

namespace Gamification.Domain.Awards
{
    public sealed class AwardResult
    {
        public XpAmount Xp { get; }
        public RewardLog Log { get; }

        public AwardResult(XpAmount xp, RewardLog log)
        {
            Xp = xp;
            Log = log;
        }
    }

    public sealed class AwardBadgeService
    {
        private readonly IAwardsReadStore _read;
        private readonly IAwardsWriteStore _write;
        private readonly int _xpBase;
        private readonly int _xpFullWeight;
        private readonly int _xpReducedWeight;

        public AwardBadgeService(IAwardsReadStore read, IAwardsWriteStore write, int xpBase = 0, int xpFullWeight = 100, int xpReducedWeight = 50)
        {
            _read = read;
            _write = write;
            _xpBase = xpBase;
            _xpFullWeight = xpFullWeight;
            _xpReducedWeight = xpReducedWeight;
        }

        public async Task<AwardResult> AwardBadge(Guid studentId, Guid themeId, Guid missionId, Badge badge, DateTimeOffset now, string? requestId = null)
        {
            // Eligibility
            var concluded = await _read.MissaoConcluida(studentId, missionId);
            if (!concluded) throw new IneligibleException("Missão não concluída: elegibilidade não satisfeita");

            // Idempotency by requestId
            if (!string.IsNullOrWhiteSpace(requestId))
            {
                var processed = await _read.RequestProcessed(requestId);
                if (processed) throw new AlreadyAwardedException("Request já processado (idempotência por requestId)"); 
            }

            // Uniqueness by natural key
            var already = await _read.AlreadyAwarded(studentId, themeId, missionId, badge.Slug);
            if (already) throw new AlreadyAwardedException("Badge já concedida para este estudante/missão/tema");

            // Bonus policy decision
            var dates = (await _read.GetThemeBonusDates(themeId)) ?? new ThemeBonusDates();
            var decision = BonusPolicy.Decide(now, dates, _xpBase, _xpFullWeight, _xpReducedWeight);

            var xp = decision.Xp;
            var reason = decision.Justification;

            var log = new RewardLog(studentId, themeId, missionId, badge.Slug, xp, now, "mission_completion", reason);

            try
            {
                await _write.SaveAwardAtomic(studentId, themeId, missionId, badge, xp, log, requestId);
            }
            catch (Exception ex)
            {
                throw new AtomicPersistenceException("Falha ao persistir concessão de badge de forma atômica", ex);
            }

            return new AwardResult(xp, log);
        }
    }
}