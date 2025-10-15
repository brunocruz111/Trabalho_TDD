using System;
using Xunit;
using Gamification.Domain.Policies;
using Gamification.Domain.Model;

namespace Gamification.Domain.Tests
{
    public class BonusPolicyTests
    {
        [Fact]
        public void Decide_returns_full_weight_before_or_on_full_end_date()
        {
            var now = DateTimeOffset.UtcNow;
            var dates = new ThemeBonusDates
            {
                BonusStartDate = now.AddDays(-2),
                BonusFullWeightEndDate = now.AddDays(1),
                BonusFinalDate = now.AddDays(5)
            };

            var decision = BonusPolicy.Decide(now, dates, 0, 150, 75);
            Assert.Equal(150, decision.Xp.Value);
            Assert.Equal("full-weight-window", decision.Justification);
        }

        [Fact]
        public void Decide_reduced_between_full_and_final()
        {
            var now = DateTimeOffset.UtcNow.AddDays(2);
            var dates = new ThemeBonusDates
            {
                BonusStartDate = now.AddDays(-10),
                BonusFullWeightEndDate = now.AddDays(-5),
                BonusFinalDate = now.AddDays(5)
            };

            var decision = BonusPolicy.Decide(now, dates, 0, 150, 60);
            Assert.Equal(60, decision.Xp.Value);
            Assert.Equal("reduced-weight-window", decision.Justification);
        }

        [Fact]
        public void Decide_no_bonus_after_final()
        {
            var now = DateTimeOffset.UtcNow.AddDays(10);
            var dates = new ThemeBonusDates
            {
                BonusStartDate = now.AddDays(-20),
                BonusFullWeightEndDate = now.AddDays(-15),
                BonusFinalDate = now.AddDays(-1)
            };

            var decision = BonusPolicy.Decide(now, dates, 0, 150, 60);
            Assert.Equal(0, decision.Xp.Value);
            Assert.Equal("after-final-no-bonus", decision.Justification);
        }
    }
}