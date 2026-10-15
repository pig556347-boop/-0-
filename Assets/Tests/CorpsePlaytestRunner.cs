using System;
using System.Collections;
using UnityEngine;

// Separate fixture built only into HUDTest. Death is caused by real ReceiveHit calls.
public sealed class CorpsePlaytestRunner : MonoBehaviour
{
    private int checks;
    private void Check(bool value,string label)
    {if(!value)throw new Exception("CORPSE_TEST_FAIL "+label);checks++;}
    private IEnumerator Start()
    {
        var camera=new GameObject("Corpse test camera").AddComponent<Camera>();
        camera.transform.position=new Vector3(0,5,-9);camera.transform.LookAt(new Vector3(0,.3f,0));
        camera.orthographic=true;camera.orthographicSize=3.7f;
        camera.backgroundColor=new Color(.18f,.19f,.2f);camera.clearFlags=CameraClearFlags.SolidColor;
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Classroom floor";
        floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(20,1,15);
        var actors=new CombatActor[3];
        for(int i=0;i<3;i++)
        {
            var root=new GameObject(new[]{"Ordinary defeated enemy","Deongchi defeated enemy","Defeated player"}[i]);
            root.transform.position=new Vector3((i-1)*3,3+i*.4f,0);
            root.AddComponent<CapsuleCollider>().height=2;
            var body=root.AddComponent<Rigidbody>();body.constraints=RigidbodyConstraints.FreezeRotation;
            body.interpolation=RigidbodyInterpolation.Interpolate;
            if(i==2)root.AddComponent<PlayerController>();
            var actor=root.AddComponent<CombatActor>();actor.Configure(i==2?0:1);actor.ConfigureStats(20,10,.12f,.28f);
            actor.GameplayEnabled=false;
            var view=new GameObject("Visual");view.transform.SetParent(root.transform,false);
            if(i==2)root.GetComponent<PlayerController>().SetVisualRoot(view.transform);
            var character=view.AddComponent<PrototypeCharacterView>();character.Build(actor);
            if(i==1)Check(character.UseDeongchiModel(),"supplied Deongchi model ready");
            actors[i]=actor;
        }
        yield return null;
        foreach(var actor in actors)
        {
            Check(actor.ReceiveHit(new HitRequest(null,999,Vector3.right,false,1)),"lethal damage accepted");
            Check(!actor.Health.Alive,"actor is defeated");
        }
        yield return new WaitForSeconds(.8f);
        foreach(var actor in actors)
        {
            Check(actor.GetComponent<Rigidbody>().isKinematic,"defeated body frozen after settling");
            Check(!actor.GetComponent<Collider>().enabled,"corpse does not block living actors");
            if(actor.GetComponent<PlayerController>()!=null)Check(!actor.GetComponent<PlayerController>().enabled,"dead player motor stopped");
        }
        float until=Time.time+12;
        while(Time.time<until&&(!actors[0].GetComponent<DefeatedBody>().Settled||
            !actors[1].GetComponent<DefeatedBody>().Settled||!actors[2].GetComponent<DefeatedBody>().Settled))yield return null;
        foreach(var actor in actors)
        {
            var corpse=actor.GetComponent<DefeatedBody>();
            Check(corpse.Settled,"death animation completed");
            Check(corpse.TryBounds(out Bounds bounds),"visible corpse mesh measured");
            Check(Mathf.Abs(bounds.min.y-.015f)<.025f,"mesh rests on floor: "+actor.name+" "+bounds);
            Check(bounds.size.y<1,"corpse lies down: "+actor.name+" "+bounds);
        }
        Debug.Log("CORPSE_PREVIEW_READY");
        yield return new WaitForSeconds(1);
        foreach(var actor in actors)
        {
            var corpse=actor.GetComponent<DefeatedBody>();corpse.TryBounds(out Bounds before);
            yield return new WaitForSeconds(.3f);corpse.TryBounds(out Bounds after);
            Check((before.center-after.center).sqrMagnitude<.0001f,"corpse remains stable");
            actor.RestoreForTutorial();actor.GameplayEnabled=true;
            Check(actor.Health.Alive&&!actor.GetComponent<Rigidbody>().isKinematic,"retry restores living physics");
            Check(actor.GetComponent<Collider>().enabled&&!corpse.Settled,"retry restores collider and death state");
            Check(actor.GetComponent<Rigidbody>().interpolation==RigidbodyInterpolation.Interpolate,"retry restores interpolation");
            if(actor.GetComponent<PlayerController>()!=null)Check(actor.GetComponent<PlayerController>().enabled,"retry restores player motor");
            yield return new WaitForSeconds(.1f);
            Check(actor.TryAttack(false),"revived actor can attack");
            actor.Stagger(.01f);
        }
        Debug.Log("CORPSE_TEST_PASS checks="+checks);
    }
}
