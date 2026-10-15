namespace SchoolRuleZero
{
    public enum TutorialStep { Opening, Move, Jump, Crouch, Basic, Heavy, Guard,
        Parry, Dodge, Punch, Charge, Ultimate, Complete }
    public enum TutorialSignal { Moved, Jumped, Crouched, BasicHit, HeavyHit, Guarded,
        Parried, Dodged, Punched, FullyCharged, UltimateTierThreeHit }
    public sealed class TutorialProgress
    {
        public TutorialStep Step { get; private set; } = TutorialStep.Opening;
        public void FinishOpening() { if (Step == TutorialStep.Opening) Step = TutorialStep.Move; }
        public bool Accept(TutorialSignal signal)
        {
            if (Step == TutorialStep.Opening || Step == TutorialStep.Complete) return false;
            TutorialSignal expected = (TutorialSignal)((int)Step - 1);
            if (signal != expected) return false;
            Step = (TutorialStep)((int)Step + 1); return true;
        }
    }
}
