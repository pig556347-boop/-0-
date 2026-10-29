using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SchoolEscape
{
    // Native C# gameplay. JSON is authored content, never a browser or JS runtime.
    public sealed class NativeRun017 : MonoBehaviour
    {
        [Serializable] public class Definition { public string id,label,type,description,lockId; public bool usable; }
        [Serializable] public class Lock { public string id,label,type,need,answer,sourceId,rewardUid,classClue; public bool unlocked; }
        [Serializable] public class Card { public string zone,itemId,sourceId,prior; }
        [Serializable] public class Student { public int no; public string name,role,deskId; }
        [Serializable] public class Document { public string id,title,text,sourceId,itemId,lockId,classLabel; public Student[] roster; }
        [Serializable] public class Run { public int seed; public Definition[] defs; public ItemInventory.Item[] items; public ItemInventory.Searchable[] sources; public Lock[] locks; public Card[] cards; public Document[] documents; public string[] library; }
        [Serializable] class Layouts { public int version; public Run[] runs; }
        [Serializable] class Saved { public int version=17,lives,mode,bookStep,mapSeed; public float time,stamina; public string spaceId; public Vector2 point; public Run run; public string[] inventory,zones,inserted,read; public string equipped; public bool specimen,kitchenExamined,kitchenCleared; public int[] broken; public bool eventStateSaved,eventWasDark; public float eventElapsed,eventShadowAt; public int eventBlackouts; }
        public ExplorationGame Game { get; private set; }
        public Run Content { get; private set; }
        public readonly HashSet<string> Zones=new HashSet<string>{"start"},Read=new HashSet<string>();
        public bool Captured,Specimen,KitchenExamined,KitchenCleared;
        public int Lives=5,Mode,BookStep;
        public float MaximumStamina=>Mode==0?200:Mode==1?150:100;
        public string Panel="",Feedback="";
        public bool OwnsOverlay=>Panel!="";
        public bool RoomEncounter=>new[]{"music","science","art","gym"}.Contains(Game.Current.subjectEvent);
        public bool KitchenActive=>kitchenActive;
        public bool ThreatActive=>enemies.Count>0||kitchenActive;
        readonly List<GameObject> gateKeys=new List<GameObject>();
        public bool Dark=>Game.Current.subjectEvent=="science"&&elapsed>=10&&elapsed%10<2&&!Captured;
        Layouts layouts; string lastSpace="",lockInput="",documentText=""; Lock activeLock;
        ItemInventory.Candidate target; int compartment,inventorySelection; Vector2 scroll;
        bool settings,english,handledDeath;Document currentDocument;
        readonly List<GameObject> chains=new List<GameObject>(); float elapsed,shadowAt=-1; int blackouts; bool wasDark;
        readonly List<Enemy> enemies=new List<Enemy>();
        class Enemy { public string kind; public Vector2 at,direction=Vector2.down; public GameObject visual; public float grace=1.5f,repath,charge,speed; public List<Vector2> path=new List<Vector2>(); }
        FloorData chaseMap; FixtureData[] originalKitchen;
        readonly List<Vector2> kitchenPath=new List<Vector2>();
        readonly List<GameObject> rubble=new List<GameObject>(); readonly List<FixtureData> blocks=new List<FixtureData>();
        readonly HashSet<int> broken=new HashSet<int>(); Enemy chef; int kitchenIndex; float kitchenStarted; bool kitchenActive;
        public static void PrepareMap(MapData data,int seed)
        {
            var random=new System.Random(seed);
            foreach(var floor in data.floors){var slots=floor.rooms.Where(r=>r.interiorKind=="classroom"||r.interiorKind=="staff").ToArray();var values=slots.Select(r=>Tuple.Create(r.label,r.interiorId,r.interiorKind)).ToArray();for(int n=values.Length-1;n>0;n--){int j=random.Next(n+1);var tmp=values[n];values[n]=values[j];values[j]=tmp;}for(int n=0;n<slots.Length;n++){slots[n].label=values[n].Item1;slots[n].interiorId=values[n].Item2;slots[n].interiorKind=values[n].Item3;var room=data.FindSpace(slots[n].interiorId);Vector2 d=slots[n].facing=="north"?Vector2.up:slots[n].facing=="south"?Vector2.down:slots[n].facing=="east"?Vector2.right:Vector2.left;room.returnX=slots[n].doorX+(int)d.x;room.returnY=slots[n].doorY+(int)d.y;}}
            foreach(var room in data.roomMaps.Where(r=>r.kind=="classroom")){int theme=random.Next(3);room.theme=new[]{"일반 교실","파손된 교실","핏자국 교실"}[theme];var desks=room.fixtures.Where(f=>f.kind=="student-desk").ToArray();foreach(var desk in desks){float x=desk.x,y=desk.y,dx=(float)(random.NextDouble()-.5)*.18f;foreach(var part in room.fixtures.Where(f=>Math.Abs(f.x-x)<.5&&f.y-y>-.92&&f.y-y<.4))part.x+=dx;if(theme==1)desk.color="#514a3e";}if(theme==2){var fixtures=room.fixtures.ToList();for(int n=0;n<5;n++)fixtures.Add(new FixtureData{id=room.id+"-native-stain-"+n,kind="old-blood-stain",x=3+(float)random.NextDouble()*12,y=5+(float)random.NextDouble()*7,width=.5f,depth=.3f,height=.008f,elevation=.028f,color="#492b29",solid=false,shape="sphere"});room.fixtures=fixtures.ToArray();}}
        }
        public void Initialize(ExplorationGame game)
        {
            Game=game;layouts=JsonUtility.FromJson<Layouts>(Resources.Load<TextAsset>("RunLayouts017").text);
            chaseMap=game.Data.FindSpace("annex-kitchen-chase");originalKitchen=chaseMap.fixtures;
            int[,] way={{24,35},{24,28},{28,28},{28,33},{46,33},{46,28},{42,28},{42,19},{48,19},{48,15},{40,15},{40,11},{35,11},{35,15},{30,15},{30,10},{24,10},{24,13},{20,13},{20,10},{15,10},{15,15},{11,15},{11,20},{2,20}};
            Vector2 p=new Vector2(24,35);kitchenPath.Add(p);for(int n=1;n<way.GetLength(0);n++){Vector2 dest=new Vector2(way[n,0],way[n,1]);while(p!=dest){p+=new Vector2(Math.Sign(dest.x-p.x),Math.Sign(dest.y-p.y));kitchenPath.Add(p);}}
            for(int n=0;n<3;n++){var root=new GameObject("교문 삽입 열쇠 "+n);root.transform.position=game.Data.World(34+(n-1)*.7f,1.5f)+Vector3.up*1.1f;Part(root,"열쇠 머리",Vector3.zero,new Vector3(.19f,.19f,.06f),new Color(.8f,.65f,.3f));Part(root,"열쇠 몸통",Vector3.down*.16f,new Vector3(.045f,.25f,.05f),new Color(.8f,.65f,.3f));gateKeys.Add(root);}
            NewRun();
        }
        public void NewRun()
        {
            ClearEnemies();ClearRubble();chaseMap=Game.Data.FindSpace("annex-kitchen-chase");originalKitchen=chaseMap.fixtures;Content=JsonUtility.FromJson<Run>(JsonUtility.ToJson(layouts.runs[UnityEngine.Random.Range(0,layouts.runs.Length)]));
            // Passwords and book order are fresh each run, independently of the safe layout.
            foreach(var l in Content.locks.Where(l=>!string.IsNullOrEmpty(l.answer))){string old=l.answer;l.answer="";for(int n=0;n<old.Length;n++)l.answer+=l.type=="direction"?new[]{"↑","→","↓","←"}[UnityEngine.Random.Range(0,4)]:UnityEngine.Random.Range(0,10).ToString();foreach(var d in Content.defs.Where(d=>d.lockId==l.id))d.description=d.description.Replace(old,l.answer);foreach(var d in Content.documents.Where(d=>d.lockId==l.id))d.text=d.text.Replace(old,l.answer);}
            for(int n=Content.library.Length-1;n>0;n--){int j=UnityEngine.Random.Range(0,n+1);string old=Content.library[n];Content.library[n]=Content.library[j];Content.library[j]=old;}var record=Content.documents.First(d=>d.id=="library-return-log");record.text=string.Join("\n",Content.library.Select((code,n)=>(n+1)+". "+BookName(code)+" · 분류 "+code))+"\n\n순서대로 해당 분류 서가에 책을 반납하면 안쪽 보관함이 열립니다.";
            foreach(var i in Content.items)i.picked=i.consumed=false;
            Game.Items.LoadNative(Content.items,Content.sources,Content.defs,Content.seed);
            Game.Items.Grant("master-key");Zones.Clear();Zones.Add("start");Read.Clear();Lives=Mode>=2?(Mode==2?3:1):5;
            Captured=handledDeath=Specimen=KitchenExamined=KitchenCleared=kitchenActive=false;BookStep=0;broken.Clear();lastSpace="";Panel="";Game.Player.MaximumStamina=MaximumStamina;Game.Player.RestoreStamina();
        }
        public string Zone(string id){var f=Game.Data.FindSpace(id);if(f==null)return "start";return f.building=="annex"?"annex":f.building=="west"?"west":id=="main-rooftop"?"roof":(f.sourceFloor>0?f.sourceFloor:f.number)>=2?"floor"+(f.sourceFloor>0?f.sourceFloor:f.number):"start";}
        public bool Has(string id)=>Game.Items.Inventory.Any(i=>i.id==id);
        void Consume(string id){foreach(var i in Game.Items.Inventory.Where(i=>i.id==id).ToArray()){i.consumed=true;Game.Items.Inventory.Remove(i);}}
        public void Unlock(Lock l){l.unlocked=true;foreach(var d in Content.defs.Where(d=>d.type=="note"&&d.lockId==l.id)){Consume(d.id);foreach(var i in Game.Items.WorldItems.Where(i=>i.id==d.id)){i.picked=true;i.consumed=true;}}Game.Audio.Cue("unlock",true);BuildChains();Game.ShowNotice(l.label+" 잠금 해제");}
        public bool Travel(string id)
        {
            var f=Game.Data.FindSpace(id);if(f?.subjectEvent=="science"&&(!Has("lab-coat")||!Has("flashlight"))){Game.ShowNotice("실험복과 손전등을 소지하세요. 둘 다 본관 3층에 있습니다.");return false;}
            string zone=Zone(id);if(!Zones.Contains(zone)){var card=Content.cards.FirstOrDefault(c=>c.zone==zone);if(Has("master-key"))Zones.Add(zone);else if(card!=null&&Has(card.itemId)){Consume(card.itemId);Zones.Add(zone);Game.Audio.Cue("unlock");}else{Game.ShowNotice("해당 층/건물 카드키가 필요합니다.");return false;}}
            var l=Content.locks.FirstOrDefault(l=>l.id==id);if(l==null||l.unlocked)return true;
            if(Has("master-key")||(!string.IsNullOrEmpty(l.need)&&Has(l.need))){if(!Has("master-key")&&l.type=="key")Consume(l.need);Unlock(l);return true;}
            if(!string.IsNullOrEmpty(l.need)){Game.ShowNotice(l.label+" · "+ItemInventory.Name(l.need)+" 필요");return false;}
            activeLock=l;lockInput="";Open("lock");return false;
        }
        public bool InterceptExit(string portal="")
        {
            if(Game.Current.id=="annex-kitchen"&&KitchenExamined&&!KitchenCleared&&Game.Pursuer.SimulationEnabled){BeginKitchen();return true;}
            if(Game.Current.id==chaseMap.id&&kitchenActive&&portal=="kitchen-entry"){Game.ShowNotice("퇴로가 무너졌다. 서쪽 비상문으로!");return true;}
            if(Game.Current.subjectEvent=="science"&&portal=="science-rear-exit"&&!Specimen){Game.ShowNotice("뒷문 캐비넷의 밀봉된 실험 표본을 가져오세요.");return true;}return false;
        }
        void Open(string panel){Panel=panel;scroll=Vector2.zero;Feedback="";Game.SetPaused(true);}
        public void Close(){Panel="";activeLock=null;target=null;Game.SetPaused(false);}
        public bool HandleInput()
        {
            if(Panel!=""){if(Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.I))Close();return true;}
            if(Game.Started&&!Game.Caught&&!Game.Won&&Input.GetKeyDown(KeyCode.I)){Open("inventory");return true;}return false;
        }
        public bool Search(ItemInventory.Candidate candidate)
        {
            var l=Content.locks.FirstOrDefault(l=>l.id=="container:"+candidate.source?.id);
            if(l!=null&&!l.unlocked){if(Has("master-key"))Unlock(l);else{activeLock=l;lockInput="";Open("lock");return true;}}
            if(candidate.source?.kind=="library-reward-cabinet"&&BookStep<3){Game.ShowNotice("먼저 대출대의 반납 기록을 읽고 세 권을 순서대로 반납하세요.");return true;}
            target=candidate;compartment=0;documentText="";currentDocument=null;Open("search");Game.Audio.Cue("cabinet",true);
            if(candidate.source?.kind=="kitchen-reward-crate")KitchenExamined=true;
            if(candidate.source!=null){candidate.source.searched=true;Game.EmitNoise(candidate.source.spaceId,candidate.source.point,4,"search");}return true;
        }
        string BookName(string code){switch(code){case "000":return "색인 없는 사전";case "300":return "사라진 학생의 사회";case "400":return "그림자의 생물학";case "600":return "끝나지 않는 악보";case "800":return "마지막 동화";default:return "폐교의 연대기";}}
        void ReturnBook(string code)
        {
            if(BookStep>=3)return;
            if(Content.library[BookStep]==code){BookStep++;Feedback=BookStep==3?"반납 완료 보관함이 열렸습니다.":"반납 "+BookStep+" / 3";Game.Audio.Cue("unlock",true);}
            else{BookStep=0;Feedback="순서가 다릅니다. 반납 기록을 확인하세요.";Game.EmitNoise(Game.Current.id,Game.Data.Grid(Game.Player.transform.position),18,"wrong-book");Game.Audio.Cue("crash",true);}
        }
        void ClearEnemies(){foreach(var e in enemies)if(e.visual!=null)Destroy(e.visual);enemies.Clear();}
        bool Sight(Vector2 a,Vector2 b){int n=Mathf.CeilToInt(Vector2.Distance(a,b)*5);for(int k=0;k<=n;k++){var p=Vector2.Lerp(a,b,k/(float)Mathf.Max(1,n));if(!Game.Current.Open(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y)))return false;}return true;}
        Enemy Spawn(string kind,int index=0)
        {
            var f=Game.Current;var player=Game.Data.Grid(Game.Player.transform.position);var points=Game.Items.ReachablePoints(f.id).Where(p=>Vector2.Distance(p,player)*Game.Data.tileSize>8).ToList();
            Vector2 anchor=kind=="musician"?new Vector2(6,16):index==0?new Vector2(f.width-3,f.height-4):new Vector2(2,12);
            var e=new Enemy{kind=kind,at=points.OrderBy(p=>(p-anchor).sqrMagnitude).FirstOrDefault()};e.visual=Monster(kind);e.visual.transform.position=Game.Data.World(e.at.x,e.at.y);enemies.Add(e);return e;
        }
        GameObject Monster(string kind)
        {
            var root=new GameObject(kind+" · SchoolEscape");root.transform.SetParent(transform);Color skin=kind=="shadow"?new Color(.025f,.035f,.045f):kind=="statue"?new Color(.58f,.61f,.56f):new Color(.52f,.58f,.51f);
            Part(root,"body",new Vector3(0,1.3f,0),new Vector3(kind=="chef"?1.6f:.75f,1.7f,.65f),skin);
            Part(root,"head",new Vector3(0,kind=="dunky"?3.2f:2.6f,.05f),kind=="musician"?new Vector3(1.85f,1.95f,1.4f):new Vector3(.65f,.7f,.55f),skin,PrimitiveType.Sphere);
            foreach(float side in new[]{-1f,1f}){Part(root,"arm",new Vector3(side*.55f,1,0),new Vector3(.2f,1.8f,.25f),skin);Part(root,"leg",new Vector3(side*.25f,.4f,0),new Vector3(.28f,.8f,.35f),skin);Part(root,"eye",new Vector3(side*.18f,kind=="dunky"?3.25f:2.7f,kind=="musician"?.68f:.32f),new Vector3(.14f,.19f,.04f),Color.black);}
            if(kind=="chef")Part(root,"grease apron",new Vector3(0,1.3f,.36f),new Vector3(1.25f,1.6f,.1f),new Color(.65f,.65f,.51f));return root;
        }
        static GameObject Part(GameObject root,string label,Vector3 p,Vector3 size,Color color,PrimitiveType shape=PrimitiveType.Cube){var o=GameObject.CreatePrimitive(shape);o.name=label;o.transform.SetParent(root.transform,false);o.transform.localPosition=p;o.transform.localScale=size;Destroy(o.GetComponent<Collider>());var m=new Material(Shader.Find("Standard"));m.color=color;o.GetComponent<Renderer>().material=m;return o;}
        List<Vector2> Route(Vector2 from,Vector2 to)
        {
            var f=Game.Current;var start=new Vector2Int(Mathf.RoundToInt(from.x),Mathf.RoundToInt(from.y));var end=new Vector2Int(Mathf.RoundToInt(to.x),Mathf.RoundToInt(to.y));var q=new Queue<Vector2Int>();var prev=new Dictionary<Vector2Int,Vector2Int>();q.Enqueue(start);prev[start]=start;
            while(q.Count>0){var p=q.Dequeue();if(p==end)break;foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}){var n=p+d;if(prev.ContainsKey(n)||!f.Clear(n.x,n.y,Game.Data.tileSize,.27f)||!Game.Items.ClearSegment(f,p,n))continue;prev[n]=p;q.Enqueue(n);}}
            var result=new List<Vector2>();if(!prev.ContainsKey(end))return result;var at=end;while(at!=start){result.Add(at);at=prev[at];}result.Reverse();return result;
        }
        void Move(Enemy e,float dt,bool enabled)
        {
            e.grace-=dt;e.speed=0;e.visual.SetActive(Game.Pursuer.SimulationEnabled);if(!enabled)return;
            Vector2 player=Game.Data.Grid(Game.Player.transform.position);e.repath-=dt;if(e.repath<=0||e.path.Count==0){e.path=Route(e.at,player);e.repath=.45f;}
            float speed=e.kind=="musician"?3.15f:e.kind=="statue"?3.8f:3.6f;
            if(e.kind=="dunky"&&e.path.Count>0){bool straight=Vector2.Dot((e.path[0]-e.at).normalized,e.direction)>.94f;e.charge=straight?Mathf.Min(1,e.charge+dt*.32f):0;speed=2.5f+e.charge*3.5f;}
            float budget=speed*dt/Game.Data.tileSize;while(budget>0&&e.path.Count>0){Vector2 delta=e.path[0]-e.at;float d=Mathf.Min(delta.magnitude,budget);if(delta.sqrMagnitude>.00001f){var next=e.at+delta.normalized*d;if(!Game.Items.ClearSegment(Game.Current,e.at,next))break;e.at=next;e.direction=delta.normalized;}budget-=d;if(d>=delta.magnitude-.001f)e.path.RemoveAt(0);e.speed=speed;}
            e.visual.transform.position=Game.Data.World(e.at.x,e.at.y);e.visual.transform.rotation=Quaternion.LookRotation(new Vector3(e.direction.x,0,e.direction.y));
            if(e.grace<=0&&Vector2.Distance(e.at,player)*Game.Data.tileSize<.68f&&Sight(e.at,player))Capture();
        }
        public void Tick(float dt)
        {
            for(int n=0;n<gateKeys.Count;n++)gateKeys[n].SetActive(Game.Current.id=="playground"&&Game.Items.InsertedKeys.Contains(new[]{"key-a","key-b","key-c"}[n]));
            if(Game.Current.id!=lastSpace){ClearEnemies();elapsed=0;blackouts=0;wasDark=false;shadowAt=-1;lastSpace=Game.Current.id;BuildChains();if(Game.Current.subjectEvent=="art")Spawn("statue");if(Game.Current.subjectEvent=="gym")Spawn("dunky");}
            if(Game.Caught){if(!handledDeath){Game.Audio.Cue("discord");handledDeath=true;Lives=Mathf.Max(0,Lives-1);if(Mode==1||Mode==2)Game.Items.DropOnDeath(Mode==2);if(Game.Current.subjectEvent=="science")foreach(var i in Game.Items.WorldItems.Where(i=>!i.picked&&(i.id=="lab-coat"||i.id=="flashlight")&&i.spaceId==Game.Current.id&&string.IsNullOrEmpty(i.sourceId))){i.spaceId="floor-3";i.point=new Vector2(Game.Current.returnX,Game.Current.returnY);}if(kitchenActive)KitchenExamined=false;kitchenActive=false;}return;}
            if(Game.Paused||Game.Won)return;elapsed+=dt;Specimen|=Has("lab-sample");
            string kind=Game.Current.subjectEvent;
            if(kind=="music"){if(elapsed<32)Game.Audio.RoomPiano(68+Mathf.Min(1,elapsed/32)*152);if(elapsed>=32&&elapsed-dt<32)Game.Audio.Cue("discord");if(elapsed>=33&&enemies.Count==0&&Game.Pursuer.SimulationEnabled)Spawn("musician");}
            if(kind=="science"){if(Dark&&!wasDark){blackouts++;Game.Audio.Cue("switch");}if(!Dark&&wasDark&&blackouts>=3&&shadowAt<0){shadowAt=elapsed+1.2f;Game.Audio.Cue("monster");}wasDark=Dark;if(shadowAt>0&&elapsed>=shadowAt&&enemies.Count==0&&Game.Pursuer.SimulationEnabled){Spawn("shadow");Spawn("shadow",1);}}
            foreach(var e in enemies){bool enabled=Game.Pursuer.SimulationEnabled&&(kind!="science"||Dark);if(e.kind=="statue"){Vector2 p=Game.Data.Grid(Game.Player.transform.position),delta=e.at-p;var forward=new Vector2(Game.Player.Visual.forward.x,Game.Player.Visual.forward.z);enabled&=!(delta.magnitude<1||Vector2.Dot(delta.normalized,forward)>.5f&&Sight(p,e.at));}Move(e,dt,enabled);}
            if(enemies.Count>0)Game.Audio.EnemyStep(enemies.Any(e=>e.speed>.1f),enemies.Min(e=>Vector2.Distance(e.at,Game.Data.Grid(Game.Player.transform.position))*Game.Data.tileSize),dt);
            if(Game.Current.id=="annex-kitchen"&&KitchenExamined&&!KitchenCleared&&Game.Pursuer.SimulationEnabled&&Vector2.Distance(Game.Data.Grid(Game.Player.transform.position),new Vector2(Game.Current.exitX,Game.Current.exitY+1))<1.4f)BeginKitchen();
            StepKitchen(dt);
        }
        void Capture(){Captured=true;Game.Player.InputEnabled=false;}
        public void Revive(){if(Lives<=0)return;Captured=false;Game.Pursuer.ResetPursuit();Game.Player.InputEnabled=true;Game.Player.RestoreStamina();Game.SwitchFloor(1,Game.Data.spawnX,Game.Data.spawnY,false);handledDeath=false;lastSpace="";}
        void ClearRubble(){foreach(var r in rubble)if(r!=null)Destroy(r);rubble.Clear();blocks.Clear();if(chaseMap!=null&&originalKitchen!=null)chaseMap.fixtures=originalKitchen;if(chef?.visual!=null)Destroy(chef.visual);chef=null;}
        void BeginKitchen()
        {
            Game.SwitchSpace(chaseMap.id,24,35);kitchenActive=true;kitchenStarted=Game.SimulationTime;kitchenIndex=-1;
            if(chef?.visual!=null)Destroy(chef.visual);chef=new Enemy{kind="chef",at=new Vector2(24,36.5f),visual=Monster("chef")};
            BuildRubble();
            SyncRubble();Game.Audio.Cue("crash");Game.ShowNotice("비상 작업 통로가 열렸다. 서쪽 출구로 달려!");
        }
        void BuildRubble(){
            if(blocks.Count==0)for(int y=2;y<chaseMap.height-1;y++)for(int x=1;x<chaseMap.width-1;x++){if(kitchenPath.Contains(new Vector2(x,y))||!chaseMap.Clear(x,y,Game.Data.tileSize,.27f))continue;var f=new FixtureData{id="native-collapse-"+blocks.Count,x=x,y=y,width=Game.Data.tileSize,depth=Game.Data.tileSize,height=1.2f,solid=true};blocks.Add(f);var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=f.id;o.transform.position=Game.Data.World(x,y)+Vector3.up*.6f;o.transform.localScale=new Vector3(f.width,1.2f,f.depth);var m=new Material(Shader.Find("Standard"));m.color=new Color(.4f,.45f,.4f);o.GetComponent<Renderer>().material=m;rubble.Add(o);}
        }
        void SyncRubble(){chaseMap.fixtures=originalKitchen.Concat(blocks.Where((b,n)=>!broken.Contains(n))).ToArray();for(int n=0;n<rubble.Count;n++)rubble[n].SetActive(Game.Current.id==chaseMap.id&&!broken.Contains(n));}
        void StepKitchen(float dt)
        {
            if(blocks.Count>0)for(int n=0;n<rubble.Count;n++)rubble[n].SetActive(Game.Current.id==chaseMap.id&&!broken.Contains(n));
            if(!kitchenActive){if(chef?.visual!=null)chef.visual.SetActive(false);return;}
            if(Game.Current.id!=chaseMap.id){kitchenActive=false;KitchenCleared=true;return;}
            if(!Game.Pursuer.SimulationEnabled){kitchenActive=false;KitchenCleared=true;return;}
            Game.Player.RestoreStamina();if(Game.SimulationTime-kitchenStarted<1.4f)return;
            Vector2 player=Game.Data.Grid(Game.Player.transform.position);float speed=new[]{4.15f,4.4f,4.55f,4.65f}[Mode]+(kitchenPath.Select((p,n)=>new{p,n}).OrderBy(v=>(v.p-player).sqrMagnitude).First().n-kitchenIndex>12/Game.Data.tileSize?.6f:0),budget=speed*dt/Game.Data.tileSize;
            while(budget>0&&kitchenIndex<kitchenPath.Count-1){Vector2 dest=kitchenPath[kitchenIndex+1],delta=dest-chef.at;float d=Mathf.Min(budget,delta.magnitude);chef.at+=delta.normalized*d;chef.direction=delta.normalized;budget-=d;if(d>=delta.magnitude-.001f)kitchenIndex++;}
            chef.visual.transform.position=Game.Data.World(chef.at.x,chef.at.y);chef.visual.transform.rotation=Quaternion.LookRotation(new Vector3(chef.direction.x,0,chef.direction.y));
            bool changed=false;for(int n=0;n<blocks.Count;n++)if(!broken.Contains(n)&&Vector2.Distance(new Vector2(blocks[n].x,blocks[n].y),chef.at)<1.65f){broken.Add(n);changed=true;}if(changed){SyncRubble();Game.Audio.Cue("crash");}
            Game.Audio.EnemyStep(true,Vector2.Distance(chef.at,player)*Game.Data.tileSize,dt);if(Vector2.Distance(chef.at,player)*Game.Data.tileSize<1.12f)Capture();
        }
        void BuildChains()
        {
            foreach(var c in chains)if(c!=null)Destroy(c);chains.Clear();if(Content==null)return;
            var f=Game.Current;var doors=new List<Tuple<string,Vector2,string>>();foreach(var d in f.rooms??Array.Empty<RoomData>())doors.Add(Tuple.Create(d.interiorId,new Vector2(d.doorX,d.doorY),d.facing));foreach(var d in f.portals??Array.Empty<PortalData>())doors.Add(Tuple.Create(d.targetId,new Vector2(d.doorX,d.doorY),d.facing));
            foreach(var d in doors){var l=Content.locks.FirstOrDefault(l=>l.id==d.Item1);if(l?.type!="cutter"||l.unlocked)continue;var root=new GameObject("쇠사슬 잠금");root.transform.position=Game.Data.World(d.Item2.x,d.Item2.y);bool horizontal=d.Item3=="north"||d.Item3=="south";for(int n=0;n<8;n++){var link=Part(root,"chain link",new Vector3(horizontal?(n-3.5f)*.15f:0,.9f+(n%2)*.06f,horizontal?0:(n-3.5f)*.15f),new Vector3(.18f,.05f,.07f),new Color(.4f,.44f,.43f));link.transform.localRotation=Quaternion.Euler(0,horizontal?0:90,n%2==0?35:-35);}Part(root,"padlock",new Vector3(0,.7f,0),new Vector3(.18f,.23f,.14f),new Color(.65f,.55f,.32f));chains.Add(root);}
        }
        void DrawRecord(){if(currentDocument?.roster!=null&&currentDocument.roster.Length>0){GUILayout.Label(currentDocument.classLabel+" · 출석부");GUILayout.BeginHorizontal();GUILayout.Label("번호",GUILayout.Width(80));GUILayout.Label("이름",GUILayout.Width(180));GUILayout.Label("역할");GUILayout.EndHorizontal();foreach(var student in currentDocument.roster){GUILayout.BeginHorizontal("box");GUILayout.Label(student.no.ToString("00"),GUILayout.Width(80));GUILayout.Label(student.name,GUILayout.Width(180));GUILayout.Label(student.role=="학생"?"—":student.role);GUILayout.EndHorizontal();}}else if(documentText!="")GUILayout.Label(documentText);}
        public void Save()
        {
            if(Captured||Game.Caught||Game.Won||kitchenActive||enemies.Count>0||Game.Opening){Feedback="추격·이동 중에는 저장할 수 없습니다.";return;}
            Content.items=Game.Items.WorldItems.ToArray();var d=new Saved{eventStateSaved=true,eventElapsed=elapsed,eventBlackouts=blackouts,eventWasDark=wasDark,eventShadowAt=shadowAt,mapSeed=Game.MapSeed,lives=Lives,mode=Mode,time=Game.SimulationTime,stamina=Game.Player.Stamina,spaceId=Game.Current.id,point=Game.Data.Grid(Game.Player.transform.position),run=Content,inventory=Game.Items.Inventory.Select(i=>i.uid).ToArray(),equipped=Game.Items.Equipped?.uid,zones=Zones.ToArray(),inserted=Game.Items.InsertedKeys.ToArray(),read=Read.ToArray(),bookStep=BookStep,specimen=Specimen,kitchenExamined=KitchenExamined,kitchenCleared=KitchenCleared,broken=broken.ToArray()};
            PlayerPrefs.SetString("SchoolEscape.Native017",JsonUtility.ToJson(d));PlayerPrefs.Save();Feedback="현재 판을 저장했습니다.";
        }
        public void Load()
        {
            if(!PlayerPrefs.HasKey("SchoolEscape.Native017"))return;var d=JsonUtility.FromJson<Saved>(PlayerPrefs.GetString("SchoolEscape.Native017"));if(d?.version!=17)return;
            Game.Restart(d.mapSeed);Content=d.run;Game.Items.LoadNative(Content.items,Content.sources,Content.defs,Content.seed);Game.Items.RestoreCarry(d.inventory,d.equipped,d.inserted);Zones.Clear();foreach(var z in d.zones)Zones.Add(z);Read.Clear();foreach(var id in d.read)Read.Add(id);Mode=d.mode;Lives=d.lives;BookStep=d.bookStep;Specimen=d.specimen;KitchenExamined=d.kitchenExamined;KitchenCleared=d.kitchenCleared;Game.Player.MaximumStamina=MaximumStamina;Game.Player.SetStamina(d.stamina);Game.RestoreClock(d.time);Game.RestorePosition(d.spaceId,d.point);if(d.eventStateSaved){lastSpace=d.spaceId;elapsed=d.eventElapsed;blackouts=d.eventBlackouts;wasDark=d.eventWasDark;shadowAt=d.eventShadowAt;BuildChains();}foreach(var n in d.broken)broken.Add(n);if(d.broken.Length>0){BuildRubble();SyncRubble();}Panel="";Game.SetPaused(false);
        }
        public bool Draw()
        {
            var style=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true};GUI.skin.label=style;GUI.skin.button.fontSize=16;
            if(Game.Started){GUI.Label(new Rect(24,244,350,30),"플레이 시간 "+TimeSpan.FromSeconds(Game.SimulationTime).ToString(@"hh\:mm\:ss")+" · 목숨 "+Lives);if(Dark){GUI.color=Color.black;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;}
                if(Game.Current.subjectEvent=="art"&&Event.current.type==EventType.Repaint){var center=Game.Data.Grid(Game.Player.transform.position);var facing=new Vector2(Game.Player.Visual.forward.x,Game.Player.Visual.forward.z);Plane plane=new Plane(Vector3.up,Vector3.zero);GUI.color=new Color(.01f,.025f,.035f);const int size=24;for(int y=0;y<Screen.height;y+=size)for(int x=0;x<Screen.width;x+=size){Ray ray=Game.View.ScreenPointToRay(new Vector3(x+size*.5f,Screen.height-y-size*.5f));if(plane.Raycast(ray,out float t)){var point=Game.Data.Grid(ray.GetPoint(t));var delta=point-center;if(delta.magnitude>1&&(Vector2.Dot(delta.normalized,facing)<.5f||!Sight(center,point)))GUI.DrawTexture(new Rect(x,y,size,size),Texture2D.whiteTexture);}}GUI.color=Color.white;}}
            bool lobby=!Game.Started,dead=Game.Caught;
            if(!lobby&&!dead&&Panel==""&&!Game.Paused)return false;
            float width=Mathf.Min(760,Screen.width-36),height=Mathf.Min(780,Screen.height-40);Rect rect=new Rect((Screen.width-width)/2,(Screen.height-height)/2,width,height);GUI.Box(rect,"");GUILayout.BeginArea(new Rect(rect.x+22,rect.y+18,width-44,height-36));scroll=GUILayout.BeginScrollView(scroll);
            if(lobby){GUILayout.Label("폐교탈출",new GUIStyle(style){fontSize=36});GUILayout.Space(20);if(settings){DrawSettings();if(GUILayout.Button(english?"Back":"뒤로"))settings=false;}else{if(GUILayout.Button(english?"Start game":"게임 시작",GUILayout.Height(55))){NewRun();Game.BeginRun();}if(PlayerPrefs.HasKey("SchoolEscape.Native017")&&GUILayout.Button(english?"Continue saved game":"저장한 판 계속하기",GUILayout.Height(45)))Load();GUILayout.Label(english?"Difficulty":"난이도");Mode=GUILayout.SelectionGrid(Mode,english?new[]{"Easy","Normal","Hard","Extreme"}:new[]{"이지","노말","하드","익스트림"},4);if(GUILayout.Button(english?"Settings":"설정"))settings=true;if(GUILayout.Button(english?"Language: English":"언어: 한국어"))english=!english;GUILayout.Label("이동 WASD · 달리기 Shift · 조사/문 E · 인벤토리 I\n게임 내 문서와 방 이름은 한국어입니다.");}}
            else if(dead){GUILayout.Label("붙잡혔습니다 · 남은 목숨 "+Lives);if(Lives>0&&GUILayout.Button("다시 일어나기",GUILayout.Height(50)))Revive();if(GUILayout.Button("새 판 시작"))Game.Restart();}
            else if(Panel==""){GUILayout.Label(english?"Paused":"잠시 멈춤");if(GUILayout.Button(english?"Resume":"계속하기 · Esc",GUILayout.Height(50)))Game.SetPaused(false);if(GUILayout.Button("현재 판 저장"))Save();if(GUILayout.Button("가이드북 · 별도 보관"))Open("guide");DrawSettings();if(GUILayout.Button("새 판 시작"))Game.Restart();}
            else{
                GUILayout.BeginHorizontal();GUILayout.Label(Panel=="inventory"?"소지품 · "+Game.Items.Inventory.Count+" / 9":Panel=="guide"?"가이드북":Panel=="lock"?activeLock.label+" · 잠금 해제":target?.label??"조사");if(GUILayout.Button("닫기 · E / Esc",GUILayout.Width(160)))Close();GUILayout.EndHorizontal();
                if(Panel=="lock")DrawLock();else if(Panel=="search")DrawSearch();else if(Panel=="inventory")DrawInventory();else if(Panel=="guide")DrawGuide();
            }
            GUILayout.Space(12);GUILayout.Label(Feedback);GUILayout.EndScrollView();GUILayout.EndArea();return true;
        }
        void DrawSettings(){GUILayout.Label(english?"Master volume":"전체 음량");Game.Audio.SetVolume(GUILayout.HorizontalSlider(Game.Audio.Volume,0,1));bool mute=GUILayout.Toggle(Game.Audio.Muted,english?"Mute":"음소거");if(mute!=Game.Audio.Muted)Game.Audio.SetMuted(mute);}
        void DrawLock()
        {
            var l=activeLock;if(l==null)return;foreach(var d in Content.defs.Where(d=>d.type=="note"&&d.lockId==l.id&&Has(d.id)))GUILayout.Label(d.description);
            GUILayout.Label(lockInput.PadRight(l.type=="code6"?6:4,'_'),new GUIStyle(GUI.skin.label){fontSize=32,alignment=TextAnchor.MiddleCenter});
            string[] keys=l.type=="direction"?new[]{"","↑","","←","","→","","↓",""}:new[]{"1","2","3","4","5","6","7","8","9","⌫","0","C"};
            for(int n=0;n<keys.Length;n+=3){GUILayout.BeginHorizontal();for(int k=n;k<Math.Min(n+3,keys.Length);k++){string key=keys[k];GUI.enabled=key!="";if(GUILayout.Button(key,GUILayout.Height(48))){Game.Audio.Cue("keypad",true);if(key=="C")lockInput="";else if(key=="⌫"){if(lockInput.Length>0)lockInput=lockInput.Substring(0,lockInput.Length-1);}else if(lockInput.Length<l.answer.Length)lockInput+=key;}GUI.enabled=true;}GUILayout.EndHorizontal();}
            if(l.type=="direction"&&GUILayout.Button("입력 지우기"))lockInput="";
            if(GUILayout.Button("잠금 해제",GUILayout.Height(50))){if(lockInput==l.answer){Unlock(l);Feedback="잠금이 풀렸습니다. E로 닫고 문으로 들어가세요.";}else Feedback="입력이 다릅니다. 소지한 쪽지의 대상 방 이름과 번호를 확인하세요.";}
        }
        void DrawSearch()
        {
            if(target==null)return;var source=target.source;
            foreach(var d in Content.documents.Where(d=>d.sourceId==source?.id&&string.IsNullOrEmpty(d.itemId))){if(GUILayout.Button(d.title)){Read.Add(d.id);currentDocument=d;documentText=d.text;if(d.roster!=null&&d.roster.Length>0)documentText=d.classLabel+"\n번호       이름          역할\n"+string.Join("\n",d.roster.Select(s=>s.no.ToString("00")+"        "+s.name+"       "+(s.role=="학생"?"—":s.role)));}}
            DrawRecord();
            var fixture=Game.Current.fixtures.FirstOrDefault(f=>f.id==source?.fixtureId);
            if(!string.IsNullOrEmpty(fixture?.bookCode)){GUILayout.Label("대출대 반납 기록의 순서대로 같은 분류 서가를 찾아 반납하세요. "+BookStep+" / 3");if(GUILayout.Button(BookName(fixture.bookCode)+" · "+fixture.bookCode+" 반납하기"))ReturnBook(fixture.bookCode);}
            if(source!=null&&source.compartments>1){var labels=Enumerable.Range(0,source.compartments).Select(n=>source.compartmentNames!=null&&n<source.compartmentNames.Length?source.compartmentNames[n]:"공용 보관함 "+(n+1)).ToArray();int selected=GUILayout.SelectionGrid(compartment,labels,4);if(selected!=compartment){compartment=selected;Game.Audio.Cue("cabinet",true);}}
            var found=Game.Items.WorldItems.Where(i=>!i.picked&&!i.consumed&&(target.item!=null?i.uid==target.item.uid:i.sourceId==source?.id&&i.compartment==compartment)).ToArray();
            for(int n=0;n<9;n+=3){GUILayout.BeginHorizontal();for(int k=n;k<n+3;k++){var i=k<found.Length?found[k]:null;GUI.enabled=i!=null;if(GUILayout.Button(i==null?"빈칸":ItemInventory.Name(i.id)+" · 가져오기",GUILayout.Height(70))&&i!=null){Game.Items.Pickup(i);if(i.id=="lab-sample"&&i.picked)Specimen=true;}GUI.enabled=true;}GUILayout.EndHorizontal();}
        }
        void DrawInventory()
        {
            var inv=Game.Items.Inventory;for(int n=0;n<9;n+=3){GUILayout.BeginHorizontal();for(int k=n;k<n+3;k++)if(GUILayout.Button(k<inv.Count?ItemInventory.Name(inv[k].id):"빈칸",GUILayout.Height(65)))inventorySelection=k;GUILayout.EndHorizontal();}
            GUILayout.Label("착용 1칸 · "+(Game.Items.Equipped!=null?ItemInventory.Name(Game.Items.Equipped.id):"비어 있음"));if(Game.Items.Equipped!=null&&GUILayout.Button("착용 해제"))Game.Items.Unequip();
            if(inventorySelection<inv.Count){var item=inv[inventorySelection];var def=Content.defs.FirstOrDefault(d=>d.id==item.id);GUILayout.Label(def?.description??ItemInventory.Name(item.id));GUILayout.BeginHorizontal();if(GUILayout.Button("사용 / 착용"))Game.Items.Use(inventorySelection);if(GUILayout.Button("내려놓기"))Game.Items.Drop(inventorySelection);GUILayout.EndHorizontal();}
            if(GUILayout.Button("가이드북 · 가방 칸과 별도"))Open("guide");
        }
        void DrawGuide()
        {
            GUILayout.Label("지금 할 일",new GUIStyle(GUI.skin.label){fontSize=24});GUILayout.Label("열쇠를 찾을 때마다 운동장 교문에 하나씩 꽂으세요. 세 개를 모두 꽂으면 탈출합니다.");
            string next=new[]{"key-a","key-b","key-c"}.FirstOrDefault(id=>!Game.Items.InsertedKeys.Contains(id));if(next==null)GUILayout.Label("교문에서 E를 눌러 탈출하세요.");else if(Has(next))GUILayout.Label(ItemInventory.Name(next)+" 확보 · 운동장 교문에 꽂을 수 있습니다.");else if(Mode<2){var item=Game.Items.WorldItems.FirstOrDefault(i=>i.id==next&&!i.picked&&!i.consumed);var f=Game.Data.FindSpace(item?.spaceId);if(f!=null)GUILayout.Label(ItemInventory.Name(next)+" 단서 · "+(Mode==0?f.label+" · "+(item.hidden?"수납물":"바닥"):(f.isOutdoor?"바깥 흙과 나뭇잎 냄새가 난다.":"본관 "+f.sourceFloor+"층의 "+(f.kind.Contains("Restroom")?"물기와 타일이 있는 곳.":f.kind=="library"?"묵은 종이 냄새가 나는 곳.":"사람들이 머물던 공간."))));}
            if(!Has("master-key")&&Mode<2){var nextCard=Content.cards.FirstOrDefault(c=>!Zones.Contains(c.zone)&&Zones.Contains(c.prior));if(nextCard!=null){var item=Game.Items.WorldItems.FirstOrDefault(i=>i.id==nextCard.itemId&&!i.consumed);var source=item!=null?Game.Data.FindSpace(item.spaceId):null;GUILayout.Label(Has(nextCard.itemId)?ItemInventory.Name(nextCard.itemId)+" 확보 · 해당 층 입구로 이동하세요.":ItemInventory.Name(nextCard.itemId)+" 단서 · "+(Mode==0?source?.label:"지금 개방된 구역의 수납물과 바닥을 살펴보세요."));}}
            if(Game.Current.subjectEvent=="library"&&Mode<2)GUILayout.Label(BookStep>=3?"안쪽 반납 완료 보관함에서 손전등을 가져오세요.":!Read.Contains("library-return-log")?"입구 대출·반납 기록대에서 반납 기록을 읽으세요.":"다음 반납: "+BookName(Content.library[BookStep])+" · 분류 "+Content.library[BookStep]);
            GUILayout.Space(15);GUILayout.Label("발견한 기록");foreach(var d in Content.documents.Where(d=>string.IsNullOrEmpty(d.itemId)&&Read.Contains(d.id)))if(GUILayout.Button((d.classLabel??"")+" · "+d.title)){currentDocument=d;documentText=d.text;}DrawRecord();
        }
    }
}
