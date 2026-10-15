using System;
using System.Collections;
using UnityEngine;

// Tests the supplied skin in the real enemy factory and combat state machine.
public sealed class Minion1PlaytestRunner : MonoBehaviour
{
    private int checks;
    private void Check(bool value,string label){if(!value)throw new Exception("MINION_TEST_FAIL "+label);checks++;}
    private Bounds SkinBounds(SkinnedMeshRenderer skin,Vector3 origin)
    {
        Mesh mesh=skin.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes;
        var matrices=new Matrix4x4[bind.Length];for(int i=0;i<bind.Length;i++)matrices[i]=skin.bones[i].localToWorldMatrix*bind[i];
        Bounds bounds=default;bool good=true;
        for(int i=0;i<vertices.Length;i++)
        {
            var w=weights[i];var v=vertices[i];
            Vector3 p=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+
                matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
            good&=!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&(p-origin).magnitude<3.5f;
            if(i==0)bounds=new Bounds(p,Vector3.zero);else bounds.Encapsulate(p);
        }
        Check(good,"finite weighted skin vertices");return bounds;
    }
    private IEnumerator Start()
    {
        var camera=new GameObject("Minion test camera").AddComponent<Camera>();
        camera.transform.position=new Vector3(3,3,-6);camera.transform.LookAt(Vector3.up);camera.orthographic=true;camera.orthographicSize=2.8f;
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Classroom floor";floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(20,1,12);
        var target=new GameObject("Player target");target.transform.position=new Vector3(-4,1,0);
        var player=target.AddComponent<CombatActor>();player.Configure(0);player.ConfigureStats(500,10,.2f,.3f);
        player.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll;player.GetComponent<Rigidbody>().useGravity=false;player.GameplayEnabled=false;
        var enemy=PrototypeBootstrap.CreateOpponent("잔몹1",new Vector3(0,1,0),player);
        var brain=enemy.GetComponent<EnemyBrain>();brain.Active=false;
        var body=enemy.GetComponent<Rigidbody>();body.useGravity=false;body.constraints=RigidbodyConstraints.FreezeAll;
        enemy.ConfigureStats(100,8,.2f,.3f);enemy.SetFacing(Vector3.back);
        Check(enemy.GetComponentInChildren<PrototypeCharacterView>().UseMinion1Model(),"supplied skin installed");
        var view=enemy.GetComponentInChildren<Minion1View>();var skin=view.GetComponentInChildren<SkinnedMeshRenderer>();
        Check(skin.sharedMesh.triangles.Length==150000,"all 50000 source triangles retained");Check(skin.sharedMaterial.mainTexture!=null,"supplied texture loaded");
        foreach(var weight in skin.sharedMesh.boneWeights)Check(Mathf.Abs(weight.weight0+weight.weight1+weight.weight2+weight.weight3-1)<.005f,"normalized skin weights");
        foreach(string clip in new[]{"PunchLeft","PunchRight","MmaKick","Kicking","Walking","FightingIdle","HitReaction","InwardBlock"})
        {
            for(int frame=0;frame<=10;frame++)
            {
                Vector3 root=enemy.transform.position;view.Preview(clip,frame/10f);
                var bounds=SkinBounds(skin,root);Check(bounds.size.y<2.3f&&bounds.size.y>.6f,"plausible character size "+clip);
                Check(enemy.transform.position==root,"preview preserves physics root");
            }
            if(clip=="PunchRight")view.Preview(clip,22/51f);
            if(clip=="PunchLeft")view.Preview(clip,34/62f);
            if(clip.StartsWith("Punch"))
            {
                string side=clip=="PunchRight"?"Right":"Left";
                Vector3 upper=view.Bone(side+"Arm").position,elbow=view.Bone(side+"ForeArm").position,hand=view.Bone(side+"Hand").position;
                float angle=Vector3.Angle(upper-elbow,hand-elbow);
                Check(angle>(side=="Right"?165:125),"source elbow extension preserved "+clip+" "+angle);
            }
            else if(clip=="MmaKick")view.Preview(clip,37/96f);
            else if(clip=="Kicking")view.Preview(clip,69/129f);
            Debug.Log("MINION_PREVIEW_READY clip="+clip);yield return new WaitForSeconds(.4f);
        }
        view.ResumeRuntime();enemy.RestoreForTutorial();
        for(int i=0;i<4;i++)
        {
            bool heavy=i>=2;Check(enemy.TryAttack(heavy),"real combat attack starts");yield return new WaitForSeconds(.1f);
            Check(view.CurrentMotion==(i==0?"PunchLeft":i==1?"PunchRight":i==2?"MmaKick":"Kicking"),"correct runtime attack selection "+i);
            yield return new WaitForSeconds(heavy?1:.6f);Check(enemy.CanAct,"attack returns control");
        }
        body.useGravity=true;body.constraints=RigidbodyConstraints.FreezeRotation;brain.MoveSpeed=1.6f;brain.ThinkInterval=.25f;brain.Active=true;
        Vector3 before=enemy.transform.position;yield return new WaitForSeconds(.6f);
        Check(view.CurrentMotion=="Walking"&&Vector3.Distance(before,enemy.transform.position)>.4f,"AI walking and animation agree");
        float hp=player.Health.Current,timeout=Time.time+12;
        while(player.Health.Current>=hp&&Time.time<timeout)yield return null;
        Check(player.Health.Current<hp,"AI punches deal real damage");brain.Active=false;yield return new WaitForSeconds(1);
        Check(enemy.ReceiveHit(new HitRequest(player.gameObject,8,Vector3.right,false,0)),"enemy receives real damage");
        yield return new WaitForSeconds(.08f);Check(view.CurrentMotion=="HitReaction","hit reaction connected");
        yield return new WaitForSeconds(.4f);
        Check(enemy.ReceiveHit(new HitRequest(player.gameObject,9999,Vector3.right,false,0)),"lethal hit accepted");yield return new WaitForSeconds(.8f);
        var corpse=enemy.GetComponent<DefeatedBody>();Check(corpse.Settled,"corpse settles");Check(corpse.TryBounds(out Bounds dead),"corpse actual mesh measured");
        Check(Mathf.Abs(dead.min.y-.015f)<.025f,"corpse rests on floor");Check(!enemy.GetComponent<Collider>().enabled,"corpse collision disabled");
        enemy.RestoreForTutorial();yield return new WaitForSeconds(.2f);Check(enemy.Health.Alive&&enemy.GetComponent<Collider>().enabled,"revival restores actor");
        Debug.Log("MINION_TEST_PASS checks="+checks);
    }
}
