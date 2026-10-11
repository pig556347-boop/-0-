using System;
namespace SchoolRuleZero
{
    public sealed class CombatProgress
    {
        public int Combo { get; private set; }
        public float ComboExpires { get; private set; }
        public float Gauge { get; private set; }
        public int UltimateTier => Gauge >= 100 ? 3 : Gauge >= 66 ? 2 : Gauge >= 33 ? 1 : 0;
        public void ConfirmHit(float time, float comboWindow = 1.3f, float gain = 12)
        {
            if (time > ComboExpires) Combo = 0;
            Combo++; ComboExpires = time + comboWindow;
            Gauge = Math.Min(100, Gauge + Math.Max(0, gain));
        }
        public void Tick(float time) { if (time > ComboExpires) Combo = 0; }
        public void BreakCombo() { Combo = 0; ComboExpires = 0; }
        public int ConsumeUltimate()
        {
            int tier = UltimateTier;
            if (tier > 0) Gauge = 0;
            return tier;
        }
    }
}
