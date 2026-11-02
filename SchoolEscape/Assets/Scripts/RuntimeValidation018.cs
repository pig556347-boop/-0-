using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace SchoolEscape {
// Explicit Play-mode smoke test; setup warps isolate the physical controller.
public sealed class RuntimeValidation018:MonoBehaviour {
 public ExplorationGame Game;
 readonly List<string> passed=new List<string>(),failed=new List<string>(),errors=new List<string>();
 [Serializable] class Report { public string utc,engine,method;public string[] passed,failed,errors; }
 void OnEnable(){Application.logMessageReceived+=Log;}
 void OnDisable(){Application.logMessageReceived-=Log;}
 void Log(string text,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)errors.Add(text);}
 void Check(bool ok,string label){(ok?passed:failed).Add(label);}
 void Move(Vector2 axes,bool sprint,float seconds){for(int n=0;n<Mathf.RoundToInt(seconds*100);n++)Game.Player.Step(axes,sprint,.01f);}
 IEnumerator Start(){
  yield return null;yield return new WaitForFixedUpdate();
  Game.AutomaticStairsEnabled=false;Game.Player.MaximumStamina=150;Game.Player.ResetState();
  Check(Game.Native!=null&&Game.Items!=null,"Native systems initialized in Unity Play");
  foreach(bool run in new[]{false,true}){
   Game.RestorePosition("floor-1",new Vector2(24,9));Physics.SyncTransforms();var at=Game.Player.transform.position;Move(Vector2.right,run,.4f);float straight=Vector3.Distance(at,Game.Player.transform.position);
   Game.RestorePosition("floor-1",new Vector2(24,9));Physics.SyncTransforms();at=Game.Player.transform.position;Move(Vector2.one,run,.4f);float diagonal=Vector3.Distance(at,Game.Player.transform.position);
   Check(Mathf.Abs(straight-(run?5.5f:3.1f)*.4f)<.06f,"Physical movement speed "+run);Check(Mathf.Abs(straight-diagonal)<.06f,"Physical normalized diagonal "+run);
  }
  Game.RestorePosition("floor-1",new Vector2(24,9));Game.Player.ResetState();Physics.SyncTransforms();Move(Vector2.right,true,1);Check(Mathf.Abs(Game.Player.Stamina-134)<.1f,"Sprint consumes 16 stamina per second");
  Move(Vector2.zero,false,.8f);Check(Mathf.Abs(Game.Player.Stamina-134)<.1f,"Recovery waits one second");Move(Vector2.zero,false,.7f);Check(Game.Player.Stamina>145&&Game.Player.Stamina<148,"Recovery restores 24 stamina per second after delay");
  var before=Game.Player.transform.position;float stamina=Game.Player.Stamina;Game.SetPaused(true);Move(Vector2.right,true,1);Check(Game.Player.transform.position==before&&Game.Player.Stamina==stamina,"Pause freezes controller and stamina");Game.SetPaused(false);
  var keys=Game.Items.WorldItems.Where(i=>i.id.StartsWith("key-")).ToArray();Check(keys.Length==3&&keys.Count(i=>i.spaceId=="main-rooftop")==1&&keys.Count(i=>i.spaceId=="playground")==1,"Native layout contains roof and yard escape keys");
  var library=Game.Data.roomMaps.First(f=>f.kind=="library");Check(library.fixtures.Any(f=>f.kind=="loan-counter")&&library.fixtures.Any(f=>f.kind=="librarian-chair")&&!library.fixtures.Any(f=>!string.IsNullOrEmpty(f.bookCode)),"Library has librarian station and no return puzzle");
  Game.RestorePosition("main-rooftop",new Vector2(14,5));Physics.SyncTransforms();Check(Game.Current.fixtures.Any(f=>f.kind=="roof-water-tank"),"Roof dressing loaded");
  var roof=Game.Current;Game.RestorePosition(roof.id,new Vector2(12,17));Physics.SyncTransforms();Move(Vector2.up,true,2);var p=Game.Data.Grid(Game.Player.transform.position);Check(p.y<19&&roof.Clear(p.x,p.y,Game.Data.tileSize,Game.Data.playerRadius-.025f),"Physical tank collision blocks passage");
  Game.RestorePosition(roof.id,new Vector2(14,5));Game.Player.RestoreStamina();Physics.SyncTransforms();Game.AutomaticStairsEnabled=true;yield return new WaitForSecondsRealtime(1);
  for(int n=0;n<140&&Game.Current.id==roof.id;n++){Game.Player.Step(Vector2.down,false,.01f);Game.CheckAutomaticStairs();}
  Check(Game.Current.id=="floor-4","Outdoor roof automatic stairs return to floor four");
  Check(errors.Count==0,"No Unity runtime exceptions");
  Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/unity018-runtime.json",JsonUtility.ToJson(new Report{utc=DateTime.UtcNow.ToString("O"),engine=Application.unityVersion,method="Unity Play-mode physical CharacterController smoke; controlled placements, no human visual or audio review",passed=passed.ToArray(),failed=failed.ToArray(),errors=errors.ToArray()},true));
  Debug.Log("SCHOOL_ESCAPE_018 passed="+passed.Count+" failed="+failed.Count+" errors="+errors.Count);
  #if UNITY_EDITOR
  UnityEditor.EditorApplication.Exit(failed.Count+errors.Count==0?0:1);
  #else
  Application.Quit(failed.Count+errors.Count==0?0:1);
  #endif
 }
}}
