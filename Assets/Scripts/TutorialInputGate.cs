using SchoolRuleZero;
using UnityEngine;

// Per-player input policy; AI, pause and free combat are unaffected.
public sealed class TutorialInputGate : MonoBehaviour
{
    public TutorialDirector Director;
    public bool Allows(KeyCode key) => Director==null || Allows(Director.Progress.Step,key);
    public static bool Allows(TutorialStep step,KeyCode key)
    {
        if(key==KeyCode.Escape||key==KeyCode.R)return true;
        if(step==TutorialStep.Complete)return true;
        bool move=key==KeyCode.W||key==KeyCode.A||key==KeyCode.S||key==KeyCode.D;
        switch(step)
        {
            case TutorialStep.Move:return move;
            case TutorialStep.Jump:return key==KeyCode.Space;
            case TutorialStep.Crouch:return key==KeyCode.C;
            case TutorialStep.Basic:return move||key==KeyCode.J;
            case TutorialStep.Heavy:return move||key==KeyCode.K;
            case TutorialStep.Guard:case TutorialStep.Parry:return key==KeyCode.L;
            case TutorialStep.Dodge:return move||key==KeyCode.LeftShift||key==KeyCode.RightShift;
            case TutorialStep.Grab:return move||key==KeyCode.I;
            case TutorialStep.Charge:return move||key==KeyCode.J||key==KeyCode.K;
            case TutorialStep.Ultimate:return move||key==KeyCode.Q;
            default:return false;
        }
    }
}
