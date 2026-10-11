using SchoolRuleZero;

// Opening must retain its scripted knockout; protection ends at the final lesson.
public static class TutorialTrainingRules
{
    public static bool HasInfiniteHealth(TutorialStep step) =>
        step != TutorialStep.Opening && step != TutorialStep.Complete;
    public static bool UsesDeongchi(TutorialStep step) =>
        (int)step >= (int)TutorialStep.Basic;
}
