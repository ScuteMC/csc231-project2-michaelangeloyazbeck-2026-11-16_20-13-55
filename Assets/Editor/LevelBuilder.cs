using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using VesperTwilightCourier;

public static class LevelBuilder
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string AnimatorPath = "Assets/Animations/VesperAnimator.controller";
    private const string AnimationFolder = "Assets/Animations";
    private const string SpriteFolder = "Assets/Art/Sprites";
    private const string AudioFolder = "Assets/Audio";
    private const string ArtFolder = "Assets/Art";
    private const string SettingsFolder = "Assets/Settings";

    private class BuiltAssets
    {
        public Sprite vesperIdle, vesperRun1, vesperRun2, vesperJump, vesperFall, vesperGlide, vesperHurt;
        public Sprite tileRooftop, hazardSpikes, enemySentinel, collectibleShard, propCheckpoint, propGoalBeacon;
        public Sprite bgFarSky, bgMidCity, bgNearSilhouettes;
        public Sprite uiWhiteSprite, uiHeartSprite, softParticleSprite;
        public Material particleMaterial;
        public PhysicsMaterial2D frictionlessMaterial;
        public RuntimeAnimatorController vesperAnimator;
        public AudioClip bgmClip, jumpClip, landClip, collectClip, hurtClip, checkpointClip, glideClip, winClip;
    }

    [MenuItem("Vesper/Build Level, Screenshots, and Windows Player")]
    public static void BuildAll()
    {
        Debug.Log("[LevelBuilder] Starting full project build...");
        EnsureDirectories();
        GenerateHelperArtAssets();
        ConfigureSpriteImporters();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        BuiltAssets assets = LoadAndCreateAssets();
        BuildLevelOneScene(assets);
        RenderVerificationScreenshots();
        BuildWindowsPlayer();

        Debug.Log("[LevelBuilder] BuildAll completed successfully.");
    }

    [MenuItem("Vesper/Rebuild Level 1 Scene Only")]
    public static void BuildSceneOnly()
    {
        EnsureDirectories();
        GenerateHelperArtAssets();
        ConfigureSpriteImporters();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        BuiltAssets assets = LoadAndCreateAssets();
        BuildLevelOneScene(assets);
        RenderVerificationScreenshots();
    }

    private static void EnsureDirectories()
    {
        string[] folders =
        {
            "Assets/Animations",
            "Assets/Art",
            "Assets/Art/Sprites",
            "Assets/Audio",
            "Assets/Scenes",
            "Assets/Settings",
            "Screenshots",
            "Builds",
            "Builds/Windows"
        };

        foreach (string dir in folders)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }

    private static void GenerateHelperArtAssets()
    {
        // Soft radial glow particle sprite (64x64 RGBA)
        string particlePath = Path.Combine(ArtFolder, "VFX_SoftParticle.png");
        Texture2D pTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float nx = (x - 31.5f) / 31.5f;
                float ny = (y - 31.5f) / 31.5f;
                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha;
                pTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        pTex.Apply();
        File.WriteAllBytes(particlePath, pTex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(pTex);

        // Solid white 16x16 UI sprite
        string whitePath = Path.Combine(ArtFolder, "UI_SolidWhite.png");
        Texture2D wTex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        Color[] whitePixels = new Color[16 * 16];
        for (int i = 0; i < whitePixels.Length; i++) whitePixels[i] = Color.white;
        wTex.SetPixels(whitePixels);
        wTex.Apply();
        File.WriteAllBytes(whitePath, wTex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(wTex);

        // Crisp 64x64 Heart UI icon
        string heartPath = Path.Combine(ArtFolder, "UI_Heart.png");
        Texture2D hTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float px = (x - 31.5f) / 22.0f;
                float py = (y - 29.0f) / 22.0f;
                // Implicit heart equation: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
                float a = px * px + py * py - 1f;
                float val = a * a * a - (px * px) * (py * py * py);
                float alpha = val <= 0f ? 1f : Mathf.Clamp01(1f - val * 8f);
                hTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        hTex.Apply();
        File.WriteAllBytes(heartPath, hTex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(hTex);
    }

    private static void ConfigureSpriteImporters()
    {
        ConfigureSingleSprite($"{SpriteFolder}/Vesper_Idle.png", 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSingleSprite($"{SpriteFolder}/Vesper_Run1.png", 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSingleSprite($"{SpriteFolder}/Vesper_Run2.png", 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSingleSprite($"{SpriteFolder}/Vesper_Jump.png", 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSingleSprite($"{SpriteFolder}/Vesper_Fall.png", 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSingleSprite($"{SpriteFolder}/Vesper_Glide.png", 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSingleSprite($"{SpriteFolder}/Vesper_Hurt.png", 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);

        ConfigureSingleSprite($"{SpriteFolder}/Tile_Rooftop.png", 128f, new Vector2(0.5f, 0.5f), TextureWrapMode.Repeat, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Hazard_Spikes.png", 128f, new Vector2(0.5f, 0.0f), TextureWrapMode.Repeat, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Enemy_Sentinel.png", 160f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSingleSprite($"{SpriteFolder}/Collectible_Shard.png", 180f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_Checkpoint.png", 110f, new Vector2(0.5f, 0.0f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_GoalBeacon.png", 95f, new Vector2(0.5f, 0.0f), TextureWrapMode.Clamp, SpriteMeshType.Tight);

        ConfigureSingleSprite($"{SpriteFolder}/BG_FarSky.png", 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/BG_MidCity.png", 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/BG_NearSilhouettes.png", 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);

        ConfigureSingleSprite($"{ArtFolder}/VFX_SoftParticle.png", 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{ArtFolder}/UI_SolidWhite.png", 16f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{ArtFolder}/UI_Heart.png", 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
    }

    private static void ConfigureSingleSprite(string assetPath, float ppu, Vector2 pivot, TextureWrapMode wrapMode, SpriteMeshType meshType)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[LevelBuilder] Could not find TextureImporter at {assetPath}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = wrapMode;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteMeshType = meshType;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
    }

    private static BuiltAssets LoadAndCreateAssets()
    {
        BuiltAssets a = new BuiltAssets();

        a.vesperIdle = LoadSprite($"{SpriteFolder}/Vesper_Idle.png");
        a.vesperRun1 = LoadSprite($"{SpriteFolder}/Vesper_Run1.png");
        a.vesperRun2 = LoadSprite($"{SpriteFolder}/Vesper_Run2.png");
        a.vesperJump = LoadSprite($"{SpriteFolder}/Vesper_Jump.png");
        a.vesperFall = LoadSprite($"{SpriteFolder}/Vesper_Fall.png");
        a.vesperGlide = LoadSprite($"{SpriteFolder}/Vesper_Glide.png");
        a.vesperHurt = LoadSprite($"{SpriteFolder}/Vesper_Hurt.png");

        a.tileRooftop = LoadSprite($"{SpriteFolder}/Tile_Rooftop.png");
        a.hazardSpikes = LoadSprite($"{SpriteFolder}/Hazard_Spikes.png");
        a.enemySentinel = LoadSprite($"{SpriteFolder}/Enemy_Sentinel.png");
        a.collectibleShard = LoadSprite($"{SpriteFolder}/Collectible_Shard.png");
        a.propCheckpoint = LoadSprite($"{SpriteFolder}/Prop_Checkpoint.png");
        a.propGoalBeacon = LoadSprite($"{SpriteFolder}/Prop_GoalBeacon.png");

        a.bgFarSky = LoadSprite($"{SpriteFolder}/BG_FarSky.png");
        a.bgMidCity = LoadSprite($"{SpriteFolder}/BG_MidCity.png");
        a.bgNearSilhouettes = LoadSprite($"{SpriteFolder}/BG_NearSilhouettes.png");

        a.softParticleSprite = LoadSprite($"{ArtFolder}/VFX_SoftParticle.png");
        a.uiWhiteSprite = LoadSprite($"{ArtFolder}/UI_SolidWhite.png");
        a.uiHeartSprite = LoadSprite($"{ArtFolder}/UI_Heart.png");

        a.bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/bgm_twilight_rooftops.wav");
        a.jumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_jump.wav");
        a.landClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_land.wav");
        a.collectClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_collect.wav");
        a.hurtClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_hurt.wav");
        a.checkpointClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_checkpoint.wav");
        a.glideClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_glide.wav");
        a.winClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_win.wav");

        // Particle Material using soft radial texture + Sprites/Default (unlit & transparent in URP 2D)
        string matPath = $"{ArtFolder}/VFX_ParticleMat.mat";
        Material pMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (pMat == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            pMat = new Material(shader);
            AssetDatabase.CreateAsset(pMat, matPath);
        }
        pMat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtFolder}/VFX_SoftParticle.png");
        EditorUtility.SetDirty(pMat);
        a.particleMaterial = pMat;

        // Zero-friction PhysicsMaterial2D for smooth rooftop edge contact
        string physMatPath = $"{SettingsFolder}/PlayerFrictionless.physicsMaterial2D";
        PhysicsMaterial2D physMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(physMatPath);
        if (physMat == null)
        {
            physMat = new PhysicsMaterial2D("PlayerFrictionless")
            {
                friction = 0f,
                bounciness = 0f
            };
            AssetDatabase.CreateAsset(physMat, physMatPath);
        }
        a.frictionlessMaterial = physMat;

        a.vesperAnimator = BuildVesperAnimatorController(a);
        AssetDatabase.SaveAssets();
        return a;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null)
        {
            Debug.LogError($"[LevelBuilder] Failed to load sprite at {path}");
        }
        return s;
    }

    private static RuntimeAnimatorController BuildVesperAnimatorController(BuiltAssets a)
    {
        if (File.Exists(AnimatorPath))
        {
            AssetDatabase.DeleteAsset(AnimatorPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsGliding", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

        AnimationClip idleClip = CreateSpriteClip("Vesper_Idle", new[] { a.vesperIdle, a.vesperIdle }, 0.4f, true);
        AnimationClip runClip = CreateSpriteClip("Vesper_Run", new[] { a.vesperRun1, a.vesperRun2, a.vesperRun1 }, 0.12f, true);
        AnimationClip jumpClip = CreateSpriteClip("Vesper_Jump", new[] { a.vesperJump, a.vesperJump }, 0.25f, true);
        AnimationClip fallClip = CreateSpriteClip("Vesper_Fall", new[] { a.vesperFall, a.vesperFall }, 0.25f, true);
        AnimationClip glideClip = CreateSpriteClip("Vesper_Glide", new[] { a.vesperGlide, a.vesperGlide }, 0.25f, true);
        AnimationClip hurtClip = CreateSpriteClip("Vesper_Hurt", new[] { a.vesperHurt, a.vesperHurt }, 0.25f, false);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState idleState = sm.AddState("Idle");
        idleState.motion = idleClip;
        sm.defaultState = idleState;

        AnimatorState runState = sm.AddState("Run");
        runState.motion = runClip;

        AnimatorState jumpState = sm.AddState("Jump");
        jumpState.motion = jumpClip;

        AnimatorState fallState = sm.AddState("Fall");
        fallState.motion = fallClip;

        AnimatorState glideState = sm.AddState("Glide");
        glideState.motion = glideClip;

        AnimatorState hurtState = sm.AddState("Hurt");
        hurtState.motion = hurtClip;

        // Ground movement transitions
        AddTransition(idleState, runState, (t) =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.Greater, 0.2f, "Speed");
        });
        AddTransition(runState, idleState, (t) =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");
        });

        // Airborne transitions
        AddAnyAirTransition(idleState, jumpState, fallState, glideState);
        AddAnyAirTransition(runState, jumpState, fallState, glideState);

        AddTransition(jumpState, fallState, (t) =>
        {
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGliding");
            t.AddCondition(AnimatorConditionMode.Less, 0f, "VerticalVelocity");
        });
        AddTransition(jumpState, glideState, (t) =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGliding");
        });
        AddTransition(jumpState, idleState, (t) =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
        });

        AddTransition(fallState, glideState, (t) =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGliding");
        });
        AddTransition(fallState, jumpState, (t) =>
        {
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGliding");
            t.AddCondition(AnimatorConditionMode.Greater, 0.2f, "VerticalVelocity");
        });
        AddTransition(fallState, idleState, (t) =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
        });

        AddTransition(glideState, fallState, (t) =>
        {
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGliding");
        });
        AddTransition(glideState, idleState, (t) =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
        });

        // Hurt / Death override from AnyState
        AnimatorStateTransition anyHurt = sm.AddAnyStateTransition(hurtState);
        anyHurt.hasExitTime = false;
        anyHurt.duration = 0f;
        anyHurt.canTransitionToSelf = false;
        anyHurt.AddCondition(AnimatorConditionMode.If, 0f, "IsHurt");

        AddTransition(hurtState, idleState, (t) =>
        {
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsHurt");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
        });

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void AddAnyAirTransition(AnimatorState from, AnimatorState jumpState, AnimatorState fallState, AnimatorState glideState)
    {
        AddTransition(from, glideState, (t) =>
        {
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGliding");
        });
        AddTransition(from, jumpState, (t) =>
        {
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGliding");
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VerticalVelocity");
        });
        AddTransition(from, fallState, (t) =>
        {
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGliding");
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "VerticalVelocity");
        });
    }

    private static void AddTransition(AnimatorState from, AnimatorState to, Action<AnimatorStateTransition> configure)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0f;
        configure(t);
    }

    private static AnimationClip CreateSpriteClip(string clipName, Sprite[] frames, float frameDuration, bool loop)
    {
        string clipPath = $"{AnimationFolder}/{clipName}.anim";
        if (File.Exists(clipPath))
        {
            AssetDatabase.DeleteAsset(clipPath);
        }

        AnimationClip clip = new AnimationClip { frameRate = 24f };
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i * frameDuration,
                value = frames[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static void BuildLevelOneScene(BuiltAssets a)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Clear all existing objects except keep reusable references or rebuild cleanly
        GameObject[] existingRoots = scene.GetRootGameObjects();
        foreach (GameObject root in existingRoots)
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        // 1. URP Global Light 2D (ensures Lit2D / default sprites render cleanly)
        GameObject lightGo = new GameObject("Global Light 2D");
        Light2D globalLight = lightGo.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.intensity = 1.05f;
        globalLight.color = Color.white;

        // 2. Main Camera + Follow Controller + Ambient Particles
        GameObject camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.055f, 0.065f, 0.16f, 1f);
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        camGo.transform.position = new Vector3(6f, 2f, -10f);
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<UniversalAdditionalCameraData>();
        CameraController camController = camGo.AddComponent<CameraController>();

        CreateAmbientParticles(camGo.transform, a.particleMaterial);

        // 3. Parallax Background Layers
        CreateParallaxRig(a);

        // 4. AudioManager & GameManager
        GameObject audioGo = new GameObject("AudioManager");
        AudioManager audioMgr = audioGo.AddComponent<AudioManager>();
        audioMgr.bgmClip = a.bgmClip;
        audioMgr.jumpClip = a.jumpClip;
        audioMgr.landClip = a.landClip;
        audioMgr.collectClip = a.collectClip;
        audioMgr.hurtClip = a.hurtClip;
        audioMgr.checkpointClip = a.checkpointClip;
        audioMgr.glideClip = a.glideClip;
        audioMgr.winClip = a.winClip;

        GameObject gmGo = new GameObject("GameManager");
        GameManager gm = gmGo.AddComponent<GameManager>();

        // 5. Player (Vesper)
        GameObject playerGo = CreatePlayer(a);
        gm.playerController = playerGo.GetComponent<PlayerController>();
        gm.playerHealth = playerGo.GetComponent<PlayerHealth>();
        camController.target = playerGo.transform;

        // 6. Level Geometry, Hazards, Enemies, Updrafts, Collectibles, Checkpoints, Goal
        BuildLevelGeometryAndProps(a);

        // 7. Complete UI Canvas (Title, HUD, Pause, Win, Game Over) + EventSystem
        BuildUiCanvas(a, gm);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        Debug.Log("[LevelBuilder] Saved SampleScene.unity and registered in EditorBuildSettings.");
    }

    private static void CreateAmbientParticles(Transform parent, Material particleMat)
    {
        GameObject psGo = new GameObject("AmbientTwilightParticles");
        psGo.transform.SetParent(parent, false);
        psGo.transform.localPosition = new Vector3(0f, 0f, 8f);

        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.45f, 0.92f, 1f, 0.55f),
            new Color(0.98f, 0.84f, 0.45f, 0.5f)
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 120;

        var emission = ps.emission;
        emission.rateOverTime = 16f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(26f, 15f, 1f);

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMat;
        renderer.sortingOrder = -5;
    }

    private static void CreateParallaxRig(BuiltAssets a)
    {
        GameObject bgRoot = new GameObject("ParallaxBackgrounds");

        CreateParallaxLayer("BG_FarSky", a.bgFarSky, bgRoot.transform, 0.86f, 0.72f, 5.8f, -30, 2.25f, new Color(1f, 1f, 1f, 1f));
        CreateParallaxLayer("BG_MidCity", a.bgMidCity, bgRoot.transform, 0.58f, 0.48f, 1.8f, -20, 1.55f, new Color(0.92f, 0.92f, 1f, 0.95f));
        CreateParallaxLayer("BG_NearSilhouettes", a.bgNearSilhouettes, bgRoot.transform, 0.32f, 0.28f, 0.6f, -10, 1.45f, new Color(0.88f, 0.88f, 1f, 0.95f));
    }

    private static void CreateParallaxLayer(
        string layerName,
        Sprite sprite,
        Transform parent,
        float factorX,
        float factorY,
        float baseY,
        int sortingOrder,
        float scale,
        Color tint)
    {
        GameObject layerGo = new GameObject(layerName);
        layerGo.transform.SetParent(parent, false);
        layerGo.transform.position = new Vector3(0f, baseY, 0f);

        ParallaxLayer parallax = layerGo.AddComponent<ParallaxLayer>();
        parallax.parallaxFactorX = factorX;
        parallax.parallaxFactorY = factorY;

        float tileWidth = (sprite != null ? sprite.bounds.size.x : 20f) * scale;
        // Mirror alternating panels for seamless panoramic parallax with zero vertical seams
        for (int i = -1; i <= 9; i++)
        {
            GameObject tileGo = new GameObject($"{layerName}_{i + 1}");
            tileGo.transform.SetParent(layerGo.transform, false);
            tileGo.transform.localPosition = new Vector3(i * tileWidth, 0f, 0f);
            tileGo.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer sr = tileGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            sr.color = tint;
            sr.flipX = Mathf.Abs(i) % 2 == 1;
        }
    }

    private static GameObject CreatePlayer(BuiltAssets a)
    {
        GameObject playerGo = new GameObject("Vesper_Player") { tag = "Player" };
        playerGo.transform.position = new Vector3(2f, 0.2f, 0f);

        SpriteRenderer sr = playerGo.AddComponent<SpriteRenderer>();
        sr.sprite = a.vesperIdle;
        sr.sortingOrder = 10;

        Animator anim = playerGo.AddComponent<Animator>();
        anim.runtimeAnimatorController = a.vesperAnimator;

        Rigidbody2D rb = playerGo.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3.2f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        BoxCollider2D col = playerGo.AddComponent<BoxCollider2D>();
        col.offset = new Vector2(0f, 0.78f);
        col.size = new Vector2(0.66f, 1.56f);
        col.edgeRadius = 0.02f;
        col.sharedMaterial = a.frictionlessMaterial;

        PlayerController pc = playerGo.AddComponent<PlayerController>();
        PlayerHealth ph = playerGo.AddComponent<PlayerHealth>();

        pc.jumpDustParticles = CreateOneShotBurstParticles(
            "JumpDustParticles", playerGo.transform, new Vector3(0f, 0.1f, 0f),
            a.particleMaterial, new Color(0.55f, 0.9f, 1f, 0.85f), 10, 0.14f, 0.28f, 3.2f, 35f, 5
        );
        pc.landingDustParticles = CreateOneShotBurstParticles(
            "LandingDustParticles", playerGo.transform, new Vector3(0f, 0.05f, 0f),
            a.particleMaterial, new Color(0.75f, 0.78f, 0.95f, 0.85f), 14, 0.12f, 0.25f, 3.8f, 15f, 5
        );
        pc.glideTrailParticles = CreateLoopingTrailParticles(
            "GlideTrailParticles", playerGo.transform, new Vector3(0f, 0.9f, 0f),
            a.particleMaterial, new Color(0.32f, 0.88f, 1f, 0.8f), new Color(0.55f, 0.35f, 0.95f, 0.75f), 4
        );
        ph.hurtBurstParticles = CreateOneShotBurstParticles(
            "HurtBurstParticles", playerGo.transform, new Vector3(0f, 0.9f, 0f),
            a.particleMaterial, new Color(1f, 0.28f, 0.4f, 0.95f), 18, 0.16f, 0.32f, 5.2f, 180f, 12
        );

        return playerGo;
    }

    private static void BuildLevelGeometryAndProps(BuiltAssets a)
    {
        GameObject levelRoot = new GameObject("Level_TwilightRooftops");

        // ============================================================
        // ZONE 1: Courtyard of Whispers (x = -2 .. 52)
        // Introduces Movement, Jumping, Shards, and Shadow-Cloak Glide safely
        // ============================================================
        CreatePlatform("Roof_1A_Start", -2f, 14f, 0f, 6f, a.tileRooftop, levelRoot.transform);
        CreatePlatform("Roof_1B_Step", 16f, 22f, 1.4f, 6f, a.tileRooftop, levelRoot.transform);
        CreatePlatform("Roof_1C_Launch", 24f, 30f, 3.0f, 6f, a.tileRooftop, levelRoot.transform);
        // Safe glide catch floor below the first glide gap so players learn risk-free
        CreatePlatform("Roof_1_SafetyFloor", 28f, 43f, -1.5f, 4f, a.tileRooftop, levelRoot.transform);
        CreatePlatform("Roof_1_SafetyStep", 39f, 43f, 0.0f, 1.5f, a.tileRooftop, levelRoot.transform);
        CreatePlatform("Roof_1D_Landing", 40f, 56f, 1.2f, 6f, a.tileRooftop, levelRoot.transform);

        CreateTutorialTrigger("Tut_1_Controls", new Vector2(3.5f, 1.5f), new Vector2(4f, 4f),
            "COURTYARD OF WHISPERS: Move with A/D or Arrows, Jump with SPACE or W!", levelRoot.transform);
        CreateTutorialTrigger("Tut_2_Glide", new Vector2(27f, 4.5f), new Vector2(4f, 4f),
            "SIGNATURE MOVE: Hold SPACE or SHIFT in mid-air to open your Shadow-Cloak Glide!", levelRoot.transform);

        // Shards 1..7
        CreateShard(1, new Vector2(8f, 1.1f), a, levelRoot.transform);
        CreateShard(2, new Vector2(12f, 1.1f), a, levelRoot.transform);
        CreateShard(3, new Vector2(19f, 2.5f), a, levelRoot.transform);
        CreateShard(4, new Vector2(27f, 4.1f), a, levelRoot.transform);
        CreateShard(5, new Vector2(33f, 3.9f), a, levelRoot.transform);
        CreateShard(6, new Vector2(35.5f, 3.3f), a, levelRoot.transform);
        CreateShard(7, new Vector2(38f, 2.7f), a, levelRoot.transform);

        // ============================================================
        // ZONE 2: Gale-Draft Chimneys (x = 46 .. 108)
        // Introduces Checkpoints, Updraft Vents, and Electrified Roof Spikes
        // ============================================================
        CreateCheckpoint(1, "Gale-Draft Overlook", new Vector2(45f, 1.2f), a, levelRoot.transform);
        CreateTutorialTrigger("Tut_3_Updraft", new Vector2(50f, 2.8f), new Vector2(4f, 4f),
            "UPDRAFT CHIMNEYS: Glide inside cyan wind columns to soar to high rooftops!", levelRoot.transform);

        // Safe Updraft intro: low floor still underneath Roof_2B
        CreateUpdraft("Updraft_1_Intro", new Vector2(53.5f, 1.2f), 3.4f, 8.2f, 9.5f, a.particleMaterial, levelRoot.transform);
        CreatePlatform("Roof_2A_Under", 54f, 62f, 1.2f, 4f, a.tileRooftop, levelRoot.transform);
        CreatePlatform("Roof_2B_Upper", 57f, 73f, 5.4f, 2f, a.tileRooftop, levelRoot.transform);

        CreateTutorialTrigger("Tut_4_Spikes", new Vector2(59.5f, 7.0f), new Vector2(3.5f, 4f),
            "HAZARD ALERT: Leap cleanly over Electrified Roof Spikes!", levelRoot.transform);
        CreateSpikes("Spikes_2_Intro", new Vector2(65.5f, 5.4f), 2.0f, a.hazardSpikes, levelRoot.transform);

        // Gap to Roof 2C with spikes + updraft challenge
        CreatePlatform("Roof_2C_Mid", 76.5f, 88.5f, 3.2f, 6f, a.tileRooftop, levelRoot.transform);
        CreateSpikes("Spikes_2C", new Vector2(82.0f, 3.2f), 2.2f, a.hazardSpikes, levelRoot.transform);

        CreateUpdraft("Updraft_2_Chasm", new Vector2(91.0f, 1.5f), 4.0f, 8.8f, 10.2f, a.particleMaterial, levelRoot.transform);
        CreatePlatform("Roof_2D_High", 94.5f, 120f, 6.0f, 6f, a.tileRooftop, levelRoot.transform);
        CreateSpikes("Spikes_2D", new Vector2(99.5f, 6.0f), 2.0f, a.hazardSpikes, levelRoot.transform);

        // Shards 8..14
        CreateShard(8, new Vector2(53.5f, 4.5f), a, levelRoot.transform);
        CreateShard(9, new Vector2(53.5f, 6.8f), a, levelRoot.transform);
        CreateShard(10, new Vector2(65.5f, 7.6f), a, levelRoot.transform);
        CreateShard(11, new Vector2(82.0f, 5.5f), a, levelRoot.transform);
        CreateShard(12, new Vector2(86.0f, 4.4f), a, levelRoot.transform);
        CreateShard(13, new Vector2(91.0f, 6.0f), a, levelRoot.transform);
        CreateShard(14, new Vector2(93.2f, 7.8f), a, levelRoot.transform);

        // ============================================================
        // ZONE 3: Clockwork Foundry Roofs (x = 104 .. 164)
        // Introduces Clockwork Sentinel Drones & High/Low Routes
        // ============================================================
        CreateCheckpoint(2, "Clockwork Foundry Watch", new Vector2(104.5f, 6.0f), a, levelRoot.transform);
        CreateTutorialTrigger("Tut_5_Sentinels", new Vector2(107.5f, 7.5f), new Vector2(3.5f, 4f),
            "CLOCKWORK SENTINELS: Land on top of patrolling drones to stomp-bounce over them!", levelRoot.transform);

        // Safe Sentinel intro on wide flat rooftop (108..120)
        CreateSentinel("Sentinel_1_Intro", new Vector2(114f, 6.95f), -3.2f, 3.2f, 2.2f, a, levelRoot.transform);

        // Sky Bridge with spikes + Sentinel
        CreatePlatform("Roof_3A_Foundry", 123.5f, 136.5f, 4.6f, 6f, a.tileRooftop, levelRoot.transform);
        CreateSpikes("Spikes_3A", new Vector2(126.5f, 4.6f), 1.8f, a.hazardSpikes, levelRoot.transform);
        CreateSentinel("Sentinel_2_Foundry", new Vector2(132.5f, 5.55f), -2.5f, 2.5f, 2.7f, a, levelRoot.transform);

        // Dual route: Upper bonus shard skybridge via Updraft_3 OR lower combat roof
        CreateUpdraft("Updraft_3_Split", new Vector2(138.0f, 2.0f), 2.8f, 8.0f, 9.5f, a.particleMaterial, levelRoot.transform);
        CreatePlatform("Roof_3B_Lower", 139.5f, 149.0f, 2.0f, 6f, a.tileRooftop, levelRoot.transform);
        CreatePlatform("Roof_3B_High", 140.5f, 148.0f, 6.6f, 1.5f, a.tileRooftop, levelRoot.transform);
        CreateSentinel("Sentinel_3_Low", new Vector2(144.2f, 2.95f), -2.8f, 2.8f, 2.6f, a, levelRoot.transform);

        // Twin Spike Gauntlet leading to Zone 4 Checkpoint
        CreatePlatform("Roof_3C_Gauntlet", 151.5f, 164.5f, 3.5f, 6f, a.tileRooftop, levelRoot.transform);
        CreateSpikes("Spikes_3B", new Vector2(154.5f, 3.5f), 1.8f, a.hazardSpikes, levelRoot.transform);
        CreateSpikes("Spikes_3C", new Vector2(159.0f, 3.5f), 1.8f, a.hazardSpikes, levelRoot.transform);

        // Shards 15..20
        CreateShard(15, new Vector2(114.0f, 9.2f), a, levelRoot.transform);
        CreateShard(16, new Vector2(126.5f, 6.9f), a, levelRoot.transform);
        CreateShard(17, new Vector2(132.5f, 7.9f), a, levelRoot.transform);
        CreateShard(18, new Vector2(142.5f, 8.0f), a, levelRoot.transform);
        CreateShard(19, new Vector2(146.0f, 8.0f), a, levelRoot.transform);
        CreateShard(20, new Vector2(156.7f, 5.4f), a, levelRoot.transform);

        // ============================================================
        // ZONE 4: Twilight Sanctum Ascent (x = 162 .. 212)
        // Combines Platforming, Sentinels, and Dual Updraft Glides to Goal
        // ============================================================
        CreateCheckpoint(3, "Sanctum Stair Gate", new Vector2(162.5f, 3.5f), a, levelRoot.transform);
        CreateTutorialTrigger("Tut_6_Final", new Vector2(163.0f, 5.0f), new Vector2(3.5f, 4f),
            "TWILIGHT SANCTUM ASCENT: Combine Updrafts, Glides, and Stomps to reach the Beacon!", levelRoot.transform);

        CreatePlatform("Roof_4A_Bastion", 167.0f, 176.5f, 4.6f, 6f, a.tileRooftop, levelRoot.transform);
        CreateSentinel("Sentinel_4_Bastion", new Vector2(171.8f, 5.55f), -2.8f, 2.8f, 2.9f, a, levelRoot.transform);

        // Dual Updraft Chasm
        CreateUpdraft("Updraft_4_AscentA", new Vector2(179.5f, 2.5f), 3.6f, 9.0f, 10.2f, a.particleMaterial, levelRoot.transform);
        CreatePlatform("Roof_4B_Perch", 182.5f, 188.0f, 7.2f, 1.8f, a.tileRooftop, levelRoot.transform);
        CreateUpdraft("Updraft_5_AscentB", new Vector2(190.5f, 4.5f), 3.6f, 9.5f, 10.5f, a.particleMaterial, levelRoot.transform);

        // Final Sanctum Terrace & Beacon
        CreatePlatform("Roof_4C_Sanctum", 193.5f, 212.0f, 9.0f, 8f, a.tileRooftop, levelRoot.transform);
        CreateSentinel("Sentinel_5_Guard", new Vector2(197.5f, 9.95f), -2.5f, 2.5f, 3.0f, a, levelRoot.transform);
        CreateSpikes("Spikes_4_Final", new Vector2(201.5f, 9.0f), 1.8f, a.hazardSpikes, levelRoot.transform);

        // Shards 21..25
        CreateShard(21, new Vector2(171.8f, 7.9f), a, levelRoot.transform);
        CreateShard(22, new Vector2(179.5f, 7.2f), a, levelRoot.transform);
        CreateShard(23, new Vector2(185.2f, 8.6f), a, levelRoot.transform);
        CreateShard(24, new Vector2(197.5f, 12.1f), a, levelRoot.transform);
        CreateShard(25, new Vector2(201.5f, 11.4f), a, levelRoot.transform);

        CreateLevelGoal(new Vector2(206.5f, 9.0f), a, levelRoot.transform);

        // Invisible Solid World Boundaries (Left & Right) + Abyss Pit Killzone
        CreateBoundaryWall("LeftWorldWall", new Vector2(-2.5f, 10f), new Vector2(1f, 40f), levelRoot.transform);
        CreateBoundaryWall("RightWorldWall", new Vector2(212.5f, 10f), new Vector2(1f, 40f), levelRoot.transform);
        CreateAbyssKillzone(new Vector2(105f, -10.5f), new Vector2(240f, 8f), levelRoot.transform);
    }

    private static void CreatePlatform(string name, float startX, float endX, float topY, float height, Sprite tileSprite, Transform parent)
    {
        float width = endX - startX;
        float centerX = (startX + endX) * 0.5f;
        float centerY = topY - height * 0.5f;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(centerX, centerY, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = tileSprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(width, height);
        sr.sortingOrder = 0;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, height);
    }

    private static void CreateSpikes(string name, Vector2 baseCenter, float width, Sprite spikeSprite, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(baseCenter.x, baseCenter.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spikeSprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(width, 0.85f);
        sr.sortingOrder = 3;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.offset = new Vector2(0f, 0.35f);
        col.size = new Vector2(width * 0.88f, 0.6f);

        Hazard hazard = go.AddComponent<Hazard>();
        hazard.isKillzone = false;
        hazard.damageAmount = 1;
    }

    private static void CreateSentinel(string name, Vector2 pos, float leftOffset, float rightOffset, float speed, BuiltAssets a, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.enemySentinel;
        sr.sortingOrder = 6;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.62f;

        PatrolEnemy enemy = go.AddComponent<PatrolEnemy>();
        enemy.leftOffset = leftOffset;
        enemy.rightOffset = rightOffset;
        enemy.patrolSpeed = speed;
        enemy.defeatParticles = CreateOneShotBurstParticles(
            $"{name}_DefeatParticles", go.transform, Vector3.zero,
            a.particleMaterial, new Color(1f, 0.55f, 0.25f, 0.95f), 20, 0.14f, 0.3f, 5.5f, 180f, 11
        );
    }

    private static void CreateShard(int index, Vector2 pos, BuiltAssets a, Transform parent)
    {
        GameObject go = new GameObject($"MemoryShard_{index:00}");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.collectibleShard;
        sr.sortingOrder = 5;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.52f;

        CollectibleShard shard = go.AddComponent<CollectibleShard>();
        shard.pickupParticles = CreateOneShotBurstParticles(
            $"ShardPickupParticles_{index:00}", go.transform, Vector3.zero,
            a.particleMaterial, new Color(0.32f, 0.96f, 1f, 0.95f), 16, 0.14f, 0.26f, 4.5f, 180f, 12
        );
    }

    private static void CreateCheckpoint(int index, string checkpointName, Vector2 basePos, BuiltAssets a, Transform parent)
    {
        GameObject go = new GameObject($"Checkpoint_{index}_{checkpointName.Replace(" ", "")}");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(basePos.x, basePos.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.propCheckpoint;
        sr.sortingOrder = 2;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.offset = new Vector2(0f, 1.1f);
        col.size = new Vector2(1.6f, 2.4f);

        Checkpoint cp = go.AddComponent<Checkpoint>();
        cp.checkpointIndex = index;
        cp.checkpointName = checkpointName;
        cp.lanternRenderer = sr;
        cp.respawnOffset = new Vector3(0f, 0.5f, 0f);

        cp.activationParticles = CreateOneShotBurstParticles(
            $"CheckpointBurst_{index}", go.transform, new Vector3(0f, 1.5f, 0f),
            a.particleMaterial, new Color(1f, 0.85f, 0.3f, 0.95f), 22, 0.16f, 0.34f, 5.0f, 180f, 11
        );
        cp.idleFlameParticles = CreateLoopingTrailParticles(
            $"CheckpointFlame_{index}", go.transform, new Vector3(0f, 1.6f, 0f),
            a.particleMaterial, new Color(1f, 0.92f, 0.45f, 0.85f), new Color(0.35f, 0.9f, 1f, 0.7f), 8
        );
        cp.idleFlameParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = cp.idleFlameParticles.main;
        main.playOnAwake = false;
    }

    private static void CreateUpdraft(string name, Vector2 baseCenter, float width, float height, float speed, Material particleMat, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(baseCenter.x, baseCenter.y + height * 0.5f, 0f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(width, height);

        UpdraftVent vent = go.AddComponent<UpdraftVent>();
        vent.updraftSpeed = speed;

        // Visible rising cyan wind streams via ParticleSystem
        GameObject psGo = new GameObject("UpdraftWindParticles");
        psGo.transform.SetParent(go.transform, false);
        psGo.transform.localPosition = new Vector3(0f, -height * 0.48f, 0f);
        psGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(height / 7.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6.5f, 8.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.36f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.35f, 0.95f, 1f, 0.75f),
            new Color(0.68f, 0.96f, 1f, 0.55f)
        );
        main.maxParticles = 60;

        var emission = ps.emission;
        emission.rateOverTime = 24f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width * 0.8f, 0.25f, 0.25f);

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMat;
        renderer.sortingOrder = 1;
    }

    private static void CreateTutorialTrigger(string name, Vector2 center, Vector2 size, string message, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, center.y, 0f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = size;

        TutorialTrigger trigger = go.AddComponent<TutorialTrigger>();
        trigger.tutorialMessage = message;
        trigger.displayDuration = 5.0f;
        trigger.triggerOnce = true;
    }

    private static void CreateLevelGoal(Vector2 basePos, BuiltAssets a, Transform parent)
    {
        GameObject go = new GameObject("SanctumBeacon_Goal");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(basePos.x, basePos.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.propGoalBeacon;
        sr.sortingOrder = 4;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.offset = new Vector2(0f, 1.5f);
        col.size = new Vector2(2.0f, 3.2f);

        LevelGoal goal = go.AddComponent<LevelGoal>();
        goal.idleBeaconParticles = CreateLoopingTrailParticles(
            "GoalBeaconIdleParticles", go.transform, new Vector3(0f, 2.0f, 0f),
            a.particleMaterial, new Color(1f, 0.92f, 0.45f, 0.9f), new Color(0.35f, 0.95f, 1f, 0.9f), 11
        );
        goal.victoryBurstParticles = CreateOneShotBurstParticles(
            "GoalVictoryBurstParticles", go.transform, new Vector3(0f, 2.0f, 0f),
            a.particleMaterial, new Color(1f, 0.95f, 0.55f, 1f), 45, 0.2f, 0.45f, 7.5f, 180f, 15
        );
    }

    private static void CreateBoundaryWall(string name, Vector2 center, Vector2 size, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, center.y, 0f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    private static void CreateAbyssKillzone(Vector2 center, Vector2 size, Transform parent)
    {
        GameObject go = new GameObject("AbyssKillzone");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, center.y, 0f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = size;

        Hazard hazard = go.AddComponent<Hazard>();
        hazard.isKillzone = true;
        hazard.damageAmount = 1;
    }

    private static ParticleSystem CreateOneShotBurstParticles(
        string name,
        Transform parent,
        Vector3 localPos,
        Material mat,
        Color color,
        int burstCount,
        float minSize,
        float maxSize,
        float speed,
        float angleSpread,
        int sortingOrder)
    {
        GameObject psGo = new GameObject(name);
        psGo.transform.SetParent(parent, false);
        psGo.transform.localPosition = localPos;
        psGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = color;
        main.gravityModifier = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = Mathf.Clamp(angleSpread, 5f, 180f);
        shape.radius = 0.25f;

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.sortingOrder = sortingOrder;

        return ps;
    }

    private static ParticleSystem CreateLoopingTrailParticles(
        string name,
        Transform parent,
        Vector3 localPos,
        Material mat,
        Color colorA,
        Color colorB,
        int sortingOrder)
    {
        GameObject psGo = new GameObject(name);
        psGo.transform.SetParent(parent, false);
        psGo.transform.localPosition = localPos;

        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.30f);
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 24f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.35f;

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.sortingOrder = sortingOrder;

        return ps;
    }

    private static void BuildUiCanvas(BuiltAssets a, GameManager gm)
    {
        Font uiFont = ResolveUiFont();

        GameObject canvasGo = new GameObject("GameCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        UIManager uiMgr = canvasGo.AddComponent<UIManager>();

        // EventSystem configured cleanly for Unity InputSystem (or Legacy fallback)
        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        esGo.AddComponent<InputSystemUIInputModule>();
#else
        esGo.AddComponent<StandaloneInputModule>();
#endif

        // 1. TITLE SCREEN PANEL
        GameObject titlePanel = CreateFullScreenPanel("TitlePanel", canvasGo.transform, a.uiWhiteSprite, new Color(0.05f, 0.06f, 0.15f, 0.88f));
        uiMgr.titlePanel = titlePanel;

        CreateText("TitleLogo", titlePanel.transform, uiFont, "VESPER: TWILIGHT COURIER", 52, FontStyle.Bold,
            new Color(0.42f, 0.94f, 1f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 165f), new Vector2(960f, 80f));

        CreateText("TitleSubtitle", titlePanel.transform, uiFont, "LEVEL 1  -  NOCTIS REACH ROOFTOPS", 24, FontStyle.Bold,
            new Color(1f, 0.86f, 0.42f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 108f), new Vector2(820f, 44f));

        CreateText("TitlePitch", titlePanel.transform, uiFont,
            "As Vesper, a shadow-stitcher courier of the twilight district, cross the skylines\n" +
            "to ignite the Sanctum Beacon and deliver the sealed Memory Shards.\n\n" +
            "Move: A/D or Arrows   |   Jump: SPACE / W   |   Shadow-Cloak Glide: Hold SPACE or SHIFT\n" +
            "Stomp Clockwork Sentinels from above   |   Pause: ESC / P",
            20, FontStyle.Normal, new Color(0.90f, 0.93f, 1f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(980f, 150f));

        Button startBtn = CreateButton("StartRunButton", titlePanel.transform, a.uiWhiteSprite, uiFont,
            "START RUN", new Vector2(0f, -120f), new Vector2(280f, 62f),
            new Color(0.16f, 0.62f, 0.82f, 1f), Color.white);
        startBtn.onClick.AddListener(gm.StartGame);

        Button titleQuitBtn = CreateButton("TitleQuitButton", titlePanel.transform, a.uiWhiteSprite, uiFont,
            "QUIT", new Vector2(0f, -200f), new Vector2(220f, 50f),
            new Color(0.24f, 0.22f, 0.36f, 1f), new Color(0.9f, 0.9f, 1f, 1f));
        titleQuitBtn.onClick.AddListener(gm.QuitGame);

        // 2. HUD PANEL
        GameObject hudPanel = CreateFullScreenPanel("HUDPanel", canvasGo.transform, a.uiWhiteSprite, new Color(0f, 0f, 0f, 0f));
        uiMgr.hudPanel = hudPanel;

        // Top-left Hearts
        uiMgr.heartIcons = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject heartGo = new GameObject($"HeartIcon_{i + 1}");
            heartGo.transform.SetParent(hudPanel.transform, false);
            Image hImg = heartGo.AddComponent<Image>();
            hImg.sprite = a.uiHeartSprite;
            hImg.color = uiMgr.fullHeartColor;

            RectTransform hRt = heartGo.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(0f, 1f);
            hRt.pivot = new Vector2(0f, 1f);
            hRt.anchoredPosition = new Vector2(24f + i * 48f, -20f);
            hRt.sizeDelta = new Vector2(40f, 40f);
            uiMgr.heartIcons[i] = hImg;
        }

        // Top-left Shard Counter & Timer
        uiMgr.shardCounterText = CreateText("ShardCounterText", hudPanel.transform, uiFont, "MEMORY SHARDS: 0 / 25", 24, FontStyle.Bold,
            new Color(0.45f, 0.96f, 1f, 1f), TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(215f, -84f), new Vector2(380f, 38f));

        uiMgr.timerText = CreateText("TimerText", hudPanel.transform, uiFont, "TIME: 00:00", 20, FontStyle.Bold,
            new Color(1f, 0.9f, 0.58f, 1f), TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(215f, -118f), new Vector2(260f, 32f));

        // Top-right Audio Controls + Pause Button + Glide Status
        uiMgr.hudVolumeSlider = CreateSlider("HudVolumeSlider", hudPanel.transform, a.uiWhiteSprite,
            new Vector2(1f, 1f), new Vector2(-285f, -38f), new Vector2(150f, 22f),
            out _);

        Button hudMuteBtn = CreateButton("HudMuteButton", hudPanel.transform, a.uiWhiteSprite, uiFont,
            "MUTE AUDIO", new Vector2(0f, 0f), new Vector2(130f, 36f),
            new Color(0.18f, 0.22f, 0.38f, 0.92f), Color.white);
        RectTransform hudMuteRt = hudMuteBtn.GetComponent<RectTransform>();
        hudMuteRt.anchorMin = new Vector2(1f, 1f);
        hudMuteRt.anchorMax = new Vector2(1f, 1f);
        hudMuteRt.anchoredPosition = new Vector2(-195f, -38f);
        uiMgr.hudMuteButtonText = hudMuteBtn.GetComponentInChildren<Text>();
        uiMgr.hudMuteButtonText.fontSize = 15;
        hudMuteBtn.onClick.AddListener(uiMgr.OnToggleMuteClicked);

        Button pauseBtn = CreateButton("HudPauseButton", hudPanel.transform, a.uiWhiteSprite, uiFont,
            "PAUSE", new Vector2(0f, 0f), new Vector2(96f, 36f),
            new Color(0.28f, 0.22f, 0.45f, 0.92f), Color.white);
        RectTransform pauseBtnRt = pauseBtn.GetComponent<RectTransform>();
        pauseBtnRt.anchorMin = new Vector2(1f, 1f);
        pauseBtnRt.anchorMax = new Vector2(1f, 1f);
        pauseBtnRt.anchoredPosition = new Vector2(-72f, -38f);
        pauseBtn.GetComponentInChildren<Text>().fontSize = 16;
        pauseBtn.onClick.AddListener(gm.TogglePause);

        // Glide badge at bottom-left
        GameObject badgeGo = new GameObject("GlideStatusBadge");
        badgeGo.transform.SetParent(hudPanel.transform, false);
        Image badgeImg = badgeGo.AddComponent<Image>();
        badgeImg.sprite = a.uiWhiteSprite;
        badgeImg.color = new Color(0.12f, 0.14f, 0.28f, 0.85f);
        RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0f, 0f);
        badgeRt.anchorMax = new Vector2(0f, 0f);
        badgeRt.anchoredPosition = new Vector2(155f, 38f);
        badgeRt.sizeDelta = new Vector2(265f, 38f);
        uiMgr.glideBadgeBackground = badgeImg;

        uiMgr.glideBadgeText = CreateText("GlideBadgeText", badgeGo.transform, uiFont, "CLOAK: READY", 18, FontStyle.Bold,
            Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 34f));

        // Center-bottom Tutorial / Checkpoint Banner
        GameObject bannerGo = new GameObject("BannerContainer");
        bannerGo.transform.SetParent(hudPanel.transform, false);
        Image bannerImg = bannerGo.AddComponent<Image>();
        bannerImg.sprite = a.uiWhiteSprite;
        bannerImg.color = new Color(0.06f, 0.08f, 0.18f, 0.86f);
        RectTransform bannerRt = bannerGo.GetComponent<RectTransform>();
        bannerRt.anchorMin = new Vector2(0.5f, 0f);
        bannerRt.anchorMax = new Vector2(0.5f, 0f);
        bannerRt.anchoredPosition = new Vector2(0f, 48f);
        bannerRt.sizeDelta = new Vector2(740f, 54f);
        uiMgr.bannerContainer = bannerGo;
        uiMgr.bannerText = CreateText("BannerText", bannerGo.transform, uiFont, "", 19, FontStyle.Bold,
            new Color(0.95f, 0.96f, 1f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(710f, 48f));
        bannerGo.SetActive(false);

        // 3. PAUSE PANEL
        GameObject pausePanel = CreateFullScreenPanel("PausePanel", canvasGo.transform, a.uiWhiteSprite, new Color(0.04f, 0.05f, 0.14f, 0.86f));
        uiMgr.pausePanel = pausePanel;

        CreateText("PauseTitle", pausePanel.transform, uiFont, "GAME PAUSED", 42, FontStyle.Bold,
            new Color(0.45f, 0.94f, 1f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 175f), new Vector2(600f, 70f));

        CreateText("PauseVolLabel", pausePanel.transform, uiFont, "MUSIC & SFX VOLUME", 19, FontStyle.Bold,
            Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(300f, 34f));

        uiMgr.pauseVolumeSlider = CreateSlider("PauseVolumeSlider", pausePanel.transform, a.uiWhiteSprite,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 75f), new Vector2(260f, 26f),
            out _);

        Button pauseMuteBtn = CreateButton("PauseMuteButton", pausePanel.transform, a.uiWhiteSprite, uiFont,
            "MUTE AUDIO", new Vector2(0f, 25f), new Vector2(220f, 44f),
            new Color(0.22f, 0.25f, 0.42f, 1f), Color.white);
        uiMgr.pauseMuteButtonText = pauseMuteBtn.GetComponentInChildren<Text>();
        pauseMuteBtn.onClick.AddListener(uiMgr.OnToggleMuteClicked);

        Button resumeBtn = CreateButton("ResumeButton", pausePanel.transform, a.uiWhiteSprite, uiFont,
            "RESUME", new Vector2(0f, -45f), new Vector2(250f, 52f),
            new Color(0.16f, 0.62f, 0.82f, 1f), Color.white);
        resumeBtn.onClick.AddListener(gm.ResumeGame);

        Button pauseRestartBtn = CreateButton("PauseRestartButton", pausePanel.transform, a.uiWhiteSprite, uiFont,
            "RESTART LEVEL", new Vector2(0f, -110f), new Vector2(250f, 48f),
            new Color(0.28f, 0.25f, 0.48f, 1f), Color.white);
        pauseRestartBtn.onClick.AddListener(gm.RestartLevel);

        Button pauseTitleBtn = CreateButton("PauseTitleButton", pausePanel.transform, a.uiWhiteSprite, uiFont,
            "MAIN MENU", new Vector2(0f, -172f), new Vector2(250f, 46f),
            new Color(0.22f, 0.22f, 0.34f, 1f), Color.white);
        pauseTitleBtn.onClick.AddListener(gm.ReturnToTitle);

        Button pauseQuitBtn = CreateButton("PauseQuitButton", pausePanel.transform, a.uiWhiteSprite, uiFont,
            "QUIT GAME", new Vector2(0f, -230f), new Vector2(250f, 44f),
            new Color(0.45f, 0.16f, 0.24f, 1f), Color.white);
        pauseQuitBtn.onClick.AddListener(gm.QuitGame);

        // 4. WIN SCREEN PANEL
        GameObject winPanel = CreateFullScreenPanel("WinPanel", canvasGo.transform, a.uiWhiteSprite, new Color(0.04f, 0.08f, 0.18f, 0.88f));
        uiMgr.winPanel = winPanel;

        CreateText("WinTitle", winPanel.transform, uiFont, "LEVEL COMPLETE!", 48, FontStyle.Bold,
            new Color(1f, 0.88f, 0.42f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 155f), new Vector2(720f, 75f));

        uiMgr.winSummaryText = CreateText("WinSummaryText", winPanel.transform, uiFont,
            "Sanctum Beacon Reached!\n\nMemory Shards: 0 / 25\nClear Time: 00:00\nRetries: 0",
            25, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), new Vector2(680f, 160f));

        Button winPlayAgainBtn = CreateButton("WinPlayAgainButton", winPanel.transform, a.uiWhiteSprite, uiFont,
            "PLAY AGAIN", new Vector2(0f, -95f), new Vector2(260f, 54f),
            new Color(0.16f, 0.66f, 0.78f, 1f), Color.white);
        winPlayAgainBtn.onClick.AddListener(gm.RestartLevel);

        Button winMenuBtn = CreateButton("WinMenuButton", winPanel.transform, a.uiWhiteSprite, uiFont,
            "MAIN MENU", new Vector2(0f, -165f), new Vector2(260f, 48f),
            new Color(0.24f, 0.24f, 0.38f, 1f), Color.white);
        winMenuBtn.onClick.AddListener(gm.ReturnToTitle);

        // 5. GAME OVER PANEL
        GameObject gameOverPanel = CreateFullScreenPanel("GameOverPanel", canvasGo.transform, a.uiWhiteSprite, new Color(0.12f, 0.03f, 0.08f, 0.88f));
        uiMgr.gameOverPanel = gameOverPanel;

        CreateText("GameOverTitle", gameOverPanel.transform, uiFont, "COURIER FALLEN", 48, FontStyle.Bold,
            new Color(1f, 0.38f, 0.46f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 130f), new Vector2(700f, 75f));

        CreateText("GameOverHint", gameOverPanel.transform, uiFont,
            "Vesper lost all hearts on the twilight rooftops.", 23, FontStyle.Normal,
            new Color(0.95f, 0.88f, 0.92f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 65f), new Vector2(680f, 45f));

        Button retryCpBtn = CreateButton("RetryCheckpointButton", gameOverPanel.transform, a.uiWhiteSprite, uiFont,
            "RETRY CHECKPOINT", new Vector2(0f, -25f), new Vector2(280f, 56f),
            new Color(0.18f, 0.62f, 0.84f, 1f), Color.white);
        retryCpBtn.onClick.AddListener(gm.RetryFromCheckpoint);

        Button goRestartBtn = CreateButton("GameOverRestartButton", gameOverPanel.transform, a.uiWhiteSprite, uiFont,
            "RESTART LEVEL", new Vector2(0f, -95f), new Vector2(260f, 48f),
            new Color(0.28f, 0.24f, 0.42f, 1f), Color.white);
        goRestartBtn.onClick.AddListener(gm.RestartLevel);

        Button goTitleBtn = CreateButton("GameOverTitleButton", gameOverPanel.transform, a.uiWhiteSprite, uiFont,
            "MAIN MENU", new Vector2(0f, -158f), new Vector2(260f, 46f),
            new Color(0.22f, 0.22f, 0.32f, 1f), Color.white);
        goTitleBtn.onClick.AddListener(gm.ReturnToTitle);

        // Initial visible panel in Scene (Title active, others inactive)
        titlePanel.SetActive(true);
        hudPanel.SetActive(false);
        pausePanel.SetActive(false);
        winPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }

    private static Font ResolveUiFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null) return f;

        f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (f != null) return f;

        return Font.CreateDynamicFontFromOSFont("Arial", 20);
    }

    private static GameObject CreateFullScreenPanel(string name, Transform parent, Sprite whiteSprite, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.sprite = whiteSprite;
        img.color = color;
        img.raycastTarget = color.a > 0.01f;

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return panel;
    }

    private static Text CreateText(
        string name,
        Transform parent,
        Font font,
        string content,
        int fontSize,
        FontStyle style,
        Color color,
        TextAnchor alignment,
        Vector2 anchor,
        Vector2 anchoredPos,
        Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text txt = go.AddComponent<Text>();
        txt.font = font;
        txt.text = content;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = alignment;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return txt;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        Sprite whiteSprite,
        Font font,
        string label,
        Vector2 anchoredPos,
        Vector2 sizeDelta,
        Color bgColor,
        Color textColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.sprite = whiteSprite;
        img.color = bgColor;

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.2f, 1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.9f, 1f);
        btn.colors = cb;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        CreateText("Label", go.transform, font, label, 20, FontStyle.Bold, textColor,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, sizeDelta);

        return btn;
    }

    private static Slider CreateSlider(
        string name,
        Transform parent,
        Sprite whiteSprite,
        Vector2 anchor,
        Vector2 anchoredPos,
        Vector2 sizeDelta,
        out Image fillImage)
    {
        GameObject sliderGo = new GameObject(name);
        sliderGo.transform.SetParent(parent, false);
        RectTransform rt = sliderGo.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Slider slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.75f;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderGo.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.sprite = whiteSprite;
        bgImg.color = new Color(0.16f, 0.18f, 0.30f, 0.9f);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.25f);
        bgRt.anchorMax = new Vector2(1f, 0.75f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGo.transform, false);
        RectTransform faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.25f);
        faRt.anchorMax = new Vector2(1f, 0.75f);
        faRt.offsetMin = new Vector2(4f, 0f);
        faRt.offsetMax = new Vector2(-4f, 0f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        fillImage = fill.AddComponent<Image>();
        fillImage.sprite = whiteSprite;
        fillImage.color = new Color(0.32f, 0.88f, 1f, 1f);
        RectTransform fRt = fill.GetComponent<RectTransform>();
        fRt.sizeDelta = Vector2.zero;
        slider.fillRect = fRt;

        // Handle Slide Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGo.transform, false);
        RectTransform haRt = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.offsetMin = new Vector2(8f, 0f);
        haRt.offsetMax = new Vector2(-8f, 0f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        Image hImg = handle.AddComponent<Image>();
        hImg.sprite = whiteSprite;
        hImg.color = new Color(1f, 0.92f, 0.55f, 1f);
        RectTransform hRt = handle.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(16f, 24f);
        slider.handleRect = hRt;
        slider.targetGraphic = hImg;

        return slider;
    }

    public static void RenderVerificationScreenshots()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[LevelBuilder] No Main Camera found for screenshot capture.");
            return;
        }

        // Disable overlay TitlePanel temporarily so screenshots show actual 2D level art clearly
        GameObject titlePanel = GameObject.Find("TitlePanel");
        bool wasTitleActive = titlePanel != null && titlePanel.activeSelf;
        if (titlePanel != null) titlePanel.SetActive(false);

        Vector3 originalPos = cam.transform.position;

        ParticleSystem[] systems = UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (ParticleSystem ps in systems)
        {
            if (ps.main.playOnAwake)
            {
                ps.Simulate(1.2f, true, true);
            }
        }

        CaptureCameraView(cam, new Vector3(8f, 2.2f, -10f), "Screenshots/gameplay_preview.png");
        CaptureCameraView(cam, new Vector3(53.5f, 4.2f, -10f), "Screenshots/zone2_updraft_preview.png");
        CaptureCameraView(cam, new Vector3(114f, 7.2f, -10f), "Screenshots/zone3_sentinel_preview.png");
        CaptureCameraView(cam, new Vector3(201f, 10.5f, -10f), "Screenshots/level_goal_preview.png");

        cam.transform.position = originalPos;
        if (titlePanel != null) titlePanel.SetActive(wasTitleActive);
    }

    private static void CaptureCameraView(Camera cam, Vector3 camPos, string outputPath)
    {
        cam.transform.position = camPos;
        int width = 1280;
        int height = 720;

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        cam.Render();

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        File.WriteAllBytes(outputPath, tex.EncodeToPNG());

        cam.targetTexture = null;
        RenderTexture.active = prev;
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);

        Debug.Log($"[LevelBuilder] Captured verification screenshot: {outputPath}");
    }

    [MenuItem("Vesper/Build Windows Standalone Player")]
    public static void BuildWindowsPlayer()
    {
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Windows/VesperTwilightCourier.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new Exception($"[LevelBuilder] Windows build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
        }

        Debug.Log($"[LevelBuilder] BuildSucceeded: Builds/Windows/VesperTwilightCourier.exe ({report.summary.totalSize / (1024f * 1024f):F2} MB)");
    }
}
