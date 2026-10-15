using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class GameLobby : MonoBehaviour
{
    private PrototypeBootstrap bootstrap;
    private GameObject canvas, main, modal, pauseNavigation;
    private Font font;
    private readonly Dictionary<string,Button> buttons = new Dictionary<string,Button>();
    private string screen = "Main";
    private float nextProbe;
    private bool starting;
    public void Initialize(PrototypeBootstrap owner)
    {
        bootstrap=owner;GameLanguage.English=PlayerPrefs.GetInt("KickBack.English",0)==1;
        AudioListener.volume=PlayerPrefs.GetFloat("KickBack.Master",1);
        gameObject.AddComponent<GameLanguage>();
        Camera camera=new GameObject("Lobby camera").AddComponent<Camera>();
        camera.gameObject.AddComponent<AudioListener>();
        gameObject.AddComponent<GameAudioChannels>();
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        font=Resources.Load<Font>("Fonts/NotoSansCJKkr-Regular");
        if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        canvas=new GameObject("KICK Back Lobby",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvas.transform.SetParent(transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().sortingOrder=50;
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        if(EventSystem.current==null)
        {
            var events=new GameObject("Menu EventSystem",typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
#else
            events.AddComponent<StandaloneInputModule>();
#endif
        }
        var bg=Rect("Lobby background",canvas.transform,Vector2.zero,Vector2.one).gameObject.AddComponent<RawImage>();
        bg.texture=Resources.Load<Texture2D>("UI/Lobby/Background");bg.raycastTarget=false;
        var fit=bg.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio=bg.texture==null?16/9f:(float)bg.texture.width/bg.texture.height;
        Label("KICK Back",canvas.transform,new Vector2(.075f,.72f),new Vector2(.65f,.87f),112).fontStyle=FontStyle.Bold;
        Label("교실에서 시작되는 한 방",canvas.transform,new Vector2(.08f,.64f),new Vector2(.48f,.70f),25);
        main=new GameObject("Main menu",typeof(RectTransform));main.transform.SetParent(canvas.transform,false);
        var mr=(RectTransform)main.transform;mr.anchorMin=Vector2.zero;mr.anchorMax=Vector2.one;mr.offsetMin=mr.offsetMax=Vector2.zero;
        string[] ids={"Start","Language","Settings","Credits"};string[] labels={"게임시작","언어","설정","제작자"};
        for(int i=0;i<4;i++)
        {
            string id=ids[i];float y=.48f-i*.09f;
            Button(id,labels[i],main.transform,new Vector2(.08f,y),new Vector2(.35f,y+.065f),()=>
            {
                if(id=="Start")
                {
                    if(starting)return;starting=true;canvas.SetActive(false);camera.enabled=false;camera.GetComponent<AudioListener>().enabled=false;Destroy(camera.gameObject);
                    if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
                    bootstrap.BeginGame();Debug.Log("LOBBY_START_GAME");
                }
                else Open(id);
            });
        }
        Label("KICK Back  ·  SCHOOL ACTION",canvas.transform,new Vector2(.08f,.035f),new Vector2(.58f,.085f),16).color=new Color(.7f,.7f,.7f);
        pauseNavigation=new GameObject("Pause navigation",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        pauseNavigation.transform.SetParent(transform,false);pauseNavigation.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        pauseNavigation.GetComponent<Canvas>().sortingOrder=40;
        var pauseScaler=pauseNavigation.GetComponent<CanvasScaler>();pauseScaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        pauseScaler.referenceResolution=new Vector2(1920,1080);pauseScaler.matchWidthOrHeight=.5f;
        Button("ReturnLobby","로비로 돌아가기",pauseNavigation.transform,new Vector2(.35f,.23f),new Vector2(.65f,.29f),()=>
        {Time.timeScale=1;UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);});
        pauseNavigation.SetActive(false);
        if(bg.texture==null)Debug.LogError("Lobby background missing");
        Debug.Log("LOBBY_READY title=KICK Back");
    }
    private void Open(string page)
    {
        if(modal!=null) { modal.SetActive(false);Destroy(modal); }
        screen=page;main.SetActive(page=="Main");if(page=="Main"){PlayerPrefs.Save();return;}
        modal=Rect("Lobby "+page,canvas.transform,new Vector2(.075f,.15f),new Vector2(.57f,.68f)).gameObject;
        modal.AddComponent<Image>().color=new Color(.035f,.035f,.04f,.96f);
        Label(page=="Language"?"언어":page=="Settings"?"설정":"제작자",modal.transform,new Vector2(.06f,.80f),new Vector2(.94f,.95f),38);
        if(page=="Language")
        {
            Button("Korean","한국어",modal.transform,new Vector2(.08f,.54f),new Vector2(.92f,.69f),()=>SetLanguage(false));
            Button("English","English",modal.transform,new Vector2(.08f,.33f),new Vector2(.92f,.48f),()=>SetLanguage(true));
        }
        else if(page=="Settings")
        {
            Volume("Master","전체 음량",.64f);Volume("Music","배경음악",.46f);Volume("Effects","효과음",.28f);
            Button("Fullscreen","전체화면 전환",modal.transform,new Vector2(.08f,.08f),new Vector2(.59f,.20f),()=>Screen.fullScreen=!Screen.fullScreen);
        }
        else Label("KICK Back\n제작: pig556347-boop\n\n개인 개발 프로젝트",modal.transform,new Vector2(.08f,.26f),new Vector2(.92f,.72f),27);
        Button("Back","뒤로",modal.transform,new Vector2(.66f,.08f),new Vector2(.92f,.20f),()=>Open("Main"));
    }
    private void SetLanguage(bool english)
    { GameLanguage.English=english;PlayerPrefs.SetInt("KickBack.English",english?1:0);PlayerPrefs.Save(); }
    private void Volume(string id,string caption,float y)
    {
        Label(caption,modal.transform,new Vector2(.08f,y+.045f),new Vector2(.43f,y+.14f),23);
        var track=Rect(id+" volume",modal.transform,new Vector2(.45f,y+.05f),new Vector2(.91f,y+.13f));
        track.gameObject.AddComponent<Image>().color=new Color(.22f,.22f,.24f);
        var fill=Rect("Fill",track,Vector2.zero,Vector2.one);fill.gameObject.AddComponent<Image>().color=new Color(.80f,.76f,.65f);
        var handle=Rect("Handle",track,new Vector2(0,0),new Vector2(0,1));handle.sizeDelta=new Vector2(18,0);
        handle.gameObject.AddComponent<Image>().color=Color.white;
        var slider=track.gameObject.AddComponent<Slider>();slider.fillRect=fill;slider.handleRect=handle;
        slider.targetGraphic=handle.GetComponent<Image>();slider.minValue=0;slider.maxValue=1;
        slider.value=PlayerPrefs.GetFloat("KickBack."+id,1);
        slider.onValueChanged.AddListener(value=>
        {PlayerPrefs.SetFloat("KickBack."+id,value);if(id=="Master")AudioListener.volume=value;});
    }
    private void Update()
    {
        if(starting)
        {
            bool paused=HitImpactManager.Instance!=null&&HitImpactManager.Instance.Paused;
            pauseNavigation.SetActive(paused);if(!paused)return;
        }
        if(!starting&&GameKeys.Down(KeyCode.Escape)&&screen!="Main")Open("Main");
        if(!Application.absoluteURL.Contains("motionVerification=1")||Time.unscaledTime<nextProbe)return;
        nextProbe=Time.unscaledTime+.15f;
        var samples=new List<ButtonSample>();
        foreach(var pair in buttons)
        {
            if(pair.Value==null||!pair.Value.gameObject.activeInHierarchy)continue;
            var r=(RectTransform)pair.Value.transform;Vector3[] corners=new Vector3[4];r.GetWorldCorners(corners);
            samples.Add(new ButtonSample {id=pair.Key,x=(corners[0].x+corners[2].x)/2/Screen.width,
                y=1-(corners[0].y+corners[2].y)/2/Screen.height});
        }
        if(modal!=null)foreach(Slider slider in modal.GetComponentsInChildren<Slider>())
        {
            Vector3[] corners=new Vector3[4];((RectTransform)slider.transform).GetWorldCorners(corners);
            samples.Add(new ButtonSample{id=slider.name.Split(' ')[0],x=(corners[0].x+corners[2].x)/2/Screen.width,
                y=1-(corners[0].y+corners[2].y)/2/Screen.height,w=(corners[2].x-corners[0].x)/Screen.width});
        }
        Debug.Log("LOBBY_UI "+JsonUtility.ToJson(new Snapshot{page=starting?"Paused":screen,english=GameLanguage.English,
            text=VisibleText(),master=AudioListener.volume,music=PlayerPrefs.GetFloat("KickBack.Music",1),effects=PlayerPrefs.GetFloat("KickBack.Effects",1),buttons=samples.ToArray()}));
    }
    private string VisibleText()
    {var values=new List<string>();foreach(Text text in canvas.GetComponentsInChildren<Text>())values.Add(text.text);return string.Join("\n",values.ToArray());}
    private void OnApplicationFocus(bool focus){if(!focus)PlayerPrefs.Save();}
    private void OnDestroy(){PlayerPrefs.Save();}
    private Button Button(string id,string title,Transform parent,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
    {
        var r=Rect(id,parent,min,max);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.10f,.10f,.11f,.8f);
        var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.65f,.62f,.55f,.7f);outline.effectDistance=new Vector2(1,-1);
        var button=r.gameObject.AddComponent<Button>();var colors=button.colors;
        colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.82f,.6f);colors.selectedColor=colors.highlightedColor;
        colors.pressedColor=new Color(.65f,.55f,.43f);button.colors=colors;button.onClick.AddListener(action);
        Label(title,r,new Vector2(.08f,.08f),new Vector2(.92f,.92f),30);buttons[id]=button;return button;
    }
    private Text Label(string value,Transform parent,Vector2 min,Vector2 max,int size)
    {
        var text=Rect("Text",parent,min,max).gameObject.AddComponent<Text>();text.font=font;text.text=value;
        text.fontSize=size;text.color=new Color(.94f,.92f,.87f);text.alignment=TextAnchor.MiddleLeft;
        text.resizeTextForBestFit=true;text.resizeTextMinSize=12;text.resizeTextMaxSize=size;text.raycastTarget=false;return text;
    }
    public static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max)
    {
        var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);
        r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return r;
    }
    [Serializable]private sealed class ButtonSample {public string id;public float x,y,w;}
    [Serializable]private sealed class Snapshot {public string page,text;public bool english;public float master,music,effects;public ButtonSample[] buttons;}
}
