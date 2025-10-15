using System;

namespace Gamification.Domain.Model
{
    public sealed class RewardLog
    {
        public Guid StudentId { get; }
        public Guid ThemeId { get; }
        public Guid MissionId { get; }
        public string BadgeSlug { get; }
        public XpAmount Xp { get; }
        public DateTimeOffset When { get; }
        public string Source { get; }
        public string Reason { get; }

        public RewardLog(Guid studentId, Guid themeId, Guid missionId, string badgeSlug, XpAmount xp, DateTimeOffset when, string source, string reason)
        {
            StudentId = studentId;
            ThemeId = themeId;
            MissionId = missionId;
            BadgeSlug = badgeSlug;
            Xp = xp;
            When = when;
            Source = source;
            Reason = reason;
        }
    }
}