using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class GameKeys
{
#if ENABLE_INPUT_SYSTEM
    private static Key ConvertKey(KeyCode code)
    {
        switch (code)
        {
            case KeyCode.W: return Key.W; case KeyCode.A: return Key.A;
            case KeyCode.S: return Key.S; case KeyCode.D: return Key.D;
            case KeyCode.R: return Key.R; case KeyCode.Return: return Key.Enter;
            case KeyCode.C: return Key.C; case KeyCode.J: return Key.J;
            case KeyCode.K: return Key.K; case KeyCode.L: return Key.L;
            case KeyCode.I: return Key.I; case KeyCode.Q: return Key.Q;
            case KeyCode.Space: return Key.Space; case KeyCode.Escape: return Key.Escape;
            case KeyCode.LeftShift: return Key.LeftShift;
            case KeyCode.RightShift: return Key.RightShift;
            default: return Key.None;
        }
    }
    public static bool Held(KeyCode code) => Keyboard.current != null &&
        ConvertKey(code) != Key.None && Keyboard.current[ConvertKey(code)].isPressed;
    public static bool Down(KeyCode code) => Keyboard.current != null &&
        ConvertKey(code) != Key.None && Keyboard.current[ConvertKey(code)].wasPressedThisFrame;
#else
    public static bool Held(KeyCode code) => Input.GetKey(code);
    public static bool Down(KeyCode code) => Input.GetKeyDown(code);
#endif
    public static Vector3 Direction => Vector3.ClampMagnitude(new Vector3(
        (Held(KeyCode.D) ? 1 : 0) - (Held(KeyCode.A) ? 1 : 0), 0,
        (Held(KeyCode.W) ? 1 : 0) - (Held(KeyCode.S) ? 1 : 0)), 1);
}

[DefaultExecutionOrder(-100)]
public sealed class CombatInput : MonoBehaviour
{
    public bool GameplayEnabled { get; set; } = true;
    public bool AttackPressed { get; private set; }
    public bool HeavyPressed { get; private set; }
    public bool GuardPressed { get; private set; }
    public bool GuardHeld { get; private set; }
    public bool DodgePressed { get; private set; }
    public bool GrabPressed { get; private set; }
    public bool UltimatePressed { get; private set; }
    public bool PausePressed { get; private set; }
    public Vector3 DodgeDirection { get; private set; }
    private bool focused = true;

    private void Update()
    {
        PausePressed = focused && GameKeys.Down(KeyCode.Escape);
        bool active = focused && GameplayEnabled && Time.timeScale > 0;
        AttackPressed = active && GameKeys.Down(KeyCode.J);
        HeavyPressed = active && GameKeys.Down(KeyCode.K);
        GuardPressed = active && GameKeys.Down(KeyCode.L);
        GuardHeld = active && GameKeys.Held(KeyCode.L);
        GrabPressed = active && GameKeys.Down(KeyCode.I);
        UltimatePressed = active && GameKeys.Down(KeyCode.Q);
        DodgeDirection = active ? GameKeys.Direction : Vector3.zero;
        bool shift = GameKeys.Held(KeyCode.LeftShift) || GameKeys.Held(KeyCode.RightShift);
        bool directionDown = GameKeys.Down(KeyCode.W) || GameKeys.Down(KeyCode.A) ||
            GameKeys.Down(KeyCode.S) || GameKeys.Down(KeyCode.D);
        DodgePressed = active && DodgeDirection.sqrMagnitude > .01f && shift &&
            (GameKeys.Down(KeyCode.LeftShift) || GameKeys.Down(KeyCode.RightShift) || directionDown);
    }
    private void OnApplicationFocus(bool focus) => focused = focus;
}
