using System;
namespace SchoolRuleZero
{
    public enum DamageKind { Ignored, Hit, Guarded, Parried }
    public readonly struct DamageResult
    {
        public readonly DamageKind Kind;
        public readonly float Damage;
        public DamageResult(DamageKind kind, float damage) { Kind = kind; Damage = damage; }
    }
    public sealed class CombatHealth
    {
        public float Maximum { get; }
        public float Current { get; private set; }
        public bool Alive => Current > 0;
        public bool Infinite { get; set; }
        public CombatHealth(float maximum)
        {
            if (maximum <= 0 || float.IsNaN(maximum) || float.IsInfinity(maximum))
                throw new ArgumentOutOfRangeException(nameof(maximum));
            Maximum = maximum; Current = maximum;
        }
        public void Reset() => Current = Maximum;
        public void Heal(float amount)
        {
            if (!Alive || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            Current = Math.Min(Maximum, Current + amount);
        }
        public DamageResult Apply(float damage, bool invulnerable = false,
            bool guarding = false, bool parry = false, bool frontal = true,
            bool unblockable = false, float guardMultiplier = .25f)
        {
            if (!Alive || invulnerable || damage <= 0 || float.IsNaN(damage) || float.IsInfinity(damage))
                return new DamageResult(DamageKind.Ignored, 0);
            if (guarding && frontal && !unblockable && parry)
                return new DamageResult(DamageKind.Parried, 0);
            bool blocked = guarding && frontal && !unblockable;
            if (blocked) damage *= Math.Max(0, Math.Min(1, guardMultiplier));
            float applied = Infinite ? 0 : Math.Min(Current, damage);
            Current -= applied;
            return new DamageResult(blocked ? DamageKind.Guarded : DamageKind.Hit, applied);
        }
    }
}
