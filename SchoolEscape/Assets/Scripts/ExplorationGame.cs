using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace SchoolEscape
{
    public sealed class ExplorationGame : MonoBehaviour
    {
        public MapData Data { get; private set; }
        public FloorData Current { get; private set; }
        public string RoomId => Current!=null&&Current.isRoom?Current.id:string.Empty;
        public int RoomTransitions { get; private set; }
        public bool Opening { get; private set; }
        public bool Started => started;
        public bool Paused { get; private set; }
        public bool Won { get; private set; }
        public bool InventoryOpen { get; private set; }
        public ItemInventory Items { get; private set; }
        public Vector2 ExitGatePoint=>new Vector2(Data.spawnX,Mathf.Max(1,Data.spawnY-3));
        public float SimulationTime { get; private set; }
        public bool Caught => Pursuer != null && Pursuer.Caught;
        public Pursuer Pursuer { get; private set; }
        public SchoolAudio Audio { get; private set; }
        public int ActiveSpaceCount { get { int count=0;foreach(var entry in spaces)if(entry.Value.activeSelf)count++;return count; } }
        public int Floor { get; private set; }
        public Explorer Player { get; private set; }
        public Camera View { get; private set; }
        public int Transitions { get; private set; }
        public bool AutomaticStairsEnabled=true;
        public bool ItemInteractionsEnabled=true;
        readonly Dictionary<string,GameObject> spaces = new Dictionary<string,GameObject>();
        readonly Dictionary<string,Transform> doorLeaves = new Dictionary<string,Transform>();
        readonly Dictionary<string,Vector3> doorSlides = new Dictionary<string,Vector3>();
        Coroutine openingRoutine;
        readonly Dictionary<string,Material> fixtureMaterials = new Dictionary<string,Material>();
        Material floorMat, wallMat, edgeMat, doorMat, metalMat, warmMat, glassMat, frameMat;
        Font font;
        string notice = "본관 탐방 · 교실과 특별실 내부를 탐방하세요";
        float noticeUntil, lastTransition = -10, lastRoomInteraction = -10;
        bool mapVisible, started, validationMode, inventoryPreviousPause;
        int inventorySelection=-1;
        StairData nearby;
        string arrivalLock;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<ExplorationGame>() == null)
                new GameObject("School Escape · Independent exploration").AddComponent<ExplorationGame>();
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            Data = MapData.Load();
            font = Resources.Load<Font>("SchoolFont");
            RenderSettings.ambientLight = new Color(.38f, .43f, .50f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .012f;
            RenderSettings.fogColor = new Color(.035f, .055f, .075f);
            floorMat = Mat(new Color(.42f, .43f, .39f), "FloorTexture");
            wallMat = Mat(new Color(.57f, .59f, .52f), "WallTexture");
            edgeMat = Mat(new Color(.14f, .23f, .23f));
            doorMat = Mat(new Color(.26f, .20f, .14f), "WallTexture");
            doorMat.color=new Color(.46f,.34f,.22f);doorMat.EnableKeyword("_EMISSION");doorMat.SetColor("_EmissionColor",new Color(.10f,.065f,.03f));
            metalMat = Mat(new Color(.23f, .29f, .30f));
            glassMat = Mat(new Color(.12f, .24f, .31f));
            frameMat = Mat(new Color(.66f, .65f, .55f));
            warmMat = Mat(new Color(.77f, .62f, .33f));
            warmMat.EnableKeyword("_EMISSION"); warmMat.SetColor("_EmissionColor", new Color(.42f, .30f, .12f));
            var light = new GameObject("Moonlight").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = .75f;
            light.color = new Color(.60f, .72f, .87f);
            light.transform.rotation = Quaternion.Euler(55, -30, 0);
            light.shadows = LightShadows.Soft;
            foreach (var f in Data.floors) {var root=BuildFloor(f);root.SetActive(false);spaces.Add(f.id,root);}
            CreatePlayer();
            View = new GameObject("Exploration camera").AddComponent<Camera>();
            View.tag = "MainCamera";
            View.orthographic = true; View.orthographicSize = 8.3f;
            View.nearClipPlane = .1f; View.farClipPlane = 100;
            View.backgroundColor = new Color(.025f, .04f, .055f);
            View.transform.rotation = Quaternion.Euler(57, 0, 0);
            View.gameObject.AddComponent<AudioListener>();
            SwitchFloor(Data.spawnFloor, Data.spawnX, Data.spawnY, false);
            SnapCamera();
            Audio=gameObject.AddComponent<SchoolAudio>();
            Pursuer=new GameObject("Caretaker · original pursuit").AddComponent<Pursuer>();
            Pursuer.Initialize(this);
            Items=gameObject.AddComponent<ItemInventory>();Items.Initialize(this);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-escapeValidate") >= 0)
            {
                validationMode=true;started = true; Player.InputEnabled = false;Pursuer.SimulationEnabled=false;ItemInteractionsEnabled=false;
                gameObject.AddComponent<RuntimeValidation>().Game = this;
            }
            else Player.InputEnabled = false;
        }

        Material Mat(Color c, string texture = null)
        {
            var m = new Material(Shader.Find("Standard")); m.color = c;
            m.SetFloat("_Glossiness", .08f);
            if (texture != null) m.mainTexture = Resources.Load<Texture2D>(texture);
            return m;
        }
        GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool solid = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) Destroy(go.GetComponent<Collider>());
            return go;
        }
        void FixtureCollider(GameObject go,FixtureData fixture)
        {
            // A solid footprint covers the entire furniture assembly in this
            // flat exploration controller, including a low easel's tall frame.
            // Otherwise CharacterController.stepOffset could step over its foot
            // even though both navigation and the web controller block it.
            var collider=go.AddComponent<BoxCollider>();
            float top=Mathf.Max(1.1f,fixture.elevation+fixture.height),scaleY=go.transform.localScale.y;
            collider.size=new Vector3(1,top/scaleY,1);
            collider.center=new Vector3(0,(top*.5f-go.transform.localPosition.y)/scaleY,0);
        }
        void Label(string text, Transform parent, Vector3 position, float size = .18f)
        {
            var go = new GameObject(text); go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.rotation = Quaternion.Euler(40, 0, 0);
            var tm = go.AddComponent<TextMesh>(); tm.text = text; tm.font = font;
            tm.fontSize = 48; tm.characterSize = size; tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(.88f, .88f, .76f);
            if (font != null) go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
        GameObject BuildFloor(FloorData f)
        {
            float t = Data.tileSize;
            var root = new GameObject(f.isRoom?"Room · "+f.id:"Main building · " + f.number + "F");
            if(!f.isRoom&&f.number==1)
                Label("교문 · 열쇠 A / B / C",root.transform,Data.World(ExitGatePoint.x,ExitGatePoint.y)+Vector3.up*1.9f,.15f);
            // Single ground collider; no holes or unsupported landing points.
            Vector3 groundCenter=Data.World((f.width-1)*.5f,(f.height-1)*.5f);groundCenter.y=-.15f;
            Box("Ground", root.transform, groundCenter, new Vector3(f.width*t+2, .3f, f.height*t+2), floorMat, true);
            // Walls follow shared navigation rows. Lower interior walls keep the
            // camera view clear while colliders retain their full height.
            for (int y = 0; y < f.height; y++)
            {
                for (int x = 0; x < f.width;)
                {
                    if (f.Open(x,y)) { x++; continue; }
                    int start = x;
                    bool cut=f.DoorCell(x,y),low=f.LowInteriorWall(x,y);
                    while (x < f.width && !f.Open(x,y)&&f.DoorCell(x,y)==cut&&f.LowInteriorWall(x,y)==low) x++;
                    float height=cut?.18f:low?.45f:1.7f;
                    Vector3 center = Data.World((start + x - 1) * .5f, y);
                    center.y = height*.5f;
                    var wall=Box(cut?"Door wall cutaway · collision retained":low?"Interior cutaway wall · collision retained":"Solid wall", root.transform, center, new Vector3((x-start)*t, height, t), wallMat, true);
                    if(cut||low)
                    {
                        var collider=wall.GetComponent<BoxCollider>();
                        collider.size=new Vector3(1,1.7f/height,1);
                        collider.center=new Vector3(0,(.85f-height*.5f)/height,0);
                    }
                }
            }
            for (int y = 0; y < f.height; y++) for (int x = 0; x < f.width; x++)
            {
                if (!f.Open(x,y)) continue;
                Vector3 p = Data.World(x,y);
                if ((x+y)%2 == 0) Box("Worn floor tile",root.transform,p+Vector3.up*.006f,new Vector3(t-.025f,.01f,t-.025f),floorMat);
                if ((x*17+y*11+f.number)%43==0) Box("Abandoned paper",root.transform,p+new Vector3(.13f,.02f,.19f),new Vector3(.22f,.012f,.30f),Mat(new Color(.66f,.65f,.55f)));
                // Corridor-facing plinths preserve edge contrast against the floor.
                int[] dx={-1,1,0,0}, dy={0,0,-1,1};
                for(int d=0;d<4;d++) if(!f.Open(x+dx[d],y+dy[d]))
                {
                    if(f.DoorCell(x+dx[d],y+dy[d]))continue;
                    var q=p+new Vector3(dx[d]*t*.49f,.22f,dy[d]*t*.49f);
                    Box("Green lower wall",root.transform,q,new Vector3(dx[d]!=0?.08f:t,.44f,dy[d]!=0?.08f:t),edgeMat);
                    if ((x+y)%5==0&&!f.LowInteriorWall(x+dx[d],y+dy[d]))
                    {
                        var wp = q + Vector3.up * .88f;
                        Box("Dusty window", root.transform, wp, new Vector3(dx[d]!=0?.09f:.75f,.5f,dy[d]!=0?.09f:.75f), glassMat);
                        Box("Window mullion", root.transform, wp, new Vector3(dx[d]!=0?.11f:.035f,.5f,dy[d]!=0?.11f:.035f), metalMat);
                    }
                }
                if (x%6==2 && y%6==2)
                {
                    Box("Emergency light bollard",root.transform,p+new Vector3(.42f,.18f,.42f),new Vector3(.14f,.36f,.14f),warmMat);
                    // Readable warm pools without hundreds of real-time lights.
                    Box("Pool of warm light",root.transform,p+Vector3.up*.015f,new Vector3(t*.85f,.008f,t*.85f),Mat(new Color(.48f,.44f,.32f)));
                }
            }
            foreach (var r in f.rooms)BuildDoor(r,root.transform,t,string.IsNullOrEmpty(r.interiorId)?f.id+"|"+r.label:r.interiorId);
            if(f.isRoom)BuildDoor(new RoomData{label="복도로 나가기",facing="north",doorX=f.exitX,doorY=f.exitY},root.transform,t,f.id+":exit");
            foreach(var fixture in f.fixtures??Array.Empty<FixtureData>())
            {
                string key=string.IsNullOrEmpty(fixture.color)?"#88765a":fixture.color;
                if(!fixtureMaterials.TryGetValue(key,out var material))
                {
                    Color color;if(!ColorUtility.TryParseHtmlString(key,out color))color=new Color(.52f,.46f,.36f);
                    material=Mat(color);fixtureMaterials.Add(key,material);
                }
                Vector3 p=Data.World(fixture.x,fixture.y);p.y=fixture.elevation+fixture.height*.5f;
                string name="Fixture · "+fixture.kind+" · "+fixture.room+" · "+fixture.id;
                GameObject fixtureObject;
                if(fixture.shape=="sphere"||fixture.shape=="cylinder")
                {
                    bool cylinder=fixture.shape=="cylinder";
                    var go=GameObject.CreatePrimitive(cylinder?PrimitiveType.Cylinder:PrimitiveType.Sphere);
                    fixtureObject=go;go.name=name;go.transform.SetParent(root.transform,false);go.transform.localPosition=p;
                    go.transform.localScale=new Vector3(fixture.width,fixture.height*(cylinder?.5f:1),fixture.depth);
                    go.GetComponent<Renderer>().sharedMaterial=material;
                    Destroy(go.GetComponent<Collider>());
                    // Authored furniture collision is rectangular in both
                    // runtimes even when the visual primitive is rounded.
                    if(fixture.solid)FixtureCollider(go,fixture);
                }
                else
                {
                    var go=Box(name,root.transform,p,new Vector3(fixture.width,fixture.height,fixture.depth),material);
                    fixtureObject=go;if(fixture.solid)FixtureCollider(go,fixture);
                }
                if(fixture.kind=="mirror-surface")PlanarMirror.Attach(fixtureObject);
                if(fixture.kind=="books"||(fixture.kind??"").Contains("book-"))
                {
                    var renderer=fixtureObject.GetComponent<Renderer>();
                    renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                }
            }
            foreach(var zone in f.interiorZones??Array.Empty<InteriorZoneData>())
            {
                var owner=Array.Find(f.rooms,r=>r.label==zone.room);
                if(((owner!=null&&owner.interiorKind=="music")||(f.isRoom&&f.kind=="music"))&&(zone.label=="보컬 연습실"||zone.label=="연결 통로"||zone.label=="악기보관창고"))
                    Label(zone.label,root.transform,Data.World(zone.x,zone.y+.65f)+Vector3.up*1.15f,.07f);
                // Small, nonblocking light marker makes room circulation legible.
                var lamp=new GameObject("Interior light · "+zone.label).AddComponent<Light>();
                lamp.transform.SetParent(root.transform,false);lamp.transform.localPosition=Data.World(zone.x,zone.y)+Vector3.up*2.8f;
                lamp.type=LightType.Point;lamp.range=6;lamp.intensity=.65f;lamp.color=new Color(.82f,.78f,.64f);
            }
            foreach (var s in f.stairs)
            {
                Vector3 p = Data.World(s.x,s.y);
                for(int i=0;i<6;i++)
                    Box("Stair tread",root.transform,p+new Vector3(0,.02f,-.54f+i*.19f),new Vector3(1.8f,.035f,.13f),metalMat);
                Box("Stair left handrail",root.transform,p+new Vector3(-.98f,.55f,0),new Vector3(.06f,.08f,1.6f),warmMat);
                Box("Stair right handrail",root.transform,p+new Vector3(.98f,.55f,0),new Vector3(.06f,.08f,1.6f),warmMat);
                Label(s.label,root.transform,p+Vector3.up*1.9f,.19f);
            }
            foreach(var b in f.barriers)
            {
                Vector3 p=Data.World(b.x,b.y);
                // Pad is at an enclosed corridor endpoint. Add a physical gate.
                bool lateral=b.x<5||b.x>43;
                Box("Out of beta · sealed connection",root.transform,p+Vector3.up*.65f,new Vector3(lateral?.22f:3.6f,1.3f,lateral?3.6f:.22f),metalMat,true);
                Label(b.label,root.transform,p+Vector3.up*1.7f,.15f);
            }
            return root;
        }

        void BuildDoor(RoomData r,Transform root,float t,string doorKey)
        {
                Vector3 outward = r.facing=="north"?Vector3.forward:r.facing=="south"?Vector3.back:r.facing=="west"?Vector3.left:Vector3.right;
                Vector3 p = Data.World(r.doorX,r.doorY)+outward*t*.51f;
                bool horizontal = Mathf.Abs(outward.z)>.5f;
                Vector3 size = new Vector3(horizontal?1.02f:.14f,1.55f,horizontal?.14f:1.02f);
                Vector3 tangent=horizontal?Vector3.right:Vector3.forward;
                Vector3 leaf=p;
                var assembly=new GameObject("Door leaf assembly · "+r.label).transform;
                assembly.SetParent(root,false);doorLeaves.Add(doorKey,assembly);doorSlides.Add(doorKey,tangent*1.12f);
                Box("Closed door · "+r.label,assembly,leaf+Vector3.up*.775f,size,doorMat,true);
                Vector3 postSize=new Vector3(horizontal?.12f:.36f,1.7f,horizontal?.36f:.12f);
                float nearHeight=horizontal?1.7f:.42f;
                Box("Projecting door jamb left",root,p+outward*.07f-tangent*.59f+Vector3.up*(nearHeight*.5f),new Vector3(postSize.x,nearHeight,postSize.z),frameMat);
                Box("Projecting door jamb right",root,p+outward*.07f+tangent*.59f+Vector3.up*.85f,postSize,frameMat);
                if(horizontal)Box("Visible door lintel",root,p+outward*.07f+Vector3.up*1.67f,new Vector3(1.3f,.12f,.36f),frameMat);
                Box("Door threshold",root,p+outward*.28f+Vector3.up*.025f,new Vector3(horizontal?1.4f:.65f,.035f,horizontal?.65f:1.4f),frameMat);
                Box("Door locator light",root,p+outward*.19f+Vector3.up*1.66f,new Vector3(horizontal?.24f:.06f,.065f,horizontal?.06f:.24f),warmMat);
                Box("Door glass",assembly,leaf+outward*.085f+Vector3.up*1.12f,new Vector3(horizontal?.55f:.03f,.38f,horizontal?.03f:.55f),glassMat);
                Box("Door glass back",assembly,leaf-outward*.085f+Vector3.up*1.12f,new Vector3(horizontal?.55f:.03f,.38f,horizontal?.03f:.55f),glassMat);
                Box("Handle",assembly,leaf+outward*.12f+Vector3.up*.72f,new Vector3(.10f,.06f,.10f),warmMat);
                Box("Handle back",assembly,leaf-outward*.12f+Vector3.up*.72f,new Vector3(.10f,.06f,.10f),warmMat);
                Box("Room nameplate",root,p+Vector3.up*1.81f,new Vector3(horizontal?1.35f:.13f,.28f,horizontal?.13f:1.35f),edgeMat);
                Label(r.label,root,p+outward*.22f+Vector3.up*1.95f,.115f);
        }

        void CreatePlayer()
        {
            var go = new GameObject("Explorer · new reference-based placeholder");
            var cc = go.AddComponent<CharacterController>();
            cc.height=1.68f; cc.radius=Data.playerRadius; cc.center=Vector3.up*.84f;
            cc.skinWidth=.025f; cc.stepOffset=.12f; cc.minMoveDistance=0;
            Player=go.AddComponent<Explorer>(); Player.Data=Data;Player.Game=this;
            var visual=new GameObject("Character"); visual.transform.SetParent(go.transform,false); Player.Visual=visual.transform;
            var cloth=Mat(new Color(.68f,.69f,.66f)); var trousers=Mat(new Color(.12f,.15f,.18f));
            var skin=Mat(new Color(.76f,.57f,.44f)); var hair=Mat(new Color(.035f,.045f,.055f));
            var shirt=Mat(new Color(.89f,.88f,.80f));
            Box("Jacket",visual.transform,new Vector3(0,1.02f,0),new Vector3(.47f,.52f,.28f),cloth);
            Box("Shirt",visual.transform,new Vector3(0,1.07f,.151f),new Vector3(.19f,.38f,.025f),shirt);
            Box("Tie",visual.transform,new Vector3(0,1.05f,.17f),new Vector3(.06f,.29f,.025f),trousers);
            Sphere("Head",visual.transform,new Vector3(0,1.52f,0),new Vector3(.43f,.46f,.40f),skin);
            Sphere("Black hair",visual.transform,new Vector3(0,1.65f,-.035f),new Vector3(.47f,.34f,.43f),hair);
            for(int i=0;i<5;i++) Box("Fringe",visual.transform,new Vector3((i-2)*.077f,1.59f-(i%2)*.035f,.17f),new Vector3(.09f,.16f,.09f),hair);
            for(int side=-1;side<=1;side+=2)
            {
                Box("Eye",visual.transform,new Vector3(side*.10f,1.51f,.195f),new Vector3(.055f,.055f,.025f),hair);
                Transform arm=Limb("Arm",visual.transform,new Vector3(side*.30f,1.23f,0),new Vector3(.15f,.40f,.17f),cloth);
                Box("Hand",arm,new Vector3(0,-.46f,0),new Vector3(.14f,.15f,.15f),skin);
                Transform leg=Limb("Thigh",visual.transform,new Vector3(side*.13f,.76f,0),new Vector3(.18f,.31f,.21f),trousers);
                Transform knee=Limb("Shin",leg,new Vector3(0,-.31f,0),new Vector3(.17f,.29f,.20f),trousers);
                Box("Shoe",knee,new Vector3(0,-.36f,.055f),new Vector3(.21f,.14f,.32f),hair);
                if(side<0){Player.LeftArm=arm;Player.LeftLeg=leg;Player.LeftKnee=knee;}else{Player.RightArm=arm;Player.RightLeg=leg;Player.RightKnee=knee;}
            }
            var glow=new GameObject("Personal visibility light").AddComponent<Light>();
            glow.transform.SetParent(go.transform,false);glow.transform.localPosition=Vector3.up*2.8f;
            glow.type=LightType.Point;glow.range=9;glow.intensity=2.5f;glow.color=new Color(1,.83f,.60f);
        }
        void Sphere(string name,Transform parent,Vector3 position,Vector3 scale,Material mat)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;
            g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=mat;Destroy(g.GetComponent<Collider>());
        }
        Transform Limb(string name,Transform parent,Vector3 pivot,Vector3 size,Material mat)
        {
            var go=new GameObject(name+" pivot");go.transform.SetParent(parent,false);go.transform.localPosition=pivot;
            Box(name,go.transform,new Vector3(0,-size.y*.5f,0),size,mat);return go.transform;
        }
        public bool CheckAutomaticStairs()
        {
            if(Current.isRoom||Opening||Caught||Won||Paused)return false;
            if(arrivalLock!=null)
            {
                var locked=Array.Find(Current.stairs,s=>s.id==arrivalLock);
                if(locked!=null&&Vector3.Distance(Data.World(locked.x,locked.y),new Vector3(Player.transform.position.x,0,Player.transform.position.z))<=Data.stairExitRadius)return false;
                arrivalLock=null;
            }
            if(SimulationTime-lastTransition<Data.stairCooldown)return false;
            foreach(var s in Current.stairs)
                if(Mathf.Abs(Player.transform.position.x-Data.World(s.x,s.y).x)<Data.stairEntryHalfWidth&&Mathf.Abs(Player.transform.position.z-Data.World(s.x,s.y).z)<Data.stairEntryHalfDepth)
                {
                    int previous=Floor;
                    SwitchFloor(s.target,s.arrivalX,s.arrivalY,true);
                    arrivalLock=Array.Find(Current.stairs,next=>next.target==previous)?.id;
                    return true;
                }
            return false;
        }
        void ActivateSpace(FloorData space,float x,float y)
        {
            if(!space.Clear(x,y,Data.tileSize,Data.playerRadius))throw new InvalidOperationException("Blocked space landing: "+space.id);
            if(!spaces.TryGetValue(space.id,out var active))
            {
                active=BuildFloor(space);active.SetActive(false);spaces.Add(space.id,active);
            }
            foreach(var entry in spaces)entry.Value.SetActive(entry.Key==space.id);
            Current=space;Floor=space.isRoom?space.sourceFloor:space.number;
            Player.Warp(Data.World(x,y));arrivalLock=null;nearby=null;
            Items?.SyncVisuals();
            Physics.SyncTransforms();
            if(View!=null)SnapCamera();
        }
        public void SwitchFloor(int target,int x,int y,bool count)
        {
            if(openingRoutine!=null){StopCoroutine(openingRoutine);openingRoutine=null;Opening=false;ResetDoorLeaves();}
            var from=Current;var source=Data.Grid(Player.transform.position);
            ActivateSpace(Data.floors[target-1],x,y);
            if(count&&from!=null)
            {
                Pursuer?.ObserveTransition(from.id,Current.id,source,new Vector2(x,y));
                Audio?.PlayStairs();
            }
            lastTransition=SimulationTime;if(count)Transitions++;
            notice=target+"층 · "+Current.subtitle;noticeUntil=SimulationTime+3;
        }
        static Vector2 Outward(RoomData room)=>room.facing=="north"?Vector2.up:room.facing=="south"?Vector2.down:room.facing=="west"?Vector2.left:Vector2.right;
        bool CloseTo(float x,float y,float range=1.6f)
        {
            Vector2 p=Data.Grid(Player.transform.position);
            return Vector2.Distance(p,new Vector2(x,y))*Data.tileSize<=range;
        }
        public RoomData NearbyRoom()
        {
            if(Current.isRoom)return null;
            RoomData nearest=null;float distance=1.6f;
            var p=Data.Grid(Player.transform.position);
            foreach(var room in Current.rooms)
            {
                if(string.IsNullOrEmpty(room.interiorId))continue;
                Vector2 target=new Vector2(room.doorX,room.doorY)+Outward(room);
                float d=Vector2.Distance(p,target)*Data.tileSize;
                if(d<=distance){distance=d;nearest=room;}
            }
            return nearest;
        }
        public bool CanExitRoom()=>Current.isRoom&&CloseTo(Current.exitX,Current.exitY+1);
        void ResetDoorLeaves(){foreach(var leaf in doorLeaves.Values)leaf.localPosition=Vector3.zero;}
        IEnumerator OpenPortal(FloorData destination,int x,int y,string doorKey,string message)
        {
            doorLeaves.TryGetValue(doorKey,out var leaf);doorSlides.TryGetValue(doorKey,out var slide);
            var from=Current;var source=Data.Grid(Player.transform.position);
            Audio?.PlayDoor(true);
            EmitNoise(from.id,source,6,"door");
            float startedAt=SimulationTime;
            while(SimulationTime-startedAt<.22f)
            {
                if(leaf!=null)leaf.localPosition=slide*Mathf.SmoothStep(0,1,Mathf.Clamp01((SimulationTime-startedAt)/.22f));
                yield return null;
            }
            ActivateSpace(destination,x,y);ResetDoorLeaves();RoomTransitions++;
            Pursuer?.ObserveTransition(from.id,destination.id,source,new Vector2(x,y));
            Audio?.PlayDoor(false);
            lastRoomInteraction=SimulationTime;Opening=false;openingRoutine=null;
            notice=message;noticeUntil=SimulationTime+3;
        }
        public bool EnterRoom(string id)
        {
            if(Caught||Won||Opening||Current.isRoom||SimulationTime-lastRoomInteraction<.6f)return false;
            var doorway=Array.Find(Current.rooms,r=>r.interiorId==id);
            var room=Array.Find(Data.roomMaps??Array.Empty<FloorData>(),r=>r.id==id&&r.sourceFloor==Floor);
            if(doorway==null||room==null)return false;
            Vector2 target=new Vector2(doorway.doorX,doorway.doorY)+Outward(doorway);
            if(!CloseTo(target.x,target.y))return false;
            Opening=true;openingRoutine=StartCoroutine(OpenPortal(room,room.spawnX,room.spawnY,id,room.label+" · 내부"));
            return true;
        }
        public bool ExitRoom()
        {
            if(Caught||Won||Opening||!CanExitRoom()||SimulationTime-lastRoomInteraction<.6f)return false;
            var room=Current;Opening=true;
            openingRoutine=StartCoroutine(OpenPortal(Data.floors[room.sourceFloor-1],room.returnX,room.returnY,room.id+":exit",room.sourceFloor+"층 복도"));return true;
        }
        public bool Interact()
        {
            if(Caught||Won||Opening||Paused)return false;
            if(ItemInteractionsEnabled&&NearExitGate())
            {
                if(Items.ConsumeEscapeKeys()){Won=true;Player.InputEnabled=false;Audio?.SetPaused(true);ShowNotice("탈출 성공");}
                else ShowNotice("교문 잠금 · 열쇠 A, B, C가 모두 필요합니다. ("+Items.KeyCount+"/3)");
                return true;
            }
            // Authored room exits retain priority when standing directly in the
            // threshold, so nearby rear lockers cannot trap the player inside.
            if(CanExitRoom())return ExitRoom();
            var room=NearbyRoom();if(room!=null&&EnterRoom(room.interiorId))return true;
            return InteractItems();
        }
        public bool InteractItems()=>ItemInteractionsEnabled&&!Caught&&!Won&&!Opening&&!Paused&&Items!=null&&Items.Interact();
        public bool NearExitGate()=>Current!=null&&!Current.isRoom&&Floor==1&&CloseTo(ExitGatePoint.x,ExitGatePoint.y,1.7f)&&
            (Items==null||Items.ClearSegment(Current,Data.Grid(Player.transform.position),ExitGatePoint));
        public void ShowNotice(string message){notice=message;noticeUntil=SimulationTime+4;}
        public void EmitNoise(string spaceId,Vector2 point,float radius,string kind)=>Pursuer?.HearNoise(spaceId,point,radius,kind);
        public void ToggleInventory()
        {
            if(Caught||Won||Opening)return;
            if(InventoryOpen){InventoryOpen=false;SetPaused(inventoryPreviousPause);}
            else{inventoryPreviousPause=Paused;InventoryOpen=true;inventorySelection=-1;SetPaused(true);}
        }
        void Update()
        {
            if(!started||Won)return;
            if(Input.GetKeyDown(KeyCode.I))ToggleInventory();
            if(Input.GetKeyDown(KeyCode.Escape)){if(InventoryOpen)ToggleInventory();else SetPaused(!Paused);}
            if(Paused||Won)return;
            SimulationTime+=Mathf.Min(Time.deltaTime,.05f);
            Pursuer?.Tick(Mathf.Min(Time.deltaTime,.05f));
            if(Audio!=null)Audio.OnStairs=Array.Exists(Current.stairs,s=>Vector2.Distance(Data.Grid(Player.transform.position),new Vector2(s.x,s.y))*Data.tileSize<1.25f);
            Audio?.SetState(Player.Speed,Player.IsRunning,Player.Stamina,Pursuer!=null&&Pursuer.IsChasing,Caught);
            Audio?.SetWatchProximity(Items!=null&&Items.WatchEquipped,Pursuer!=null&&Pursuer.SimulationEnabled&&Pursuer.SpaceId==Current.id?Vector2.Distance(Data.Grid(Player.transform.position),Pursuer.Position)*Data.tileSize:float.PositiveInfinity);
            Items?.SyncVisuals();
            if(Caught)return;
            nearby=null;
            foreach(var s in Current.stairs)
                if(Vector3.Distance(Data.World(s.x,s.y),Player.transform.position)<1.55f)nearby=s;
            if(Input.GetKeyDown(KeyCode.Tab))mapVisible=!mapVisible;
            if(Input.GetKeyDown(KeyCode.E))Interact();
            if(Input.GetKeyDown(KeyCode.F))InteractItems();
        }
        public void Restart()
        {
            if(openingRoutine!=null){StopCoroutine(openingRoutine);openingRoutine=null;}
            Opening=false;Won=false;InventoryOpen=false;inventoryPreviousPause=false;inventorySelection=-1;ResetDoorLeaves();RoomTransitions=0;Transitions=0;SimulationTime=0;SetPaused(false);
            lastRoomInteraction=-10;mapVisible=false;arrivalLock=null;
            Player.ResetState();SwitchFloor(Data.spawnFloor,Data.spawnX,Data.spawnY,false);
            Pursuer.ResetPursuit();Player.InputEnabled=true;started=true;
            Items?.ResetRun();
            Audio?.ResetState();Audio?.StartAudio();notice="열쇠 A·B·C를 찾아 1층 현관 교문으로 돌아오세요.";noticeUntil=SimulationTime+4;
        }
        public void SetPaused(bool paused)
        {
            Paused=paused;Audio?.SetPaused(paused);
        }
        void OnApplicationFocus(bool focus){if(!focus&&started&&!validationMode)SetPaused(true);}
        void OnApplicationPause(bool pause){if(pause&&started&&!validationMode)SetPaused(true);}
        void SnapCamera()=>View.transform.position=Player.transform.position+new Vector3(0,16,-10.4f);
        void LateUpdate()
        {
            Vector3 goal=Player.transform.position+new Vector3(0,16,-10.4f);
            View.transform.position=Vector3.Lerp(View.transform.position,goal,1-Mathf.Exp(-10*Time.deltaTime));
        }
        void OnGUI()
        {
            GUI.skin.font=font;
            var text=new GUIStyle(GUI.skin.label){fontSize=18,normal={textColor=new Color(.86f,.88f,.82f)}};
            var title=new GUIStyle(text){fontSize=32};
            if(!started)
            {
                GUI.Box(new Rect(Screen.width*.5f-250,Screen.height*.5f-160,500,320),"");
                GUI.Label(new Rect(Screen.width*.5f-215,Screen.height*.5f-130,430,60),"폐교탈출",title);
                GUI.Label(new Rect(Screen.width*.5f-215,Screen.height*.5f-65,440,100),"열쇠 A·B·C를 찾아 현관 교문으로 돌아오세요.\nWASD 이동 · Shift 달리기 · E 조사/줍기/문\nI 인벤토리 · 계단 자동 이동 · Tab 안내도",text);
                if(GUI.Button(new Rect(Screen.width*.5f-215,Screen.height*.5f+60,430,50),"탐방 시작")){started=true;Player.InputEnabled=true;SetPaused(false);Audio.StartAudio();Pursuer.ResetPursuit();}
                return;
            }
            GUI.Box(new Rect(18,18,360,82),"");
            GUI.Label(new Rect(32,26,340,30),"폐교탈출    본관 "+Floor+"F",text);
            GUI.Label(new Rect(32,58,340,30),Current.isRoom?Current.label+" · 내부":Current.subtitle,new GUIStyle(text){fontSize=14});
            GUI.Label(new Rect(24,112,240,24),"스태미나 "+Mathf.CeilToInt(Player.Stamina)+(Player.Exhausted?" · 숨 고르기":""),new GUIStyle(text){fontSize=14});
            var oldColor=GUI.color;GUI.color=new Color(.12f,.18f,.18f);GUI.DrawTexture(new Rect(24,140,220,10),Texture2D.whiteTexture);
            GUI.color=Player.Exhausted?new Color(.70f,.37f,.26f):new Color(.62f,.74f,.55f);GUI.DrawTexture(new Rect(24,140,220*Player.Stamina/100,10),Texture2D.whiteTexture);GUI.color=oldColor;
            if(Audio!=null&&GUI.Button(new Rect(Screen.width-120,20,100,30),Audio.Muted?"소리 끔":"소리 켬"))Audio.SetMuted(!Audio.Muted);
            if(Pursuer!=null&&GUI.Button(new Rect(Screen.width-350,20,220,30),"개발 · 추적자 "+(Pursuer.SimulationEnabled?"ON":"OFF")))
            {Pursuer.SetEnabled(!Pursuer.SimulationEnabled);if(!Caught&&!Won)Player.InputEnabled=true;}
            GUI.Label(new Rect(24,160,350,26),"열쇠 "+(Items!=null?Items.KeyCount:0)+" / 3    인벤토리 "+(Items!=null?Items.Inventory.Count:0)+" / 9",new GUIStyle(text){fontSize=14});
            var guidance=Items?.Compass();
            if(guidance!=null)
            {
                string arrow=guidance.direction.sqrMagnitude<.01f?"●":Mathf.Abs(guidance.direction.x)>Mathf.Abs(guidance.direction.y)?(guidance.direction.x>0?"→":"←"):(guidance.direction.y>0?"↑":"↓");
                GUI.Label(new Rect(24,190,460,30),"나침반 "+arrow+" "+guidance.label,new GUIStyle(text){fontSize=14});
            }
            if(Items!=null&&Items.WatchEquipped)GUI.Label(new Rect(24,220,350,24),"착용 · 스마트워치",new GUIStyle(text){fontSize=13});
            if(Pursuer!=null&&Pursuer.IsChasing)GUI.Label(new Rect(Screen.width*.5f-150,22,450,35),"발소리가 가까워지고 있습니다",text);
            if(Caught)
            {
                GUI.Box(new Rect(Screen.width*.5f-260,Screen.height*.5f-115,520,230),"");
                GUI.Label(new Rect(Screen.width*.5f-220,Screen.height*.5f-85,450,50),"붙잡혔습니다",title);
                GUI.Label(new Rect(Screen.width*.5f-220,Screen.height*.5f-28,460,40),"문을 넘어도 추격이 이어집니다. 달릴 때를 골라보세요.",new GUIStyle(text){fontSize=15});
                if(GUI.Button(new Rect(Screen.width*.5f-220,Screen.height*.5f+40,440,50),"다시 시작"))Restart();
                return;
            }
            if(Won)
            {
                GUI.Box(new Rect(Screen.width*.5f-260,Screen.height*.5f-120,520,240),"");
                GUI.Label(new Rect(Screen.width*.5f-220,Screen.height*.5f-85,450,50),"탈출 성공",title);
                GUI.Label(new Rect(Screen.width*.5f-220,Screen.height*.5f-25,450,50),"세 개의 열쇠로 교문을 열었습니다.",text);
                if(GUI.Button(new Rect(Screen.width*.5f-220,Screen.height*.5f+45,440,45),"새로운 배치로 다시 시작"))Restart();
                return;
            }
            if(InventoryOpen){DrawInventory(text,title);return;}
            if(Paused)
            {
                GUI.Box(new Rect(Screen.width*.5f-230,Screen.height*.5f-100,460,200),"");
                GUI.Label(new Rect(Screen.width*.5f-190,Screen.height*.5f-70,400,40),"일시 정지",title);
                if(GUI.Button(new Rect(Screen.width*.5f-190,Screen.height*.5f+10,380,50),"계속하기 · Esc"))SetPaused(false);
                return;
            }
            GUI.Label(new Rect(24,Screen.height-62,Screen.width-40,50),"WASD 이동   Shift 달리기   E 조사/줍기/문   I 인벤토리   계단 자동   Tab 안내도   Esc 정지",text);
            var roomPrompt=NearbyRoom();
            var itemPrompt=Items?.Nearby();
            if(Opening)GUI.Label(new Rect(Screen.width*.5f-210,Screen.height-116,650,40),"문을 여는 중…",text);
            else if(NearExitGate())GUI.Label(new Rect(Screen.width*.5f-210,Screen.height-116,650,40),"E · 교문에서 열쇠 A·B·C 사용 ("+Items.KeyCount+"/3)",text);
            else if(CanExitRoom())GUI.Label(new Rect(Screen.width*.5f-210,Screen.height-116,650,40),"E · 문 열고 복도로 나가기"+(itemPrompt!=null?"  /  F · "+itemPrompt.label:""),text);
            else if(roomPrompt!=null)GUI.Label(new Rect(Screen.width*.5f-210,Screen.height-116,650,40),"E · "+roomPrompt.label+" 문 열고 입장"+(itemPrompt!=null?"  /  F · "+itemPrompt.label:""),text);
            else if(itemPrompt!=null)GUI.Label(new Rect(Screen.width*.5f-210,Screen.height-116,650,40),"E · "+itemPrompt.label,text);
            else if(nearby!=null)GUI.Label(new Rect(Screen.width*.5f-210,Screen.height-116,650,40),arrivalLock==nearby.id?"계단 구역을 벗어난 뒤 다시 진입하세요":nearby.label+"  ·  계단 안으로 이동",text);
            else if(SimulationTime<noticeUntil)GUI.Label(new Rect(Screen.width*.5f-230,110,600,40),notice,text);
            if(mapVisible)DrawMap();
        }
        void DrawInventory(GUIStyle text,GUIStyle title)
        {
            float left=Screen.width*.5f-300,top=Screen.height*.5f-280;
            GUI.Box(new Rect(left,top,600,560),"");
            GUI.Label(new Rect(left+25,top+20,500,45),"인벤토리 "+Items.Inventory.Count+" / 9",title);
            if(GUI.Button(new Rect(left+510,top+24,65,30),"닫기 · I"))ToggleInventory();
            GUI.Label(new Rect(left+25,top+68,550,26),"인벤토리를 연 동안 이동·추격·시간이 멈춥니다.",new GUIStyle(text){fontSize=13});
            for(int n=0;n<9;n++)
            {
                int col=n%3,row=n/3;string label=n<Items.Inventory.Count?ItemInventory.Name(Items.Inventory[n].id):"빈 칸";
                if(GUI.Button(new Rect(left+25+col*183,top+104+row*82,175,72),(n+1)+" · "+label))inventorySelection=n<Items.Inventory.Count?n:-1;
            }
            GUI.Label(new Rect(left+25,top+359,550,28),"착용 1칸 · "+(Items.Equipped==null?"없음":ItemInventory.Name(Items.Equipped.id)),text);
            if(Items.Equipped!=null&&GUI.Button(new Rect(left+435,top+357,140,32),"착용 해제"))Items.Unequip();
            if(inventorySelection>=Items.Inventory.Count)inventorySelection=-1;
            if(inventorySelection>=0)
            {
                var selected=Items.Inventory[inventorySelection];GUI.Label(new Rect(left+25,top+406,550,30),ItemInventory.Name(selected.id),text);
                if(GUI.Button(new Rect(left+25,top+445,265,40),selected.id=="smartwatch"?"착용":"사용 / 정보"))Items.Use(inventorySelection);
                if(GUI.Button(new Rect(left+308,top+445,265,40),"현재 위치에 내려놓기"))Items.Drop(inventorySelection);
            }
            if(SimulationTime<noticeUntil)GUI.Label(new Rect(left+25,top+500,550,45),notice,new GUIStyle(text){fontSize=13,wordWrap=true});
        }
        void DrawMap()
        {
            float s=Mathf.Min(10,Mathf.Min((Screen.height-160f)/Current.height,(Screen.width-80f)/Current.width));
            float ox=Screen.width*.5f-Current.width*s*.5f,oy=110;
            GUI.Box(new Rect(ox-10,oy-10,Current.width*s+20,Current.height*s+20),"");
            var old=GUI.color;
            for(int y=0;y<Current.height;y++)for(int x=0;x<Current.width;x++)
                if(Current.Open(x,y)){GUI.color=new Color(.35f,.45f,.43f);GUI.DrawTexture(new Rect(ox+x*s,oy+(Current.height-1-y)*s,s,s),Texture2D.whiteTexture);}
            foreach(var st in Current.stairs){GUI.color=new Color(.85f,.63f,.28f);GUI.DrawTexture(new Rect(ox+st.x*s,oy+(Current.height-1-st.y)*s,s*1.5f,s*1.5f),Texture2D.whiteTexture);}
            Vector2 p=Data.Grid(Player.transform.position);GUI.color=Color.white;
            GUI.DrawTexture(new Rect(ox+p.x*s-3,oy+(Current.height-1-p.y)*s-3,6,6),Texture2D.whiteTexture);
            GUI.color=old;
        }
    }
}
