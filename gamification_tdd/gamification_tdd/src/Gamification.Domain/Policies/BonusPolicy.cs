using System;
using Gamification.Domain.Model;

namespace Gamification.Domain.Policies
{
    public sealed class ThemeBonusDates
    {
        public DateTimeOffset? BonusStartDate { get; init; }
        public DateTimeOffset? BonusFullWeightEndDate { get; init; }
        public DateTimeOffset? BonusFinalDate { get; init; }
    }

    public sealed class BonusDecision
    {
        public XpAmount Xp { get; }
        public string Justification { get; }

        public BonusDecision(XpAmount xp, string justification)
        {
            Xp = xp;
            Justification = justification;
        }
    }

    public static class BonusPolicy
    {
        public static BonusDecision Decide(DateTimeOffset now, ThemeBonusDates dates, int xpBase, int xpFullWeight, int xpReducedWeight)
        {
            if (dates.BonusStartDate != null && dates.BonusFinalDate != null && dates.BonusStartDate > dates.BonusFinalDate)
                throw new ArgumentException("Inconsistent bonus dates: start after final");

            // No bonus config => zero XP
            if (dates.BonusStartDate == null || dates.BonusFullWeightEndDate == null || dates.BonusFinalDate == null)
                return new BonusDecision(XpAmount.Zero, "no-bonus-config");

            if (now <= dates.BonusFullWeightEndDate)
            {
                return new BonusDecision(new XpAmount(xpFullWeight), "full-weight-window");
            }

            if (now > dates.BonusFullWeightEndDate && now <= dates.BonusFinalDate)
            {
                return new BonusDecision(new XpAmount(xpReducedWeight), "reduced-weight-window");
            }

            return new BonusDecision(XpAmount.Zero, "after-final-no-bonus");
        }
    }
}