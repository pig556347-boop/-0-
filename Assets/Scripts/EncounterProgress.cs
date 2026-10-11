using System.Collections.Generic;
namespace SchoolRuleZero
{
    public sealed class EncounterProgress
    {
        private readonly HashSet<int> defeated = new HashSet<int>();
        public const int Target = 4;
        public bool Active { get; private set; }
        public int Count => defeated.Count;
        public bool Complete => Count == Target;
        public void Begin() => Active = true;
        public bool RecordDefeat(int opponentId)
        {
            if (!Active || Complete || opponentId < 0 || opponentId >= Target) return false;
            return defeated.Add(opponentId);
        }
    }
    public sealed class TutorialComboLesson
    {
        public int Hits { get; private set; }
        private float expires;
        public void Break() { Hits = 0; expires = 0; }
        public void Tick(float time) { if (time > expires) Hits = 0; }
        public bool ConfirmBasicHit(float time)
        {
            if (time > expires) Hits = 0;
            Hits++; expires = time + 1.3f;
            return Hits >= 3;
        }
    }
}
