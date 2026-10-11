using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Attached only to the separately built test scene, never the shipping Prototype scene.
public sealed class HUDPlaytestRunner : MonoBehaviour
{
    private int checks;
    private CombatActor Actor(string label,int team,float hp)
    {
        GameObject go=new GameObject(label); CombatActor actor=go.AddComponent<CombatActor>();
        actor.Configure(team);actor.ConfigureStats(hp,10,.24f,.48f);
        go.GetComponent<Rigidbody>().useGravity=false;
        go.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll; return actor;
    }
    private void Check(bool pass,string label)
    {
        if(!pass) { Debug.LogError("HUD_TEST_FAIL "+label); throw new Exception(label); }
        checks++;Debug.Log("HUD_TEST_CHECK "+label);
    }
    private IEnumerator Start()
    {
        new GameObject("Camera").AddComponent<Camera>().backgroundColor=new Color(.18f,.19f,.2f);
        CombatActor player=Actor("권민준",0,120),enemy=Actor("불량 학생",1,100);
        GameObject playerVisual = new GameObject("Player model test visual");
        playerVisual.transform.SetParent(player.transform, false);
        PrototypeCharacterView character = playerVisual.AddComponent<PrototypeCharacterView>();
        character.Build(player);
        ImportedPlayerView imported = playerVisual.GetComponent<ImportedPlayerView>();
        Check(imported != null && imported.IsReady, "imported player rig ready");
        SkinnedMeshRenderer render = playerVisual.GetComponentInChildren<SkinnedMeshRenderer>();
        Check(render != null && render.sharedMesh.vertexCount < 25000, "web player mesh simplified and skinned");
        Check(render.sharedMaterial.mainTexture != null, "player source texture bound");
        Check(render.bounds.size.y > 1.6f && render.bounds.size.y < 2.1f, "player model normalized to game scale");
        character.CinematicPose = true;
        Mesh restMesh = new Mesh();
        render.BakeMesh(restMesh);
        character.SetForcedKick(.5f);
        Mesh kickMesh = new Mesh();
        render.BakeMesh(kickMesh);
        Vector3[] restVertices = restMesh.vertices, kickVertices = kickMesh.vertices;
        float deformation = 0;
        for (int i = 0; i < restVertices.Length; i++)
            deformation = Mathf.Max(deformation, Vector3.Distance(restVertices[i], kickVertices[i]));
        Check(deformation > .03f, "imported mesh skin deforms during kick");
        Destroy(restMesh); Destroy(kickMesh);
        Transform kickBone = null;
        foreach (Transform bone in playerVisual.GetComponentsInChildren<Transform>())
            if (bone.name == "RightThigh") kickBone = bone;
        Quaternion kickPose = kickBone.localRotation;
        character.SetForcedKick(0);
        Check(Quaternion.Angle(kickPose, kickBone.localRotation) > 40, "imported kick bone responds to existing cinematic pose");
        character.CinematicPose = false;
        PrototypeHUD hud=gameObject.AddComponent<PrototypeHUD>();hud.Initialize(player,enemy);
        hud.EncounterText="학교 1-5 · 잔몹 라운드 1\n라운드 1/3 · 남은 적 4";
        CombatHUDView view=GetComponent<CombatHUDView>();
        yield return null;yield return null;
        Check(view.PlayerMeter.ValueText=="120 / 120","initial player HP");
        foreach(string label in new[]{"Player panel surface","Enemy panel surface","State overlay surface","Player HP fill","Player HP outline"})
        {
            Image visual=GameObject.Find("Combat HUD").GetComponent<RectTransform>().GetComponentsInChildren<Image>(true)[0];
            foreach(Image candidate in GameObject.Find("Combat HUD").GetComponentsInChildren<Image>(true))
                if(candidate.name==label) {visual=candidate;break;}
            Check(visual.name==label && visual.sprite!=null && visual.sprite.name.StartsWith("Pixel "),"Pixel exported sprite bound "+label);
        }
        player.ReceiveHit(new HitRequest(enemy.gameObject,30,Vector3.left,true,0));
        yield return new WaitForSecondsRealtime(.08f);
        Check(view.PlayerMeter.ValueText=="90 / 120","real player damage numeric binding");
        Check(view.PlayerMeter.DisplayedRatio>.75f&&view.PlayerMeter.DisplayedRatio<1,"HP decrease interpolates");
        Image playerFill=GameObject.Find("Player HP fill").GetComponent<Image>();
        Check(playerFill.color.r>playerFill.color.g,"damage pulse visible");
        yield return new WaitForSecondsRealtime(.3f);
        Check(Mathf.Abs(view.PlayerMeter.DisplayedRatio-.75f)<.001f,"player decrease settles");
        enemy.ReceiveHit(new HitRequest(player.gameObject,25,Vector3.right,true,0));
        yield return new WaitForSecondsRealtime(.35f);
        Check(view.EnemyMeter.ValueText=="75 / 100"&&Mathf.Abs(view.EnemyMeter.DisplayedRatio-.75f)<.001f,"real enemy damage binding");
        CombatActor boss=Actor("도재학",1,240);boss.BossUltimateEnabled=true;hud.SetEnemyTarget(boss);
        yield return null;yield return null;
        Check(view.EnemyRoleText.Contains("BOSS")&&view.EnemyNameText=="도재학","boss name and role");
        Check(GameObject.Find("Enemy HP outline").GetComponent<Image>().sprite.name=="Pixel BossFrame","dedicated boss frame");
        Check(view.EnemyMeter.ValueText=="240 / 240"&&view.EnemyMeter.DisplayedRatio==1,"new target resets bar");
        boss.ReceiveHit(new HitRequest(player.gameObject,60,Vector3.right,true,0));
        yield return new WaitForSecondsRealtime(.35f);
        Check(view.EnemyMeter.ValueText=="180 / 240","boss damage binding");
        for(int i=0;i<6;i++) player.Progress.ConfirmHit(Time.time);
        yield return null;yield return null;
        Check(view.ComboText=="6 HIT  GOOD!","combo read from combat progress");
        Check(GameObject.Find("Combo").GetComponent<Text>().font.name.Contains("Arcade"),"arcade font bound to real combo count");
        hud.StoryMessage="1-5 클리어! Enter · 다음 스테이지";
        yield return null;yield return null;
        Check(view.ModalText=="전투 완료","clear overlay uses director message");
        HitImpactManager impact=gameObject.AddComponent<HitImpactManager>();impact.SetPaused(true);
        yield return null;yield return null;
        Check(view.ModalText=="일시정지","pause overlay at timescale zero");impact.SetPaused(false);
        player.ReceiveHit(new HitRequest(boss.gameObject,999,Vector3.left,true,0));
        yield return new WaitForSecondsRealtime(.35f);
        Check(view.ModalText=="쓰러졌다"&&view.PlayerMeter.ValueText=="0 / 120","game over real health");
        player.RestoreForTutorial();hud.StoryMessage="";hud.SetEnemyTarget(null);
        yield return null;yield return null;
        Check(view.ModalText==""&&view.EnemyMeter.ValueText=="— / —","retry and empty target reset");
        Check(GameObject.Find("Current controls").GetComponent<Text>().text.Contains("C 앉기"),"existing C input guide retained");
        yield return new WaitForEndOfFrame();Canvas.ForceUpdateCanvases();
        string[] panels={"Player panel","Enemy panel","Objective panel","Combo panel","Key guide","Dialogue panel","State overlay"};
        RectTransform ui=GameObject.Find("Combat HUD").GetComponent<RectTransform>();
        Rect area=ui.rect;Vector3[] corners=new Vector3[4];
        foreach(string label in panels)
        {
            Transform found=ui.Find(label);Check(found!=null,"panel exists "+label);
            RectTransform panel=(RectTransform)found;panel.GetWorldCorners(corners);
            foreach(Vector3 corner in corners)
            {
                Vector3 local=ui.InverseTransformPoint(corner);
                Check(local.x>=area.xMin-1&&local.x<=area.xMax+1&&local.y>=area.yMin-1&&local.y<=area.yMax+1,"anchored bounds "+label);
            }
        }
        RectTransform a=(RectTransform)ui.Find("Player panel"),b=(RectTransform)ui.Find("Enemy panel"),c=(RectTransform)ui.Find("Objective panel");
        Check(a.anchorMax.x<b.anchorMin.x&&b.anchorMax.x<c.anchorMin.x,"top panels never overlap");
        Check(player.Health.Current==120&&boss.Health.Current==180,"presentation does not modify health");
        hud.SetEnemyTarget(boss);
        Debug.Log("HUD_TEST_PASS checks="+checks+" resolution="+Screen.width+"x"+Screen.height);
    }
}
