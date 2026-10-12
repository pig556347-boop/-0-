using UnityEngine;

public sealed class PrototypeBootstrap : MonoBehaviour
{
    public static bool RunSmokeChecks;
    private static Transform scenery;
    private void Start()
    {
#if UNITY_EDITOR
        // Read after domain reload, directly in scene startup, rather than relying
        // on runtime initialization callbacks inside an editor-only assembly.
        RunSmokeChecks = UnityEditor.SessionState.GetBool("SchoolRuleZero.RunSmoke", RunSmokeChecks);
        UnityEditor.SessionState.EraseBool("SchoolRuleZero.RunSmoke");
#endif
        CreateBox("Classroom floor", new Vector3(0, -.5f, 0),
            new Vector3(28, 1, 18), new Color(.55f, .55f, .53f));
        CreateClassroom();
        GameObject player = new GameObject("권민준");
        player.transform.position = new Vector3(-3, 1.05f, -2);
        CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
        capsule.height = 2; capsule.radius = .4f;
        Rigidbody body = player.AddComponent<Rigidbody>();
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        GameObject visual = new GameObject("Sian school uniform visual");
        visual.transform.SetParent(player.transform, false);
        player.AddComponent<PlayerController>().SetVisualRoot(visual.transform);
        player.AddComponent<CombatInput>();
        player.AddComponent<CombatActor>().Configure(0);
        player.AddComponent<ActorSeparation>();
        visual.AddComponent<PrototypeCharacterView>().Build(player.GetComponent<CombatActor>());
        GameObject enemy = CreateOpponent("Enemy", new Vector3(1, 1.05f, -2),
            player.GetComponent<CombatActor>()).gameObject;
        MotionVerificationProbe.Install(player.GetComponent<CombatActor>(), enemy.GetComponent<CombatActor>());
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        GameObject cameraRig = new GameObject("Combat camera rig");
        camera.transform.SetParent(cameraRig.transform, false);
        camera.transform.rotation = Quaternion.Euler(35, 0, 0);
        camera.orthographic = true; camera.orthographicSize = 6.5f;
        camera.transform.localPosition = Quaternion.Euler(35, 0, 0) * Vector3.back * 20;
        cameraRig.AddComponent<CombatCameraFollow>().Initialize(player.GetComponent<CombatActor>());
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.12f, .12f, .14f);
        cameraObject.AddComponent<AudioListener>();
        HitImpactManager impact = gameObject.AddComponent<HitImpactManager>();
        impact.Initialize(camera);
        player.GetComponent<CombatActor>().HitConfirmed += impact.OnHit;
        enemy.GetComponent<CombatActor>().HitConfirmed += impact.OnHit;
        CombatEffects effects = gameObject.AddComponent<CombatEffects>();
        player.GetComponent<CombatActor>().HitConfirmed += effects.OnHit;
        enemy.GetComponent<CombatActor>().HitConfirmed += effects.OnHit;
        player.GetComponent<CombatActor>().Dodged += () => effects.DodgeTrail(player.GetComponent<CombatActor>());
        PrototypeHUD hud = gameObject.AddComponent<PrototypeHUD>();
        hud.Initialize(player.GetComponent<CombatActor>(), enemy.GetComponent<CombatActor>());
        if (RunSmokeChecks) gameObject.AddComponent<PrototypeSmokeTest>();
        else
        {
            TutorialDirector lessons = gameObject.AddComponent<TutorialDirector>();
            lessons.Initialize(player.GetComponent<CombatActor>(), enemy.GetComponent<CombatActor>(), hud);
            gameObject.AddComponent<EncounterDirector>().Initialize(player.GetComponent<CombatActor>(),
                enemy.GetComponent<CombatActor>(), lessons, hud);
        }
        RunSmokeChecks = false;
        Light light = new GameObject("Key light").AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.1f;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
    }

    public static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name; box.transform.position = position; box.transform.localScale = scale;
        if (name != "Classroom floor" && name != "Back wall" && !name.EndsWith("boundary"))
            box.GetComponent<Collider>().enabled = false;
        Shader shader = Resources.Load<Shader>("Shaders/SchoolToon");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader); material.color = color;
        box.GetComponent<Renderer>().sharedMaterial = material;
        box.AddComponent<OwnedMaterial>().Material = material;
        if (scenery != null && name != "Classroom floor" && name != "Opening desk")
            box.transform.SetParent(scenery, true);
        return box;
    }
    public static CombatActor CreateOpponent(string name, Vector3 position, CombatActor player)
    {
        GameObject enemy = new GameObject(name); enemy.transform.position = position;
        CapsuleCollider capsule = enemy.AddComponent<CapsuleCollider>();
        capsule.height = 2; capsule.radius = .4f;
        Rigidbody body = enemy.AddComponent<Rigidbody>();
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        CombatActor actor = enemy.AddComponent<CombatActor>(); actor.Configure(1);
        GameObject visual = new GameObject("School uniform visual");
        visual.transform.SetParent(enemy.transform, false);
        visual.AddComponent<PrototypeCharacterView>().Build(actor);
        enemy.AddComponent<EnemyBrain>().Target = player;
        enemy.AddComponent<ActorSeparation>();
        enemy.AddComponent<EnemyHealthBar>();
        return actor;
    }
    private static void CreateClassroom()
    {
        scenery = new GameObject("Stage scenery").transform;
        Color wall = new Color(.7f, .7f, .68f);
        CreateBoundary("Back wall", new Vector3(0, 2, 8.8f), new Vector3(28, 4, .3f), wall);
        CreateBoundary("Left boundary", new Vector3(-13.8f, 1, 0), new Vector3(.3f, 2, 18), wall);
        CreateBoundary("Right boundary", new Vector3(13.8f, 1, 0), new Vector3(.3f, 2, 18), wall);
        CreateBoundary("Front boundary", new Vector3(0, .2f, -8.8f), new Vector3(28, .4f, .3f), wall);
        CreateBox("Chalkboard", new Vector3(0, 2.2f, 8.55f), new Vector3(8, 2, .12f),
            new Color(.1f, .15f, .14f));
        for (int row = 0; row < 2; row++) for (int col = 0; col < 4; col++)
        {
            Vector3 desk = new Vector3(-6 + col * 4, .8f, 1.3f + row * 2.3f);
            CreateBox("Desk top", desk, new Vector3(1.5f, .15f, .9f), new Color(.5f, .45f, .38f));
            CreateBox("Desk support", desk + Vector3.down * .4f,
                new Vector3(.75f, .75f, .5f), new Color(.18f, .18f, .2f));
        }
        for (int i = 0; i < 3; i++)
            CreateBox("Window", new Vector3(-9.6f, 2.4f, -2 + i * 2.5f),
                new Vector3(.08f, 1.5f, 1.8f), new Color(.85f, .63f, .43f));
        CreateBox("Classroom drawers", new Vector3(7.8f, .65f, 5),
            new Vector3(2.4f, 1.3f, 1), new Color(.43f, .4f, .36f));
        for (int i = 0; i < 3; i++)
        {
            CreateBox("Drawer front", new Vector3(7.8f, .25f + .4f * i, 4.48f),
                new Vector3(2.2f, .32f, .06f), new Color(.5f, .46f, .4f));
            CreateBox("Drawer handle", new Vector3(7.8f, .25f + .4f * i, 4.43f),
                new Vector3(.35f, .05f, .04f), new Color(.15f, .15f, .17f));
            GameObject sunbeam = CreateBox("Sunset floor light", new Vector3(-7.2f, .012f, -2 + i * 2.5f),
                new Vector3(3.8f, .008f, 1.4f), new Color(.68f, .54f, .4f));
            sunbeam.GetComponent<Collider>().enabled = false;
            sunbeam.transform.rotation = Quaternion.Euler(0, -15, 0);
        }
    }
    public static void SetArena(int stage, int area, bool boss)
    {
        if (scenery != null) { scenery.gameObject.SetActive(false); Destroy(scenery.gameObject); }
        CreateClassroom();
        // Reuse the classroom kit, but open the centre for corridor/boss layouts.
        bool corridor = area == 2;
        for (int i = scenery.childCount - 1; i >= 0; i--)
        {
            Transform child = scenery.GetChild(i);
            if ((corridor || boss) && child.name.StartsWith("Desk")) child.gameObject.SetActive(false);
        }
        if (corridor)
        {
            for (int i = 0; i < 8; i++)
                CreateBox("Locker", new Vector3(-10 + i * 2.8f, 1.1f, 7.5f),
                    new Vector3(1.5f, 2.2f, .8f), new Color(.3f,.38f,.43f));
        }
        if (boss)
        {
            CreateBox("Fight mat", new Vector3(0,.015f,0), new Vector3(19,.02f,12),
                new Color(.24f,.32f,.34f));
            for (int i = -1; i <= 1; i++)
                CreateBox("Mat stripe", new Vector3(i*8,.03f,0), new Vector3(.08f,.01f,12), Color.white);
        }
    }
    private static void CreateBoundary(string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject wall = CreateBox(name, position, scale, color);
        BoxCollider collider = wall.GetComponent<BoxCollider>();
        // Keep the visible low wall, but prevent jumps from leaving the arena.
        Vector3 size = collider.size;
        size.y = 6f / scale.y;
        collider.size = size;
        collider.center = new Vector3(0, (3f - position.y) / scale.y, 0);
    }
}
