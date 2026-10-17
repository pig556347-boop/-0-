// Deliberate attacks: a short input buffer never cancels windup or recovery.
public static class PlayerCombatRules
{
    public sealed class Strike
    {
        public readonly float Damage,Windup,Recovery,Reach,Knockback,Gain;
        public Strike(float damage,float windup,float recovery,float reach,float knockback,float gain)
        {Damage=damage;Windup=windup;Recovery=recovery;Reach=reach;Knockback=knockback;Gain=gain;}
    }
    private static readonly Strike[] kicks={new Strike(10,.18f,.28f,1.60f,1.2f,8),new Strike(12,.20f,.30f,1.65f,1.6f,9),new Strike(16,.24f,.46f,1.75f,4.2f,12)};
    private static readonly Strike[] punches={new Strike(7,.13f,.20f,1.30f,.7f,5),new Strike(8,.14f,.22f,1.35f,.9f,6),new Strike(9,.16f,.30f,1.40f,1.8f,7)};
    public static readonly Strike Heavy=new Strike(30,.44f,.70f,1.90f,6,14);
    public static Strike Kick(int index)=>kicks[System.Math.Max(0,System.Math.Min(2,index))];
    public static Strike Punch(int index)=>punches[System.Math.Max(0,System.Math.Min(2,index))];
    public const float HeavyReuse=1.85f,DodgeReuse=.85f,ParryReuse=.42f,InputBuffer=.18f;
    public static float UltimateDamage(int tier,int hit)=>tier==3?(hit==5?24:8):tier==2?(hit==2?20:12):(hit==2?14:8);
}
