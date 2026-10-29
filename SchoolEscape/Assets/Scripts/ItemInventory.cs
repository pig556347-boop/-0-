using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SchoolEscape
{
    // Original prototype inventory and reachable loot placement. Every item has
    // one identity across carry, equipment, drops and consumption.
    public sealed class ItemInventory : MonoBehaviour
    {
        [Serializable] public sealed class Item
        {
            public string uid,id,spaceId,sourceId,targetKey;
            public Vector2 point;
            public bool hidden,revealed,picked,consumed;
            [NonSerialized] public GameObject visual;
            public int compartment;
        }
        [Serializable] public sealed class Searchable
        {
            public string id,spaceId,label,kind,fixtureId;
            public int compartments=1;
            public string[] compartmentNames;
            public Vector2 point;
            public bool searched;
        }
        public sealed class Candidate
        {
            public Item item;
            public Searchable source;
            public string label;
            public float distance;
        }
        public sealed class Guidance
        {
            public Vector2 direction;
            public string label,key;
        }
        sealed class Portal
        {
            public string from,to,label;
            public Vector2 point,arrival;
        }
        public readonly HashSet<string> InsertedKeys=new HashSet<string>();
        public static readonly Dictionary<string,string> Labels=new Dictionary<string,string>();
        public readonly List<Item> Inventory=new List<Item>();
        public readonly List<Item> WorldItems=new List<Item>();
        public readonly List<Searchable> Searchables=new List<Searchable>();
        public Item Equipped {get;private set;}
        public int Seed {get;private set;}
        public int Capacity => 9;
        public bool WatchEquipped=>Equipped!=null&&Equipped.id=="smartwatch";
        ExplorationGame game;
        readonly Dictionary<string,FloorData> maps=new Dictionary<string,FloorData>();
        readonly Dictionary<string,List<Vector2>> reachable=new Dictionary<string,List<Vector2>>();
        readonly List<Portal> portals=new List<Portal>();
        System.Random random;
        Material keyMat,drinkMat,otherMat;
        public void Initialize(ExplorationGame owner)
        {
            game=owner;
            maps.Clear();reachable.Clear();portals.Clear();Searchables.Clear();
            foreach(var f in game.Data.AllSpaces())maps[f.id]=f;
            foreach(var f in maps.Values)
            {
                reachable[f.id]=Reachable(f);
                foreach(var fixture in f.fixtures??Array.Empty<FixtureData>())
                {
                    if(!SearchableKind(fixture.kind))continue;
                    float rx=fixture.width/game.Data.tileSize*.5f+game.Data.playerRadius/game.Data.tileSize+.13f;
                    float ry=fixture.depth/game.Data.tileSize*.5f+game.Data.playerRadius/game.Data.tileSize+.13f;
                    Vector2? at=null;
                    foreach(var offset in new[]{new Vector2(0,-ry),new Vector2(rx,0),new Vector2(-rx,0),new Vector2(0,ry)})
                    {
                        Vector2 p=new Vector2(fixture.x,fixture.y)+offset;
                        if(!Clear(f,p))continue;
                        foreach(var n in reachable[f.id])
                            if(Vector2.Distance(p,n)<1.6f&&Segment(f,p,n)){at=p;break;}
                        if(at.HasValue)break;
                    }
                    if(at.HasValue)Searchables.Add(new Searchable{id=fixture.id,spaceId=f.id,point=at.Value,label=SearchLabel(fixture.kind)});
                }
                if(f.isRoom&&!f.noExit)portals.Add(new Portal{from=f.id,to=string.IsNullOrEmpty(f.returnSpaceId)?"floor-"+f.sourceFloor:f.returnSpaceId,point=new Vector2(f.exitX,f.exitY+1),arrival=new Vector2(f.returnX,f.returnY),label="복도 출입문"});
                foreach(var r in f.rooms??Array.Empty<RoomData>())if(!string.IsNullOrEmpty(r.interiorId))
                    portals.Add(new Portal{from=f.id,to=r.interiorId,point=new Vector2(r.doorX,r.doorY)+Outward(r.facing),arrival=new Vector2(maps[r.interiorId].spawnX,maps[r.interiorId].spawnY),label=r.label+" 출입문"});
                foreach(var s in f.stairs??Array.Empty<StairData>())portals.Add(new Portal{from=f.id,to=string.IsNullOrEmpty(s.targetId)?"floor-"+s.target:s.targetId,point=new Vector2(s.x,s.y),arrival=new Vector2(s.arrivalX,s.arrivalY),label=s.target+"층 계단"});
                foreach(var p in f.portals??Array.Empty<PortalData>())if(!p.locked)portals.Add(new Portal{from=f.id,to=p.targetId,point=new Vector2(p.doorX,p.doorY)+Outward(p.facing),arrival=new Vector2(p.arrivalX,p.arrivalY),label=p.label});
            }
            keyMat=Material(new Color(.77f,.63f,.31f));drinkMat=Material(new Color(.41f,.73f,.68f));otherMat=Material(new Color(.72f,.67f,.51f));
            ResetRun();
        }
        static Vector2 Outward(string face)=>face=="north"?Vector2.up:face=="south"?Vector2.down:face=="west"?Vector2.left:Vector2.right;
        static bool SearchableKind(string kind)
        {
            if(string.IsNullOrEmpty(kind))return false;
            return kind=="yard-trash-bin"||kind=="yard-parcel-box"||kind=="yard-water-station"||kind.EndsWith("desk",StringComparison.Ordinal)||kind.EndsWith("cabinet",StringComparison.Ordinal)||kind.EndsWith("-table",StringComparison.Ordinal)||kind.EndsWith("-workbench",StringComparison.Ordinal)||kind.EndsWith("-crate",StringComparison.Ordinal)||kind=="locker-bank"||kind=="shelf-body";
        }
        static string SearchLabel(string kind)=>kind=="yard-trash-bin"?"쓰레기통":kind=="yard-parcel-box"?"택배 상자":kind=="yard-water-station"?"수도 주변":kind=="locker-bank"?"사물함":kind=="shelf-body"?"책장":kind.Contains("cabinet")?"캐비넷":kind.Contains("teacher")?"교탁":kind.EndsWith("-workbench",StringComparison.Ordinal)?"작업대":kind.EndsWith("-crate",StringComparison.Ordinal)?"보관 상자":"책상";
        static Material Material(Color color){var m=new Material(Shader.Find("Standard"));m.color=color;m.SetFloat("_Glossiness",.12f);return m;}
        bool Clear(FloorData f,Vector2 p)
        {
            if(!f.Clear(p.x,p.y,game.Data.tileSize,game.Data.playerRadius))return false;
            foreach(var r in f.rooms??Array.Empty<RoomData>())
                if(Vector2.Distance(p,new Vector2(r.doorX,r.doorY))<.55f)return false;
            foreach(var portal in f.portals??Array.Empty<PortalData>())
                if(Vector2.Distance(p,new Vector2(portal.doorX,portal.doorY))<.55f)return false;
            return !f.isRoom||Vector2.Distance(p,new Vector2(f.exitX,f.exitY))>=.55f;
        }
        bool Segment(FloorData f,Vector2 a,Vector2 b)
        {
            int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)*game.Data.tileSize/.12f));
            for(int i=0;i<=n;i++)if(!Clear(f,Vector2.Lerp(a,b,i/(float)n)))return false;
            return true;
        }
        public bool ClearSegment(FloorData f,Vector2 a,Vector2 b)=>Segment(f,a,b);
        List<Vector2> Reachable(FloorData f)
        {
            var start=new Vector2Int(f.spawnX,f.spawnY);var list=new List<Vector2Int>();var seen=new HashSet<Vector2Int>();
            if(!Clear(f,start))
                for(int y=0;y<f.height&&list.Count==0;y++)for(int x=0;x<f.width;x++)if(Clear(f,new Vector2(x,y))){start=new Vector2Int(x,y);list.Add(start);break;}
            if(list.Count==0)list.Add(start);seen.Add(start);
            for(int i=0;i<list.Count;i++)foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})
            {var next=list[i]+d;if(seen.Contains(next)||!Clear(f,next)||!Segment(f,list[i],next))continue;seen.Add(next);list.Add(next);}
            var result=new List<Vector2>();foreach(var p in list)result.Add(p);return result;
        }
        public void ResetRun(int? seed=null)
        {
            Seed=seed??Guid.NewGuid().GetHashCode();random=new System.Random(Seed);
            foreach(var i in WorldItems)if(i.visual!=null)Destroy(i.visual);
            WorldItems.Clear();Inventory.Clear();Equipped=null;foreach(var s in Searchables)s.searched=false;
            // Every run keeps three unique letters, at least one key in the
            // playground and one in the main building, with both hiding modes.
            if(maps.ContainsKey("playground"))
            {
                var letters=new List<string>{"key-a","key-b","key-c"};
                var regions=new List<string>{"playground","main",random.Next(2)==0?"playground":"main"};
                var modes=new List<bool>{true,false,random.Next(2)==0};
                Shuffle(letters);Shuffle(regions);Shuffle(modes);
                for(int n=0;n<3;n++)Spawn(letters[n],regions[n],modes[n]);
            }
            else
            {
                bool firstHidden=random.Next(2)==0;
                Spawn("key-a",1,firstHidden);Spawn("key-b",2,!firstHidden);Spawn("key-c",3,random.Next(2)==0);
            }
            for(int n=0;n<3;n++){Spawn("vitamin-water",n+1,random.Next(2)==0);Spawn("chocolate-bar",n+1,random.Next(2)==0);}
            Spawn("clock-second-hand",1,random.Next(2)==0);Spawn("clock-minute-hand",2,random.Next(2)==0);Spawn("clock-body",3,random.Next(2)==0);
            Spawn("smartwatch",random.Next(1,4),random.Next(2)==0);
            SyncVisuals();
        }
        void Shuffle<T>(List<T> values)
        {
            for(int n=values.Count-1;n>0;n--){int other=random.Next(n+1);T value=values[n];values[n]=values[other];values[other]=value;}
        }
        void Spawn(string id,int floor,bool hidden)=>Spawn(id,floor.ToString(),hidden);
        bool SelectedSpace(FloorData f,string region)
        {
            if(region=="main")return !f.isOutdoor&&f.building!="annex"&&f.building!="west"&&f.id!="playground";
            int floor;if(int.TryParse(region,out floor))return !f.isOutdoor&&FloorOf(f)==floor;
            return f.id==region;
        }
        bool SafeKeyLocation(FloorData f,Vector2 p)
        {
            foreach(var edge in portals)
            {
                if(edge.from==f.id&&Vector2.Distance(p,edge.point)*game.Data.tileSize<4)return false;
                if(edge.to==f.id&&Vector2.Distance(p,edge.arrival)*game.Data.tileSize<4)return false;
            }
            var gate=game.Data.escapeGate;
            return gate==null||gate.spaceId!=f.id||Vector2.Distance(p,new Vector2(gate.x,gate.y))*game.Data.tileSize>=4;
        }
        void Spawn(string id,string region,bool hidden)
        {
            var item=new Item{uid=Seed+":"+WorldItems.Count,id=id};bool key=id.StartsWith("key-",StringComparison.Ordinal);
            var options=Searchables.FindAll(s=>SelectedSpace(maps[s.spaceId],region)&&!WorldItems.Exists(i=>i.sourceId==s.id)&&(!key||SafeKeyLocation(maps[s.spaceId],s.point)));
            if(hidden&&key&&maps.ContainsKey("playground")&&options.Count==0)throw new InvalidOperationException("No safe hidden native key location in "+region);
            if(hidden&&options.Count>0)
            {var source=options[random.Next(options.Count)];item.spaceId=source.spaceId;item.point=source.point;item.sourceId=source.id;item.hidden=true;}
            else
            {
                var choices=new List<Tuple<string,Vector2>>();
                foreach(var f in maps.Values)if(SelectedSpace(f,region))
                    foreach(var p in reachable[f.id])
                        if(!NearPortal(f,p)&&(!key||SafeKeyLocation(f,p))&&!WorldItems.Exists(i=>i.spaceId==f.id&&Vector2.Distance(i.point,p)<.8f))choices.Add(Tuple.Create(f.id,p));
                if(choices.Count==0)throw new InvalidOperationException("No reachable native item location in "+region);
                var chosen=choices[random.Next(choices.Count)];item.spaceId=chosen.Item1;item.point=chosen.Item2;
            }
            WorldItems.Add(item);CreateVisual(item);
        }
        static int FloorOf(FloorData f)=>f.isRoom?f.sourceFloor:f.number;
        bool NearPortal(FloorData f,Vector2 p)
        {
            foreach(var edge in portals)if(edge.from==f.id&&Vector2.Distance(p,edge.point)*game.Data.tileSize<2.2f)return true;
            return false;
        }
        void CreateVisual(Item item)
        {
            var root=new GameObject("Loot · "+item.uid);root.transform.SetParent(transform,false);root.transform.position=game.Data.World(item.point.x,item.point.y);
            bool key=item.id.StartsWith("key-",StringComparison.Ordinal);var body=GameObject.CreatePrimitive(key?PrimitiveType.Sphere:PrimitiveType.Cube);
            body.transform.SetParent(root.transform,false);body.transform.localPosition=new Vector3(0,.14f,0);body.transform.localScale=key?new Vector3(.21f,.06f,.21f):new Vector3(.22f,.23f,.17f);
            body.GetComponent<Renderer>().sharedMaterial=key?keyMat:item.id=="vitamin-water"?drinkMat:otherMat;Destroy(body.GetComponent<Collider>());
            if(key)
            {
                var stem=GameObject.CreatePrimitive(PrimitiveType.Cube);stem.transform.SetParent(root.transform,false);stem.transform.localPosition=new Vector3(.13f,.14f,0);stem.transform.localScale=new Vector3(.22f,.045f,.055f);
                stem.GetComponent<Renderer>().sharedMaterial=keyMat;Destroy(stem.GetComponent<Collider>());
            }
            var tag=new GameObject("Loot label");tag.transform.SetParent(root.transform,false);tag.transform.localPosition=new Vector3(0,.40f,0);tag.transform.rotation=Quaternion.Euler(40,0,0);
            var tm=tag.AddComponent<TextMesh>();tm.text=Name(item.id);tm.font=Resources.Load<Font>("SchoolFont");tm.fontSize=36;tm.characterSize=.07f;tm.anchor=TextAnchor.MiddleCenter;tm.color=new Color(.9f,.83f,.6f);
            if(tm.font!=null)tag.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;
            item.visual=root;
        }
        public void SyncVisuals()
        {
            foreach(var item in WorldItems)
            {
                if(item.visual==null)continue;
                item.visual.transform.position=game.Data.World(item.point.x,item.point.y);
                item.visual.SetActive(game.Current!=null&&item.spaceId==game.Current.id&&!item.picked&&!item.consumed&&!item.hidden);
            }
        }
        public Candidate Nearby()
        {
            if(game.Current==null)return null;Vector2 p=game.Data.Grid(game.Player.transform.position);Candidate best=null;
            foreach(var i in WorldItems)
            {
                if(i.spaceId!=game.Current.id||i.picked||i.consumed||i.hidden)continue;
                float distance=Vector2.Distance(p,i.point)*game.Data.tileSize;
                if(distance<=1.55f&&Segment(game.Current,p,i.point)&&(best==null||distance<best.distance))best=new Candidate{item=i,label=Name(i.id)+" 줍기",distance=distance};
            }
            if(best!=null)return best;
            foreach(var s in Searchables)
            {
                if(s.spaceId!=game.Current.id)continue;float distance=Vector2.Distance(p,s.point)*game.Data.tileSize;
                if(distance<=1.55f&&Segment(game.Current,p,s.point)&&(best==null||distance<best.distance))best=new Candidate{source=s,label=s.label+(s.searched?" 다시 확인":" 조사"),distance=distance};
            }
            return best;
        }
        public bool Interact()
        {
            var near=Nearby();if(near==null)return false;
            if(game.Native!=null)return game.Native.Search(near);
            if(near.item!=null){Pickup(near.item);return true;}
            var source=near.source;source.searched=true;game.EmitNoise(source.spaceId,source.point,4,"search");
            var found=WorldItems.Find(i=>i.sourceId==source.id&&!i.consumed&&!i.picked);
            if(found==null){game.ShowNotice("조사했지만 아무것도 없습니다.");return true;}
            found.revealed=true;Pickup(found);return true;
        }
        bool Has(string id)=>Inventory.Exists(i=>i.id==id);
        bool IsClock(string id)=>id=="clock-second-hand"||id=="clock-minute-hand"||id=="clock-body";
        bool CompletesClock(Item item)
        {
            if(!IsClock(item.id))return false;
            return (item.id=="clock-second-hand"||Has("clock-second-hand"))&&(item.id=="clock-minute-hand"||Has("clock-minute-hand"))&&(item.id=="clock-body"||Has("clock-body"));
        }
        public bool Pickup(Item item)
        {
            var expired=WorldItems.FindAll(i=>i.id=="compass"&&i.targetKey==item.uid&&!i.consumed);
            int replaced=expired.FindAll(i=>Inventory.Contains(i)).Count;
            if(Inventory.Count-replaced>=9&&!CompletesClock(item)){game.ShowNotice("인벤토리가 가득 찼습니다. 아이템은 그 자리에 남아 있습니다.");SyncVisuals();return false;}
            item.picked=true;
            foreach(var compass in expired)
            {compass.consumed=true;Inventory.Remove(compass);}
            Inventory.Add(item);game.ShowNotice(Name(item.id)+" 획득");
            CraftClock();SyncVisuals();return true;
        }
        void CraftClock()
        {
            if(!Has("clock-second-hand")||!Has("clock-minute-hand")||!Has("clock-body"))return;
            foreach(var part in Inventory.FindAll(i=>IsClock(i.id))){part.consumed=true;Inventory.Remove(part);}
            var target=WorldItems.Find(i=>i.id.StartsWith("key-",StringComparison.Ordinal)&&!i.picked&&!i.consumed);
            var compass=new Item{uid=Seed+":compass",id="compass",picked=true,targetKey=target?.uid};WorldItems.Add(compass);Inventory.Add(compass);game.ShowNotice("시계 부품 세 개가 나침반으로 조립되었습니다.");
        }
        public bool Use(int index)
        {
            if(index<0||index>=Inventory.Count)return false;var item=Inventory[index];
            if(item.id=="smartwatch")return Equip(index);
            if(item.id=="vitamin-water"){game.Player.ApplyVitamin();game.ShowNotice("비타민워터 · 2초 동안 이동 속도 30% 증가");}
            else if(item.id=="chocolate-bar"){game.Player.RestoreStamina();game.ShowNotice("초코바 · 스태미나 즉시 회복");}
            else{game.ShowNotice(item.id.StartsWith("key-",StringComparison.Ordinal)?"열쇠 세 개를 모아 운동장 교문에서 E로 사용하세요.":item.id=="compass"?"나침반은 소지하는 동안 자동으로 방향을 표시합니다.":"시계 부품 세 개를 모으면 나침반을 자동 획득합니다.");return false;}
            item.consumed=true;Inventory.RemoveAt(index);return true;
        }
        public bool Equip(int index)
        {
            if(index<0||index>=Inventory.Count||Inventory[index].id!="smartwatch")return false;
            if(Equipped!=null){game.ShowNotice("이미 착용한 아이템이 있습니다.");return false;}
            Equipped=Inventory[index];Inventory.RemoveAt(index);game.ShowNotice("스마트워치 착용 · 추격자가 가까울수록 심박음이 빨라집니다.");return true;
        }
        public bool Unequip()
        {
            if(Equipped==null)return false;if(Inventory.Count>=9){game.ShowNotice("해제하려면 인벤토리 한 칸을 비워주세요.");return false;}
            Inventory.Add(Equipped);Equipped=null;return true;
        }
        public bool Drop(int index)
        {
            if(index<0||index>=Inventory.Count)return false;var item=Inventory[index];if(game.Current.subjectEvent=="science"&&(item.id=="lab-coat"||item.id=="flashlight")){game.ShowNotice("입장 장비는 과학실 밖에서 내려놓으세요.");return false;}Inventory.RemoveAt(index);
            item.spaceId=game.Current.id;item.point=game.Data.Grid(game.Player.transform.position);item.sourceId=null;item.hidden=false;item.revealed=true;item.picked=false;
            if(item.visual==null)CreateVisual(item);SyncVisuals();game.ShowNotice(Name(item.id)+" 내려놓음");return true;
        }
        public bool HasAllKeys()=>Has("key-a")&&Has("key-b")&&Has("key-c");
        public int KeyCount=>Inventory.FindAll(i=>i.id.StartsWith("key-",StringComparison.Ordinal)).Count;
        public bool ConsumeEscapeKeys()
        {
            if(!AtEscapeGate())return false;
            var keys=Inventory.FindAll(i=>new[]{"key-a","key-b","key-c"}.Contains(i.id));foreach(var key in keys){InsertedKeys.Add(key.id);key.consumed=true;Inventory.Remove(key);}
            if(keys.Count>0){game.Audio.Cue("unlock");game.ShowNotice("교문에 열쇠를 꽂았습니다. "+InsertedKeys.Count+" / 3 · 모두 꽂았으면 E로 탈출");return false;}return InsertedKeys.Count==3;
        }
        bool AtEscapeGate()
        {
            var gate=game.Data.escapeGate;
            if(gate==null||game.Current==null||game.Current.id!=gate.spaceId||game.Player==null)return false;
            Vector2 p=game.Data.Grid(game.Player.transform.position),target=new Vector2(gate.x,gate.y);
            return Vector2.Distance(p,target)*game.Data.tileSize<=gate.range&&Segment(game.Current,p,target);
        }
        public Guidance Compass()
        {
            var compass=Inventory.Find(i=>i.id=="compass");if(compass==null)return null;
            var target=WorldItems.Find(i=>i.uid==compass.targetKey&&!i.picked&&!i.consumed);if(target==null)return new Guidance{label="모든 열쇠를 확보했습니다",key=""};
            Vector2 point=target.point;string label="열쇠 방향";
            if(target.spaceId!=game.Current.id)
            {
                var q=new List<string>{game.Current.id};var previous=new Dictionary<string,Portal>{{game.Current.id,null}};
                for(int n=0;n<q.Count&&!previous.ContainsKey(target.spaceId);n++)foreach(var edge in portals)
                    if(edge.from==q[n]&&!previous.ContainsKey(edge.to)){previous[edge.to]=edge;q.Add(edge.to);}
                if(!previous.ContainsKey(target.spaceId))return new Guidance{label="방향을 찾을 수 없습니다",key=Name(target.id)};
                Portal first=previous[target.spaceId];while(first.from!=game.Current.id)first=previous[first.from];point=first.point;label=first.label+" 방향";
            }
            return new Guidance{direction=(point-game.Data.Grid(game.Player.transform.position)).normalized,label=label,key=Name(target.id)};
        }
        public List<Vector2> ReachablePoints(string id)=>reachable[id];
        public void LoadNative(Item[] items,Searchable[] sources,NativeRun017.Definition[] defs,int seed)
        {
            foreach(var i in WorldItems)if(i.visual!=null)Destroy(i.visual);WorldItems.Clear();Inventory.Clear();Equipped=null;InsertedKeys.Clear();Labels.Clear();foreach(var d in defs)Labels[d.id]=d.label;
            Seed=seed;WorldItems.AddRange(items);Searchables.Clear();Searchables.AddRange(sources);
            foreach(var i in WorldItems)CreateVisual(i);SyncVisuals();
        }
        public void Grant(string id){var i=new Item{uid=Guid.NewGuid().ToString(),id=id,picked=true};WorldItems.Add(i);Inventory.Add(i);}
        public void RestoreCarry(string[] ids,string wear,string[] inserted){Inventory.Clear();foreach(var id in ids){var i=WorldItems.Find(i=>i.uid==id);if(i!=null)Inventory.Add(i);}Equipped=WorldItems.Find(i=>i.uid==wear);InsertedKeys.Clear();foreach(var id in inserted)InsertedKeys.Add(id);SyncVisuals();}
        public void DropOnDeath(bool scatter){if(Equipped!=null){Inventory.Add(Equipped);Equipped=null;}var all=Inventory.ToArray();Inventory.Clear();var at=game.Data.Grid(game.Player.transform.position);var points=reachable[game.Current.id].Where(p=>Vector2.Distance(p,at)*game.Data.tileSize<=8).ToArray();foreach(var i in all){i.spaceId=game.Current.id;i.sourceId=null;i.hidden=false;i.revealed=true;i.picked=false;i.point=scatter&&points.Length>0?points[random.Next(points.Length)]:at;if(i.visual==null)CreateVisual(i);}SyncVisuals();}
        public static string Name(string id)
        {
            if(id!=null&&Labels.TryGetValue(id,out var label))return label;
            switch(id){case "key-a":return "열쇠 A";case "key-b":return "열쇠 B";case "key-c":return "열쇠 C";case "vitamin-water":return "비타민워터";case "chocolate-bar":return "초코바";case "clock-second-hand":return "시계초침";case "clock-minute-hand":return "시계분침";case "clock-body":return "시계본체";case "compass":return "나침반";case "smartwatch":return "스마트워치";default:return id;}
        }
    }
}
