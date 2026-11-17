using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEngine.UI;

public static class BattlegroundXSceneGenerator
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string SceneFolder  = "Assets/Scenes";
    private const string MatFolder    = "Assets/Materials";
    private const string AnimFolder   = "Assets/Animations";

    
    [MenuItem("Tools/Battleground X/Generate Full Scene")]
    public static void Generate()
    {
        EnsureFolders();

        
        var playerPrefab     = CreatePlayerPrefab();
        var medkitPrefab     = CreateLootPrefab("Loot_Medkit",     LootType.Medkit,      40, Color.red);
        var rifleAmmoPrefab  = CreateLootPrefab("Loot_RifleAmmo",  LootType.AmmoRifle,   30, new Color(1f, 0.65f, 0f));
        var shotgunAmmoPrefab= CreateLootPrefab("Loot_ShotgunAmmo",LootType.AmmoShotgun, 16, new Color(0.8f, 0.4f, 0f));
        var sniperAmmoPrefab = CreateLootPrefab("Loot_SniperAmmo", LootType.AmmoSniper,   5, new Color(0.3f, 0.3f, 1f));

        
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        
        BuildTerrain();

        
        BuildRoads();
        BuildNamedZones();
        BuildTrees();
        BuildCoverObjects();
        BuildBridges();
        BuildZoneLabels();

        
        var nm = BuildNetworkManager(playerPrefab);
        BuildMatchObjects();
        var spawnParent = BuildSpawnPoints();
        var lootParent  = BuildLootSpawner(medkitPrefab, rifleAmmoPrefab, shotgunAmmoPrefab, sniperAmmoPrefab);

        
        BuildAudioManager();

        
        BuildUI();

        
        BuildVFXManager();

        
        BuildMinimap();

        
        SetupLighting();

        
        var animCtrl = AnimatorControllerGenerator.Generate();

        
        WireAnimatorControllerIntoPrefab(animCtrl);

        
        string scenePath = SceneFolder + "/BattlegroundX_Main.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Battleground X — Phase 2",
            "Full scene generated at:\n" + scenePath +
            "\n\nPhase 2 systems wired:\n" +
            "• Animator Controller (Assets/Animations/NetworkPlayer.controller)\n" +
            "• VFX Manager (pooled muzzle flash, hit sparks, blood)\n" +
            "• Minimap (overhead camera + RenderTexture + safe-zone ring)\n" +
            "• Settings Menu (sensitivity, quality, volume)\n" +
            "• Ping Display (RTT via ServerRpc echo)\n" +
            "• Name Tags (world-space TMP billboards)\n\n" +
            "Remaining manual steps:\n" +
            "1. Assign AudioClips to AudioManager in Inspector.\n" +
            "2. Assign animation motions to Animator states.\n" +
            "3. Link Unity Cloud project (Edit → Project Settings → Services).\n" +
            "4. Enable Authentication & Relay in Unity Dashboard.\n" +
            "5. Press Play → Host → share the join code.",
            "Got it!");
    }

    
    
    
    private static void BuildTerrain()
    {
        
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Terrain_Ground";
        ground.transform.localScale = new Vector3(36, 1, 36);  
        ground.transform.position   = Vector3.zero;
        SetColor(ground, new Color(0.25f, 0.42f, 0.18f));
        ground.tag   = "Ground";
        ground.layer = LayerMask.NameToLayer("Ground") >= 0 ? LayerMask.NameToLayer("Ground") : 0;

        
        int[,] hillData = { {-130,0,-130},{130,0,-130},{-130,0,130},{130,0,130},{0,0,-155},{0,0,155} };
        for (int i = 0; i < hillData.GetLength(0); i++)
        {
            var hill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hill.name = "Hill_" + i;
            hill.transform.position   = new Vector3(hillData[i,0], 2.5f, hillData[i,2]);
            hill.transform.localScale = new Vector3(30, 5, 30);
            SetColor(hill, new Color(0.3f, 0.5f, 0.2f));
        }
    }

    
    
    
    private static void BuildRoads()
    {
        var parent = new GameObject("Roads").transform;

        
        CreateRoad(parent, "Road_NS", new Vector3(0, 0.02f, 0),   new Vector3(2, 0.1f, 180));
        CreateRoad(parent, "Road_EW", new Vector3(0, 0.02f, 0),   new Vector3(180, 0.1f, 2));
        
        CreateRoad(parent, "Road_NE", new Vector3(60, 0.02f, 60), new Vector3(1.5f, 0.1f, 80),  45);
        CreateRoad(parent, "Road_NW", new Vector3(-60, 0.02f, 60),new Vector3(1.5f, 0.1f, 80), -45);
        CreateRoad(parent, "Road_SE", new Vector3(60, 0.02f,-60), new Vector3(1.5f, 0.1f, 80),  45);
        CreateRoad(parent, "Road_SW", new Vector3(-60, 0.02f,-60),new Vector3(1.5f, 0.1f, 80), -45);
    }

    private static void CreateRoad(Transform parent, string name, Vector3 pos, Vector3 scale, float yRot = 0)
    {
        var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
        r.name = name;
        r.transform.SetParent(parent);
        r.transform.position   = pos;
        r.transform.localScale = scale;
        r.transform.eulerAngles = new Vector3(0, yRot, 0);
        SetColor(r, new Color(0.3f, 0.3f, 0.3f));
        Object.DestroyImmediate(r.GetComponent<BoxCollider>());  
    }

    
    
    
    private static void BuildBuildings()
    {
    }

    private static void BuildNamedZones()
    {
        var parent = new GameObject("NamedZones").transform;
        BuildMilitaryBase(parent, new Vector3(  0, 0,  80));
        BuildSchool      (parent, new Vector3(-80, 0,   0));
        BuildPochinki    (parent, new Vector3( 80, 0,   0));
        BuildHospital    (parent, new Vector3(  0, 0, -80));
        BuildPort        (parent, new Vector3(-80, 0,  80));
        BuildRuins       (parent, new Vector3( 80, 0, -80));
        BuildFarm        (parent, new Vector3(-80, 0, -80));
        BuildShelter     (parent, new Vector3( 80, 0,  80));
    }

    private static void BuildMilitaryBase(Transform parent, Vector3 center)
    {
        var root = new GameObject("Zone_MilitaryBase");
        root.transform.SetParent(parent);
        root.transform.position = center;
        CreateBuilding(root.transform, "Barracks_A",    new Vector3(-15,0, 10), new Vector3(20,6,8));
        CreateBuilding(root.transform, "Barracks_B",    new Vector3( 15,0, 10), new Vector3(20,6,8));
        CreateBuilding(root.transform, "CommandCenter", new Vector3(  0,0,-10), new Vector3(16,10,14));
        CreateBuilding(root.transform, "Armory",        new Vector3(-18,0,-10), new Vector3(8,5,8));
        CreateBuilding(root.transform, "Watchtower_A",  new Vector3(-22,0, 22), new Vector3(4,14,4));
        CreateBuilding(root.transform, "Watchtower_B",  new Vector3( 22,0, 22), new Vector3(4,14,4));
        CreatePerimeter(root.transform, 28f, new Color(0.4f, 0.35f, 0.25f));
        var runway = GameObject.CreatePrimitive(PrimitiveType.Cube);
        runway.name = "Runway";
        runway.transform.SetParent(root.transform);
        runway.transform.localPosition = new Vector3(0, 0.05f, 30);
        runway.transform.localScale    = new Vector3(8, 0.1f, 40);
        SetColor(runway, new Color(0.25f,0.25f,0.25f));
        Object.DestroyImmediate(runway.GetComponent<BoxCollider>());
    }

    private static void BuildSchool(Transform parent, Vector3 center)
    {
        var root = new GameObject("Zone_School");
        root.transform.SetParent(parent);
        root.transform.position = center;
        CreateBuilding(root.transform, "MainBuilding", new Vector3( 0,0, 5), new Vector3(24,10,16));
        CreateBuilding(root.transform, "Gymnasium",    new Vector3( 0,0,-14),new Vector3(18,7,12));
        CreateBuilding(root.transform, "Library",      new Vector3(-16,0,5), new Vector3(8,8,8));
        CreateBuilding(root.transform, "Canteen",      new Vector3( 16,0,5), new Vector3(8,5,8));
        CreateFence(root.transform, 22f, new Color(0.6f,0.6f,0.6f));
    }

    private static void BuildPochinki(Transform parent, Vector3 center)
    {
        var root = new GameObject("Zone_Pochinki");
        root.transform.SetParent(parent);
        root.transform.position = center;
        Vector3[] hpos  = { new(-10,0,10),new(10,0,10),new(-10,0,-10),new(10,0,-10),new(-10,0,0),new(10,0,0),new(0,0,14),new(0,0,-14) };
        Vector3[] hsz   = { new(8,5,8),new(8,6,8),new(7,5,7),new(9,5,8),new(6,5,6),new(7,6,7),new(8,4,8),new(7,5,7) };
        for (int i = 0; i < hpos.Length; i++)
            CreateBuilding(root.transform, "House_" + i, hpos[i], hsz[i]);
        var church = GameObject.CreatePrimitive(PrimitiveType.Cube);
        church.name = "Church";
        church.transform.SetParent(root.transform);
        church.transform.localPosition = Vector3.zero;
        church.transform.localScale    = new Vector3(10,14,10);
        SetColor(church, new Color(0.9f,0.88f,0.82f));
        var steeple = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        steeple.name = "Steeple";
        steeple.transform.SetParent(root.transform);
        steeple.transform.localPosition = new Vector3(0,11,0);
        steeple.transform.localScale    = new Vector3(1.5f,5f,1.5f);
        SetColor(steeple, new Color(0.5f,0.4f,0.3f));
    }

    private static void BuildHospital(Transform parent, Vector3 center)
    {
        var root = new GameObject("Zone_Hospital");
        root.transform.SetParent(parent);
        root.transform.position = center;
        CreateBuilding(root.transform, "MainWing",  new Vector3( 0,0, 0),  new Vector3(20,12,16));
        CreateBuilding(root.transform, "EastWing",  new Vector3(16,0, 0),  new Vector3(10,10,16));
        CreateBuilding(root.transform, "WestWing",  new Vector3(-16,0,0),  new Vector3(10,10,16));
        CreateBuilding(root.transform, "Emergency", new Vector3( 0,0,-14), new Vector3(14,6,8));
        CreateBuilding(root.transform, "Helipad",   new Vector3( 0,0, 16), new Vector3(12,0.5f,12));
        var c1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c1.name = "RedCross_H"; c1.transform.SetParent(root.transform);
        c1.transform.localPosition = new Vector3(0,13,0);
        c1.transform.localScale    = new Vector3(6f,1f,1.5f);
        SetColor(c1, Color.red);
        var c2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c2.name = "RedCross_V"; c2.transform.SetParent(root.transform);
        c2.transform.localPosition = new Vector3(0,13,0);
        c2.transform.localScale    = new Vector3(1.5f,1f,6f);
        SetColor(c2, Color.red);
    }

    private static void BuildPort(Transform parent, Vector3 center)
    {
        var root = new GameObject("Zone_Port");
        root.transform.SetParent(parent);
        root.transform.position = center;
        CreateBuilding(root.transform, "Warehouse_A", new Vector3(-12,0, 5), new Vector3(18,8,12));
        CreateBuilding(root.transform, "Warehouse_B", new Vector3( 12,0, 5), new Vector3(18,8,12));
        CreateBuilding(root.transform, "Office",      new Vector3(  0,0,-8), new Vector3(10,7,8));
        var colors = new Color[]{ Color.red,Color.blue,new Color(1f,.6f,0f),Color.green,Color.yellow,new Color(.5f,0,.5f) };
        for (int i = 0; i < 6; i++)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = "Container_" + i;
            c.transform.SetParent(root.transform);
            c.transform.localPosition = new Vector3(-14+i*5,1.5f,-16);
            c.transform.localScale    = new Vector3(4,3,8);
            SetColor(c, colors[i]);
        }
        var crane = new GameObject("Crane");
        crane.transform.SetParent(root.transform);
        crane.transform.localPosition = new Vector3(20,0,0);
        var cb = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cb.transform.SetParent(crane.transform);
        cb.transform.localPosition = new Vector3(0,10,0);
        cb.transform.localScale    = new Vector3(1f,10f,1f);
        SetColor(cb, new Color(0.8f,0.7f,0f));
        var ca = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ca.transform.SetParent(crane.transform);
        ca.transform.localPosition = new Vector3(5,20,0);
        ca.transform.localScale    = new Vector3(12,0.5f,0.5f);
        SetColor(ca, new Color(0.8f,0.7f,0f));
    }

    private static void BuildRuins(Transform parent, Vector3 center)
    {
        var root = new GameObject("Zone_Ruins");
        root.transform.SetParent(parent);
        root.transform.position = center;
        var rng = new System.Random(55);
        for (int i = 0; i < 8; i++)
        {
            float x = (float)(rng.NextDouble()*30-15);
            float z = (float)(rng.NextDouble()*30-15);
            float h = 1f + (float)rng.NextDouble()*4f;
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = "RuinWall_"+i;
            w.transform.SetParent(root.transform);
            w.transform.localPosition = new Vector3(x,h*0.5f,z);
            w.transform.localScale    = new Vector3(6f,h,0.5f);
            w.transform.eulerAngles   = new Vector3(0,(float)(rng.NextDouble()*360),0);
            SetColor(w, new Color(0.5f+(float)rng.NextDouble()*0.2f,0.45f,0.4f));
        }
    }

    private static void BuildFarm(Transform parent, Vector3 center)
    {
        var root = new GameObject("Zone_Farm");
        root.transform.SetParent(parent);
        root.transform.position = center;
        CreateBuilding(root.transform, "Farmhouse", new Vector3(0, 0, 10), new Vector3(14,6,10));
        CreateBuilding(root.transform, "Barn",      new Vector3(0, 0, -8), new Vector3(18,9,12));
        CreateBuilding(root.transform, "Silo_A",    new Vector3(-14,0,0),  new Vector3(4,12,4));
        CreateBuilding(root.transform, "Silo_B",    new Vector3(-18,0,0),  new Vector3(4,10,4));
        for (int row = 0; row < 4; row++)
        for (int col = 0; col < 6; col++)
        {
            var crop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crop.name = $"Crop_{row}_{col}";
            crop.transform.SetParent(root.transform);
            crop.transform.localPosition = new Vector3(col*2f-5f,0.5f,row*2f-18f);
            crop.transform.localScale    = new Vector3(0.3f,0.5f,0.3f);
            SetColor(crop, new Color(0.3f,0.7f,0.1f));
        }
    }

    private static void BuildShelter(Transform parent, Vector3 center)
    {
        var root = new GameObject("Zone_Shelter");
        root.transform.SetParent(parent);
        root.transform.position = center;
        CreateBuilding(root.transform, "Bunker",     new Vector3( 0,0, 5), new Vector3(16,4,12));
        CreateBuilding(root.transform, "Hut_A",      new Vector3(-12,0,-5),new Vector3(7,4,7));
        CreateBuilding(root.transform, "Hut_B",      new Vector3( 12,0,-5),new Vector3(7,4,7));
        CreateBuilding(root.transform, "Watchtower", new Vector3( 0,0,-16),new Vector3(4,12,4));
    }

    private static void CreatePerimeter(Transform parent, float halfSize, Color wallColor)
    {
        float s = halfSize;
        CreateWall(parent,"Perim_N",new Vector3(0,1.5f,s),  new Vector3(s*2,3,0.4f));
        CreateWall(parent,"Perim_S",new Vector3(0,1.5f,-s), new Vector3(s*2,3,0.4f));
        CreateWall(parent,"Perim_E",new Vector3(s,1.5f,0),  new Vector3(0.4f,3,s*2));
        CreateWall(parent,"Perim_W",new Vector3(-s,1.5f,0), new Vector3(0.4f,3,s*2));
    }

    private static void CreateFence(Transform parent, float halfSize, Color col)
    {
        float s = halfSize;
        string[] names  = {"Fence_N","Fence_S","Fence_E","Fence_W"};
        Vector3[] pos   = { new(0,.75f,s),new(0,.75f,-s),new(s,.75f,0),new(-s,.75f,0) };
        Vector3[] scales= { new(s*2,1.5f,0.15f),new(s*2,1.5f,0.15f),new(0.15f,1.5f,s*2),new(0.15f,1.5f,s*2) };
        for (int i = 0; i < 4; i++)
        {
            var f = GameObject.CreatePrimitive(PrimitiveType.Cube);
            f.name = names[i]; f.transform.SetParent(parent);
            f.transform.localPosition = pos[i];
            f.transform.localScale    = scales[i];
            SetColor(f, col);
            Object.DestroyImmediate(f.GetComponent<BoxCollider>());
        }
    }

    private static void BuildZoneLabels()
    {
        var parent = new GameObject("ZoneLabels").transform;
        var zones = new (string name, Vector3 pos)[]
        {
            ("MILITARY BASE", new Vector3(  0,8, 80)),
            ("SCHOOL",        new Vector3(-80,8,  0)),
            ("POCHINKI",      new Vector3( 80,8,  0)),
            ("HOSPITAL",      new Vector3(  0,8,-80)),
            ("PORT",          new Vector3(-80,8, 80)),
            ("RUINS",         new Vector3( 80,8,-80)),
            ("FARM",          new Vector3(-80,8,-80)),
            ("SHELTER",       new Vector3( 80,8, 80)),
        };
        foreach (var (name, pos) in zones)
        {
            var go = new GameObject("Label_" + name.Replace(" ","_"));
            go.transform.SetParent(parent);
            go.transform.position     = pos;
            go.transform.eulerAngles  = new Vector3(90,0,0);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text         = name;
            tmp.fontSize     = 6f;
            tmp.fontStyle    = FontStyles.Bold;
            tmp.alignment    = TextAlignmentOptions.Center;
            tmp.color        = new Color(1f,0.9f,0.2f);
            tmp.outlineWidth = 0.3f;
            tmp.outlineColor = Color.black;
        }
    }
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = pos;

        
        float w = size.x, h = size.y, d = size.z, t = 0.4f;
        CreateWall(root.transform, "Wall_N",  new Vector3(0,h/2f, d/2f),  new Vector3(w, h, t));
        CreateWall(root.transform, "Wall_S",  new Vector3(0,h/2f,-d/2f),  new Vector3(w, h, t));
        CreateWall(root.transform, "Wall_E",  new Vector3(w/2f,h/2f,0),   new Vector3(t, h, d));
        CreateWall(root.transform, "Wall_W",  new Vector3(-w/2f,h/2f,0),  new Vector3(t, h, d));
        CreateWall(root.transform, "Roof",    new Vector3(0, h+t/2f, 0),  new Vector3(w+t*2, t, d+t*2));

        root.tag = "Building";
    }

    private static void CreateWall(Transform parent, string name, Vector3 localPos, Vector3 size)
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = name;
        w.transform.SetParent(parent);
        w.transform.localPosition = localPos;
        w.transform.localScale    = size;
        SetColor(w, new Color(0.72f, 0.65f, 0.55f));
        w.layer = LayerMask.NameToLayer("Building") >= 0 ? LayerMask.NameToLayer("Building") : 0;
    }



    
    
    
    private static void BuildTrees()
    {
        var parent = new GameObject("Vegetation").transform;
        var rng    = new System.Random(42);

        for (int i = 0; i < 120; i++)
        {
            float x = (float)(rng.NextDouble() * 320 - 160);
            float z = (float)(rng.NextDouble() * 320 - 160);

            
            if (Mathf.Abs(x) < 5 || Mathf.Abs(z) < 5) continue;
            
            bool nearBuilding = false;
            foreach (var bz in new[] { new Vector2(60,60), new Vector2(-60,60), new Vector2(60,-60), new Vector2(-60,-60) })
                if (Vector2.Distance(new Vector2(x,z), bz) < 20) { nearBuilding = true; break; }
            if (nearBuilding) continue;

            CreateTree(parent, i, new Vector3(x, 0, z), rng);
        }
    }

    private static void CreateTree(Transform parent, int idx, Vector3 pos, System.Random rng)
    {
        float h    = 4f + (float)rng.NextDouble() * 4f;
        float canR = 1.5f + (float)rng.NextDouble() * 2f;

        var root = new GameObject("Tree_" + idx);
        root.transform.SetParent(parent);
        root.transform.position = pos;

        
        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(root.transform);
        trunk.transform.localPosition = new Vector3(0, h * 0.5f, 0);
        trunk.transform.localScale    = new Vector3(0.3f, h * 0.5f, 0.3f);
        SetColor(trunk, new Color(0.36f, 0.22f, 0.1f));

        
        var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "Canopy";
        canopy.transform.SetParent(root.transform);
        canopy.transform.localPosition = new Vector3(0, h + canR * 0.5f, 0);
        canopy.transform.localScale    = new Vector3(canR * 2, canR * 1.5f, canR * 2);
        SetColor(canopy, new Color(0.1f + (float)rng.NextDouble() * 0.2f,
                                   0.4f + (float)rng.NextDouble() * 0.3f, 0.1f));
    }

    
    
    
    private static void BuildCoverObjects()
    {
        var parent = new GameObject("Cover").transform;
        var rng    = new System.Random(99);

        
        for (int i = 0; i < 40; i++)
        {
            float x = (float)(rng.NextDouble() * 280 - 140);
            float z = (float)(rng.NextDouble() * 280 - 140);
            if (Mathf.Abs(x) < 8 && Mathf.Abs(z) < 8) continue;

            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "Rock_" + i;
            rock.transform.SetParent(parent);
            rock.transform.position   = new Vector3(x, 0.8f, z);
            rock.transform.localScale = new Vector3(
                1f + (float)rng.NextDouble() * 2f,
                0.8f + (float)rng.NextDouble() * 1.5f,
                1f + (float)rng.NextDouble() * 2f);
            rock.transform.eulerAngles = new Vector3(0, (float)(rng.NextDouble() * 360), 0);
            SetColor(rock, new Color(0.5f, 0.5f, 0.5f));
        }

        
        for (int i = 0; i < 20; i++)
        {
            float x = (float)(rng.NextDouble() * 240 - 120);
            float z = (float)(rng.NextDouble() * 240 - 120);
            var sb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sb.name = "Sandbag_" + i;
            sb.transform.SetParent(parent);
            sb.transform.position   = new Vector3(x, 0.6f, z);
            sb.transform.localScale = new Vector3(3, 1.2f, 0.5f);
            sb.transform.eulerAngles = new Vector3(0, (float)(rng.NextDouble() * 360), 0);
            SetColor(sb, new Color(0.76f, 0.70f, 0.50f));
        }

        
        for (int i = 0; i < 30; i++)
        {
            float x = (float)(rng.NextDouble() * 200 - 100);
            float z = (float)(rng.NextDouble() * 200 - 100);
            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel_" + i;
            barrel.transform.SetParent(parent);
            barrel.transform.position   = new Vector3(x, 0.6f, z);
            barrel.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
            SetColor(barrel, new Color(0.2f, 0.2f, 0.2f));
        }
    }

    
    
    
    private static void BuildBridges()
    {
        var parent = new GameObject("Bridges").transform;
        
        CreateBridge(parent, "Bridge_NS", new Vector3(0, 2f, 0),   new Vector3(4, 0.4f, 30));
        CreateBridge(parent, "Bridge_EW", new Vector3(0, 2f, 0),   new Vector3(30, 0.4f, 4));
    }

    private static void CreateBridge(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.name = name;
        b.transform.SetParent(parent);
        b.transform.position   = pos;
        b.transform.localScale = size;
        SetColor(b, new Color(0.55f, 0.5f, 0.45f));
        
        for (int s = -1; s <= 1; s += 2)
        {
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.transform.SetParent(b.transform);
            float side = (size.x > size.z) ? (size.z * 0.5f - 0.1f) * s : 0;
            float sideZ= (size.z > size.x) ? (size.x * 0.5f - 0.1f) * s : 0;
            rail.transform.localPosition = new Vector3(side, 0.75f, sideZ);
            rail.transform.localScale    = new Vector3(
                size.x > size.z ? 1f : 0.1f,
                0.3f,
                size.z > size.x ? 1f : 0.1f);
            SetColor(rail, new Color(0.4f, 0.35f, 0.3f));
        }
    }

    
    
    
    private static GameObject BuildNetworkManager(GameObject playerPrefab)
    {
        var nmGo      = new GameObject("NetworkManager");
        var nm        = nmGo.AddComponent<NetworkManager>();
        var transport = nmGo.AddComponent<UnityTransport>();
        nm.NetworkConfig = new NetworkConfig
        {
            NetworkTransport = transport,
            PlayerPrefab     = playerPrefab,
            TickRate         = 30
        };
        return nmGo;
    }

    
    
    
    private static void BuildMatchObjects()
    {
        
        var matchGo = new GameObject("NetworkMatchManager");
        matchGo.AddComponent<NetworkObject>();
        matchGo.AddComponent<NetworkMatchManager>();

        
        var zoneGo = new GameObject("NetworkSafeZone");
        zoneGo.AddComponent<NetworkObject>();
        var zoneVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        zoneVisual.name = "SafeZoneVisual";
        zoneVisual.transform.SetParent(zoneGo.transform);
        zoneVisual.transform.localPosition = new Vector3(0, 0.03f, 0);
        zoneVisual.transform.localScale    = new Vector3(360, 0.02f, 360);
        Object.DestroyImmediate(zoneVisual.GetComponent<Collider>());
        var zoneRend = zoneVisual.GetComponent<Renderer>();
        if (zoneRend != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.0f, 0.5f, 1f, 0.25f);
            SetTransparent(mat);
            zoneRend.sharedMaterial = mat;
        }
        var zone = zoneGo.AddComponent<NetworkSafeZone>();
        var zoneSO = new SerializedObject(zone);
        zoneSO.FindProperty("visualRing").objectReferenceValue = zoneVisual.transform;
        zoneSO.ApplyModifiedPropertiesWithoutUndo();

        
        var spawnGo = new GameObject("SpawnManager");
        spawnGo.AddComponent<NetworkObject>();
        spawnGo.AddComponent<SpawnManager>();

        
        var lobbyGo = new GameObject("LobbyManager");
        lobbyGo.AddComponent<NetworkObject>();
        lobbyGo.AddComponent<LobbyManager>();

        
        var audioGo = new GameObject("AudioManager");
        audioGo.AddComponent<AudioManager>();
    }

    
    
    
    private static Transform BuildSpawnPoints()
    {
        var parent = new GameObject("SpawnPoints").transform;

        
        for (int i = 0; i < 10; i++)
        {
            float ang = (i / 10f) * Mathf.PI * 2f;
            float r   = 110f + (i % 3) * 15f;
            var sp = new GameObject("SpawnPoint_" + i);
            sp.transform.SetParent(parent);
            sp.transform.position = new Vector3(Mathf.Sin(ang) * r, 0.5f, Mathf.Cos(ang) * r);
            sp.tag = "SpawnPoint";

            
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Marker";
            marker.transform.SetParent(sp.transform);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale    = new Vector3(1f, 0.1f, 1f);
            SetColor(marker, new Color(0f, 1f, 0f, 0.5f));
            Object.DestroyImmediate(marker.GetComponent<BoxCollider>());
        }

        
        var sm = Object.FindObjectOfType<SpawnManager>();
        if (sm != null)
        {
            var smSO = new SerializedObject(sm);
            var spArr = smSO.FindProperty("spawnPoints");
            spArr.arraySize = parent.childCount;
            for (int i = 0; i < parent.childCount; i++)
                spArr.GetArrayElementAtIndex(i).objectReferenceValue = parent.GetChild(i);
            smSO.ApplyModifiedPropertiesWithoutUndo();
        }

        return parent;
    }

    
    
    
    private static Transform BuildLootSpawner(
        GameObject medkit, GameObject rifleAmmo, GameObject shotgunAmmo, GameObject sniperAmmo)
    {
        var parent = new GameObject("LootZones").transform;

        
        var rng = new System.Random(7);
        for (int i = 0; i < 30; i++)
        {
            float x = (float)(rng.NextDouble() * 280 - 140);
            float z = (float)(rng.NextDouble() * 280 - 140);

            var lp = new GameObject("LootPoint_" + i);
            lp.transform.SetParent(parent);
            lp.transform.position = new Vector3(x, 0.5f, z);

            
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "LootMarker";
            marker.transform.SetParent(lp.transform);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale    = new Vector3(0.4f, 0.4f, 0.4f);
            SetColor(marker, new Color(1f, 0.85f, 0f));
            Object.DestroyImmediate(marker.GetComponent<SphereCollider>());
        }

        
        var lsGo = new GameObject("LootSpawner");
        lsGo.AddComponent<NetworkObject>();
        var ls = lsGo.AddComponent<LootSpawner>();
        var lsSO = new SerializedObject(ls);
        
        lsSO.FindProperty("medkitPrefab").objectReferenceValue      = medkit?.GetComponent<NetworkLootPickup>();
        lsSO.FindProperty("rifleAmmoPrefab").objectReferenceValue   = rifleAmmo?.GetComponent<NetworkLootPickup>();
        lsSO.FindProperty("shotgunAmmoPrefab").objectReferenceValue = shotgunAmmo?.GetComponent<NetworkLootPickup>();
        lsSO.FindProperty("sniperAmmoPrefab").objectReferenceValue  = sniperAmmo?.GetComponent<NetworkLootPickup>();

        
        var spArr = lsSO.FindProperty("lootSpawnPoints");
        spArr.arraySize = parent.childCount;
        for (int i = 0; i < parent.childCount; i++)
            spArr.GetArrayElementAtIndex(i).objectReferenceValue = parent.GetChild(i);

        lsSO.ApplyModifiedPropertiesWithoutUndo();
        return parent;
    }

    
    
    
    private static void BuildAudioManager()
    {
        
    }

    
    
    
    private static void BuildUI()
    {
        
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        
        var canvasGo = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas   = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        
        var connPanel   = CreatePanel(canvasGo.transform, "ConnectionPanel",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(760, 220),
            new Color(0,0,0,0.75f));
        var relayGo     = new GameObject("RelaySessionUI");
        var relay       = relayGo.AddComponent<RelaySessionUI>();

        var joinInput   = CreateInput(connPanel.transform, "JoinCodeInput",   "Enter Relay Join Code", new Vector2(0, -20));
        var codeText    = CreateText(connPanel.transform,  "JoinCodeText",    "JOIN CODE: —",          new Vector2(0, -60), 24);
        var statusText  = CreateText(connPanel.transform,  "StatusText",      "Initializing…",         new Vector2(0, -90), 18);
        var playerList  = CreateText(connPanel.transform,  "PlayerListText",  "",                      new Vector2(0, -130), 16);

        CreateButton(connPanel.transform, "HostButton",  "HOST",  new Vector2(-220, -175), () => relay.HostGame());
        CreateButton(connPanel.transform, "JoinButton",  "JOIN",  new Vector2(0,    -175), () => relay.JoinGame());
        CreateButton(connPanel.transform, "LeaveButton", "LEAVE", new Vector2( 220, -175), () => relay.LeaveGame());

        var relaySO = new SerializedObject(relay);
        relaySO.FindProperty("joinCodeInput").objectReferenceValue  = joinInput;
        relaySO.FindProperty("joinCodeText").objectReferenceValue   = codeText;
        relaySO.FindProperty("statusText").objectReferenceValue     = statusText;
        relaySO.FindProperty("playerListText").objectReferenceValue = playerList;
        relaySO.ApplyModifiedPropertiesWithoutUndo();

        
        
        var healthText   = CreateText(canvasGo.transform, "HealthText",  "HP: 100", new Vector2(-820, -520), 28);
        
        var healthSlider = CreateSlider(canvasGo.transform, "HealthSlider", new Vector2(-820, -545));

        
        var ammoText      = CreateText(canvasGo.transform, "AmmoText",      "30 / 150",    new Vector2(820, -520), 28);
        var weaponText    = CreateText(canvasGo.transform, "WeaponNameText","Assault Rifle",new Vector2(820, -550), 20);
        var reloadingText = CreateText(canvasGo.transform, "ReloadingText", "RELOADING…",  new Vector2(820, -575), 20);

        
        var playersText = CreateText(canvasGo.transform, "PlayersText",    "⬟ 10",   new Vector2(820, 520), 26);
        var timerText   = CreateText(canvasGo.transform, "TimerText",      "15:00",   new Vector2(0,  520), 32);
        var phaseText   = CreateText(canvasGo.transform, "PhaseText",      "Lobby",   new Vector2(0,  490), 20);

        
        var zoneText    = CreateText(canvasGo.transform, "ZoneText",       "SAFE",    new Vector2(820, 490), 22);

        
        var killFeedGo = new GameObject("KillFeedContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        killFeedGo.transform.SetParent(canvasGo.transform, false);
        var kfRT = killFeedGo.GetComponent<RectTransform>();
        kfRT.anchorMin        = new Vector2(1,0.5f);
        kfRT.anchorMax        = new Vector2(1,0.5f);
        kfRT.pivot            = new Vector2(1,0.5f);
        kfRT.anchoredPosition = new Vector2(-20, 0);
        kfRT.sizeDelta        = new Vector2(400, 200);
        var vlg = killFeedGo.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment    = TextAnchor.LowerRight;
        vlg.spacing           = 4;

        
        var kfEntry = CreateText(canvasGo.transform, "KillFeedEntry_Prefab", "", Vector2.zero, 18);
        kfEntry.gameObject.SetActive(false);

        
        var matchEndPanel = CreatePanel(canvasGo.transform, "MatchEndPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 400),
            new Color(0,0,0,0.9f));
        matchEndPanel.gameObject.SetActive(false);
        var matchResultText = CreateText(matchEndPanel.transform, "MatchResultText", "WINNER!", Vector2.zero, 48);
        var statsText       = CreateText(matchEndPanel.transform, "StatsText", "", new Vector2(0,-80), 22);
        var returnBtn       = CreateButton(matchEndPanel.transform, "ReturnBtn", "Return to Lobby",
            new Vector2(0, -170), null);

        
        var lobbyPanel = CreatePanel(canvasGo.transform, "LobbyStatusPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 160),
            new Color(0,0,0,0.6f));
        var readyCountText = CreateText(lobbyPanel.transform, "ReadyCountText", "Ready: 0/0", Vector2.zero, 28);
        var readyBtn       = CreateButton(lobbyPanel.transform, "ReadyButton", "READY", new Vector2(0,-60), null);

        
        var countdownPanel = CreatePanel(canvasGo.transform, "CountdownPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 200),
            new Color(0,0,0,0.7f));
        countdownPanel.gameObject.SetActive(false);
        var countdownText = CreateText(countdownPanel.transform, "CountdownText", "5", Vector2.zero, 96);

        
        var hudGo = new GameObject("NetworkHUD");
        var hud   = hudGo.AddComponent<NetworkHUD>();
        var hudSO = new SerializedObject(hud);
        hudSO.FindProperty("healthText").objectReferenceValue        = healthText;
        hudSO.FindProperty("healthSlider").objectReferenceValue      = healthSlider;
        hudSO.FindProperty("ammoText").objectReferenceValue          = ammoText;
        hudSO.FindProperty("weaponNameText").objectReferenceValue    = weaponText;
        hudSO.FindProperty("reloadingText").objectReferenceValue     = reloadingText;
        hudSO.FindProperty("playersText").objectReferenceValue       = playersText;
        hudSO.FindProperty("timerText").objectReferenceValue         = timerText;
        hudSO.FindProperty("phaseText").objectReferenceValue         = phaseText;
        hudSO.FindProperty("zoneText").objectReferenceValue          = zoneText;
        hudSO.FindProperty("killFeedContainer").objectReferenceValue = killFeedGo.transform;
        hudSO.FindProperty("killFeedEntryPrefab").objectReferenceValue = kfEntry;
        hudSO.FindProperty("matchEndPanel").objectReferenceValue     = matchEndPanel.gameObject;
        hudSO.FindProperty("matchResultText").objectReferenceValue   = matchResultText;
        hudSO.FindProperty("statsText").objectReferenceValue         = statsText;
        hudSO.FindProperty("returnToLobbyButton").objectReferenceValue = returnBtn;
        hudSO.FindProperty("lobbyStatusPanel").objectReferenceValue  = lobbyPanel.gameObject;
        hudSO.FindProperty("readyCountText").objectReferenceValue    = readyCountText;
        hudSO.FindProperty("readyButton").objectReferenceValue       = readyBtn;
        hudSO.FindProperty("countdownPanel").objectReferenceValue    = countdownPanel.gameObject;
        hudSO.FindProperty("countdownText").objectReferenceValue     = countdownText;
        hudSO.ApplyModifiedPropertiesWithoutUndo();

        
        relaySO.FindProperty("lobbyPanel").objectReferenceValue      = connPanel.gameObject;
        relaySO.FindProperty("countdownPanel").objectReferenceValue  = countdownPanel.gameObject;
        relaySO.FindProperty("countdownText").objectReferenceValue   = countdownText;
        relaySO.ApplyModifiedPropertiesWithoutUndo();

        
        var minimapPanel = CreatePanel(canvasGo.transform, "MinimapPanel",
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-85f, 85f), new Vector2(160f, 160f),
            new Color(0f, 0f, 0f, 0.6f));
        var minimapImageGo = new GameObject("MinimapImage",
            typeof(RectTransform), typeof(RawImage));
        minimapImageGo.transform.SetParent(minimapPanel.transform, false);
        var mmRT = minimapImageGo.GetComponent<RectTransform>();
        mmRT.anchorMin = Vector2.zero; mmRT.anchorMax = Vector2.one;
        mmRT.offsetMin = new Vector2(4, 4); mmRT.offsetMax = new Vector2(-4, -4);
        var mmRawImage = minimapImageGo.GetComponent<RawImage>();

        
        hudSO.FindProperty("minimapImage").objectReferenceValue = mmRawImage;
        hudSO.ApplyModifiedPropertiesWithoutUndo();

        
        var pingText = CreateText(canvasGo.transform, "PingText", "Ping: -- ms",
            new Vector2(-820f, 520f), 18f);
        pingText.color = Color.green;

        
        var settingsGo = new GameObject("SettingsMenu");
        var settingsMenu = settingsGo.AddComponent<SettingsMenu>();

        var settingsPanel = CreatePanel(canvasGo.transform, "SettingsPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(560f, 520f),
            new Color(0.05f, 0.05f, 0.05f, 0.95f));
        settingsPanel.gameObject.SetActive(false);

        
        var sensTitleText  = CreateText(settingsPanel.transform, "SensTitle",   "SENSITIVITY",    new Vector2(0, 220), 22);
        var mouseSensLabel = CreateText(settingsPanel.transform, "MouseSensLbl","Mouse Sens: 2.0",new Vector2(-80,180), 18);
        var mouseSensSlider= CreateSlider(settingsPanel.transform, "MouseSensSlider", new Vector2(80, 180));
        var mobSensLabel   = CreateText(settingsPanel.transform, "MobSensLbl",  "Touch Sens: 1.5",new Vector2(-80,140), 18);
        var mobSensSlider  = CreateSlider(settingsPanel.transform, "MobSensSlider", new Vector2(80, 140));

        var graphTitle     = CreateText(settingsPanel.transform, "GraphTitle",  "GRAPHICS",       new Vector2(0, 90),  22);
        
        var qualLabel      = CreateText(settingsPanel.transform, "QualLabel",   "Quality",        new Vector2(-120, 50), 18);

        var audioTitle     = CreateText(settingsPanel.transform, "AudioTitle",  "AUDIO",          new Vector2(0, -10), 22);
        var masterVolLabel = CreateText(settingsPanel.transform, "MasterVolLbl","Master Vol: 1.0",new Vector2(-80,-50), 18);
        var masterVolSlider= CreateSlider(settingsPanel.transform, "MasterVolSlider", new Vector2(80, -50));
        var sfxVolLabel    = CreateText(settingsPanel.transform, "SFXVolLbl",   "SFX Vol: 0.8",  new Vector2(-80,-90), 18);
        var sfxVolSlider   = CreateSlider(settingsPanel.transform, "SFXVolSlider", new Vector2(80, -90));

        var applyBtn  = CreateButton(settingsPanel.transform, "ApplyButton",  "APPLY",  new Vector2(-80, -180), null);
        var closeBtn  = CreateButton(settingsPanel.transform, "CloseButton",  "CLOSE",  new Vector2( 80, -180), null);

        
        var settingsOpenBtn = CreateButton(canvasGo.transform, "SettingsOpenButton", "⚙",
            new Vector2(-820f, 490f), null);

        
        var smSO = new SerializedObject(settingsMenu);
        smSO.FindProperty("settingsPanel").objectReferenceValue      = settingsPanel.gameObject;
        smSO.FindProperty("closeButton").objectReferenceValue        = closeBtn;
        smSO.FindProperty("applyButton").objectReferenceValue        = applyBtn;
        smSO.FindProperty("mouseSensSlider").objectReferenceValue    = mouseSensSlider;
        smSO.FindProperty("mouseSensLabel").objectReferenceValue     = mouseSensLabel;
        smSO.FindProperty("mobileSensSlider").objectReferenceValue   = mobSensSlider;
        smSO.FindProperty("mobileSensLabel").objectReferenceValue    = mobSensLabel;
        smSO.FindProperty("masterVolumeSlider").objectReferenceValue = masterVolSlider;
        smSO.FindProperty("masterVolumeLabel").objectReferenceValue  = masterVolLabel;
        smSO.FindProperty("sfxVolumeSlider").objectReferenceValue    = sfxVolSlider;
        smSO.FindProperty("sfxVolumeLabel").objectReferenceValue     = sfxVolLabel;
        smSO.ApplyModifiedPropertiesWithoutUndo();

        
        settingsOpenBtn.onClick.AddListener(() => settingsMenu.ToggleSettings());

        
        var flashGo = new GameObject("ScreenFlash", typeof(RectTransform), typeof(Image));
        flashGo.transform.SetParent(canvasGo.transform, false);
        var flashRT = flashGo.GetComponent<RectTransform>();
        flashRT.anchorMin = Vector2.zero; flashRT.anchorMax = Vector2.one;
        flashRT.offsetMin = Vector2.zero; flashRT.offsetMax = Vector2.zero;
        var flashImg = flashGo.GetComponent<Image>();
        flashImg.color = new Color(1f, 0f, 0f, 0f);
        flashImg.raycastTarget = false;
        flashGo.SetActive(false);

        
        var vfxGo = Object.FindObjectOfType<VFXManager>()?.gameObject
                    ?? new GameObject("VFXManager");
        var vfx = vfxGo.GetComponent<VFXManager>() ?? vfxGo.AddComponent<VFXManager>();
        var vfxSO = new SerializedObject(vfx);
        vfxSO.FindProperty("screenFlashImage").objectReferenceValue = flashImg;
        vfxSO.ApplyModifiedPropertiesWithoutUndo();
    }

    
    
    
    private static void SetupLighting()
    {
        var sun = Object.FindObjectOfType<Light>();
        if (sun != null)
        {
            sun.type      = LightType.Directional;
            sun.intensity = 1.2f;
            sun.color     = new Color(1f, 0.97f, 0.88f);
            sun.shadows   = LightShadows.Soft;
            sun.transform.eulerAngles = new Vector3(55, -30, 0);
        }

        RenderSettings.ambientMode      = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientSkyColor  = new Color(0.5f, 0.6f, 0.7f);
        RenderSettings.fog              = true;
        RenderSettings.fogColor         = new Color(0.7f, 0.75f, 0.8f);
        RenderSettings.fogMode          = FogMode.Linear;
        RenderSettings.fogStartDistance = 100f;
        RenderSettings.fogEndDistance   = 400f;
    }

    
    
    
    private static GameObject CreatePlayerPrefab()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "NetworkPlayer";
        SetColor(go, new Color(0.25f, 0.55f, 0.9f));

        
        Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
        var cc = go.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 1f, 0);

        go.layer = LayerMask.NameToLayer("Player") >= 0 ? LayerMask.NameToLayer("Player") : 0;
        go.tag   = "Player";

        
        var pivot = new GameObject("CameraPivot").transform;
        pivot.SetParent(go.transform, false);
        pivot.localPosition = new Vector3(0, 0.7f, 0);
        pivot.gameObject.SetActive(false);

        
        pivot.gameObject.AddComponent<CameraShake>();

        
        var camGo = new GameObject("PlayerCamera");
        camGo.transform.SetParent(pivot, false);
        camGo.transform.localPosition = new Vector3(0.4f, 0.2f, -3.5f);
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView  = 65f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane  = 800f;
        camGo.AddComponent<AudioListener>();
        camGo.SetActive(false);

        
        go.AddComponent<NetworkObject>();
        go.AddComponent<OwnerNetworkTransform>();

        
        var ctrl = go.AddComponent<NetworkPlayerController>();
        var ctrlSO = new SerializedObject(ctrl);
        ctrlSO.FindProperty("cameraPivot").objectReferenceValue = pivot;
        ctrlSO.FindProperty("playerCamera").objectReferenceValue = cam;
        ctrlSO.ApplyModifiedPropertiesWithoutUndo();

        go.AddComponent<NetworkHealth>();
        go.AddComponent<NetworkWeapon>();
        var weapSO = new SerializedObject(go.GetComponent<NetworkWeapon>());
        weapSO.ApplyModifiedPropertiesWithoutUndo();

        go.AddComponent<PlayerInventory>();
        go.AddComponent<PlayerStats>();
        go.AddComponent<SpectatorController>();

        
        
        go.AddComponent<PlayerAnimatorController>();
        
        go.AddComponent<BloodEffect>();
        
        go.AddComponent<NameTag>();
        
        go.AddComponent<PingDisplay>();

        
        var muzzleGo = new GameObject("MuzzlePoint");
        muzzleGo.transform.SetParent(go.transform, false);
        muzzleGo.transform.localPosition = new Vector3(0f, 0.9f, 1.2f);
        muzzleGo.AddComponent<MuzzleFlash>();

        
        string path = PrefabFolder + "/NetworkPlayer.prefab";
        var prefab  = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    
    
    
    private static GameObject CreateLootPrefab(string name, LootType type, int amount, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        SetColor(go, color);

        
        var col = go.GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.size      = new Vector3(2f, 2f, 2f);

        go.AddComponent<NetworkObject>();
        var pickup = go.AddComponent<NetworkLootPickup>();
        var pSO = new SerializedObject(pickup);
        pSO.FindProperty("lootType").enumValueIndex = (int)type;
        pSO.FindProperty("amount").intValue         = amount;
        pSO.ApplyModifiedPropertiesWithoutUndo();

        string path   = PrefabFolder + "/" + name + ".prefab";
        var    prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    
    
    
    private static RectTransform CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Color bg)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        go.GetComponent<Image>().color = bg;
        return rt;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, Vector2 pos, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(600, 40);
        rt.anchoredPosition = pos;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = value;
        tmp.fontSize  = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return tmp;
    }

    private static Slider CreateSlider(Transform parent, string name, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(220, 20);
        rt.anchoredPosition = pos;
        var slider = go.GetComponent<Slider>();
        slider.minValue  = 0;
        slider.maxValue  = 1;
        slider.value     = 1;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.2f,0.2f,0.2f,0.8f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(go.transform, false);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = new Vector2(1,1);
        fillRT.sizeDelta = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.1f, 0.9f, 0.2f);
        slider.fillRect = fillRT;

        return slider;
    }

    private static TMP_InputField CreateInput(Transform parent, string name, string placeholder, Vector2 pos)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        root.transform.SetParent(parent, false);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(380, 36); rt.anchoredPosition = pos;
        root.GetComponent<Image>().color = Color.white;

        var text = CreateText(root.transform, "Text", "", Vector2.zero, 20);
        var textRT = text.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(8,2); textRT.offsetMax = new Vector2(-8,-2);
        text.color = Color.black;

        var ph = CreateText(root.transform, "Placeholder", placeholder, Vector2.zero, 18);
        ph.color = Color.gray;
        var phRT = ph.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = new Vector2(8,2); phRT.offsetMax = new Vector2(-8,-2);

        var input = root.GetComponent<TMP_InputField>();
        input.textComponent = text;
        input.placeholder   = ph;
        return input;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 pos,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(170, 40); rt.anchoredPosition = pos;
        go.GetComponent<Image>().color = new Color(0.1f, 0.42f, 0.22f);
        CreateText(go.transform, "Label", label, Vector2.zero, 20);
        var labelRT = go.transform.Find("Label").GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero; labelRT.offsetMax = Vector2.zero;
        var btn = go.GetComponent<Button>();
        if (onClick != null) btn.onClick.AddListener(onClick);
        return btn;
    }

    
    
    
    private static void SetColor(GameObject go, Color c)
    {
        var rend = go.GetComponent<Renderer>();
        if (rend == null) return;
        var mat  = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
        mat.color = c;
        if (c.a < 1f) SetTransparent(mat);
        rend.sharedMaterial = mat;
    }

    private static void SetTransparent(Material mat)
    {
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite",   0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    
    
    
    private static void BuildVFXManager()
    {
        
        
        var existing = Object.FindObjectOfType<VFXManager>();
        if (existing == null)
        {
            var go = new GameObject("VFXManager");
            go.AddComponent<VFXManager>();
        }
    }

    
    
    
    private static void BuildMinimap()
    {
        
        var mmGo = new GameObject("MinimapSystem");
        var mmCtrl = mmGo.AddComponent<MinimapController>();

        
        var rawImage = Object.FindObjectOfType<RawImage>();
        
        var mmImageGo = GameObject.Find("MinimapImage");
        if (mmImageGo != null)
        {
            var ri = mmImageGo.GetComponent<RawImage>();
            if (ri != null)
            {
                var so = new SerializedObject(mmCtrl);
                so.FindProperty("hudMinimapImage").objectReferenceValue = ri;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        
        mmGo.AddComponent<MinimapSafeZoneRing>();
    }

    
    
    
    private static void WireAnimatorControllerIntoPrefab(AnimatorController ctrl)
    {
        if (ctrl == null) return;

        string prefabPath = PrefabFolder + "/NetworkPlayer.prefab";
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null)
        {
            Debug.LogWarning("[BattlegroundX] NetworkPlayer prefab not found — cannot wire Animator.");
            return;
        }

        
        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var root = scope.prefabContentsRoot;

            
            var anim = root.GetComponent<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion           = false;
            anim.updateMode                = AnimatorUpdateMode.Normal;
            anim.cullingMode               = AnimatorCullingMode.AlwaysAnimate;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[BattlegroundX] Animator Controller wired into NetworkPlayer prefab.");
    }

    
    private static void EnsureFolders()
    {
        foreach (var path in new[] { PrefabFolder, SceneFolder, MatFolder, AnimFolder,
            "Assets/Scripts/Audio", "Assets/Scripts/Data" })
        {
            if (AssetDatabase.IsValidFolder(path)) continue;
            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/") ?? "Assets";
            string folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder(
                Path.GetDirectoryName(parent)?.Replace("\\","/") ?? "Assets",
                Path.GetFileName(parent));
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}