using UnityEngine;

// Temporary playtest controls. Define KICKBACK_RELEASE before a public release.
public sealed class TestCombatShortcuts : MonoBehaviour
{
    public static bool Enabled
    {
        get
        {
#if KICKBACK_RELEASE
            return false;
#else
            return true;
#endif
        }
    }
    private EncounterDirector encounter;
    private GUIStyle style;
    private void Update()
    {
        if(!Enabled||!Application.isFocused)return;
        var impact=GetComponent<HitImpactManager>();if(impact!=null&&impact.Paused)return;
        if(encounter==null)encounter=GetComponent<EncounterDirector>();if(encounter==null)return;
        if(GameKeys.Down(KeyCode.Alpha1))encounter.JumpForTesting(1);
        else if(GameKeys.Down(KeyCode.Alpha2))encounter.JumpForTesting(5);
        else if(GameKeys.Down(KeyCode.Alpha3))encounter.JumpForTesting(10);
        else if(GameKeys.Down(KeyCode.Alpha4))encounter.JumpForTesting(11);
    }
    private void OnGUI()
    {
        if(!Enabled)return;
        if(style==null)style=new GUIStyle(GUI.skin.box){font=Resources.Load<Font>("Fonts/NotoSansCJKkr-Regular"),alignment=TextAnchor.MiddleCenter,wordWrap=true};
        style.fontSize=Mathf.Clamp(Screen.height/55,12,20);
        float width=Mathf.Min(760,Screen.width-20);
        GUI.Box(new Rect((Screen.width-width)/2,Screen.height*.83f,width,Screen.height*.055f),
            "테스트 전용  |  1 튜토리얼 패스 → 1-1  ·  2 도재학 1-5  ·  3 한승빈 1-10  ·  4 황빈 1-11",style);
    }
}
