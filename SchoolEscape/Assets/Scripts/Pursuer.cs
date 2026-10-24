using System;
using System.Collections.Generic;
using UnityEngine;

namespace SchoolEscape
{
    // Original finite-knowledge caretaker. A noise supplies only its origin;
    // hidden player coordinates are never used as a navigation destination.
    public sealed class Pursuer : MonoBehaviour
    {
        public ExplorationGame Game { get; private set; }
        public string SpaceId { get; private set; }
        public string State { get; private set; } = "patrol";
        public Vector2 Position { get; private set; }
        public float Speed { get; private set; }
        public float Phase { get; private set; }
        public int Transfers { get; private set; }
        public bool Caught => State == "caught";
        public bool IsChasing => State == "chase" || State == "search" || (State == "transit" && portalAlerted);
        public bool SimulationEnabled = true;
        public string InitialSpawnSpace { get; private set; }
        public Vector2 InitialSpawnPosition { get; private set; }
        public int SpawnCandidateCount { get; private set; }
        public string GiveUpReason { get; private set; }
        public const float Radius = .31f;
        public const float SightRange = 11, HearingWalk = 2, HearingRun = 8;
        Transform visual, torso, head;
        readonly List<Transform> arms = new List<Transform>(), legs = new List<Transform>();
        readonly List<Portal> portals = new List<Portal>();
        readonly Dictionary<string, bool[,]> navigation = new Dictionary<string, bool[,]>();
        readonly Dictionary<string, bool> edges = new Dictionary<string, bool>();
        List<Vector2> path;
        Vector2 pathGoal, lastKnown, direction = Vector2.down, patrolGoal, lastNoisePoint;
        string knownSpace, lastNoiseSpace, lastNoiseKind;
        float born, lastSeen = -1000, lastEvidence = -1000, lastNoiseTime = -1000;
        float repathAt, transitAt = -1, arrivalGrace, searchUntil = -1, unreachableSince = -1, dwellUntil;
        bool hasPatrolGoal, portalAlerted;
        int visitedPatrolTargets;
        System.Random random = new System.Random();
        sealed class Portal
        {
            public string from, to, kind;
            public Vector2 source, arrival;
            public float expires;
            public bool alerted;
        }
        sealed class Spawn
        {
            public FloorData map;
            public Vector2 point;
        }
        FloorData Map(string id)
        {
            foreach(var f in Game.Data.floors)if(f.id==id)return f;
            foreach(var f in Game.Data.roomMaps)if(f.id==id)return f;
            return null;
        }
        public void Initialize(ExplorationGame game)
        {
            Game=game;BuildVisual();ResetPursuit();
        }
        public void SetEnabled(bool enabled)
        {
            bool wasCaught=Caught;
            SimulationEnabled=enabled;
            ClearKnowledge();State="patrol";Speed=0;
            if(enabled)ResetPursuit();
            if(wasCaught&&!Game.Won)Game.Player.InputEnabled=Game.Started&&!Game.Paused;
            SyncVisual(0);
        }
        public void ResetPursuit(int? seed = null)
        {
            random=new System.Random(seed??Guid.NewGuid().GetHashCode());
            var candidates=new List<Spawn>();
            Vector2 player=Game.Data.Grid(Game.Player.transform.position);
            foreach(var f in Game.Data.floors)
            {
                foreach(var landmark in f.landmarks??Array.Empty<LandmarkData>())
                {
                    var point=new Vector2(landmark.x,landmark.y);
                    if(!NavCell(f,landmark.x,landmark.y))continue;
                    if(f.id==Game.Current.id&&Vector2.Distance(point,player)*Game.Data.tileSize<15)continue;
                    bool nearExit=false;
                    foreach(var stair in f.stairs??Array.Empty<StairData>())
                        if(Vector2.Distance(point,new Vector2(stair.x,stair.y))*Game.Data.tileSize<3.2f)nearExit=true;
                    foreach(var room in f.rooms??Array.Empty<RoomData>())
                        if(Vector2.Distance(point,new Vector2(room.doorX,room.doorY))*Game.Data.tileSize<2.5f)nearExit=true;
                    if(!nearExit)candidates.Add(new Spawn{map=f,point=point});
                }
            }
            // The fixed authored landmarks are the spawn pool. Fallback is only
            // for an incomplete edited map and still uses a collision-safe cell.
            if(candidates.Count==0)
            {
                foreach(var f in Game.Data.floors)
                    foreach(var p in Reachable(f,new Vector2(f.landmarks[0].x,f.landmarks[0].y)))
                        if(f.id!=Game.Current.id||Vector2.Distance(p,player)*Game.Data.tileSize>=15)
                            candidates.Add(new Spawn{map=f,point=p});
            }
            SpawnCandidateCount=candidates.Count;
            if(candidates.Count==0)throw new InvalidOperationException("No safe designated caretaker spawn");
            var chosen=candidates[random.Next(candidates.Count)];
            SpaceId=chosen.map.id;Position=chosen.point;InitialSpawnSpace=SpaceId;InitialSpawnPosition=Position;
            State="patrol";Speed=0;Phase=0;Transfers=0;ClearKnowledge();
            born=Game.SimulationTime;arrivalGrace=born+4;direction=Vector2.up;
            hasPatrolGoal=false;visitedPatrolTargets=0;patrolGoal=Position;dwellUntil=born+1+(float)random.NextDouble()*2;
            SyncVisual(0);
        }
        void ClearKnowledge()
        {
            lastSeen=lastEvidence=lastNoiseTime=-1000;knownSpace=null;lastNoiseSpace=null;lastNoiseKind=null;
            searchUntil=-1;unreachableSince=-1;GiveUpReason=null;
            portals.Clear();path=null;repathAt=0;transitAt=-1;portalAlerted=false;
        }
        public bool HearNoise(string spaceId,Vector2 point,float radius,string kind)
        {
            if(!SimulationEnabled||!Game.Started||Game.Paused||Game.Won||Caught||spaceId!=SpaceId||radius<=0)return false;
            var f=Map(SpaceId);
            float reach=radius*(SightClear(f,Position,point)?1:.5f);
            if(Vector2.Distance(Position,point)*Game.Data.tileSize>reach)return false;
            var acoustic=FindPath(f,Position,point);if(acoustic==null)return false;
            float routeLength=0;Vector2 prior=Position;
            foreach(var node in acoustic){routeLength+=Vector2.Distance(prior,node)*Game.Data.tileSize;prior=node;}
            if(routeLength>radius)return false;
            lastNoisePoint=point;lastNoiseSpace=spaceId;lastNoiseKind=kind;lastNoiseTime=Game.SimulationTime;
            lastEvidence=Game.SimulationTime;lastKnown=point;knownSpace=spaceId;searchUntil=-1;unreachableSince=-1;GiveUpReason=null;
            if(State!="chase")State="investigate";
            portals.Clear();portalAlerted=false;transitAt=-1;path=null;repathAt=0;
            return true;
        }
        public void ObserveTransition(string from,string to,Vector2 source,Vector2 arrival)
        {
            float now=Game.SimulationTime;
            if(!SimulationEnabled||Game.Won||Caught||from!=SpaceId||knownSpace!=from)return;
            if(State!="chase"&&State!="investigate"&&State!="search")return;
            bool seenExit=now-lastSeen<=2.5f&&Vector2.Distance(lastKnown,source)*Game.Data.tileSize<=2.5f;
            bool heardExit=now-lastNoiseTime<=1.5f&&lastNoiseSpace==from&&
                (lastNoiseKind=="door"||lastNoiseKind=="stairs"||lastNoiseKind=="run")&&
                Vector2.Distance(lastNoisePoint,source)*Game.Data.tileSize<=2.5f;
            if(!seenExit&&!heardExit)return;
            var a=Map(from);var b=Map(to);if(a==null||b==null)return;
            portals.Clear();portals.Add(new Portal{from=from,to=to,source=source,arrival=arrival,
                kind=a.isRoom||b.isRoom?"door":"stairs",expires=now+12,alerted=true});
            path=null;repathAt=0;portalAlerted=true;
        }
        public void Tick(float dt)
        {
            dt=Mathf.Clamp(dt,0,.05f);
            if(Game==null||!Game.Started||!SimulationEnabled){if(visual!=null)visual.gameObject.SetActive(false);return;}
            if(Game.Paused||Game.Won){Speed=0;return;}
            if(Caught){Speed=0;SyncVisual(0);return;}
            float now=Game.SimulationTime;var f=Map(SpaceId);if(f==null)return;
            Vector2 player=Game.Data.Grid(Game.Player.transform.position);
            bool same=Game.Current.id==SpaceId;
            Vector2 offset=player-Position;float distance=same?offset.magnitude*Game.Data.tileSize:float.PositiveInfinity;
            bool withinView=distance<=1.8f||offset.sqrMagnitude<.0001f||Vector2.Dot(direction.normalized,offset.normalized)>=.5f;
            bool sees=same&&distance<=SightRange&&withinView&&SightClear(f,Position,player);
            if(now-born>=1&&sees)
            {
                lastSeen=lastEvidence=now;lastKnown=player;knownSpace=SpaceId;State="chase";
                searchUntil=-1;unreachableSince=-1;GiveUpReason=null;
                portals.Clear();portalAlerted=false;transitAt=-1;
            }
            if((State=="chase"||State=="investigate"||State=="search"||portalAlerted)&&now-lastEvidence>=12)
                GiveUp("memory-expired");
            else if(!sees&&(State=="chase"||State=="investigate")&&now-lastEvidence>=8&&portals.Count==0)
            {State="search";searchUntil=Mathf.Min(now+4,lastEvidence+12);path=null;}
            Vector2 before=Position;string beforeSpace=SpaceId;
            if(portals.Count>0)
            {
                var portal=portals[0];
                if(portal.from!=SpaceId||portal.expires<now){if(portal.alerted)GiveUp("exit-evidence-expired");else portals.Clear();}
                else
                {
                    portalAlerted=portal.alerted;State=portal.alerted?"transit":"patrol";
                    MoveToward(f,portal.source,portal.alerted?3.3f:1.35f,dt);
                    if(Vector2.Distance(Position,portal.source)*Game.Data.tileSize<.55f)
                    {
                        if(transitAt<0)transitAt=now+(portal.kind=="stairs"?1.45f:1.25f);
                        if(now>=transitAt&&TryArrival(Map(portal.to),portal.arrival,out var landing))
                        {
                            SpaceId=portal.to;Position=landing;Transfers++;portals.RemoveAt(0);
                            path=null;repathAt=0;transitAt=-1;arrivalGrace=now+.8f;unreachableSince=-1;
                            if(portal.alerted){lastKnown=portal.arrival;knownSpace=SpaceId;State="investigate";searchUntil=-1;}
                            else{State="patrol";hasPatrolGoal=false;visitedPatrolTargets=0;dwellUntil=now+1+(float)random.NextDouble()*2;}
                            portalAlerted=false;
                        }
                    }
                }
            }
            else if(State=="chase"||State=="investigate"||State=="search")
            {
                if(knownSpace!=SpaceId)GiveUp("no-known-route");
                else
                {
                    float toKnown=Vector2.Distance(Position,lastKnown)*Game.Data.tileSize;
                    if(!sees&&toKnown<.55f)
                    {
                        if(State!="search"){State="search";searchUntil=Mathf.Min(now+4,lastEvidence+12);path=null;}
                        direction=new Vector2(Mathf.Sin(now*.8f),Mathf.Cos(now*.8f));
                        if(now>=searchUntil)GiveUp("last-location-searched");
                    }
                    else MoveToward(f,lastKnown,State=="chase"?3.3f:1.9f,dt);
                }
            }
            else Patrol(f,dt);
            Speed=beforeSpace==SpaceId?Vector2.Distance(before,Position)*Game.Data.tileSize/Mathf.Max(.001f,dt):0;
            Phase+=Speed*dt*3.6f;
            bool seeksKnown=(State=="chase"||State=="investigate")&&portals.Count==0;
            bool seeksPortal=portals.Count>0&&transitAt<0;
            Vector2 seeking=seeksPortal?portals[0].source:lastKnown;
            if((seeksKnown||seeksPortal)&&Vector2.Distance(Position,seeking)*Game.Data.tileSize>.6f&&Speed<.01f)
            {
                if(unreachableSince<0)unreachableSince=now;
                if(now-unreachableSince>=3)
                {
                    if(seeksKnown||portalAlerted)GiveUp("unreachable-last-location");
                    else{portals.Clear();State="patrol";path=null;hasPatrolGoal=false;dwellUntil=now+1;}
                }
            }
            else unreachableSince=-1;
            if(Game.Current.id==SpaceId&&now>=arrivalGrace&&now-born>=4&&!Game.Opening&&State=="chase"&&
                Vector2.Distance(Position,player)*Game.Data.tileSize<.7f&&SightClear(Map(SpaceId),Position,player))
            {State="caught";Speed=0;Game.Player.InputEnabled=false;}
            SyncVisual(dt);
        }
        void GiveUp(string reason)
        {
            State="patrol";GiveUpReason=reason;lastKnown=Position;knownSpace=null;
            portals.Clear();path=null;repathAt=0;transitAt=-1;portalAlerted=false;
            searchUntil=-1;unreachableSince=-1;hasPatrolGoal=false;
            dwellUntil=Game.SimulationTime+1+(float)random.NextDouble()*2;
        }
        void Patrol(FloorData f,float dt)
        {
            State="patrol";
            float now=Game.SimulationTime;if(now<dwellUntil)return;
            if(!hasPatrolGoal||Vector2.Distance(Position,patrolGoal)*Game.Data.tileSize<.45f)
            {
                if(hasPatrolGoal){hasPatrolGoal=false;visitedPatrolTargets++;dwellUntil=now+1+(float)random.NextDouble()*2;return;}
                if(visitedPatrolTargets>=2&&random.NextDouble()<.5)
                {
                    var exits=RoamExits(f);
                    if(exits.Count>0)
                    {portals.Add(exits[random.Next(exits.Count)]);visitedPatrolTargets=0;path=null;repathAt=0;return;}
                }
                var options=new List<Vector2>();
                foreach(var landmark in f.landmarks??Array.Empty<LandmarkData>())
                    if(NavCell(f,landmark.x,landmark.y)&&Vector2.Distance(Position,new Vector2(landmark.x,landmark.y))*Game.Data.tileSize>3)
                        options.Add(new Vector2(landmark.x,landmark.y));
                if(options.Count==0)
                    foreach(var p in Reachable(f,Position))if(Vector2.Distance(p,Position)*Game.Data.tileSize>4)options.Add(p);
                if(options.Count==0){dwellUntil=now+2;return;}
                patrolGoal=options[random.Next(options.Count)];hasPatrolGoal=true;path=null;repathAt=0;
            }
            MoveToward(f,patrolGoal,1.35f,dt);
            if(path==null){hasPatrolGoal=false;dwellUntil=now+1;}
        }
        List<Portal> RoamExits(FloorData f)
        {
            var exits=new List<Portal>();float expiry=Game.SimulationTime+180;
            if(f.isRoom)
            {
                exits.Add(new Portal{from=f.id,to=Game.Data.floors[f.sourceFloor-1].id,
                    source=new Vector2(f.exitX,f.exitY+1),arrival=new Vector2(f.returnX,f.returnY),kind="door",expires=expiry});
                return exits;
            }
            foreach(var s in f.stairs??Array.Empty<StairData>())
                exits.Add(new Portal{from=f.id,to=Game.Data.floors[s.target-1].id,source=new Vector2(s.x,s.y),
                    arrival=new Vector2(s.arrivalX,s.arrivalY),kind="stairs",expires=expiry});
            // A separate draw gives the stairs a useful share of roaming choices
            // instead of burying them among dozens of classroom doors.
            if(exits.Count>0&&random.NextDouble()<.65)return exits;
            foreach(var r in f.rooms??Array.Empty<RoomData>())
            {
                if(string.IsNullOrEmpty(r.interiorId))continue;var room=Map(r.interiorId);if(room==null)continue;
                var outward=r.facing=="north"?Vector2.up:r.facing=="south"?Vector2.down:r.facing=="west"?Vector2.left:Vector2.right;
                exits.Add(new Portal{from=f.id,to=room.id,source=new Vector2(r.doorX,r.doorY)+outward,
                    arrival=new Vector2(room.spawnX,room.spawnY),kind="door",expires=expiry});
            }
            return exits;
        }
        void MoveToward(FloorData f,Vector2 goal,float speed,float dt)
        {
            float now=Game.SimulationTime;
            if(path==null||now>=repathAt||Vector2.Distance(pathGoal,goal)>.6f)
            {path=FindPath(f,Position,goal);pathGoal=goal;repathAt=now+.5f;}
            if(path==null)return;
            while(path.Count>0&&Vector2.Distance(Position,path[0])<.02f)path.RemoveAt(0);
            if(path.Count==0)return;
            while(path.Count>1&&SegmentClear(f,Position,path[1],Radius))path.RemoveAt(0);
            Vector2 delta=path[0]-Position;if(delta.sqrMagnitude<.000001f)return;
            Vector2 move=delta.normalized*Mathf.Min(delta.magnitude,speed*dt/Game.Data.tileSize),next=Position+move;
            if(Clear(f,next,Radius)&&SegmentClear(f,Position,next,Radius)){Position=next;direction=move.normalized;}
            else{path=null;repathAt=0;}
        }
        bool TryArrival(FloorData f,Vector2 at,out Vector2 landing)
        {
            landing=at;if(f==null)return false;var candidates=new List<Vector2>{at};
            for(int ring=1;ring<=3;ring++)for(int i=0;i<8;i++)
            {float a=i*Mathf.PI/4;candidates.Add(at+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*ring*.35f);}
            Vector2 player=Game.Data.Grid(Game.Player.transform.position);
            foreach(var p in candidates)
                if(Clear(f,p,Radius)&&SegmentClear(f,at,p,Radius)&&
                    (Game.Current.id!=f.id||Vector2.Distance(p,player)*Game.Data.tileSize>=1.05f))
                {landing=p;return true;}
            return false;
        }
        bool SightClear(FloorData f,Vector2 a,Vector2 b)=>SegmentClear(f,a,b,.035f);
        bool SegmentClear(FloorData f,Vector2 a,Vector2 b,float radius)
        {
            if(f==null)return false;float t=Game.Data.tileSize,r=radius/t,r2=r*r-.00000001f;
            for(int y=Mathf.FloorToInt(Mathf.Min(a.y,b.y)-r-.5f);y<=Mathf.CeilToInt(Mathf.Max(a.y,b.y)+r+.5f);y++)
                for(int x=Mathf.FloorToInt(Mathf.Min(a.x,b.x)-r-.5f);x<=Mathf.CeilToInt(Mathf.Max(a.x,b.x)+r+.5f);x++)
                    if(!f.Open(x,y)&&SegmentRectDistance2(a,b,new Vector2(x-.5f,y-.5f),new Vector2(x+.5f,y+.5f))<r2)return false;
            foreach(var v in f.fixtures??Array.Empty<FixtureData>())
                if(v.solid&&v.elevation<1.68f&&SegmentRectDistance2(a,b,new Vector2(v.x-v.width/t*.5f,v.y-v.depth/t*.5f),new Vector2(v.x+v.width/t*.5f,v.y+v.depth/t*.5f))<r2)return false;
            foreach(var door in f.rooms??Array.Empty<RoomData>())
            {
                if(door.interiorOpen)continue;
                var d=door.facing=="north"?Vector2.up:door.facing=="south"?Vector2.down:door.facing=="west"?Vector2.left:Vector2.right;
                var c=new Vector2(door.doorX,door.doorY)+d*.51f;var half=new Vector2(d.y!=0?1.02f:.14f,d.y!=0?.14f:1.02f)/t*.5f;
                if(SegmentRectDistance2(a,b,c-half,c+half)<r2)return false;
            }
            if(f.isRoom)
            {
                var c=new Vector2(f.exitX,f.exitY+.51f);var half=new Vector2(1.02f,.14f)/t*.5f;
                if(SegmentRectDistance2(a,b,c-half,c+half)<r2)return false;
            }
            foreach(var gate in f.barriers??Array.Empty<LandmarkData>())
            {
                bool side=gate.x<5||gate.x>43;var c=new Vector2(gate.x,gate.y);var half=new Vector2(side?.22f:3.6f,side?3.6f:.22f)/t*.5f;
                if(SegmentRectDistance2(a,b,c-half,c+half)<r2)return false;
            }
            return true;
        }
        static float SegmentRectDistance2(Vector2 a,Vector2 b,Vector2 min,Vector2 max)
        {
            Vector2 delta=b-a;float lo=0,hi=1;bool hit=true;
            for(int axis=0;axis<2;axis++)
            {
                float from=axis==0?a.x:a.y,d=axis==0?delta.x:delta.y,low=axis==0?min.x:min.y,high=axis==0?max.x:max.y;
                if(Mathf.Abs(d)<.0000001f){if(from<low||from>high){hit=false;break;}}
                else{float p=(low-from)/d,q=(high-from)/d;lo=Mathf.Max(lo,Mathf.Min(p,q));hi=Mathf.Min(hi,Mathf.Max(p,q));if(lo>hi){hit=false;break;}}
            }
            if(hit)return 0;
            float best=Mathf.Min(PointRectDistance2(a,min,max),PointRectDistance2(b,min,max)),length=delta.sqrMagnitude;
            foreach(var p in new[]{min,new Vector2(min.x,max.y),new Vector2(max.x,min.y),max})
            {float k=length>0?Mathf.Clamp01(Vector2.Dot(p-a,delta)/length):0;best=Mathf.Min(best,(p-a-delta*k).sqrMagnitude);}
            return best;
        }
        static float PointRectDistance2(Vector2 p,Vector2 min,Vector2 max)
        {float dx=p.x-Mathf.Clamp(p.x,min.x,max.x),dy=p.y-Mathf.Clamp(p.y,min.y,max.y);return dx*dx+dy*dy;}
        bool Clear(FloorData f,Vector2 p,float radius)
        {return f!=null&&f.Clear(p.x,p.y,Game.Data.tileSize,radius)&&SegmentClear(f,p,p,radius);}
        bool NavCell(FloorData f,int x,int y)
        {
            if(x<0||y<0||x>=f.width||y>=f.height)return false;
            if(!navigation.TryGetValue(f.id,out var cache))
            {
                cache=new bool[f.width,f.height];
                for(int j=0;j<f.height;j++)for(int i=0;i<f.width;i++)cache[i,j]=f.Open(i,j)&&Clear(f,new Vector2(i,j),Radius);
                navigation.Add(f.id,cache);
            }
            return cache[x,y];
        }
        bool Edge(FloorData f,Vector2Int a,Vector2Int b)
        {
            string key=f.id+":"+a.x+","+a.y+">"+b.x+","+b.y;
            if(!edges.TryGetValue(key,out bool clear)){clear=SegmentClear(f,a,b,Radius);edges[key]=clear;}
            return clear;
        }
        Vector2Int NearestCell(FloorData f,Vector2 at)
        {
            var rounded=new Vector2Int(Mathf.RoundToInt(at.x),Mathf.RoundToInt(at.y));
            if(NavCell(f,rounded.x,rounded.y)&&SegmentClear(f,at,rounded,Radius))return rounded;
            float best=float.PositiveInfinity;var result=rounded;
            for(int j=rounded.y-2;j<=rounded.y+2;j++)for(int i=rounded.x-2;i<=rounded.x+2;i++)
            {
                var p=new Vector2Int(i,j);float d=Vector2.Distance(at,p);
                if(d<best&&NavCell(f,i,j)&&SegmentClear(f,at,p,Radius)){best=d;result=p;}
            }
            return result;
        }
        List<Vector2> Reachable(FloorData f,Vector2 from)
        {
            var start=NearestCell(f,from);var q=new List<Vector2Int>{start};var seen=new HashSet<Vector2Int>{start};
            var ds=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
            for(int i=0;i<q.Count;i++)foreach(var d in ds)
            {var n=q[i]+d;if(!seen.Contains(n)&&NavCell(f,n.x,n.y)&&Edge(f,q[i],n)){seen.Add(n);q.Add(n);}}
            var result=new List<Vector2>();foreach(var p in q)result.Add(p);return result;
        }
        List<Vector2> FindPath(FloorData f,Vector2 from,Vector2 to)
        {
            if(Clear(f,to,Radius)&&SegmentClear(f,from,to,Radius))return new List<Vector2>{to};
            var start=NearestCell(f,from);var goal=NearestCell(f,to);
            if(!NavCell(f,start.x,start.y)||!NavCell(f,goal.x,goal.y))return null;
            var q=new Queue<Vector2Int>();var prev=new Dictionary<Vector2Int,Vector2Int>{{start,start}};q.Enqueue(start);
            var ds=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
            while(q.Count>0)
            {
                var p=q.Dequeue();if(p==goal)break;
                foreach(var d in ds){var n=p+d;if(!prev.ContainsKey(n)&&NavCell(f,n.x,n.y)&&Edge(f,p,n)){prev[n]=p;q.Enqueue(n);}}
            }
            if(!prev.ContainsKey(goal))return null;
            var result=new List<Vector2>();for(var p=goal;p!=start;p=prev[p])result.Add(p);result.Reverse();
            if(result.Count==0)result.Add(goal);
            if(Clear(f,to,Radius)&&SegmentClear(f,result[result.Count-1],to,Radius))result.Add(to);
            return result;
        }
        void SyncVisual(float dt)
        {
            if(visual==null||Game==null)return;
            visual.gameObject.SetActive(SimulationEnabled&&Game.Started&&Game.Current.id==SpaceId);
            transform.position=Game.Data.World(Position.x,Position.y);
            if(direction.sqrMagnitude>.001f)
                transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(new Vector3(direction.x,0,direction.y)),1-Mathf.Exp(-7*Mathf.Max(dt,.016f)));
            float walk=Mathf.Sin(Phase)*Mathf.Clamp01(Speed/1.5f);
            for(int i=0;i<arms.Count;i++)arms[i].localRotation=Quaternion.Euler((i==0?-1:1)*walk*21,0,(i==0?1:-1)*6);
            for(int i=0;i<legs.Count;i++)legs[i].localRotation=Quaternion.Euler((i==0?1:-1)*walk*26,0,0);
            visual.localPosition=Vector3.up*(Mathf.Abs(Mathf.Sin(Phase))*.025f);
            torso.localRotation=Quaternion.Euler(9+Mathf.Sin(Game.SimulationTime*1.7f)*.7f,0,-4);
        }
        Material Material(Color color)
        {var m=new Material(Shader.Find("Standard"));m.color=color;m.SetFloat("_Glossiness",.12f);return m;}
        Transform Shape(string name,Transform parent,Vector3 p,Vector3 scale,Material mat,bool sphere=false)
        {
            var g=GameObject.CreatePrimitive(sphere?PrimitiveType.Sphere:PrimitiveType.Cube);g.name=name;
            g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=mat;Destroy(g.GetComponent<Collider>());return g.transform;
        }
        Transform Pivot(string name,Transform parent,Vector3 p)
        {var g=new GameObject(name).transform;g.SetParent(parent,false);g.localPosition=p;return g;}
        void BuildVisual()
        {
            visual=Pivot("Original caretaker · concept 02",transform,Vector3.zero);
            var coat=Material(new Color(.12f,.23f,.22f));var patch=Material(new Color(.18f,.27f,.25f));
            var cream=Material(new Color(.62f,.61f,.49f));var skin=Material(new Color(.62f,.65f,.59f));
            var black=Material(new Color(.035f,.045f,.045f));var eyes=Material(new Color(.78f,.79f,.64f));
            var trousers=Material(new Color(.12f,.14f,.13f));
            Shape("Hips",visual,new Vector3(0,.98f,-.03f),new Vector3(.60f,.34f,.37f),trousers);
            torso=Pivot("Hunched shoulders",visual,new Vector3(0,1.4f,.01f));
            Shape("Heavy duty coat",torso,new Vector3(0,.24f,.025f),new Vector3(.83f,.82f,.48f),coat);
            Shape("Raised left shoulder",torso,new Vector3(-.34f,.57f,0),new Vector3(.35f,.33f,.44f),coat,true);
            Shape("Coat hem",torso,new Vector3(.01f,-.16f,.03f),new Vector3(.69f,.21f,.46f),coat);
            Shape("Left cream collar",torso,new Vector3(-.13f,.54f,.29f),new Vector3(.16f,.27f,.055f),cream).localRotation=Quaternion.Euler(0,0,-16);
            Shape("Right cream collar",torso,new Vector3(.15f,.52f,.29f),new Vector3(.16f,.25f,.055f),cream).localRotation=Quaternion.Euler(0,0,14);
            Shape("Pale staff badge",torso,new Vector3(-.25f,.22f,.28f),new Vector3(.10f,.27f,.025f),cream);
            for(int i=0;i<4;i++)Shape("Uneven coat button",torso,new Vector3(.02f,.38f-i*.17f,.278f),new Vector3(.032f,.032f,.025f),cream,true);
            for(int i=0;i<10;i++)Shape("Worn coat patch",torso,new Vector3((i%3-1)*.24f,-.05f+(i/3)*.16f,.271f),new Vector3(.07f,.018f,.015f),patch);
            head=Pivot("Uncanny tilted head",torso,new Vector3(.03f,.64f,.22f));head.localRotation=Quaternion.Euler(-19,0,21);
            Shape("Chalk pale head",head,new Vector3(0,.20f,.02f),new Vector3(.35f,.46f,.34f),skin,true);
            Shape("Long jaw",head,new Vector3(0,.035f,.10f),new Vector3(.25f,.20f,.23f),skin,true);
            for(int side=-1;side<=1;side+=2)
            {
                float off=side<0?.01f:-.015f;
                Shape("Deep eye socket",head,new Vector3(side*.087f,.23f+off,.166f),new Vector3(.105f,.081f,.035f),black,true);
                Shape("Uneven pale eye",head,new Vector3(side*.087f+(side<0?.008f:0),.23f+off,.188f),new Vector3(.025f,.028f,.014f),eyes,true);
            }
            Shape("Narrow frozen mouth",head,new Vector3(.005f,.065f,.218f),new Vector3(.14f,.016f,.015f),black).localRotation=Quaternion.Euler(0,0,-5);
            Shape("Thin nose",head,new Vector3(0,.15f,.19f),new Vector3(.045f,.10f,.055f),skin);
            for(int i=0;i<5;i++)Shape("Sparse dark hair",head,new Vector3((i-2)*.04f,.413f,.02f+i*.02f),new Vector3(.018f,.065f,.12f),black).localRotation=Quaternion.Euler(5,0,i*12);
            for(int side=-1;side<=1;side+=2)
            {
                var arm=Pivot("Long arm",torso,new Vector3(side*.43f,.44f,0));arms.Add(arm);
                Shape("Coat sleeve",arm,new Vector3(0,-.27f,0),new Vector3(.23f,.55f,.26f),coat);
                Shape("Long chalk forearm",arm,new Vector3(0,-.70f,.025f),new Vector3(.155f,.44f,.17f),skin,true);
                Shape("Large palm",arm,new Vector3(0,-.96f,.045f),new Vector3(.19f,.20f,.11f),skin,true);
                for(int finger=0;finger<4;finger++)Shape("Long separated finger",arm,new Vector3((finger-1.5f)*.044f,-1.12f-Mathf.Abs(finger-1.5f)*-.013f,.057f),new Vector3(.028f,.22f,.035f),skin).localRotation=Quaternion.Euler(7,0,(finger-1.5f)*-4);
                Shape("Outstretched thumb",arm,new Vector3(-side*.12f,-.99f,.065f),new Vector3(.035f,.15f,.04f),skin).localRotation=Quaternion.Euler(0,0,-side*35);
                var leg=Pivot("Heavy leg",visual,new Vector3(side*.19f,1,0));legs.Add(leg);
                Shape("Trouser leg",leg,new Vector3(0,-.43f,0),new Vector3(.27f,.84f,.30f),trousers);
                Shape("Scuffed shoe",leg,new Vector3(0,-.91f,.09f),new Vector3(.30f,.19f,.43f),black);
            }
        }
    }
}
