using System;
namespace SchoolRuleZero
{
    public static class SchoolCampaign
    {
        public const int LastStage = 11;
        public static void Validate(int stage) { if (stage < 1 || stage > LastStage) throw new ArgumentOutOfRangeException(nameof(stage)); }
        public static bool IsBoss(int stage) { Validate(stage); return stage == 5 || stage == 10 || stage == 11; }
        public static bool IsBossRound(int stage, int area) => IsBoss(stage) && area == AreaCount(stage);
        public static int AreaCount(int stage) { Validate(stage); return stage <= 4 ? 1 : stage == 11 ? 4 : IsBoss(stage) ? 3 : 2; }
        public static int EnemyCount(int stage, int area)
        {
            Validate(stage);
            if (area < 1 || area > AreaCount(stage)) throw new ArgumentOutOfRangeException(nameof(area));
            return IsBossRound(stage, area) ? 1 : stage <= 2 ? 3 : stage <= 5 ? 4 : stage == 8 || stage == 9 ? 6 : 5;
        }
        public static string BossName(int stage) => stage == 5 ? "도재학" : stage == 10 ? "한승빈" : stage == 11 ? "황빈" : "";
        public static float HP(int stage, bool boss) { Validate(stage); return boss ? stage == 5 ? 240 : stage == 10 ? 300 : 360 : stage <= 4 ? 36 : 48; }
        public static float EnemyHP(int stage, int index) => stage <= 4 ? (index % 2 == 0 ? 24 : 48) : stage >= 6 && stage <= 9 && index == EnemyCount(stage, 1) - 1 ? 84 : index % 2 == 0 ? 36 : 60;
        public static float Damage(int stage, bool boss) => stage <= 4 ? 4 : boss ? stage == 5 ? 10 : stage == 10 ? 12 : 14 : stage >= 6 && stage <= 9 ? 10 : 7;
        public static float ThinkInterval(int stage) => stage <= 4 ? .8f : stage == 11 ? .3f : .55f;
        public static int AttackLimit(int stage) => stage <= 4 ? 1 : 2;
        public static int VisibleLimit(int stage) => stage <= 4 ? 3 : 4;
        public static bool Telegraph(int stage) => stage <= 4;
        public static string AreaName(int stage, int area) => IsBossRound(stage, area) ? BossName(stage) + " · 간부 대결" : "잔몹 라운드 " + area;
    }
    public sealed class WaveProgress
    {
        public int Total { get; }
        public int Spawned { get; private set; }
        public int Defeated { get; private set; }
        public int Alive => Spawned - Defeated;
        public int Remaining => Total - Defeated;
        public bool Complete => Defeated == Total;
        public WaveProgress(int total) { if (total < 1) throw new ArgumentOutOfRangeException(nameof(total)); Total = total; }
        public bool Spawn(int limit) { if (Spawned >= Total || Alive >= limit) return false; Spawned++; return true; }
        public bool Defeat() { if (Alive <= 0) return false; Defeated++; return true; }
    }
}
