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
    // Match the renderer skinning equation directly. FBX hierarchies may contain
    // non-unit scale; applying TransformPoint to BakeMesh output can apply it twice.
    private Vector3[] WorldVertices(SkinnedMeshRenderer renderer)
    {
        Mesh mesh = renderer.sharedMesh;
        Matrix4x4[] bind = mesh.bindposes, matrices = new Matrix4x4[bind.Length];
        for (int i = 0; i < bind.Length; i++) matrices[i] = renderer.bones[i].localToWorldMatrix * bind[i];
        Vector3[] vertices = mesh.vertices, result = new Vector3[vertices.Length];
        BoneWeight[] weights = mesh.boneWeights;
        for (int i = 0; i < vertices.Length; i++)
        {
            BoneWeight w = weights[i]; Vector3 v = vertices[i];
            result[i] = matrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0 +
                matrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1 +
                matrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2 +
                matrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
        }
        return result;
    }
    private Bounds WorldBounds(SkinnedMeshRenderer renderer)
    {
        Vector3[] vertices = WorldVertices(renderer);
        Bounds bounds = new Bounds(vertices[0], Vector3.zero);
        foreach (Vector3 v in vertices) bounds.Encapsulate(v);
        return bounds;
    }
    private IEnumerator Start()
    {
        if(Application.absoluteURL.Contains("minionReview=1"))
        {gameObject.AddComponent<Minion1PlaytestRunner>();yield break;}
        if(Application.absoluteURL.Contains("punchReview=1"))
        {gameObject.AddComponent<PunchPlaytestRunner>();yield break;}
        if(Application.absoluteURL.Contains("duelReview=1"))
        {gameObject.AddComponent<DeongchiDuelPlaytestRunner>();yield break;}
        if(Application.absoluteURL.Contains("corpseReview=1"))
        {gameObject.AddComponent<CorpsePlaytestRunner>();yield break;}
        Camera fixtureCamera = new GameObject("Camera").AddComponent<Camera>();
        fixtureCamera.clearFlags = CameraClearFlags.SolidColor;
        fixtureCamera.backgroundColor = new Color(.18f,.19f,.2f);
        fixtureCamera.transform.position = new Vector3(.55f, 1, -4);
        fixtureCamera.transform.LookAt(new Vector3(.55f, 0, 0));
        fixtureCamera.orthographic = true; fixtureCamera.orthographicSize = 1.35f;
        Light fixtureLight = new GameObject("Fixture light").AddComponent<Light>();
        fixtureLight.type = LightType.Directional; fixtureLight.intensity = 1.1f;
        fixtureLight.transform.rotation = Quaternion.Euler(50, -30, 0);
        if (Application.absoluteURL.Contains("authoredReview="))
        { new GameObject("Authored motion tests").AddComponent<AuthoredMotionPlaytestRunner>(); yield break; }
        Check(TutorialInputGate.Allows(SchoolRuleZero.TutorialStep.Jump,KeyCode.Space),"jump lesson accepts Space");
        foreach(var blocked in new[]{KeyCode.W,KeyCode.J,KeyCode.K,KeyCode.L,KeyCode.C,KeyCode.I,KeyCode.Q})
            Check(!TutorialInputGate.Allows(SchoolRuleZero.TutorialStep.Jump,blocked),"jump lesson rejects "+blocked);
        Check(!TutorialInputGate.Allows(SchoolRuleZero.TutorialStep.Move,KeyCode.Space),"move lesson rejects jump");
        Check(!TutorialInputGate.Allows(SchoolRuleZero.TutorialStep.Basic,KeyCode.K),"basic lesson rejects heavy attack");
        Check(TutorialInputGate.Allows(SchoolRuleZero.TutorialStep.Complete,KeyCode.Q),"complete tutorial unlocks ultimate");
        Check(TutorialInputGate.Allows(SchoolRuleZero.TutorialStep.Jump,KeyCode.Escape),"pause remains available");
        var practiceGauge=new SchoolRuleZero.CombatProgress();practiceGauge.FillGaugeForPractice();
        Check(practiceGauge.UltimateTier==3&&practiceGauge.Combo==0,"practice refill grants meter without inventing hits");
        practiceGauge.ConsumeUltimate();practiceGauge.FillGaugeForPractice();
        Check(practiceGauge.UltimateTier==3,"missed ultimate can be retried without unrelated attack keys");
        CombatActor player=Actor("권민준",0,120),enemy=Actor("불량 학생",1,100);
        GameObject playerVisual = new GameObject("Player model test visual");
        playerVisual.transform.SetParent(player.transform, false);
        PrototypeCharacterView character = playerVisual.AddComponent<PrototypeCharacterView>();
        character.Build(player, false);
        ImportedPlayerView imported = playerVisual.GetComponent<ImportedPlayerView>();
        Check(imported != null && imported.IsReady, "imported player rig ready");
        SkinnedMeshRenderer render = playerVisual.GetComponentInChildren<SkinnedMeshRenderer>();
        Check(render != null && render.sharedMesh.vertexCount < 25000, "web player mesh simplified and skinned");
        Check(render.sharedMesh.isReadable, "imported skin vertex data readable for runtime verification");
        Debug.Log("MOTION_METRICS player vertexCount=" + render.sharedMesh.vertexCount + " scale=" + render.transform.lossyScale + " world=" + WorldBounds(render));
        Check(render.sharedMaterial.mainTexture != null, "player source texture bound");
        Check(render.bounds.size.y > 1.6f && render.bounds.size.y < 2.1f, "player model normalized to game scale");
        if (Application.absoluteURL.Contains("deongchiPose="))
        {
            playerVisual.SetActive(false);
            GameObject model = new GameObject("Deongchi pose preview"); model.transform.SetParent(enemy.transform, false);
            PrototypeCharacterView previewView = model.AddComponent<PrototypeCharacterView>(); previewView.Build(enemy, false); previewView.UseDeongchiModel(); previewView.CinematicPose = true;
            ImportedPlayerView rig = model.GetComponent<ImportedPlayerView>();
            if (Application.absoluteURL.Contains("deongchiPose=stride")) rig.PreviewFighting(1.8f, 1);
            else if (Application.absoluteURL.Contains("deongchiPose=jab")) rig.PreviewPunch(.45f);
            else if (Application.absoluteURL.Contains("deongchiPose=cross")) rig.PreviewPunch(.45f, true);
            else rig.PreviewFighting();
            yield return new WaitForEndOfFrame(); Debug.Log("DEONGCHI_PREVIEW_READY"); yield break;
        }
        if (Application.absoluteURL.Contains("hookPose="))
        {
            character.CinematicPose = true;
            float preview = Application.absoluteURL.Contains("hookPose=turn") ? .30f :
                Application.absoluteURL.Contains("hookPose=impact") ? .48f : 1;
            playerVisual.GetComponent<ImportedPlayerView>().PreviewHook(preview);
            yield return new WaitForEndOfFrame();
            Debug.Log("HOOK_PREVIEW_READY");
            yield break;
        }
        if (Application.absoluteURL.Contains("fightPose="))
        {
            character.CinematicPose = true;
            if (Application.absoluteURL.Contains("fightPose=stride")) imported.PreviewFighting(1.8f, 1);
            else if (Application.absoluteURL.Contains("fightPose=roundhouse")) imported.PreviewRoundhouse(.48f);
            else imported.PreviewFighting();
            yield return new WaitForEndOfFrame(); Debug.Log("FIGHT_PREVIEW_READY"); yield break;
        }
        character.CinematicPose = true;
        Vector3[] restVertices = WorldVertices(render);
        character.SetForcedKick(.5f);
        Vector3[] kickVertices = WorldVertices(render);
        float deformation = 0;
        for (int i = 0; i < restVertices.Length; i++)
            deformation = Mathf.Max(deformation, Vector3.Distance(restVertices[i], kickVertices[i]));
        Check(deformation > .03f, "imported mesh skin deforms during kick");
        Transform kickBone = null;
        foreach (Transform bone in playerVisual.GetComponentsInChildren<Transform>())
            if (bone.name == "RightThigh") kickBone = bone;
        Quaternion kickPose = kickBone.localRotation;
        character.SetForcedKick(0);
        ImportedPlayerView hookView = playerVisual.GetComponent<ImportedPlayerView>();
        Transform supportFoot = null, hookFoot = null;
        foreach (Transform bone in playerVisual.GetComponentsInChildren<Transform>())
        { if (bone.name == "LeftFoot") supportFoot = bone; if (bone.name == "RightFoot") hookFoot = bone; }
        character.CinematicPose = true;
        Quaternion ankleRest = hookFoot.localRotation;
        hookView.PreviewHook(0);
        Quaternion readyRotation = playerVisual.transform.rotation;
        Vector3 supportHome = supportFoot.position, kickHome = hookFoot.position;
        Vector3 physicalHome = player.transform.position;
        Vector3 facingHome = player.Facing;
        for (int frame = 0; frame <= 20; frame++)
        {
            hookView.PreviewHook(frame / 20f);
            Check(Vector3.Distance(supportFoot.position, supportHome) < .06f, "hook support ankle planted phase " + frame);
            if (frame >= 8 && frame <= 15) Check(Quaternion.Angle(hookFoot.localRotation, ankleRest) < .1f, "airborne hook ankle aligned to shin phase " + frame);
            bool bounded = true;
            foreach (Vector3 v in WorldVertices(render))
                bounded &= !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && (v - physicalHome).magnitude < 3;
            Check(bounded, "hook skin finite and bounded phase " + frame);
        }
        hookView.PreviewHook(.48f);
        Check(hookFoot.position.y > kickHome.y + 1.25f, "hook raised right heel reaches high target");
        Transform hookHip = null, hookKnee = null;
        foreach (Transform bone in playerVisual.GetComponentsInChildren<Transform>())
        { if (bone.name == "RightThigh") hookHip = bone; if (bone.name == "RightShin") hookKnee = bone; }
        // Keep a soft striking knee (source pose is ~20 degrees), clearly distinct from the folded chamber.
        Check(Vector3.Angle(hookKnee.position - hookHip.position, hookFoot.position - hookKnee.position) < 30,
            "hook striking knee extends without folding across torso");
        Check(Quaternion.Angle(playerVisual.transform.rotation, Quaternion.LookRotation(facingHome)) < .1f,
            "hook torso turn finishes by impact instead of late recovery");
        hookView.PreviewHook(1);
        Check(Vector3.Distance(hookFoot.position, kickHome) < .06f, "hook same right foot lands in ready stance");
        Check(Quaternion.Angle(playerVisual.transform.rotation, readyRotation) < .1f, "hook full turn returns to fighting stance");
        Check(player.transform.position == physicalHome && player.Facing == facingHome, "hook changes visuals without rotating combat physics");
        hookView.PreviewFighting();
        Check(Mathf.Abs(supportFoot.position.y - hookFoot.position.y) < .01f, "ready feet both planted");
        Check(Vector3.Distance(supportFoot.position, hookFoot.position) > .5f, "ready stance widened and staggered");
        Vector3 readyLeft = supportFoot.position, readyRight = hookFoot.position;
        Transform readyKnee = null, readyHand = null, readyHead = null;
        foreach (Transform bone in playerVisual.GetComponentsInChildren<Transform>())
        { if (bone.name == "RightShin") readyKnee = bone; if (bone.name == "RightHand") readyHand = bone; if (bone.name == "Head") readyHead = bone; }
        Check(Vector3.Angle(readyKnee.position - kickBone.position, hookFoot.position - readyKnee.position) > 20, "low fighting stance bends knees");
        Check(Vector3.Distance(readyHand.position, readyHead.position) < .4f, "ready guard stays near jaw");
        for (int leg = 0; leg < 2; leg++)
        for (int frame = 0; frame <= 20; frame++)
        {
            hookView.PreviewRoundhouse(frame / 20f, leg == 1);
            Transform supporting = leg == 0 ? supportFoot : hookFoot;
            Check(Vector3.Distance(supporting.position, leg == 0 ? readyLeft : readyRight) < .06f,
                "roundhouse support planted leg " + leg + " phase " + frame);
            bool finite = true;
            foreach (Vector3 v in WorldVertices(render)) finite &= !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && (v - physicalHome).magnitude < 3;
            Check(finite, "roundhouse skin finite leg " + leg + " phase " + frame);
        }
        hookView.PreviewRoundhouse(1);
        Check(Vector3.Distance(hookFoot.position, readyRight) < .02f && Quaternion.Angle(playerVisual.transform.rotation, readyRotation) < .1f, "roundhouse returns to same low ready stance");
        for (int step = 0; step < 8; step++)
        {
            hookView.PreviewFighting(step * Mathf.PI / 4, 1);
            Check(Vector3.Distance(readyHand.position, readyHead.position) < .4f, "moving guard stays near jaw phase " + step);
            Check(WorldBounds(render).size.y < 2.1f && supportFoot.position.y >= readyLeft.y - .01f && hookFoot.position.y >= readyRight.y - .01f, "guarded movement feet and silhouette phase " + step);
        }
        hookView.PreviewFighting();
        Check(player.transform.position == physicalHome && player.Facing == facingHome, "fighting poses preserve physics and facing");
        character.CinematicPose = false;
        Check(Quaternion.Angle(kickPose, kickBone.localRotation) > 40, "imported kick bone responds to existing cinematic pose");
        character.CinematicPose = false;
        // Exercise all kick phases and reject non-finite/escaping mesh vertices.
        for (int phase = 0; phase <= 10; phase++)
        {
            character.SetForcedKick(phase / 10f);
            if (phase == 0) Debug.Log("MOTION_METRICS player scale=" + render.transform.lossyScale + " world=" + WorldBounds(render));
            bool valid = true;
            foreach (Vector3 v in WorldVertices(render))
                valid &= !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && (v - player.transform.position).magnitude < 3;
            Check(valid, "kick mesh finite and bounded phase " + phase);
        }
        character.SetForcedKick(0);
        GameObject enemyVisual = new GameObject("Deongchi test visual");
        enemyVisual.transform.SetParent(enemy.transform, false);
        PrototypeCharacterView big = enemyVisual.AddComponent<PrototypeCharacterView>(); big.Build(enemy, false);
        Check(big.UseDeongchiModel(), "Deongchi supplied model replaces placeholder");
        Check(enemyVisual.GetComponent<ImportedPlayerView>().IsReady, "Deongchi complete joint rig");
        yield return null;
        SkinnedMeshRenderer bigRender = enemyVisual.GetComponentInChildren<SkinnedMeshRenderer>();
        Check(bigRender.sharedMaterial.mainTexture != null, "Deongchi texture loaded");
        ImportedPlayerView bigRig = enemyVisual.GetComponent<ImportedPlayerView>();
        big.CinematicPose = true; bigRig.PreviewFighting();
        Transform bigLeftFoot = null, bigRightFoot = null, bigHead = null, bigLeftHand = null, bigRightHand = null;
        foreach (Transform bone in enemyVisual.GetComponentsInChildren<Transform>())
        { if (bone.name == "LeftFoot") bigLeftFoot = bone; if (bone.name == "RightFoot") bigRightFoot = bone;
          if (bone.name == "Head") bigHead = bone; if (bone.name == "LeftHand") bigLeftHand = bone; if (bone.name == "RightHand") bigRightHand = bone; }
        Vector3 bigLeftHome = bigLeftFoot.position, bigRightHome = bigRightFoot.position;
        Quaternion bigReadyRotation = enemyVisual.transform.rotation;
        float bigFloor = WorldBounds(bigRender).min.y;
        for (int side = 0; side < 2; side++)
        {
            bigRig.PreviewPunch(0, side == 1);
            Vector3 handHome = side == 0 ? bigLeftHand.position : bigRightHand.position;
            for (int phase = 0; phase <= 20; phase++)
            {
                bigRig.PreviewPunch(phase / 20f, side == 1);
                Check(Vector3.Distance(bigLeftFoot.position, bigLeftHome) < .06f && Vector3.Distance(bigRightFoot.position, bigRightHome) < .06f, "Deongchi punch planted feet side " + side + " phase " + phase);
                bool finite = true;
                foreach (Vector3 v in WorldVertices(bigRender)) finite &= !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && (v-enemy.transform.position).magnitude < 3;
                Check(finite && WorldBounds(bigRender).min.y > bigFloor-.09f, "Deongchi punch bounded skin and ground side " + side + " phase " + phase);
            }
            bigRig.PreviewPunch(.45f, side == 1);
            Transform activeHand = side == 0 ? bigLeftHand : bigRightHand;
            Transform otherHand = side == 0 ? bigRightHand : bigLeftHand;
            Check(Vector3.Dot(activeHand.position-handHome, enemy.Facing) > .2f, "Deongchi punch reaches forward side " + side);
            Check(Vector3.Distance(otherHand.position,bigHead.position) < .45f, "Deongchi off hand guard side " + side);
            Check(Quaternion.Angle(bigReadyRotation,enemyVisual.transform.rotation) > 20, "Deongchi punch rotates pelvis side " + side);
            bigRig.PreviewPunch(1, side == 1);
            Check(Vector3.Distance(activeHand.position,handHome) < .03f && Quaternion.Angle(bigReadyRotation,enemyVisual.transform.rotation)<.1f, "Deongchi punch returns to guard side " + side);
        }
        for(int phase=0;phase<12;phase++)
        {
            bigRig.PreviewFighting(phase*Mathf.PI/6,1);
            Check(bigLeftFoot.position.y>=bigLeftHome.y-.03f && bigRightFoot.position.y>=bigRightHome.y-.03f, "Deongchi movement feet above ground phase " + phase);
            Check(Vector3.Distance(bigLeftHand.position,bigHead.position)<.45f && Vector3.Distance(bigRightHand.position,bigHead.position)<.45f, "Deongchi movement maintains guard phase " + phase);
        }
        bigRig.PreviewFighting(); big.CinematicPose=false;
        yield return null; yield return new WaitForEndOfFrame();
        Bounds standingBounds = WorldBounds(bigRender);
        float standingHeight = standingBounds.size.y;
        Check(enemy.TryCrouch(), "Deongchi gameplay crouch starts");
        yield return new WaitForSeconds(.2f);
        yield return new WaitForEndOfFrame();
        Bounds sittingBounds = WorldBounds(bigRender);
        Debug.Log("MOTION_METRICS crouch standing=" + standingHeight + " sitting=" + sittingBounds.size.y + " footShift=" + Mathf.Abs(sittingBounds.min.y - standingBounds.min.y));
        Check(sittingBounds.size.y < standingHeight - .35f, "crouch bends rig and lowers silhouette");
        Check(enemyVisual.transform.localScale == Vector3.one, "crouch preserves mesh proportions");
        Check(Mathf.Abs(sittingBounds.min.y - standingBounds.min.y) < .08f, "crouch feet stay near floor");
        yield return new WaitForSeconds(.3f);
        foreach (SchoolRuleZero.TutorialStep step in System.Enum.GetValues(typeof(SchoolRuleZero.TutorialStep)))
        {
            enemy.RestoreForTutorial();
            enemy.Health.Infinite = TutorialTrainingRules.HasInfiniteHealth(step);
            bool landed = enemy.ReceiveHit(new HitRequest(player.gameObject, 999, Vector3.right, true, 0));
            Check(landed, "tutorial protection retains hit confirmation " + step);
            Check(step == SchoolRuleZero.TutorialStep.Opening || step == SchoolRuleZero.TutorialStep.Complete ?
                !enemy.Health.Alive : enemy.Health.Current == enemy.Health.Maximum,
                "tutorial health lifetime " + step);
        }
        enemy.Health.Infinite = false; enemy.RestoreForTutorial();
        Transform kneeBone = null, punchArm = null, punchElbow = null;
        foreach (Transform bone in playerVisual.GetComponentsInChildren<Transform>())
            if (bone.name == "RightShin") kneeBone = bone;
        foreach (Transform bone in enemyVisual.GetComponentsInChildren<Transform>())
        {
            if (bone.name == "LeftUpperArm") punchArm = bone;
            if (bone.name == "LeftForearm") punchElbow = bone;
        }
        // Place actors at fighting distance instead of initially interpenetrating.
        player.transform.position = Vector3.zero; enemy.transform.position = Vector3.right * 1.1f;
        player.GetComponent<Rigidbody>().position = player.transform.position;
        enemy.GetComponent<Rigidbody>().position = enemy.transform.position;
        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate(); yield return null;
        Debug.Log("MOTION_METRICS hit source=" + player.transform.position + " target=" + enemy.transform.position +
            " collider=" + enemy.GetComponent<Collider>().bounds);
        Quaternion thighBefore = kickBone.localRotation, kneeBefore = kneeBone.localRotation;
        bool strikeObserved = false;
        System.Action observeStrike = () => strikeObserved = true;
        player.StrikeStarted += observeStrike;
        enemy.Health.Infinite = true;
        Check(player.TryAttack(false), "player real attack starts");
        yield return new WaitForSeconds(player.VisualWindup + .06f);
        yield return new WaitForEndOfFrame();
        Check(strikeObserved, "real gameplay strike event fired");
        Check(Quaternion.Angle(thighBefore, kickBone.localRotation) > 40, "real strike drives extended kick thigh");
        Check(Vector3.Angle(kneeBone.position - kickBone.position, hookFoot.position - kneeBone.position) < 40, "real kick extends chambered knee at impact");
        Check(enemy.Health.Current == enemy.Health.Maximum, "real hitbox preserves infinite tutorial HP");
        Check(player.Progress.Combo > 0, "real hitbox still confirms combo against infinite HP");
        player.StrikeStarted -= observeStrike;
        yield return new WaitForSeconds(.4f);
        enemy.Health.Infinite = false; enemy.RestoreForTutorial();
        Quaternion armBefore = punchArm.localRotation;
        Vector3 handBefore = bigLeftHand.position;
        float elbowBefore = Vector3.Angle(punchElbow.position-punchArm.position,bigLeftHand.position-punchElbow.position);
        bool punchStarted = false;
        System.Action observePunch = () => punchStarted = true;
        enemy.StrikeStarted += observePunch;
        Check(enemy.TryAttack(false), "Deongchi real punch starts");
        float observeUntil = Time.time + enemy.VisualWindup + .25f;
        float armExtension = 0, elbowExtension = 0, handExtension = 0;
        while (Time.time < observeUntil)
        {
            yield return new WaitForEndOfFrame();
            if (punchStarted)
            {
                armExtension = Mathf.Max(armExtension, Quaternion.Angle(armBefore, punchArm.localRotation));
                handExtension = Mathf.Max(handExtension, Vector3.Dot(bigLeftHand.position-handBefore, enemy.Facing));
                float angle = Vector3.Angle(punchElbow.position-punchArm.position,bigLeftHand.position-punchElbow.position);
                elbowExtension = Mathf.Max(elbowExtension, elbowBefore-angle);
            }
            yield return null;
        }
        enemy.StrikeStarted -= observePunch;
        Debug.Log("MOTION_METRICS punch arm=" + armExtension + " elbow=" + elbowExtension);
        Check(punchStarted && enemy.State == CombatActor.ActionState.ATTACK, "Deongchi attack retains gameplay timing");
        Check(handExtension > .2f, "Deongchi real strike extends guarded hand toward target");
        Check(elbowExtension > 20, "Deongchi punch extends elbow");
        yield return new WaitForSeconds(.4f);
        player.RestoreForTutorial(); enemy.RestoreForTutorial();
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
