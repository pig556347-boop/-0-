using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SchoolEscape
{
    // Runs only when explicitly requested with -escapeValidate. Exercises the
    // production Unity CharacterController and production stair transition code.
    public sealed class RuntimeValidation : MonoBehaviour
    {
        public ExplorationGame Game;
        readonly List<string> passed = new List<string>(), failed = new List<string>();
        readonly List<string> errors = new List<string>();
        [Serializable] sealed class Report { public string utc; public string engine; public string[] passed, failed, errors; }
        void OnEnable()=>Application.logMessageReceived+=Log;
        void OnDisable()=>Application.logMessageReceived-=Log;
        void Log(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+stack);}
        void Check(bool ok,string name){if(ok)passed.Add(name);else failed.Add(name);}
        bool ValidPosition()
        {
            var p=Game.Data.Grid(Game.Player.transform.position);
            return Game.Current.Clear(p.x,p.y,Game.Data.tileSize,Game.Data.playerRadius-.025f)&&
                Game.Player.transform.position.y>=-.05f&&Game.Player.transform.position.y<.3f;
        }
        void Move(Vector2 axes,bool run,float seconds)
        {for(int i=0;i<Mathf.RoundToInt(seconds*100);i++)Game.Player.Step(axes,run,.01f);}
        bool ClearEdge(Vector2Int start,Vector2Int end)
        {
            // Thin authored partition fixtures may sit between two clear cell
            // centres. Check their intervening clearance before routing, then
            // still exercise the actual CharacterController along the route.
            for(int sample=1;sample<=4;sample++)
            {
                Vector2 point=Vector2.Lerp(start,end,sample*.25f);
                if(!Game.Current.Clear(point.x,point.y,Game.Data.tileSize,Game.Data.playerRadius))return false;
            }
            return true;
        }
        List<Vector2Int> Path(Vector2Int start,Vector2Int goal)
        {
            var q=new Queue<Vector2Int>();var prev=new Dictionary<Vector2Int,Vector2Int>();
            q.Enqueue(start);prev[start]=start;Vector2Int[] ds={Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
            while(q.Count>0)
            {
                var p=q.Dequeue();if(p==goal)break;
                foreach(var d in ds){var n=p+d;if(!prev.ContainsKey(n)&&ClearEdge(p,n)){prev[n]=p;q.Enqueue(n);}}
            }
            if(!prev.ContainsKey(goal))return null;
            var path=new List<Vector2Int>();for(var p=goal;p!=start;p=prev[p])path.Add(p);path.Reverse();return path;
        }
        bool WalkTo(int x,int y)
        {
            int startFloor=Game.Floor;
            var p=Game.Data.Grid(Game.Player.transform.position);
            var path=Path(new Vector2Int(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y)),new Vector2Int(x,y));
            if(path==null)return false;
            foreach(var node in path)
            {
                var goal=Game.Data.World(node.x,node.y);
                for(int i=0;i<100;i++)
                {
                    var delta=goal-Game.Player.transform.position;delta.y=0;
                    if(delta.magnitude<.025f)break;
                    var axes=new Vector2(delta.x,delta.z).normalized;
                    Game.Player.Step(axes,true,Mathf.Min(.01f,delta.magnitude/(Game.Player.CanRun?Game.Data.runSpeed:Game.Data.walkSpeed)));
                    if(Game.Floor!=startFloor)return ValidPosition();
                }
                if(Vector3.Distance(goal,new Vector3(Game.Player.transform.position.x,0,Game.Player.transform.position.z))>.1f)return false;
            }
            return ValidPosition();
        }
        IEnumerator Start()
        {
            yield return null;yield return new WaitForFixedUpdate();
            Game.Player.InputEnabled=false;
            Game.AutomaticStairsEnabled=false;
            Check(Game.Pursuer!=null&&!Game.Pursuer.SimulationEnabled,"Runtime navigation regression explicitly disables active pursuit");
            if(Game.Pursuer!=null)
            {
                var enemy=Game.Pursuer.Position;
                var enemyMap=Game.FindSpace(Game.Pursuer.SpaceId);
                Check(enemyMap!=null&&enemyMap.Clear(enemy.x,enemy.y,Game.Data.tileSize,Pursuer.Radius),"Caretaker initial landing is clear on its selected map");
                Check(Game.Pursuer.SpaceId!=Game.Current.id||Vector2.Distance(enemy,Game.Data.Grid(Game.Player.transform.position))*Game.Data.tileSize>=15,"Caretaker starts on another floor or at a safe reachable distance");
            }
            Check(Resources.Load<Font>("SchoolFont")!=null,"Korean font imported");
            Check(Resources.Load<Texture2D>("FloorTexture")!=null,"Floor texture imported");
            Check(Resources.Load<Texture2D>("WallTexture")!=null,"Wall texture imported");
            if((Game.Data.outdoorMaps??Array.Empty<FloorData>()).Length>0)
            {
                Check(Resources.Load<Texture2D>("YardTexture")!=null,"Outdoor soil texture imported");
                var keys=Game.Items.WorldItems.FindAll(i=>i.id.StartsWith("key-",StringComparison.Ordinal));
                Check(Game.Items.WorldItems.Count==13&&Game.Items.Capacity==9,"Outdoor expansion preserves thirteen items and nine carried slots");
                Check(keys.Count==3&&keys.Exists(i=>i.id=="key-a")&&keys.Exists(i=>i.id=="key-b")&&keys.Exists(i=>i.id=="key-c"),"Outdoor run has exactly the three unique A/B/C keys");
                Check(keys.Exists(i=>Game.FindSpace(i.spaceId).isOutdoor)&&keys.Exists(i=>!Game.FindSpace(i.spaceId).isOutdoor),"Every native run places a key outdoors and a key in the main building");
                Check(keys.Exists(i=>i.hidden)&&keys.Exists(i=>!i.hidden),"Native key locations include furniture search and loose pickup");
            }
            Check(Shader.Find("Standard")!=null,"Built-in shader available");
            Vector3 cameraTarget=(Game.Player.transform.position-Game.View.transform.position).normalized;
            Check(Vector3.Dot(Game.View.transform.forward,cameraTarget)>.999f,"Camera points at explorer");
            Check(Game.View.transform.right.x>.99f,"Camera right matches movement right");
            Vector3 viewport=Game.View.WorldToViewportPoint(Game.Player.transform.position);
            Check(viewport.z>0&&Mathf.Abs(viewport.x-.5f)<.02f&&Mathf.Abs(viewport.y-.5f)<.02f,"Explorer inside camera viewport");
            foreach(bool run in new[]{false,true})
            {
                Game.Player.Warp(Game.Data.World(24,9));var a=Game.Player.transform.position;
                Move(Vector2.right,run,.5f);float straight=Vector3.Distance(a,Game.Player.transform.position);
                Game.Player.Warp(Game.Data.World(24,9));a=Game.Player.transform.position;
                Move(Vector2.one,run,.5f);float diagonal=Vector3.Distance(a,Game.Player.transform.position);
                Check(Mathf.Abs(straight-(run?Game.Data.runSpeed:Game.Data.walkSpeed)*.5f)<.05f,"Movement speed "+run);
                Check(Mathf.Abs(straight-diagonal)<.05f,"Normalized diagonal "+run);
            }
            Game.Player.ResetState();Game.Player.Warp(Game.Data.World(24,9));
            var pausedPosition=Game.Player.transform.position;float pausedStamina=Game.Player.Stamina,pausedClock=Game.SimulationTime;
            Game.SetPaused(true);Move(Vector2.right,true,.5f);yield return null;
            Check(Vector3.Distance(pausedPosition,Game.Player.transform.position)<.001f&&Game.Player.Stamina==pausedStamina&&Game.SimulationTime==pausedClock,"Pause freezes movement, stamina and gameplay clock");
            Game.SetPaused(false);
            Move(Vector2.zero,true,1);
            Check(Mathf.Abs(Game.Player.Stamina-100)<.01f&&!Game.Player.IsRunning,"Stationary Shift does not drain stamina");
            for(int sprint=0;sprint<14;sprint++)Move(sprint%2==0?Vector2.right:Vector2.left,true,.4f);
            Check(Game.Player.Exhausted&&Game.Player.Stamina<25&&!Game.Player.IsRunning,"Sustained movement exhausts sprint");
            Move(Vector2.zero,false,1);
            Check(Game.Player.Exhausted&&Game.Player.Stamina<25,"Exhaustion cannot instantly restart sprint");
            Move(Vector2.zero,false,1.6f);
            Check(!Game.Player.Exhausted&&Game.Player.Stamina>=25,"Stamina recovery unlocks at threshold");
            Game.Player.ResetState();
            foreach(var f in Game.Data.floors)
            {
                var start=f.landmarks[0];Game.SwitchFloor(f.number,start.x,start.y,false);
                yield return null;
                foreach(var m in f.landmarks)
                {
                    Check(WalkTo(m.x,m.y),f.number+"F reachable: "+m.label);
                    Check(WalkTo(start.x,start.y),f.number+"F return: "+m.label);
                }
                foreach(var r in f.rooms)
                {
                    int dx=r.facing=="west"?-1:r.facing=="east"?1:0;
                    int dy=r.facing=="south"?-1:r.facing=="north"?1:0;
                    Game.Player.Warp(Game.Data.World(r.doorX+dx,r.doorY+dy));
                    if(r.interiorOpen)
                    {
                        Check(WalkTo(r.doorX-dx,r.doorY-dy),f.number+"F enter open room: "+r.label);
                        Check(WalkTo(r.doorX+dx,r.doorY+dy),f.number+"F exit open room: "+r.label);
                    }
                    else
                    {
                        Move(new Vector2(-dx,-dy),true,.6f);
                        var stopped=Game.Data.Grid(Game.Player.transform.position);
                        float along=(stopped.x-r.doorX)*dx+(stopped.y-r.doorY)*dy;
                        Check(ValidPosition()&&along>.5f,f.number+"F closed door blocks: "+r.label);
                    }
                }
                foreach(var zone in f.interiorZones??Array.Empty<InteriorZoneData>())
                {
                    Game.Player.Warp(Game.Data.World(start.x,start.y));
                    Check(WalkTo(zone.x,zone.y),f.number+"F interior route: "+zone.room+" / "+zone.label);
                    Check(WalkTo(start.x,start.y),f.number+"F interior return: "+zone.room+" / "+zone.label);
                }
                // Exercise one physically solid furnishing per room rather than
                // only asserting that its data says solid.
                var furnitureRooms=new HashSet<string>();
                foreach(var fixture in f.fixtures??Array.Empty<FixtureData>())
                {
                    if(!fixture.solid||fixture.elevation>=1.68f||furnitureRooms.Contains(fixture.room))continue;
                    foreach(var direction in new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down})
                    {
                        float extent=(direction.x!=0?fixture.width:fixture.depth)*.5f+Game.Data.playerRadius+.18f;
                        Vector2 at=new Vector2(fixture.x,fixture.y)+direction*extent/Game.Data.tileSize;
                        if(!f.Clear(at.x,at.y,Game.Data.tileSize,Game.Data.playerRadius))continue;
                        Game.Player.Warp(Game.Data.World(at.x,at.y));Move(-direction,true,.6f);
                        Check(ValidPosition(),f.number+"F furniture collision: "+fixture.room+" / "+fixture.kind);
                        furnitureRooms.Add(fixture.room);break;
                    }
                }
                Game.SwitchFloor(f.number,start.x,start.y,false);
                // Test outer collision along every direction from an actual
                // corridor, never substituting a mathematical bounds assertion.
                foreach(var d in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right})
                {
                    Game.Player.Warp(Game.Data.World(start.x,start.y));Move(d,true,15);
                    Check(ValidPosition(),f.number+"F walls and boundary "+d);
                }
            }
            foreach(var roomMap in Game.Data.roomMaps??Array.Empty<FloorData>())
            {
                if(!string.IsNullOrEmpty(roomMap.building))
                {
                    Game.SwitchSpace(roomMap.id,roomMap.spawnX,roomMap.spawnY);
                    foreach(var zone in roomMap.interiorZones??Array.Empty<InteriorZoneData>())
                        Check(WalkTo(zone.x,zone.y),"Annex interior route: "+roomMap.id+" / "+zone.label);
                    foreach(var annexPortal in roomMap.portals??Array.Empty<PortalData>())
                    {
                        if(annexPortal.locked)continue;
                        Game.SwitchSpace(roomMap.id,roomMap.spawnX,roomMap.spawnY);
                        var at=new Vector2(annexPortal.doorX,annexPortal.doorY)+annexPortal.Outward;
                        Check(WalkTo(Mathf.RoundToInt(at.x),Mathf.RoundToInt(at.y)),"Annex portal approach: "+annexPortal.id);
                        yield return new WaitForSecondsRealtime(.7f);
                        Check(Game.UsePortal(annexPortal.id),"Annex E portal: "+annexPortal.id);
                        yield return new WaitForSecondsRealtime(.3f);
                        Check(Game.Current.id==annexPortal.targetId&&ValidPosition()&&Game.ActiveSpaceCount==1,"Annex safe isolated arrival: "+annexPortal.id);
                    }
                    if(!roomMap.noExit)
                    {
                        Game.SwitchSpace(roomMap.id,roomMap.exitX,roomMap.exitY+1);
                        yield return new WaitForSecondsRealtime(.7f);
                        Check(Game.Interact(),"Annex return E: "+roomMap.id);
                        yield return new WaitForSecondsRealtime(.3f);
                        Check(Game.Current.id==roomMap.returnSpaceId&&ValidPosition(),"Annex correct return: "+roomMap.id);
                    }
                    foreach(var stair in roomMap.stairs??Array.Empty<StairData>())
                    {
                        Game.SwitchSpace(roomMap.id,stair.x,stair.y);
                        yield return new WaitForSecondsRealtime(.8f);
                        Game.CheckAutomaticStairs();
                        Check(Game.Current.id==stair.targetId&&ValidPosition(),"Annex automatic stair: "+stair.id);
                    }
                    continue;
                }
                var corridor=Game.Data.floors[roomMap.sourceFloor-1];var start=corridor.landmarks[0];
                Game.SwitchFloor(roomMap.sourceFloor,start.x,start.y,false);
                var portal=Array.Find(corridor.rooms,r=>r.interiorId==roomMap.id);
                Check(portal!=null,"Room has corridor portal: "+roomMap.id);
                if(portal==null)continue;
                yield return new WaitForSecondsRealtime(.7f);
                Check(!Game.EnterRoom(roomMap.id),"Distant door cannot enter: "+roomMap.label);
                Check(WalkTo(roomMap.returnX,roomMap.returnY),"Reach closed portal: "+roomMap.label);
                Check(string.IsNullOrEmpty(Game.RoomId)&&Game.ActiveSpaceCount==1,"Corridor hides room before E: "+roomMap.label);
                int before=Game.RoomTransitions;
                Check(Game.Interact()&&Game.Opening&&string.IsNullOrEmpty(Game.RoomId),"E opens door before room switch: "+roomMap.label);
                var frozen=Game.Player.transform.position;Move(Vector2.up,true,.1f);
                Check(Vector3.Distance(frozen,Game.Player.transform.position)<.001f,"Movement freezes while door opens: "+roomMap.label);
                Check(!Game.Interact(),"Repeated E during opening blocked: "+roomMap.label);
                yield return new WaitForSecondsRealtime(.3f);
                Check(Game.RoomId==roomMap.id&&!Game.Opening&&Game.RoomTransitions==before+1,"E enters isolated room after opening: "+roomMap.label);
                Check(Game.ActiveSpaceCount==1&&Game.Current==roomMap&&ValidPosition(),"Only current room active and safe: "+roomMap.label);
                Check(!Game.Interact()&&Game.RoomTransitions==before+1,"Repeated E debounced: "+roomMap.label);
                yield return null;yield return new WaitForFixedUpdate();
                foreach(var zone in roomMap.interiorZones??Array.Empty<InteriorZoneData>())
                {
                    Check(WalkTo(zone.x,zone.y),"Isolated interior route: "+roomMap.label+" / "+zone.label);
                    Check(WalkTo(roomMap.spawnX,roomMap.spawnY),"Isolated interior return: "+roomMap.label+" / "+zone.label);
                    Check(Game.RoomId==roomMap.id&&Game.ActiveSpaceCount==1,"No subroom transition or leak: "+roomMap.label+" / "+zone.label);
                }
                bool furnitureChecked=false;
                foreach(var fixture in roomMap.fixtures??Array.Empty<FixtureData>())
                {
                    if(!fixture.solid||fixture.elevation>=1.68f||furnitureChecked)continue;
                    foreach(var direction in new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down})
                    {
                        float extent=(direction.x!=0?fixture.width:fixture.depth)*.5f+Game.Data.playerRadius+.18f;
                        Vector2 at=new Vector2(fixture.x,fixture.y)+direction*extent/Game.Data.tileSize;
                        if(!roomMap.Clear(at.x,at.y,Game.Data.tileSize,Game.Data.playerRadius))continue;
                        Game.Player.Warp(Game.Data.World(at.x,at.y));Move(-direction,true,.6f);
                        Check(ValidPosition(),"Isolated furniture blocks: "+roomMap.label+" / "+fixture.kind);
                        furnitureChecked=true;break;
                    }
                }
                Game.Player.Warp(Game.Data.World(roomMap.spawnX,roomMap.spawnY));
                foreach(var direction in new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down})
                {
                    Game.Player.Warp(Game.Data.World(roomMap.spawnX,roomMap.spawnY));Move(direction,true,20);
                    Check(ValidPosition()&&Game.RoomId==roomMap.id,"Room boundary and closed exit: "+roomMap.label+" / "+direction);
                }
                Game.Player.Warp(Game.Data.World(roomMap.spawnX,roomMap.spawnY));
                Check(WalkTo(roomMap.exitX,roomMap.exitY+1),"Reach room exit: "+roomMap.label);
                yield return new WaitForSecondsRealtime(.7f);
                Check(Game.Interact()&&Game.Opening&&Game.RoomId==roomMap.id,"E opens exit door: "+roomMap.label);
                yield return new WaitForSecondsRealtime(.3f);
                Check(string.IsNullOrEmpty(Game.RoomId)&&!Game.Opening&&Game.RoomTransitions==before+2,"E returns to corridor after opening: "+roomMap.label);
                var returned=Game.Data.Grid(Game.Player.transform.position);
                Check(Game.Floor==roomMap.sourceFloor&&Vector2.Distance(returned,new Vector2(roomMap.returnX,roomMap.returnY))<.02f&&Game.ActiveSpaceCount==1,"Precise portal return and hidden room: "+roomMap.label);
                Check(!Game.Interact(),"No immediate return loop: "+roomMap.label);
                Check(WalkTo(start.x,start.y),"Return from door to main route: "+roomMap.label);
            }
            // Exercise the exterior through the same closed-door coroutine as
            // a user, rather than treating it as another numbered floor.
            foreach(var yard in Game.Data.outdoorMaps??Array.Empty<FloorData>())
            {
                var entrance=Array.Find(Game.Data.floors[0].portals??Array.Empty<PortalData>(),p=>p.targetId==yard.id);
                Check(entrance!=null,"Outdoor map has main-entrance portal: "+yard.id);
                if(entrance==null)continue;
                Game.SwitchFloor(1,24,3,false);yield return new WaitForSecondsRealtime(.7f);
                Check(!Game.NearExitGate(),"Former first-floor gate is no longer an escape point");
                Vector2 at=new Vector2(entrance.doorX,entrance.doorY)+entrance.Outward;
                Game.Player.Warp(Game.Data.World(at.x,at.y));Move(-entrance.Outward,true,.6f);
                Check(Game.Current.id=="floor-1"&&ValidPosition(),"Closed entrance prevents walk-only outdoor transfer");
                Game.Player.Warp(Game.Data.World(at.x,at.y));int transfers=Game.RoomTransitions;
                Check(Game.Interact()&&Game.Opening&&Game.Current.id=="floor-1","Actual E opens outdoor entrance before transfer");
                yield return new WaitForSecondsRealtime(.3f);
                Check(Game.Current==yard&&yard.isOutdoor&&Game.ActiveSpaceCount==1&&string.IsNullOrEmpty(Game.RoomId)&&ValidPosition(),"Outdoor arrival is safe and isolated from interior rooms");
                Check(Vector2.Distance(Game.Data.Grid(Game.Player.transform.position),new Vector2(entrance.arrivalX,entrance.arrivalY))<.03f&&Game.Floor==0,"Outdoor arrival uses its authored landing and has no indoor floor number");
                Check(Game.RoomTransitions==transfers+1&&!Game.Interact(),"Outdoor arrival blocks repeated E and idle transfer loops");
                yield return null;yield return new WaitForFixedUpdate();
                foreach(var landmark in yard.landmarks??Array.Empty<LandmarkData>())
                {
                    Check(WalkTo(landmark.x,landmark.y),"Outdoor walking route: "+landmark.label);
                    Check(WalkTo(yard.spawnX,yard.spawnY),"Outdoor route return: "+landmark.label);
                }
                foreach(var direction in new[]{Vector2.right,Vector2.left,Vector2.up,Vector2.down})
                {
                    Game.Player.Warp(Game.Data.World(yard.spawnX,yard.spawnY));Move(direction,true,35);
                    Check(Game.Current==yard&&ValidPosition(),"Outdoor boundary/closed door prevents walking escape: "+direction);
                }
                var gate=Game.Data.escapeGate;
                Check(gate!=null&&gate.spaceId==yard.id,"Final escape gate belongs to the actual schoolyard");
                if(gate!=null&&gate.spaceId==yard.id)
                {
                    Game.Player.Warp(Game.Data.World(gate.x,gate.y));
                    Check(Game.NearExitGate(),"Outdoor south gate has a safe usable approach");
                    Check(!Game.Items.ConsumeEscapeKeys()&&!Game.Won,"Outdoor gate rejects escape while A/B/C are missing");
                    Game.Player.Warp(Game.Data.World(gate.x,gate.y+(gate.range+.25f)/Game.Data.tileSize));
                    Check(!Game.NearExitGate(),"Outdoor escape interaction remains limited to the south gate range");
                }
                Game.Player.Warp(Game.Data.World(yard.spawnX,yard.spawnY));
                var back=Array.Find(yard.portals??Array.Empty<PortalData>(),p=>p.targetId=="floor-1");
                Check(back!=null,"Schoolyard has a return portal to main building");
                if(back==null)continue;
                Vector2 returnAt=new Vector2(back.doorX,back.doorY)+back.Outward;
                Check(WalkTo(Mathf.RoundToInt(returnAt.x),Mathf.RoundToInt(returnAt.y)),"Outdoor return entrance reachable after exploration");
                yield return new WaitForSecondsRealtime(.7f);
                Check(Game.Interact()&&Game.Opening&&Game.Current==yard,"Actual E opens main-building return door");
                yield return new WaitForSecondsRealtime(.3f);
                Vector2 returned=Game.Data.Grid(Game.Player.transform.position);
                Check(Game.Current.id=="floor-1"&&Game.ActiveSpaceCount==1&&ValidPosition()&&Vector2.Distance(returned,new Vector2(back.arrivalX,back.arrivalY))<.03f,"Outdoor round trip returns safely to the precise main entrance");
                Check(!Game.Interact(),"No immediate outdoor/main-building bounce");
            }
            Game.SwitchFloor(1,24,5,false);
            Game.AutomaticStairsEnabled=true;
            foreach(int target in new[]{2,3,2,1})
            {
                yield return new WaitForSecondsRealtime(.8f);
                int previous=Game.Floor,before=Game.Transitions;
                StairData s=Array.Find(Game.Current.stairs,v=>v.target==target);
                Check(WalkTo(s.x,s.y+(target>previous?-1:1))&&Game.Floor==previous,"Nearby stair does not teleport "+previous);
                Check(WalkTo(s.x,s.y)&&Game.Floor==target&&Game.Transitions==before+1&&ValidPosition(),"Walk-only automatic transition "+previous+"→"+target);
                Move(Vector2.zero,false,.2f);
                Check(Game.Transitions==before+1,"Immediate retrigger prevented "+target);
                yield return new WaitForSecondsRealtime(.8f);
                var back=Array.Find(Game.Current.stairs,v=>v.target==previous);
                Check(WalkTo(back.x,back.y)&&Game.Transitions==before+1,"Arrival landing guard blocks delayed return "+target);
                Move(Vector2.zero,false,.5f);
                Check(Game.Transitions==before+1,"No idle automatic loop "+target);
                var exit=Game.Current.landmarks[0];
                Check(WalkTo(exit.x,exit.y)&&Game.Floor==target,"Can exit arrival stair landing "+target);
            }
            Check(FindObjectsByType<Explorer>(FindObjectsSortMode.None).Length==1,"Exactly one explorer");
            Check(errors.Count==0,"No runtime errors or exceptions");
            Directory.CreateDirectory("TestResults");
            var report=new Report{utc=DateTime.UtcNow.ToString("O"),engine=Application.unityVersion,passed=passed.ToArray(),failed=failed.ToArray(),errors=errors.ToArray()};
            File.WriteAllText("TestResults/unity-runtime.json",JsonUtility.ToJson(report,true));
            Debug.Log("SCHOOL_ESCAPE_VALIDATION passed="+passed.Count+" failed="+failed.Count+" errors="+errors.Count);
            int code=failed.Count+errors.Count==0?0:1;
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
            #else
            Application.Quit(code);
            #endif
        }
    }
}
