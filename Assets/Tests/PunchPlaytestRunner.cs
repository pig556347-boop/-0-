using System;
using System.Collections;
using UnityEngine;

public sealed class PunchPlaytestRunner : MonoBehaviour
{
    private int checks;
    private void Check(bool ok,string message){if(!ok)throw new Exception("PUNCH_TEST_FAIL "+message);checks++;}
    private IEnumerator Start()
    {
        var camera=new GameObject("Punch camera").AddComponent<Camera>();
        camera.transform.position=new Vector3(0,4,-8);camera.transform.LookAt(Vector3.zero);
        camera.orthographic=true;camera.orthographicSize=3.5f;
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Classroom floor";
        floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(20,1,12);
        for(int kind=0;kind<3;kind++)
        {
            var root=new GameObject(new[]{"Player punch","Deongchi punch","Placeholder punch"}[kind]);
            root.transform.position=new Vector3((kind-1)*3,1,0);
            root.AddComponent<CapsuleCollider>().height=2;
            root.AddComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeRotation;
            if(kind==0)root.AddComponent<CombatInput>();
            var actor=root.AddComponent<CombatActor>();actor.Configure(kind==0?0:1);actor.ConfigureStats(100,10,.2f,.3f);
            actor.SetFacing(Vector3.forward);
            var visual=new GameObject("Visual");visual.transform.SetParent(root.transform,false);
            var view=visual.AddComponent<PrototypeCharacterView>();view.Build(actor);
            if(kind==1)Check(view.UseDeongchiModel(),"Deongchi rig loaded");
            for(int strike=0;strike<6;strike++)
            {
                Check(kind==0?actor.TryPunch():actor.TryAttack(false),"punch starts kind="+kind);
                Check(actor.PunchStrike,"attack is a punch");
                if(strike==0)Check(actor.PunchLeft,"first punch is left");
                if(strike==1)Check(!actor.PunchLeft,"second punch is right");
                Check(actor.PunchCount==strike+1,"sequence continues past two");
                yield return new WaitForSeconds(.22f);
                string clip=actor.PunchLeft?"PunchLeft":"PunchRight";
                if(kind==0)Check(visual.GetComponent<AuthoredPlayerView>().CurrentMotion==clip,"player uses selected supplied clip");
                if(kind==1)Check(visual.GetComponent<DeongchiWalkView>().CurrentMotion==clip,"Deongchi uses selected supplied clip");
                foreach(var bone in visual.GetComponentsInChildren<Transform>())
                    Check(!float.IsNaN(bone.position.x)&&Vector3.Distance(bone.position,root.transform.position)<5,"finite bounded pose");
                if(strike==1){Debug.Log("PUNCH_PREVIEW_READY kind="+kind);yield return new WaitForSeconds(.1f);}
                yield return new WaitForSeconds(.4f);
                Check(actor.CanAct,"punch recovers");
            }
            yield return new WaitForSeconds(3.1f);
            Check(kind==0?actor.TryPunch():actor.TryAttack(false),"fresh sequence starts");
            Check(actor.PunchLeft&&actor.PunchCount==1,"inactivity resets sequence to left");
            yield return new WaitForSeconds(.6f);
            actor.RestoreForTutorial();Check(actor.PunchCount==0,"revival resets punch sequence");
            if(kind==0)
            {
                var target=new GameObject("Punch hit target");target.transform.position=root.transform.position+Vector3.forward*1.3f;
                target.AddComponent<CapsuleCollider>().height=2;target.AddComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll;
                var victim=target.AddComponent<CombatActor>();victim.Configure(1);victim.ConfigureStats(100,1,.2f,.3f);
                bool landed=false;actor.Punched+=()=>landed=true;
                Debug.Log("PUNCH_INPUT_READY");float timeout=Time.time+15;
                while(actor.State==CombatActor.ActionState.IDLE&&Time.time<timeout)yield return null;
                Check(actor.State==CombatActor.ActionState.ATTACK&&actor.PunchStrike,"I starts punch instead of grab");
                yield return new WaitForSeconds(.6f);
                Check(landed&&victim.Health.Current<100,"I punch actually deals damage and emits lesson signal");
                target.SetActive(false);Destroy(target);
            }
        }
        Debug.Log("PUNCH_TEST_PASS checks="+checks);
    }
}
