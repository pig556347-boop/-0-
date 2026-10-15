using UnityEngine;
using UnityEngine.EventSystems;

// Menu navigation reads keys directly, without requiring legacy named InputManager axes.
public sealed class GameMenuInput : BaseInput
{
    public override float GetAxisRaw(string axis)
    {
        if(axis=="Horizontal")return (GameKeys.Held(KeyCode.D)||GameKeys.Held(KeyCode.RightArrow)?1:0)-
            (GameKeys.Held(KeyCode.A)||GameKeys.Held(KeyCode.LeftArrow)?1:0);
        if(axis=="Vertical")return (GameKeys.Held(KeyCode.W)||GameKeys.Held(KeyCode.UpArrow)?1:0)-
            (GameKeys.Held(KeyCode.S)||GameKeys.Held(KeyCode.DownArrow)?1:0);
        return 0;
    }
    public override bool GetButtonDown(string button)
    {return button=="Submit" ? GameKeys.Down(KeyCode.Return)||GameKeys.Down(KeyCode.Space) : button=="Cancel"&&GameKeys.Down(KeyCode.Escape);}
}
