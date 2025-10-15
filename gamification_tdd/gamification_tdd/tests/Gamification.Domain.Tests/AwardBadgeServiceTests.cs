using System;
using System.Threading.Tasks;
using Xunit;
using Gamification.Domain.Awards;
using Gamification.Domain.Model;
using Gamification.Domain.Ports;
using Gamification.Domain.Exceptions;
using Gamification.Domain.Policies;

namespace Gamification.Domain.Tests
{
    // Simple in-memory fakes for testing the domain logic
    class InMemoryReadStore : IAwardsReadStore
    {
        public bool Concluded { get; set; }
        public bool Already { get; set; }
        public bool RequestProcessedFlag { get; set; }
        public ThemeBonusDates? ThemeDates { get; set; }

        public Task<bool> MissaoConcluida(Guid studentId, Guid missionId) => Task.FromResult(Concluded);
        public Task<bool> AlreadyAwarded(Guid studentId, Guid themeId, Guid missionId, string badgeSlug) => Task.FromResult(Already);
        public Task<bool> RequestProcessed(string requestId) => Task.FromResult(RequestProcessedFlag);
        public Task<ThemeBonusDates?> GetThemeBonusDates(Guid themeId) => Task.FromResult(ThemeDates);
    }

    class InMemoryWriteStore : IAwardsWriteStore
    {
        public bool ThrowOnSave { get; set; }
        public bool Saved { get; private set; }
        public Guid LastStudent { get; private set; }
        public string? LastRequestId { get; private set; }

        public Task SaveAwardAtomic(Guid studentId, Guid themeId, Guid missionId, Badge badge, XpAmount xp, Gamification.Domain.Model.RewardLog log, string? requestId)
        {
            if (ThrowOnSave) throw new InvalidOperationException("Simulated storage failure");
            Saved = true;
            LastStudent = studentId;
            LastRequestId = requestId;
            return Task.CompletedTask;
        }
    }

    public class AwardBadgeServiceTests
    {
        [Fact]
        public async Task ConcederBadge_quando_missao_concluida_concede_uma_unica_vez()
        {
            var read = new InMemoryReadStore { Concluded = true, Already = false };
            var write = new InMemoryWriteStore();
            var svc = new AwardBadgeService(read, write);

            var student = Guid.NewGuid();
            var theme = Guid.NewGuid();
            var mission = Guid.NewGuid();
            var badge = new Badge("mission-1", "Mission 1 Badge");

            var result = await svc.AwardBadge(student, theme, mission, badge, DateTimeOffset.UtcNow);

            Assert.True(write.Saved);
            Assert.Equal(result.Log.BadgeSlug, badge.Slug);
        }

        [Fact]
        public async Task RepetirConcessao_da_mesma_badge_para_mesmo_estudante_deve_ser_idempotente()
        {
            var read = new InMemoryReadStore { Concluded = true, Already = true };
            var write = new InMemoryWriteStore();
            var svc = new AwardBadgeService(read, write);

            var ex = await Assert.ThrowsAsync<AlreadyAwardedException>(async () =>
            {
                await svc.AwardBadge(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Badge("s", "t"), DateTimeOffset.UtcNow);
            });

            Assert.Contains("Badge já concedida", ex.Message);
        }

        [Fact]
        public async Task ConcederBadge_sem_concluir_missao_deve_falhar()
        {
            var read = new InMemoryReadStore { Concluded = false };
            var write = new InMemoryWriteStore();
            var svc = new AwardBadgeService(read, write);

            await Assert.ThrowsAsync<IneligibleException>(async () =>
            {
                await svc.AwardBadge(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Badge("s", "t"), DateTimeOffset.UtcNow);
            });
        }

        [Fact]
        public async Task ConcederBadge_ate_bonusFullWeightEndDate_concede_bonus_integral()
        {
            var now = DateTimeOffset.UtcNow;
            var dates = new ThemeBonusDates
            {
                BonusStartDate = now.AddDays(-5),
                BonusFullWeightEndDate = now.AddDays(1),
                BonusFinalDate = now.AddDays(10)
            };
            var read = new InMemoryReadStore { Concluded = true, Already = false, ThemeDates = dates };
            var write = new InMemoryWriteStore();
            var svc = new AwardBadgeService(read, write, xpBase:0, xpFullWeight:200, xpReducedWeight:100);

            var res = await svc.AwardBadge(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Badge("s","t"), now);

            Assert.Equal(200, res.Xp.Value);
            Assert.Equal("full-weight-window", res.Log.Reason);
        }

        [Fact]
        public async Task ConcederBadge_entre_fullWeight_e_finalDate_concede_bonus_reduzido()
        {
            var now = DateTimeOffset.UtcNow;
            var dates = new ThemeBonusDates
            {
                BonusStartDate = now.AddDays(-10),
                BonusFullWeightEndDate = now.AddDays(-2),
                BonusFinalDate = now.AddDays(2)
            };
            var read = new InMemoryReadStore { Concluded = true, Already = false, ThemeDates = dates };
            var write = new InMemoryWriteStore();
            var svc = new AwardBadgeService(read, write, xpBase:0, xpFullWeight:200, xpReducedWeight:120);

            var res = await svc.AwardBadge(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Badge("s","t"), now);

            Assert.Equal(120, res.Xp.Value);
            Assert.Equal("reduced-weight-window", res.Log.Reason);
        }

        [Fact]
        public async Task ConcederBadge_apos_bonusFinalDate_nao_concede_bonus_mas_concede_badge()
        {
            var now = DateTimeOffset.UtcNow;
            var dates = new ThemeBonusDates
            {
                BonusStartDate = now.AddDays(-20),
                BonusFullWeightEndDate = now.AddDays(-10),
                BonusFinalDate = now.AddDays(-1)
            };
            var read = new InMemoryReadStore { Concluded = true, Already = false, ThemeDates = dates };
            var write = new InMemoryWriteStore();
            var svc = new AwardBadgeService(read, write, xpBase:0, xpFullWeight:200, xpReducedWeight:120);

            var res = await svc.AwardBadge(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Badge("s","t"), now);

            Assert.Equal(0, res.Xp.Value);
            Assert.Equal("after-final-no-bonus", res.Log.Reason);
            Assert.True(write.Saved);
        }

        [Fact]
        public async Task ConcederBadge_falha_na_gravacao_nao_deve_gerar_efeitos_parciais()
        {
            var read = new InMemoryReadStore { Concluded = true, Already = false };
            var write = new InMemoryWriteStore { ThrowOnSave = true };
            var svc = new AwardBadgeService(read, write);

            await Assert.ThrowsAsync<DomainException>(async () =>
            {
                await svc.AwardBadge(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Badge("s","t"), DateTimeOffset.UtcNow);
            });

            Assert.False(write.Saved);
        }

        [Fact]
        public async Task ConcederBadge_requisicao_repetida_com_requestId_igual_nao_duplica()
        {
            var read = new InMemoryReadStore { Concluded = true, Already = false, RequestProcessedFlag = true };
            var write = new InMemoryWriteStore();
            var svc = new AwardBadgeService(read, write);

            await Assert.ThrowsAsync<AlreadyAwardedException>(async () =>
            {
                await svc.AwardBadge(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Badge("s","t"), DateTimeOffset.UtcNow, requestId: "req-1");
            });
            Assert.False(write.Saved);
        }
    }
}