using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
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
    private const string AnimFramesFolder = "Assets/Art/Sprites/AnimFrames";
    private const string AudioFolder = "Assets/Audio";
    private const string ArtFolder = "Assets/Art";
    private const string SettingsFolder = "Assets/Settings";

    private class BuiltAssets
    {
        public Sprite vesperIdle, vesperRun1, vesperRun2, vesperJump, vesperFall, vesperGlide, vesperHurt;
        public Sprite[] vesperIdleFrames, vesperRunFrames, vesperJumpFrames, vesperFallFrames, vesperGlideFrames, vesperHurtFrames;
        public Sprite[] sentinelFrames, shardFrames, checkpointFrames, goalBeaconFrames, waterFrames;
        public Sprite tileRooftop, tileRooftopBody, hazardSpikes, enemySentinel, collectibleShard, propCheckpoint, propGoalBeacon;
        public Sprite tileSpikeSlot, propGothicPillar, tileMovingPlatform, tileCrumblePlatform;
        public Sprite bgFarSky, bgMidCity, bgNearSilhouettes;
        public Sprite uiWhiteSprite, uiHeartSprite, softParticleSprite;
        public Material particleMaterial;
        public PhysicsMaterial2D frictionlessMaterial;
        public RuntimeAnimatorController vesperAnimator;
        public RuntimeAnimatorController sentinelAnimator, shardAnimator, checkpointAnimator, goalBeaconAnimator, waterAnimator;
        public AudioClip bgmClip, jumpClip, landClip, collectClip, hurtClip, checkpointClip, glideClip, winClip;
    }

    private struct PlatformFootprint
    {
        public string name;
        public float minX;
        public float maxX;
        public float minY;
        public float maxY;
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
        RenderVerificationScreenshots(assets);
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
        RenderVerificationScreenshots(assets);
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
        ConfigureSingleSprite($"{SpriteFolder}/Tile_Rooftop_Body.png", 128f, new Vector2(0.5f, 0.5f), TextureWrapMode.Repeat, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Tile_SpikeSlot.png", 128f, new Vector2(0.5f, 0.5f), TextureWrapMode.Repeat, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_GothicPillar.png", 128f, new Vector2(0.5f, 0.5f), TextureWrapMode.Repeat, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Tile_MovingPlatform.png", 128f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Tile_CrumblePlatform.png", 128f, new Vector2(0.5f, 0.5f), TextureWrapMode.Repeat, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Hazard_Spikes.png", 128f, new Vector2(0.5f, 0.0f), TextureWrapMode.Repeat, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Enemy_Sentinel.png", 160f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSingleSprite($"{SpriteFolder}/Collectible_Shard.png", 180f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_Checkpoint.png", 110f, new Vector2(0.5f, 0.0f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_GoalBeacon.png", 95f, new Vector2(0.5f, 0.0f), TextureWrapMode.Clamp, SpriteMeshType.Tight);

        ConfigureSpriteSequence("Vesper_Idle", 6, 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Vesper_Run", 6, 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Vesper_Jump", 4, 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Vesper_Fall", 4, 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Vesper_Glide", 4, 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Vesper_Hurt", 4, 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Enemy_Sentinel", 6, 160f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Collectible_Shard", 6, 180f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Prop_Checkpoint", 4, 110f, new Vector2(0.5f, 0.0f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Prop_GoalBeacon", 6, 95f, new Vector2(0.5f, 0.0f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Water", 6, 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Repeat, SpriteMeshType.FullRect);

        ConfigureSingleSprite($"{SpriteFolder}/BG_FarSky.png", 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/BG_MidCity.png", 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/BG_NearSilhouettes.png", 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);

        ConfigureSingleSprite($"{ArtFolder}/VFX_SoftParticle.png", 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{ArtFolder}/UI_SolidWhite.png", 16f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{ArtFolder}/UI_Heart.png", 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
    }

    private static void ConfigureSpriteSequence(string prefix, int count, float ppu, Vector2 pivot, TextureWrapMode wrapMode, SpriteMeshType meshType)
    {
        for (int i = 1; i <= count; i++)
        {
            ConfigureSingleSprite($"{AnimFramesFolder}/{prefix}_{i:00}.png", ppu, pivot, wrapMode, meshType);
        }
    }

    private static void ConfigureSingleSprite(string assetPath, float ppu, Vector2 pivot, TextureWrapMode wrapMode, SpriteMeshType meshType)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
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
        a.tileRooftopBody = LoadSprite($"{SpriteFolder}/Tile_Rooftop_Body.png");
        a.tileSpikeSlot = LoadSprite($"{SpriteFolder}/Tile_SpikeSlot.png");
        a.propGothicPillar = LoadSprite($"{SpriteFolder}/Prop_GothicPillar.png");
        a.tileMovingPlatform = LoadSprite($"{SpriteFolder}/Tile_MovingPlatform.png");
        a.tileCrumblePlatform = LoadSprite($"{SpriteFolder}/Tile_CrumblePlatform.png");
        a.hazardSpikes = LoadSprite($"{SpriteFolder}/Hazard_Spikes.png");
        a.enemySentinel = LoadSprite($"{SpriteFolder}/Enemy_Sentinel.png");
        a.collectibleShard = LoadSprite($"{SpriteFolder}/Collectible_Shard.png");
        a.propCheckpoint = LoadSprite($"{SpriteFolder}/Prop_Checkpoint.png");
        a.propGoalBeacon = LoadSprite($"{SpriteFolder}/Prop_GoalBeacon.png");

        a.vesperIdleFrames = LoadSpriteSequence("Vesper_Idle", 6);
        a.vesperRunFrames = LoadSpriteSequence("Vesper_Run", 6);
        a.vesperJumpFrames = LoadSpriteSequence("Vesper_Jump", 4);
        a.vesperFallFrames = LoadSpriteSequence("Vesper_Fall", 4);
        a.vesperGlideFrames = LoadSpriteSequence("Vesper_Glide", 4);
        a.vesperHurtFrames = LoadSpriteSequence("Vesper_Hurt", 4);
        a.sentinelFrames = LoadSpriteSequence("Enemy_Sentinel", 6);
        a.shardFrames = LoadSpriteSequence("Collectible_Shard", 6);
        a.checkpointFrames = LoadSpriteSequence("Prop_Checkpoint", 4);
        a.goalBeaconFrames = LoadSpriteSequence("Prop_GoalBeacon", 6);
        a.waterFrames = LoadSpriteSequence("Water", 6);

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
        a.sentinelAnimator = BuildLoopingPropAnimator("SentinelAnimator", "Enemy_Sentinel_Hover", a.sentinelFrames, 0.11f);
        a.shardAnimator = BuildLoopingPropAnimator("ShardAnimator", "Collectible_Shard_Shimmer", a.shardFrames, 0.13f);
        a.checkpointAnimator = BuildLoopingPropAnimator("CheckpointAnimator", "Prop_Checkpoint_Flicker", a.checkpointFrames, 0.16f);
        a.goalBeaconAnimator = BuildLoopingPropAnimator("GoalBeaconAnimator", "Prop_GoalBeacon_Pulse", a.goalBeaconFrames, 0.14f);
        a.waterAnimator = BuildLoopingPropAnimator("WaterCanalAnimator", "Water_Canal_Flow", a.waterFrames, 0.16f);
        AssetDatabase.SaveAssets();
        return a;
    }

    private static Sprite[] LoadSpriteSequence(string prefix, int count)
    {
        Sprite[] frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            frames[i] = LoadSprite($"{AnimFramesFolder}/{prefix}_{i + 1:00}.png");
        }
        return frames;
    }

    private static RuntimeAnimatorController BuildLoopingPropAnimator(string controllerName, string clipName, Sprite[] frames, float frameDuration)
    {
        string path = $"{AnimationFolder}/{controllerName}.controller";
        if (File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimationClip clip = CreateSpriteClip(clipName, frames, frameDuration, true);
        AnimatorState state = controller.layers[0].stateMachine.AddState("Loop");
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);
        return controller;
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

        AnimationClip idleClip = CreateSpriteClip("Vesper_Idle", a.vesperIdleFrames, 0.14f, true);
        AnimationClip runClip = CreateSpriteClip("Vesper_Run", a.vesperRunFrames, 0.075f, true);
        AnimationClip jumpClip = CreateSpriteClip("Vesper_Jump", a.vesperJumpFrames, 0.09f, true);
        AnimationClip fallClip = CreateSpriteClip("Vesper_Fall", a.vesperFallFrames, 0.09f, true);
        AnimationClip glideClip = CreateSpriteClip("Vesper_Glide", a.vesperGlideFrames, 0.09f, true);
        AnimationClip hurtClip = CreateSpriteClip("Vesper_Hurt", a.vesperHurtFrames, 0.07f, false);

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

        int keyCount = loop && frames.Length > 1 ? frames.Length + 1 : frames.Length;
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[keyCount];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i * frameDuration,
                value = frames[i]
            };
        }
        if (loop && frames.Length > 1)
        {
            keys[frames.Length] = new ObjectReferenceKeyframe
            {
                time = frames.Length * frameDuration,
                value = frames[0]
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
        camController.maxX = 221f;

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
        BuildUiCanvas(a, gm, cam);

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
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.45f, 0.94f, 1f, 0.58f),
            new Color(0.98f, 0.84f, 0.45f, 0.52f)
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 140;

        var emission = ps.emission;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(28f, 16f, 1f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.25f);
        vel.y = new ParticleSystem.MinMaxCurve(0.18f);
        vel.z = new ParticleSystem.MinMaxCurve(0f);

        ConfigureParticleNoise(ps, 0.65f, 0.45f, 0.35f);

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMat;
        renderer.sortingOrder = -2;
        ConfigureParticleFade(ps, 0.76f, true);
    }

    private static void CreateParallaxRig(BuiltAssets a)
    {
        GameObject bgRoot = new GameObject("ParallaxBackgrounds");

        CreateParallaxLayer("BG_FarSky", a.bgFarSky, bgRoot.transform, 0.86f, 0.72f, 1.0f, -30, 2.25f, new Color(1f, 1f, 1f, 1f));
        CreateParallaxLayer("BG_MidCity", a.bgMidCity, bgRoot.transform, 0.58f, 0.48f, 3.2f, -20, 1.65f, new Color(0.92f, 0.92f, 1f, 0.96f));
        CreateParallaxLayer("BG_NearSilhouettes", a.bgNearSilhouettes, bgRoot.transform, 0.32f, 0.28f, 1.4f, -10, 1.45f, new Color(0.95f, 0.94f, 1f, 0.98f));
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
        // Mirror alternating panels for seamless panoramic parallax across the entire extended level
        for (int i = -1; i <= 12; i++)
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
        sr.sprite = a.vesperIdleFrames != null && a.vesperIdleFrames.Length > 0 ? a.vesperIdleFrames[0] : a.vesperIdle;
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
            a.particleMaterial, new Color(0.55f, 0.92f, 1f, 0.88f), 12, 0.14f, 0.28f, 3.4f, 40f, 5
        );
        pc.landingDustParticles = CreateOneShotBurstParticles(
            "LandingDustParticles", playerGo.transform, new Vector3(0f, 0.05f, 0f),
            a.particleMaterial, new Color(0.75f, 0.8f, 0.96f, 0.85f), 14, 0.12f, 0.25f, 3.8f, 22f, 5
        );
        pc.glideTrailParticles = CreateLoopingTrailParticles(
            "GlideTrailParticles", playerGo.transform, new Vector3(-0.22f, 0.96f, 0f),
            a.particleMaterial, new Color(0.32f, 0.94f, 1f, 0.92f), new Color(0.62f, 0.36f, 1f, 0.85f), 4,
            enableRibbons: true, noiseStrength: 0.5f
        );
        var glideVel = pc.glideTrailParticles.velocityOverLifetime;
        glideVel.enabled = true;
        glideVel.x = new ParticleSystem.MinMaxCurve(-2.4f);
        glideVel.y = new ParticleSystem.MinMaxCurve(0.55f);
        glideVel.z = new ParticleSystem.MinMaxCurve(0f);
        var glideMain = pc.glideTrailParticles.main;
        glideMain.playOnAwake = false;
        pc.glideTrailParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ph.hurtBurstParticles = CreateOneShotBurstParticles(
            "HurtBurstParticles", playerGo.transform, new Vector3(0f, 0.9f, 0f),
            a.particleMaterial, new Color(1f, 0.28f, 0.4f, 0.95f), 20, 0.16f, 0.34f, 5.6f, 180f, 12,
            enableTrails: true
        );

        return playerGo;
    }

    private static void BuildLevelGeometryAndProps(BuiltAssets a)
    {
        GameObject levelRoot = new GameObject("Level_TwilightRooftops");
        List<PlatformFootprint> footprints = new List<PlatformFootprint>();
        const float GroundBottomY = -9.0f;

        // ============================================================
        // TWILIGHT CANAL WATER (x = -14 .. 244, surfaceY = -4.6)
        // Animated 6-frame moonlit canal water + mist + splash particles
        // ============================================================
        CreateWaterCanal(-14f, 244f, -4.6f, a, levelRoot.transform);

        // ============================================================
        // ZONE 1: Courtyard of Whispers (x = -2 .. 56)
        // Strictly disjoint rooftops grounded into the canal; teaches movement,
        // jumping, Shards, and Shadow-Cloak Glide safely
        // ============================================================
        CreateGroundedPlatform("Roof_1A_Start", -2f, 14f, 0f, GroundBottomY, a, levelRoot.transform, footprints);
        CreateGroundedPlatform("Roof_1B_Step", 16.5f, 23.0f, 1.4f, GroundBottomY, a, levelRoot.transform, footprints);
        CreateGroundedPlatform("Roof_1C_Launch", 25.5f, 31.5f, 3.0f, GroundBottomY, a, levelRoot.transform, footprints);
        // Disjoint safety floor & climb-out step cleanly recessed inside the glide chasm
        CreateGroundedPlatform("Roof_1_SafetyFloor", 32.0f, 38.8f, -1.8f, GroundBottomY, a, levelRoot.transform, footprints);
        CreateGroundedPlatform("Roof_1_SafetyStep", 39.0f, 41.6f, -0.3f, GroundBottomY, a, levelRoot.transform, footprints);
        CreateGroundedPlatform("Roof_1D_Landing", 42.0f, 56.0f, 1.2f, GroundBottomY, a, levelRoot.transform, footprints);

        CreateTutorialTrigger("Tut_1_Controls", new Vector2(3.5f, 1.5f), new Vector2(4f, 4f),
            "COURTYARD OF WHISPERS: Move with A/D or Arrows, Jump with SPACE or W!", levelRoot.transform);
        CreateTutorialTrigger("Tut_2_Glide", new Vector2(28.5f, 4.5f), new Vector2(4f, 4f),
            "SIGNATURE MOVE: Hold SPACE or SHIFT in mid-air to open your Shadow-Cloak Glide!", levelRoot.transform);

        // Shards 1..7
        CreateShard(1, new Vector2(8f, 1.1f), a, levelRoot.transform);
        CreateShard(2, new Vector2(12f, 1.1f), a, levelRoot.transform);
        CreateShard(3, new Vector2(19.8f, 2.5f), a, levelRoot.transform);
        CreateShard(4, new Vector2(28.5f, 4.1f), a, levelRoot.transform);
        CreateShard(5, new Vector2(34.0f, 3.9f), a, levelRoot.transform);
        CreateShard(6, new Vector2(36.8f, 3.3f), a, levelRoot.transform);
        CreateShard(7, new Vector2(39.6f, 2.6f), a, levelRoot.transform);

        // ============================================================
        // ZONE 2: Gale-Draft Chimneys (x = 45 .. 127)
        // Introduces Checkpoints, Updraft Vents, Ground-Embedded Pop-Up Spikes,
        // Gothic Pillar Aqueducts, and Non-Colliding Clockwork Moving Platforms
        // ============================================================
        CreateCheckpoint(1, "Gale-Draft Overlook", new Vector2(45.5f, 1.2f), a, levelRoot.transform);
        CreateTutorialTrigger("Tut_3_Updraft", new Vector2(50.0f, 2.8f), new Vector2(4f, 4f),
            "UPDRAFT CHIMNEYS: Glide inside cyan wind columns to soar to high rooftops!", levelRoot.transform);

        CreateUpdraft("Updraft_1_Intro", new Vector2(53.5f, 1.2f), 3.4f, 8.2f, 9.6f, a.particleMaterial, levelRoot.transform);

        // Elevated aqueduct bridge visibly supported by grounded Gothic stone pillars down into the canal
        CreateElevatedPlatform("Roof_2B_Upper", 58.0f, 74.0f, 5.4f, 2.0f, a, levelRoot.transform, footprints);
        CreateGothicPillar("Pillar_2B_Left", 60.5f, 3.45f, GroundBottomY, 1.6f, a, levelRoot.transform);
        CreateGothicPillar("Pillar_2B_Right", 71.5f, 3.45f, GroundBottomY, 1.6f, a, levelRoot.transform);

        CreateTutorialTrigger("Tut_4_Spikes", new Vector2(61.0f, 7.0f), new Vector2(3.5f, 4f),
            "AMBUSH TRAPS: Watch the stone grates - concealed spikes spark and erupt when crossed!", levelRoot.transform);
        CreatePopUpSpikes("Spikes_2_Ambush", new Vector2(66.2f, 5.4f), 2.0f, PopUpSpikes.TrapMode.ProximityAmbush, 0f, a, levelRoot.transform);

        // Horizontal Moving Clockwork Lift spanning 74.0 .. 87.0 with >= 1.2u clearance on both ends
        CreateMovingPlatform("MovingPlat_2A_Shuttle", new Vector3(77.0f, 3.72f, 0f), new Vector3(84.0f, 3.72f, 0f),
            3.6f, 0.95f, 2.4f, 0.45f, 0.0f, a, levelRoot.transform, footprints);

        CreateGroundedPlatform("Roof_2C_Mid", 87.0f, 99.0f, 3.2f, GroundBottomY, a, levelRoot.transform, footprints);
        CreatePopUpSpikes("Spikes_2C_Timed", new Vector2(92.6f, 3.2f), 2.2f, PopUpSpikes.TrapMode.TimedCycle, 0.5f, a, levelRoot.transform);

        // Deep canal chasm updraft lifting up to Roof_2D_High
        CreateUpdraft("Updraft_2_Chasm", new Vector2(102.2f, -1.5f), 4.0f, 11.2f, 10.6f, a.particleMaterial, levelRoot.transform);
        CreateGroundedPlatform("Roof_2D_High", 105.5f, 127.0f, 6.0f, GroundBottomY, a, levelRoot.transform, footprints);
        CreatePopUpSpikes("Spikes_2D_Ambush", new Vector2(109.8f, 6.0f), 2.0f, PopUpSpikes.TrapMode.ProximityAmbush, 0f, a, levelRoot.transform);

        // Shards 8..14
        CreateShard(8, new Vector2(53.5f, 4.5f), a, levelRoot.transform);
        CreateShard(9, new Vector2(53.5f, 6.9f), a, levelRoot.transform);
        CreateShard(10, new Vector2(66.2f, 7.6f), a, levelRoot.transform);
        CreateShard(11, new Vector2(80.5f, 5.8f), a, levelRoot.transform);
        CreateShard(12, new Vector2(92.6f, 5.5f), a, levelRoot.transform);
        CreateShard(13, new Vector2(102.2f, 5.8f), a, levelRoot.transform);
        CreateShard(14, new Vector2(104.2f, 7.9f), a, levelRoot.transform);

        // ============================================================
        // ZONE 3: Clockwork Foundry Roofs (x = 113 .. 178.5)
        // Introduces Clockwork Sentinels, Crumbling Stone Ledges, and
        // Dual Disjoint Moving Clockwork Platforms (zero collision!)
        // ============================================================
        CreateCheckpoint(2, "Clockwork Foundry Watch", new Vector2(113.5f, 6.0f), a, levelRoot.transform);
        CreateTutorialTrigger("Tut_5_Sentinels", new Vector2(116.5f, 7.5f), new Vector2(3.5f, 4f),
            "CLOCKWORK FOUNDRY: Stomp Sentinels from above and move fast across cracked stone!", levelRoot.transform);

        CreateSentinel("Sentinel_1_Intro", new Vector2(121.5f, 6.95f), -3.0f, 3.0f, 2.3f, a, levelRoot.transform);

        // Fragile crumbling stone span in canal gap 127.0 .. 136.0 (2.5u clean gaps on each side)
        CreateCrumblingPlatform("Crumble_3A_Span", 129.5f, 133.5f, 5.2f, 1.0f, a, levelRoot.transform, footprints);

        CreateGroundedPlatform("Roof_3A_Foundry", 136.0f, 148.5f, 4.6f, GroundBottomY, a, levelRoot.transform, footprints);
        CreatePopUpSpikes("Spikes_3A_Ambush", new Vector2(139.2f, 4.6f), 1.8f, PopUpSpikes.TrapMode.ProximityAmbush, 0f, a, levelRoot.transform);
        CreateSentinel("Sentinel_2_Foundry", new Vector2(144.6f, 5.55f), -2.4f, 2.4f, 2.7f, a, levelRoot.transform);

        // Wide Foundry Canal (148.5 .. 164.5): Dual strictly non-colliding Moving Platforms
        // Lift A stays strictly within X [150.8 .. 154.2], Shuttle B stays strictly within X [156.1 .. 162.9] -> >= 1.9u clear!
        CreateMovingPlatform("MovingPlat_3A_VerticalLift", new Vector3(152.5f, 2.8f, 0f), new Vector3(152.5f, 6.2f, 0f),
            3.4f, 0.95f, 2.3f, 0.45f, 0.0f, a, levelRoot.transform, footprints);
        CreateMovingPlatform("MovingPlat_3B_Shuttle", new Vector3(157.8f, 4.2f, 0f), new Vector3(161.2f, 4.2f, 0f),
            3.4f, 0.95f, 2.1f, 0.40f, 0.35f, a, levelRoot.transform, footprints);

        // Twin out-of-phase Pop-Up Spike Gauntlet leading to Zone 4 Checkpoint
        CreateGroundedPlatform("Roof_3C_Gauntlet", 164.5f, 178.5f, 3.6f, GroundBottomY, a, levelRoot.transform, footprints);
        CreatePopUpSpikes("Spikes_3B_TimedA", new Vector2(168.0f, 3.6f), 1.8f, PopUpSpikes.TrapMode.TimedCycle, 0.0f, a, levelRoot.transform);
        CreatePopUpSpikes("Spikes_3C_TimedB", new Vector2(172.2f, 3.6f), 1.8f, PopUpSpikes.TrapMode.TimedCycle, 1.65f, a, levelRoot.transform);

        // Shards 15..20
        CreateShard(15, new Vector2(121.5f, 9.2f), a, levelRoot.transform);
        CreateShard(16, new Vector2(131.5f, 6.8f), a, levelRoot.transform);
        CreateShard(17, new Vector2(144.6f, 7.9f), a, levelRoot.transform);
        CreateShard(18, new Vector2(152.5f, 7.6f), a, levelRoot.transform);
        CreateShard(19, new Vector2(159.5f, 6.0f), a, levelRoot.transform);
        CreateShard(20, new Vector2(170.1f, 5.7f), a, levelRoot.transform);

        // ============================================================
        // ZONE 4: Twilight Sanctum Ascent (x = 176 .. 228)
        // Combines Sentinels, Dual Updrafts, Crumbling Perch, and Final Ambush
        // ============================================================
        CreateCheckpoint(3, "Sanctum Stair Gate", new Vector2(176.2f, 3.6f), a, levelRoot.transform);
        CreateTutorialTrigger("Tut_6_Final", new Vector2(176.5f, 5.2f), new Vector2(3.5f, 4f),
            "TWILIGHT SANCTUM ASCENT: Chain Updrafts, Glides, and crumbling ledges to ignite the Beacon!", levelRoot.transform);

        CreateGroundedPlatform("Roof_4A_Bastion", 181.0f, 190.5f, 4.6f, GroundBottomY, a, levelRoot.transform, footprints);
        CreateSentinel("Sentinel_4_Bastion", new Vector2(185.8f, 5.55f), -2.8f, 2.8f, 2.9f, a, levelRoot.transform);

        // Dual Updraft Ascent with Crumbling Perch mid-air
        CreateUpdraft("Updraft_4_AscentA", new Vector2(193.8f, -1.5f), 3.6f, 12.0f, 10.6f, a.particleMaterial, levelRoot.transform);
        CreateCrumblingPlatform("Crumble_4B_Perch", 197.5f, 202.0f, 7.4f, 1.0f, a, levelRoot.transform, footprints);
        CreateUpdraft("Updraft_5_AscentB", new Vector2(205.5f, 1.0f), 3.6f, 11.5f, 10.8f, a.particleMaterial, levelRoot.transform);

        // Final Sanctum Terrace & Animated Beacon
        CreateGroundedPlatform("Roof_4C_Sanctum", 209.0f, 228.0f, 9.0f, GroundBottomY, a, levelRoot.transform, footprints);
        CreateSentinel("Sentinel_5_Guard", new Vector2(213.4f, 9.95f), -2.4f, 2.4f, 3.0f, a, levelRoot.transform);
        CreatePopUpSpikes("Spikes_4_FinalAmbush", new Vector2(217.6f, 9.0f), 1.8f, PopUpSpikes.TrapMode.ProximityAmbush, 0f, a, levelRoot.transform);

        // Shards 21..25
        CreateShard(21, new Vector2(185.8f, 7.9f), a, levelRoot.transform);
        CreateShard(22, new Vector2(193.8f, 7.4f), a, levelRoot.transform);
        CreateShard(23, new Vector2(199.8f, 9.0f), a, levelRoot.transform);
        CreateShard(24, new Vector2(213.4f, 12.1f), a, levelRoot.transform);
        CreateShard(25, new Vector2(217.6f, 11.4f), a, levelRoot.transform);

        CreateLevelGoal(new Vector2(222.8f, 9.0f), a, levelRoot.transform);

        // Verify mathematically that zero static, crumbling, or moving platform swept envelopes overlap!
        ValidateNoPlatformOverlaps(footprints);

        // Invisible Solid World Boundaries (Left & Right) + Deep Backup Abyss Killzone
        CreateBoundaryWall("LeftWorldWall", new Vector2(-2.5f, 10f), new Vector2(1f, 40f), levelRoot.transform);
        CreateBoundaryWall("RightWorldWall", new Vector2(228.5f, 10f), new Vector2(1f, 40f), levelRoot.transform);
        CreateAbyssKillzone(new Vector2(113f, -11.5f), new Vector2(270f, 4f), levelRoot.transform);
    }

    private static void ValidateNoPlatformOverlaps(List<PlatformFootprint> footprints)
    {
        for (int i = 0; i < footprints.Count; i++)
        {
            for (int j = i + 1; j < footprints.Count; j++)
            {
                PlatformFootprint a = footprints[i];
                PlatformFootprint b = footprints[j];
                bool overlapX = a.minX < b.maxX - 0.01f && a.maxX > b.minX + 0.01f;
                bool overlapY = a.minY < b.maxY - 0.01f && a.maxY > b.minY + 0.01f;
                if (overlapX && overlapY)
                {
                    throw new Exception(
                        $"[LevelBuilder] Platform overlap detected between '{a.name}' ([{a.minX:F2}..{a.maxX:F2}], [{a.minY:F2}..{a.maxY:F2}]) " +
                        $"and '{b.name}' ([{b.minX:F2}..{b.maxX:F2}], [{b.minY:F2}..{b.maxY:F2}])!");
                }
            }
        }
        Debug.Log($"[LevelBuilder] Platform Overlap Validation PASSED across {footprints.Count} static/moving/crumbling footprints (0 overlaps).");
    }

    private static void CreateGroundedPlatform(string name, float startX, float endX, float topY, float bottomY, BuiltAssets a, Transform parent, List<PlatformFootprint> footprints)
    {
        float height = Mathf.Max(2.0f, topY - bottomY);
        CreatePlatformInternal(name, startX, endX, topY, height, a, parent, footprints);
    }

    private static void CreateElevatedPlatform(string name, float startX, float endX, float topY, float height, BuiltAssets a, Transform parent, List<PlatformFootprint> footprints)
    {
        CreatePlatformInternal(name, startX, endX, topY, Mathf.Max(2.0f, height), a, parent, footprints);
    }

    private static void CreatePlatformInternal(string name, float startX, float endX, float topY, float finalHeight, BuiltAssets a, Transform parent, List<PlatformFootprint> footprints)
    {
        float width = endX - startX;
        float centerX = (startX + endX) * 0.5f;
        float centerY = topY - finalHeight * 0.5f;

        footprints.Add(new PlatformFootprint
        {
            name = name,
            minX = startX,
            maxX = endX,
            minY = topY - finalHeight,
            maxY = topY
        });

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(centerX, centerY, 0f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, finalHeight);

        const float capHeight = 2.0f;
        GameObject capGo = new GameObject("RooftopCap");
        capGo.transform.SetParent(go.transform, false);
        capGo.transform.localPosition = new Vector3(0f, finalHeight * 0.5f - capHeight * 0.5f, 0f);

        SpriteRenderer capSr = capGo.AddComponent<SpriteRenderer>();
        capSr.sprite = a.tileRooftop;
        capSr.drawMode = SpriteDrawMode.Tiled;
        capSr.tileMode = SpriteTileMode.Continuous;
        capSr.size = new Vector2(width, capHeight);
        capSr.sortingOrder = 0;

        float bodyHeight = finalHeight - capHeight;
        if (bodyHeight > 0.05f)
        {
            GameObject bodyGo = new GameObject("StoneBody");
            bodyGo.transform.SetParent(go.transform, false);
            bodyGo.transform.localPosition = new Vector3(0f, -capHeight * 0.5f, 0f);

            SpriteRenderer bodySr = bodyGo.AddComponent<SpriteRenderer>();
            bodySr.sprite = a.tileRooftopBody;
            bodySr.drawMode = SpriteDrawMode.Tiled;
            bodySr.tileMode = SpriteTileMode.Continuous;
            bodySr.size = new Vector2(width, bodyHeight);
            bodySr.sortingOrder = 0;
        }
    }

    private static void CreateGothicPillar(string name, float centerX, float topY, float bottomY, float width, BuiltAssets a, Transform parent)
    {
        float height = Mathf.Max(1f, topY - bottomY);
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(centerX, (topY + bottomY) * 0.5f, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.propGothicPillar;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(width, height);
        sr.sortingOrder = -1;
    }

    private static void CreateMovingPlatform(
        string name,
        Vector3 startPos,
        Vector3 endPos,
        float width,
        float height,
        float travelDuration,
        float endpointPause,
        float phaseOffset,
        BuiltAssets a,
        Transform parent,
        List<PlatformFootprint> footprints)
    {
        // Register the full swept bounding box of this moving platform so no overlap can EVER happen
        footprints.Add(new PlatformFootprint
        {
            name = name,
            minX = Mathf.Min(startPos.x, endPos.x) - width * 0.5f,
            maxX = Mathf.Max(startPos.x, endPos.x) + width * 0.5f,
            minY = Mathf.Min(startPos.y, endPos.y) - height * 0.5f,
            maxY = Mathf.Max(startPos.y, endPos.y) + height * 0.5f
        });

        // Subtle glowing iron guide rail showing the platform's exact safe path
        GameObject railGo = new GameObject($"{name}_GuideRail");
        railGo.transform.SetParent(parent, false);
        Vector3 mid = (startPos + endPos) * 0.5f;
        Vector3 diff = endPos - startPos;
        float railLen = diff.magnitude + 0.5f;
        float railAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        railGo.transform.position = mid;
        railGo.transform.rotation = Quaternion.Euler(0f, 0f, railAngle);
        railGo.transform.localScale = new Vector3(railLen, 0.14f, 1f);
        SpriteRenderer railSr = railGo.AddComponent<SpriteRenderer>();
        railSr.sprite = a.uiWhiteSprite;
        railSr.color = new Color(0.28f, 0.72f, 0.88f, 0.34f);
        railSr.sortingOrder = -1;

        // Endcap nodes on guide rail
        foreach (Vector3 endPt in new[] { startPos, endPos })
        {
            GameObject cap = new GameObject("TrackAnchor");
            cap.transform.SetParent(parent, false);
            cap.transform.position = endPt;
            cap.transform.localScale = new Vector3(0.26f, 0.26f, 1f);
            SpriteRenderer capSr = cap.AddComponent<SpriteRenderer>();
            capSr.sprite = a.softParticleSprite;
            capSr.color = new Color(0.45f, 0.94f, 1f, 0.75f);
            capSr.sortingOrder = -1;
        }

        // Actual moving carriage
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = startPos;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, height * 0.78f);
        col.offset = new Vector2(0f, height * 0.11f);

        GameObject visualGo = new GameObject("CarriageVisual");
        visualGo.transform.SetParent(go.transform, false);
        SpriteRenderer sr = visualGo.AddComponent<SpriteRenderer>();
        sr.sprite = a.tileMovingPlatform;
        sr.sortingOrder = 2;
        if (a.tileMovingPlatform != null)
        {
            Vector2 spriteSize = a.tileMovingPlatform.bounds.size;
            visualGo.transform.localScale = new Vector3(width / Mathf.Max(0.01f, spriteSize.x), height / Mathf.Max(0.01f, spriteSize.y), 1f);
        }

        MovingPlatform mp = go.AddComponent<MovingPlatform>();
        mp.startPosition = startPos;
        mp.endPosition = endPos;
        mp.travelDuration = travelDuration;
        mp.endpointPause = endpointPause;
        mp.phaseOffset = phaseOffset;

        // Twin cyan thruster steam jets underneath carriage
        GameObject psGo = new GameObject($"{name}_Thrusters");
        psGo.transform.SetParent(go.transform, false);
        psGo.transform.localPosition = new Vector3(0f, -height * 0.48f, 0f);
        psGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.52f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 3.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.30f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.35f, 0.96f, 1f, 0.85f),
            new Color(0.65f, 0.92f, 1f, 0.55f)
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 45;

        var emission = ps.emission;
        emission.rateOverTime = 24f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width * 0.58f, 0.1f, 0.1f);

        ConfigureParticleNoise(ps, 0.4f, 1.4f, 0.8f);

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.material = a.particleMaterial;
        renderer.sortingOrder = 1;
        ConfigureParticleFade(ps, 0.88f, true);
        mp.thrusterParticles = ps;
    }

    private static void CreateCrumblingPlatform(
        string name,
        float startX,
        float endX,
        float topY,
        float height,
        BuiltAssets a,
        Transform parent,
        List<PlatformFootprint> footprints)
    {
        float width = endX - startX;
        float centerX = (startX + endX) * 0.5f;
        float centerY = topY - height * 0.5f;

        footprints.Add(new PlatformFootprint
        {
            name = name,
            minX = startX,
            maxX = endX,
            minY = topY - height,
            maxY = topY
        });

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(centerX, centerY, 0f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, height);

        GameObject visualGo = new GameObject("CrackedStoneVisual");
        visualGo.transform.SetParent(go.transform, false);
        SpriteRenderer sr = visualGo.AddComponent<SpriteRenderer>();
        sr.sprite = a.tileCrumblePlatform;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(width, height);
        sr.sortingOrder = 1;

        // Tumbling stone debris particles with RotationOverLifetime + gravity
        GameObject psGo = new GameObject($"{name}_DebrisParticles");
        psGo.transform.SetParent(go.transform, false);
        psGo.transform.localPosition = new Vector3(0f, -height * 0.25f, 0f);
        psGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.30f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.55f, 0.52f, 0.68f, 0.95f),
            new Color(0.98f, 0.68f, 0.32f, 0.95f)
        );
        main.gravityModifier = 1.85f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;

        var emission = ps.emission;
        emission.rateOverTime = 26f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width * 0.9f, 0.2f, 0.1f);

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-240f * Mathf.Deg2Rad, 240f * Mathf.Deg2Rad);

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.material = a.particleMaterial;
        renderer.sortingOrder = 6;
        ConfigureParticleFade(ps, 0.95f, true);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        CrumblingPlatform cp = go.AddComponent<CrumblingPlatform>();
        cp.visualTransform = visualGo.transform;
        cp.visualRenderer = sr;
        cp.debrisParticles = ps;
    }

    private static void CreatePopUpSpikes(
        string name,
        Vector2 baseCenter,
        float width,
        PopUpSpikes.TrapMode mode,
        float cycleOffset,
        BuiltAssets a,
        Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(baseCenter.x, baseCenter.y, 0f);

        // 1. Flush recessed metal trap grate sitting embedded inside the rooftop lip
        GameObject slotGo = new GameObject("FloorTrapGrate");
        slotGo.transform.SetParent(go.transform, false);
        slotGo.transform.localPosition = new Vector3(0f, -0.14f, 0f);
        SpriteRenderer slotSr = slotGo.AddComponent<SpriteRenderer>();
        slotSr.sprite = a.tileSpikeSlot;
        slotSr.drawMode = SpriteDrawMode.Tiled;
        slotSr.tileMode = SpriteTileMode.Continuous;
        slotSr.size = new Vector2(width + 0.16f, 0.36f);
        slotSr.sortingOrder = 4;

        // 2. Custom-range SpriteMask above topY so retracted spikes are 100% hidden inside the stonework
        GameObject maskGo = new GameObject("SpikeRevealMask");
        maskGo.transform.SetParent(go.transform, false);
        maskGo.transform.localPosition = new Vector3(0f, 0.56f, 0f);
        maskGo.transform.localScale = new Vector3(width + 0.35f, 1.12f, 1f);
        SpriteMask mask = maskGo.AddComponent<SpriteMask>();
        mask.sprite = a.uiWhiteSprite;
        mask.isCustomRangeActive = true;
        mask.backSortingOrder = 2;
        mask.frontSortingOrder = 4;

        // 3. Animated Spike Blades child (masked so only portion emerging above the floor grate renders)
        GameObject bladesGo = new GameObject("SpikeBlades");
        bladesGo.transform.SetParent(go.transform, false);
        bladesGo.transform.localPosition = new Vector3(0f, -0.92f, 0f);
        SpriteRenderer bladeSr = bladesGo.AddComponent<SpriteRenderer>();
        bladeSr.sprite = a.hazardSpikes;
        bladeSr.drawMode = SpriteDrawMode.Tiled;
        bladeSr.tileMode = SpriteTileMode.Continuous;
        bladeSr.size = new Vector2(width, 0.85f);
        bladeSr.sortingOrder = 3;
        bladeSr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        // 4. Crackling spark & lightning-trail ParticleSystem for telegraph & eruption
        GameObject psGo = new GameObject($"{name}_Sparks");
        psGo.transform.SetParent(go.transform, false);
        psGo.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        psGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.35f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.4f, 6.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.22f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.24f, 0.56f, 0.98f),
            new Color(0.45f, 0.96f, 1f, 0.98f)
        );
        main.gravityModifier = 0.85f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width * 0.85f, 0.1f, 0.1f);

        ConfigureParticleNoise(ps, 1.15f, 3.0f, 1.5f);
        ConfigureParticleTrails(ps, a.particleMaterial, 0.65f, 0.14f, 0.45f);

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.material = a.particleMaterial;
        renderer.sortingOrder = 6;
        ConfigureParticleFade(ps, 1.0f, true);

        // 5. PopUpSpikes controller
        PopUpSpikes trap = go.AddComponent<PopUpSpikes>();
        trap.mode = mode;
        trap.cycleOffset = cycleOffset;
        trap.spikeVisual = bladesGo.transform;
        trap.sparkParticles = ps;
        trap.lethalBoxSize = new Vector2(width * 0.84f, 0.56f);
        trap.lethalBoxOffset = new Vector2(0f, 0.34f);
    }

    private static void CreateWaterCanal(float startX, float endX, float surfaceY, BuiltAssets a, Transform parent)
    {
        float width = endX - startX;
        float centerX = (startX + endX) * 0.5f;

        GameObject root = new GameObject("TwilightCanal_Water");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(centerX, surfaceY - 1.0f, 0f);

        // Trigger collider slightly below the wave crest so falling into the water splashes & respawns
        BoxCollider2D col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.offset = new Vector2(0f, -0.35f);
        col.size = new Vector2(width, 2.6f);

        // Back parallax wave layer behind masonry & pillars
        GameObject backLayer = new GameObject("WaterBackLayer");
        backLayer.transform.SetParent(root.transform, false);
        backLayer.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        SpriteRenderer backSr = backLayer.AddComponent<SpriteRenderer>();
        backSr.sprite = a.waterFrames[0];
        backSr.drawMode = SpriteDrawMode.Tiled;
        backSr.tileMode = SpriteTileMode.Continuous;
        backSr.size = new Vector2(width + 8f, 3.6f);
        backSr.color = new Color(0.56f, 0.72f, 0.92f, 0.82f);
        backSr.sortingOrder = -3;
        Animator backAnim = backLayer.AddComponent<Animator>();
        backAnim.runtimeAnimatorController = a.waterAnimator;
        backAnim.speed = 0.78f;

        // Front luminous wave layer overlapping building foundations
        GameObject frontLayer = new GameObject("WaterFrontLayer");
        frontLayer.transform.SetParent(root.transform, false);
        frontLayer.transform.localPosition = new Vector3(0f, -0.15f, 0f);
        SpriteRenderer frontSr = frontLayer.AddComponent<SpriteRenderer>();
        frontSr.sprite = a.waterFrames[0];
        frontSr.drawMode = SpriteDrawMode.Tiled;
        frontSr.tileMode = SpriteTileMode.Continuous;
        frontSr.size = new Vector2(width + 8f, 4.0f);
        frontSr.color = new Color(0.92f, 0.98f, 1f, 0.86f);
        frontSr.sortingOrder = 14;
        Animator frontAnim = frontLayer.AddComponent<Animator>();
        frontAnim.runtimeAnimatorController = a.waterAnimator;
        frontAnim.speed = 1.05f;

        // Deep opaque abyss backdrop below the wave texture so no sky leaks under the canal
        GameObject deepFill = new GameObject("WaterDeepFill");
        deepFill.transform.SetParent(root.transform, false);
        deepFill.transform.localPosition = new Vector3(0f, -3.8f, 0f);
        deepFill.transform.localScale = new Vector3(width + 12f, 5.2f, 1f);
        SpriteRenderer deepSr = deepFill.AddComponent<SpriteRenderer>();
        deepSr.sprite = a.uiWhiteSprite;
        deepSr.color = new Color(0.03f, 0.08f, 0.19f, 0.98f);
        deepSr.sortingOrder = 13;

        // Rising Twilight Canal Mist ParticleSystems spaced along the canal
        for (float mx = 10f; mx <= 230f; mx += 44f)
        {
            GameObject mistGo = new GameObject($"CanalMistParticles_{mx:000}");
            mistGo.transform.SetParent(root.transform, false);
            mistGo.transform.position = new Vector3(mx, surfaceY + 0.1f, 0f);

            ParticleSystem mist = mistGo.AddComponent<ParticleSystem>();
            var main = mist.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 4.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.55f, 1.35f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.32f, 0.84f, 0.98f, 0.24f),
                new Color(0.45f, 0.52f, 0.92f, 0.18f)
            );
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 45;

            var emission = mist.emission;
            emission.rateOverTime = 8f;

            var shape = mist.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(42f, 0.25f, 0.2f);

            var vel = mist.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(0.35f);
            vel.y = new ParticleSystem.MinMaxCurve(0.45f);
            vel.z = new ParticleSystem.MinMaxCurve(0f);

            ConfigureParticleNoise(mist, 0.55f, 0.4f, 0.3f);

            var mr = mistGo.GetComponent<ParticleSystemRenderer>();
            mr.material = a.particleMaterial;
            mr.sortingOrder = 15;
            ConfigureParticleFade(mist, 0.34f, false);
        }

        // Dynamic Water Splash ParticleSystem triggered on fall into canal
        GameObject splashGo = new GameObject("WaterSplashParticles");
        splashGo.transform.SetParent(root.transform, false);
        splashGo.transform.position = new Vector3(0f, surfaceY, 0f);
        splashGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem splash = splashGo.AddComponent<ParticleSystem>();
        var sMain = splash.main;
        sMain.loop = false;
        sMain.playOnAwake = false;
        sMain.duration = 0.6f;
        sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        sMain.startSpeed = new ParticleSystem.MinMaxCurve(4.5f, 9.2f);
        sMain.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.36f);
        sMain.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.55f, 0.96f, 1f, 0.96f),
            new Color(0.92f, 0.98f, 1f, 0.92f)
        );
        sMain.gravityModifier = 1.9f;
        sMain.simulationSpace = ParticleSystemSimulationSpace.World;
        sMain.maxParticles = 80;

        var sEmit = splash.emission;
        sEmit.rateOverTime = 0f;
        sEmit.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });

        var sShape = splash.shape;
        sShape.shapeType = ParticleSystemShapeType.Cone;
        sShape.angle = 36f;
        sShape.radius = 0.35f;

        ConfigureParticleTrails(splash, a.particleMaterial, 0.55f, 0.16f, 0.5f);

        var sRend = splashGo.GetComponent<ParticleSystemRenderer>();
        sRend.material = a.particleMaterial;
        sRend.sortingOrder = 16;
        ConfigureParticleFade(splash, 0.95f, true);

        WaterCanal canal = root.AddComponent<WaterCanal>();
        canal.frontWaveLayer = frontLayer.transform;
        canal.backWaveLayer = backLayer.transform;
        canal.surfaceWorldY = surfaceY;
        canal.splashParticles = splash;
    }

    private static void CreateSentinel(string name, Vector2 pos, float leftOffset, float rightOffset, float speed, BuiltAssets a, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.sentinelFrames[0];
        sr.sortingOrder = 6;

        Animator anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = a.sentinelAnimator;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.62f;

        // Ambient clockwork exhaust & arcane wisps trailing the drone
        ParticleSystem exhaust = CreateLoopingTrailParticles(
            $"{name}_ExhaustParticles", go.transform, new Vector3(0f, -0.15f, 0f),
            a.particleMaterial, new Color(0.96f, 0.32f, 0.45f, 0.65f), new Color(0.88f, 0.68f, 0.32f, 0.55f), 5,
            enableRibbons: false, noiseStrength: 0.45f
        );
        var eEmit = exhaust.emission;
        eEmit.rateOverTime = 12f;
        var eMain = exhaust.main;
        eMain.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);

        PatrolEnemy enemy = go.AddComponent<PatrolEnemy>();
        enemy.leftOffset = leftOffset;
        enemy.rightOffset = rightOffset;
        enemy.patrolSpeed = speed;
        enemy.defeatParticles = CreateOneShotBurstParticles(
            $"{name}_DefeatParticles", go.transform, Vector3.zero,
            a.particleMaterial, new Color(1f, 0.58f, 0.25f, 0.96f), 26, 0.14f, 0.34f, 6.2f, 180f, 11,
            enableTrails: true
        );
    }

    private static void CreateShard(int index, Vector2 pos, BuiltAssets a, Transform parent)
    {
        GameObject go = new GameObject($"MemoryShard_{index:00}");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.shardFrames[0];
        sr.sortingOrder = 5;

        Animator anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = a.shardAnimator;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.52f;

        // Ambient orbiting cyan shimmer sparkles around uncollected shard
        ParticleSystem sparkle = CreateLoopingTrailParticles(
            $"ShardSparkle_{index:00}", go.transform, Vector3.zero,
            a.particleMaterial, new Color(0.45f, 0.98f, 1f, 0.72f), new Color(1f, 0.92f, 0.55f, 0.65f), 6,
            enableRibbons: false, noiseStrength: 0.35f
        );
        var spMain = sparkle.main;
        spMain.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.15f);
        spMain.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.65f);
        spMain.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        var spEmit = sparkle.emission;
        spEmit.rateOverTime = 7f;

        CollectibleShard shard = go.AddComponent<CollectibleShard>();
        shard.pickupParticles = CreateOneShotBurstParticles(
            $"ShardPickupParticles_{index:00}", go.transform, Vector3.zero,
            a.particleMaterial, new Color(0.32f, 0.96f, 1f, 0.98f), 20, 0.14f, 0.28f, 5.2f, 180f, 12,
            enableTrails: true
        );
    }

    private static void CreateCheckpoint(int index, string checkpointName, Vector2 basePos, BuiltAssets a, Transform parent)
    {
        GameObject go = new GameObject($"Checkpoint_{index}_{checkpointName.Replace(" ", "")}");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(basePos.x, basePos.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.checkpointFrames[0];
        sr.sortingOrder = 2;

        Animator anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = a.checkpointAnimator;

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
            $"CheckpointBurst_{index}", go.transform, new Vector3(-0.38f, 1.55f, 0f),
            a.particleMaterial, new Color(1f, 0.85f, 0.3f, 0.96f), 26, 0.16f, 0.34f, 5.5f, 180f, 11,
            enableTrails: true
        );
        cp.idleFlameParticles = CreateLoopingTrailParticles(
            $"CheckpointFlame_{index}", go.transform, new Vector3(-0.38f, 1.62f, 0f),
            a.particleMaterial, new Color(1f, 0.92f, 0.45f, 0.88f), new Color(0.35f, 0.94f, 1f, 0.78f), 8,
            enableRibbons: false, noiseStrength: 0.6f
        );
        var flameVel = cp.idleFlameParticles.velocityOverLifetime;
        flameVel.enabled = true;
        flameVel.x = new ParticleSystem.MinMaxCurve(0f);
        flameVel.y = new ParticleSystem.MinMaxCurve(1.4f);
        flameVel.z = new ParticleSystem.MinMaxCurve(0f);

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

        // Visible swirling cyan wind vortex ribbons via ParticleSystem + Noise + Trails
        GameObject psGo = new GameObject("UpdraftWindParticles");
        psGo.transform.SetParent(go.transform, false);
        psGo.transform.localPosition = new Vector3(0f, -height * 0.48f, 0f);
        psGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(height / 7.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(7.0f, 9.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.20f, 0.42f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.38f, 0.96f, 1f, 0.88f),
            new Color(0.78f, 0.98f, 1f, 0.70f)
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 95;

        var emission = ps.emission;
        emission.rateOverTime = 34f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width * 0.8f, 0.25f, 0.25f);

        ConfigureParticleNoise(ps, 0.85f, 0.85f, 1.1f);
        ConfigureParticleTrails(ps, particleMat, 0.55f, 0.20f, 0.55f);

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMat;
        renderer.sortingOrder = 1;
        ConfigureParticleFade(ps, 0.88f, true);
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
        sr.sprite = a.goalBeaconFrames[0];
        sr.sortingOrder = 4;

        Animator anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = a.goalBeaconAnimator;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.offset = new Vector2(0f, 1.5f);
        col.size = new Vector2(2.0f, 3.2f);

        LevelGoal goal = go.AddComponent<LevelGoal>();
        goal.idleBeaconParticles = CreateLoopingTrailParticles(
            "GoalBeaconIdleParticles", go.transform, new Vector3(0.18f, 2.0f, 0f),
            a.particleMaterial, new Color(1f, 0.92f, 0.45f, 0.92f), new Color(0.35f, 0.96f, 1f, 0.92f), 11,
            enableRibbons: true, noiseStrength: 0.7f
        );
        var beaconVel = goal.idleBeaconParticles.velocityOverLifetime;
        beaconVel.enabled = true;
        beaconVel.x = new ParticleSystem.MinMaxCurve(0f);
        beaconVel.y = new ParticleSystem.MinMaxCurve(2.2f);
        beaconVel.z = new ParticleSystem.MinMaxCurve(0f);
        var beaconMain = goal.idleBeaconParticles.main;
        beaconMain.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);

        goal.victoryBurstParticles = CreateOneShotBurstParticles(
            "GoalVictoryBurstParticles", go.transform, new Vector3(0.18f, 2.0f, 0f),
            a.particleMaterial, new Color(1f, 0.95f, 0.55f, 1f), 55, 0.22f, 0.48f, 8.2f, 180f, 15,
            enableTrails: true
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
        int sortingOrder,
        bool enableTrails = false)
    {
        GameObject psGo = new GameObject(name);
        psGo.transform.SetParent(parent, false);
        psGo.transform.localPosition = localPos;
        psGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.65f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.58f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = color;
        main.gravityModifier = 0.65f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = Mathf.Clamp(angleSpread, 5f, 180f);
        shape.radius = 0.25f;

        if (enableTrails)
        {
            ConfigureParticleTrails(ps, mat, 0.55f, 0.16f, 0.5f);
        }

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.sortingOrder = sortingOrder;
        ConfigureParticleFade(ps, 1.0f, true);

        return ps;
    }

    private static ParticleSystem CreateLoopingTrailParticles(
        string name,
        Transform parent,
        Vector3 localPos,
        Material mat,
        Color colorA,
        Color colorB,
        int sortingOrder,
        bool enableRibbons = false,
        float noiseStrength = 0f)
    {
        GameObject psGo = new GameObject(name);
        psGo.transform.SetParent(parent, false);
        psGo.transform.localPosition = localPos;

        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.38f, 0.68f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.34f);
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 28f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.35f;

        if (noiseStrength > 0.01f)
        {
            ConfigureParticleNoise(ps, noiseStrength, 0.9f, 0.6f);
        }
        if (enableRibbons)
        {
            ConfigureParticleTrails(ps, mat, 0.65f, 0.26f, 0.65f);
        }

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.sortingOrder = sortingOrder;
        ConfigureParticleFade(ps, 0.92f, true);

        return ps;
    }

    private static void ConfigureParticleNoise(ParticleSystem ps, float strength, float frequency, float scrollSpeed)
    {
        var noise = ps.noise;
        noise.enabled = true;
        noise.separateAxes = false;
        noise.strength = new ParticleSystem.MinMaxCurve(strength);
        noise.frequency = frequency;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(scrollSpeed);
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
    }

    private static void ConfigureParticleTrails(ParticleSystem ps, Material trailMat, float ratio, float lifetime, float widthScale)
    {
        var trails = ps.trails;
        trails.enabled = true;
        trails.ratio = ratio;
        trails.lifetime = new ParticleSystem.MinMaxCurve(lifetime);
        trails.minVertexDistance = 0.12f;
        trails.inheritParticleColor = true;
        trails.dieWithParticles = true;
        AnimationCurve widthCurve = new AnimationCurve(
            new Keyframe(0f, widthScale),
            new Keyframe(1f, 0f)
        );
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, widthCurve);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null && trailMat != null)
        {
            renderer.trailMaterial = trailMat;
        }
    }

    private static void ConfigureParticleFade(ParticleSystem ps, float peakAlpha, bool shrinkToZero)
    {
        var colLife = ps.colorOverLifetime;
        colLife.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(peakAlpha, 0.14f),
                new GradientAlphaKey(peakAlpha * 0.8f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colLife.color = new ParticleSystem.MinMaxGradient(g);

        if (shrinkToZero)
        {
            var sizeLife = ps.sizeOverLifetime;
            sizeLife.enabled = true;
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, 0.7f),
                new Keyframe(0.25f, 1.0f),
                new Keyframe(1f, 0f)
            );
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }
    }

    private static void BuildUiCanvas(BuiltAssets a, GameManager gm, Camera cam)
    {
        Font uiFont = ResolveUiFont();

        GameObject canvasGo = new GameObject("GameCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1.0f;
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

        // 1. TITLE SCREEN PANEL (semi-transparent twilight scrim so live rooftop sky & Vesper remain visible behind UI)
        GameObject titlePanel = CreateFullScreenPanel("TitlePanel", canvasGo.transform, a.uiWhiteSprite, new Color(0.05f, 0.06f, 0.15f, 0.72f));
        uiMgr.titlePanel = titlePanel;

        CreateText("TitleLogo", titlePanel.transform, uiFont, "VESPER: TWILIGHT COURIER", 52, FontStyle.Bold,
            new Color(0.42f, 0.94f, 1f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 195f), new Vector2(960f, 80f));

        CreateText("TitleSubtitle", titlePanel.transform, uiFont, "LEVEL 1  -  NOCTIS REACH ROOFTOPS", 24, FontStyle.Bold,
            new Color(1f, 0.86f, 0.42f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 138f), new Vector2(820f, 44f));

        CreateText("TitlePitch", titlePanel.transform, uiFont,
            "As Vesper, a shadow-stitcher courier of the twilight district, cross the skylines\n" +
            "to ignite the Sanctum Beacon and deliver the sealed Memory Shards.\n\n" +
            "Move: A/D or Arrows   |   Jump: SPACE / W   |   Shadow-Cloak Glide: Hold SPACE or SHIFT\n" +
            "Stomp Clockwork Sentinels from above   |   Pause: ESC / P",
            20, FontStyle.Normal, new Color(0.90f, 0.93f, 1f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 38f), new Vector2(980f, 150f));

        Button startBtn = CreateButton("StartRunButton", titlePanel.transform, a.uiWhiteSprite, uiFont,
            "START RUN", new Vector2(0f, -95f), new Vector2(280f, 62f),
            new Color(0.16f, 0.62f, 0.82f, 1f), Color.white);
        uiMgr.startRunButton = startBtn;
        UnityEventTools.AddPersistentListener(startBtn.onClick, gm.StartGame);

        Button titleQuitBtn = CreateButton("TitleQuitButton", titlePanel.transform, a.uiWhiteSprite, uiFont,
            "QUIT", new Vector2(0f, -175f), new Vector2(220f, 50f),
            new Color(0.24f, 0.22f, 0.36f, 1f), new Color(0.9f, 0.9f, 1f, 1f));
        uiMgr.titleQuitButton = titleQuitBtn;
        UnityEventTools.AddPersistentListener(titleQuitBtn.onClick, gm.QuitGame);

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
            hImg.raycastTarget = false;

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
            new Vector2(1f, 1f), new Vector2(-365f, -38f), new Vector2(150f, 22f),
            out _);

        Button hudMuteBtn = CreateButton("HudMuteButton", hudPanel.transform, a.uiWhiteSprite, uiFont,
            "MUTE AUDIO", new Vector2(0f, 0f), new Vector2(130f, 36f),
            new Color(0.18f, 0.22f, 0.38f, 0.92f), Color.white);
        RectTransform hudMuteRt = hudMuteBtn.GetComponent<RectTransform>();
        hudMuteRt.anchorMin = new Vector2(1f, 1f);
        hudMuteRt.anchorMax = new Vector2(1f, 1f);
        hudMuteRt.anchoredPosition = new Vector2(-195f, -38f);
        uiMgr.hudMuteButton = hudMuteBtn;
        uiMgr.hudMuteButtonText = hudMuteBtn.GetComponentInChildren<Text>();
        uiMgr.hudMuteButtonText.fontSize = 15;
        UnityEventTools.AddPersistentListener(hudMuteBtn.onClick, uiMgr.OnToggleMuteClicked);

        Button pauseBtn = CreateButton("HudPauseButton", hudPanel.transform, a.uiWhiteSprite, uiFont,
            "PAUSE", new Vector2(0f, 0f), new Vector2(96f, 36f),
            new Color(0.28f, 0.22f, 0.45f, 0.92f), Color.white);
        RectTransform pauseBtnRt = pauseBtn.GetComponent<RectTransform>();
        pauseBtnRt.anchorMin = new Vector2(1f, 1f);
        pauseBtnRt.anchorMax = new Vector2(1f, 1f);
        pauseBtnRt.anchoredPosition = new Vector2(-72f, -38f);
        pauseBtn.GetComponentInChildren<Text>().fontSize = 16;
        uiMgr.hudPauseButton = pauseBtn;
        UnityEventTools.AddPersistentListener(pauseBtn.onClick, gm.TogglePause);

        // Glide badge at bottom-left
        GameObject badgeGo = new GameObject("GlideStatusBadge");
        badgeGo.transform.SetParent(hudPanel.transform, false);
        Image badgeImg = badgeGo.AddComponent<Image>();
        badgeImg.sprite = a.uiWhiteSprite;
        badgeImg.color = new Color(0.12f, 0.14f, 0.28f, 0.85f);
        badgeImg.raycastTarget = false;
        RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0f, 0f);
        badgeRt.anchorMax = new Vector2(0f, 0f);
        badgeRt.anchoredPosition = new Vector2(132f, 38f);
        badgeRt.sizeDelta = new Vector2(232f, 38f);
        uiMgr.glideBadgeBackground = badgeImg;

        uiMgr.glideBadgeText = CreateText("GlideBadgeText", badgeGo.transform, uiFont, "CLOAK: READY", 17, FontStyle.Bold,
            Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 34f));

        // Center-bottom Tutorial / Checkpoint Banner (positioned with clean margin right of GlideStatusBadge)
        GameObject bannerGo = new GameObject("BannerContainer");
        bannerGo.transform.SetParent(hudPanel.transform, false);
        Image bannerImg = bannerGo.AddComponent<Image>();
        bannerImg.sprite = a.uiWhiteSprite;
        bannerImg.color = new Color(0.06f, 0.08f, 0.18f, 0.86f);
        bannerImg.raycastTarget = false;
        RectTransform bannerRt = bannerGo.GetComponent<RectTransform>();
        bannerRt.anchorMin = new Vector2(0.5f, 0f);
        bannerRt.anchorMax = new Vector2(0.5f, 0f);
        bannerRt.anchoredPosition = new Vector2(45f, 42f);
        bannerRt.sizeDelta = new Vector2(700f, 52f);
        uiMgr.bannerContainer = bannerGo;
        uiMgr.bannerText = CreateText("BannerText", bannerGo.transform, uiFont, "", 18, FontStyle.Bold,
            new Color(0.95f, 0.96f, 1f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(676f, 46f));
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
        uiMgr.pauseMuteButton = pauseMuteBtn;
        uiMgr.pauseMuteButtonText = pauseMuteBtn.GetComponentInChildren<Text>();
        UnityEventTools.AddPersistentListener(pauseMuteBtn.onClick, uiMgr.OnToggleMuteClicked);

        Button resumeBtn = CreateButton("ResumeButton", pausePanel.transform, a.uiWhiteSprite, uiFont,
            "RESUME", new Vector2(0f, -45f), new Vector2(250f, 52f),
            new Color(0.16f, 0.62f, 0.82f, 1f), Color.white);
        uiMgr.resumeButton = resumeBtn;
        UnityEventTools.AddPersistentListener(resumeBtn.onClick, gm.ResumeGame);

        Button pauseRestartBtn = CreateButton("PauseRestartButton", pausePanel.transform, a.uiWhiteSprite, uiFont,
            "RESTART LEVEL", new Vector2(0f, -110f), new Vector2(250f, 48f),
            new Color(0.28f, 0.25f, 0.48f, 1f), Color.white);
        uiMgr.pauseRestartButton = pauseRestartBtn;
        UnityEventTools.AddPersistentListener(pauseRestartBtn.onClick, gm.RestartLevel);

        Button pauseTitleBtn = CreateButton("PauseTitleButton", pausePanel.transform, a.uiWhiteSprite, uiFont,
            "MAIN MENU", new Vector2(0f, -172f), new Vector2(250f, 46f),
            new Color(0.22f, 0.22f, 0.34f, 1f), Color.white);
        uiMgr.pauseTitleButton = pauseTitleBtn;
        UnityEventTools.AddPersistentListener(pauseTitleBtn.onClick, gm.ReturnToTitle);

        Button pauseQuitBtn = CreateButton("PauseQuitButton", pausePanel.transform, a.uiWhiteSprite, uiFont,
            "QUIT GAME", new Vector2(0f, -230f), new Vector2(250f, 44f),
            new Color(0.45f, 0.16f, 0.24f, 1f), Color.white);
        uiMgr.pauseQuitButton = pauseQuitBtn;
        UnityEventTools.AddPersistentListener(pauseQuitBtn.onClick, gm.QuitGame);

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
        uiMgr.winPlayAgainButton = winPlayAgainBtn;
        UnityEventTools.AddPersistentListener(winPlayAgainBtn.onClick, gm.RestartLevel);

        Button winMenuBtn = CreateButton("WinMenuButton", winPanel.transform, a.uiWhiteSprite, uiFont,
            "MAIN MENU", new Vector2(0f, -165f), new Vector2(260f, 48f),
            new Color(0.24f, 0.24f, 0.38f, 1f), Color.white);
        uiMgr.winMenuButton = winMenuBtn;
        UnityEventTools.AddPersistentListener(winMenuBtn.onClick, gm.ReturnToTitle);

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
        uiMgr.retryCheckpointButton = retryCpBtn;
        UnityEventTools.AddPersistentListener(retryCpBtn.onClick, gm.RetryFromCheckpoint);

        Button goRestartBtn = CreateButton("GameOverRestartButton", gameOverPanel.transform, a.uiWhiteSprite, uiFont,
            "RESTART LEVEL", new Vector2(0f, -95f), new Vector2(260f, 48f),
            new Color(0.28f, 0.24f, 0.42f, 1f), Color.white);
        uiMgr.gameOverRestartButton = goRestartBtn;
        UnityEventTools.AddPersistentListener(goRestartBtn.onClick, gm.RestartLevel);

        Button goTitleBtn = CreateButton("GameOverTitleButton", gameOverPanel.transform, a.uiWhiteSprite, uiFont,
            "MAIN MENU", new Vector2(0f, -158f), new Vector2(260f, 46f),
            new Color(0.22f, 0.22f, 0.32f, 1f), Color.white);
        uiMgr.gameOverTitleButton = goTitleBtn;
        UnityEventTools.AddPersistentListener(goTitleBtn.onClick, gm.ReturnToTitle);

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
        txt.raycastTarget = false;

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

    private static void RenderVerificationScreenshots(BuiltAssets assets)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[LevelBuilder] No Main Camera found for screenshot capture.");
            return;
        }

        UIManager uiMgr = UnityEngine.Object.FindFirstObjectByType<UIManager>();
        PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        SpriteRenderer playerSr = player != null ? player.GetComponent<SpriteRenderer>() : null;
        Vector3 origPlayerPos = player != null ? player.transform.position : Vector3.zero;
        Sprite origPlayerSprite = playerSr != null ? playerSr.sprite : null;

        ParallaxLayer[] parallaxLayers = UnityEngine.Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None);
        Vector3[] origLayerPos = new Vector3[parallaxLayers.Length];
        for (int i = 0; i < parallaxLayers.Length; i++)
        {
            origLayerPos[i] = parallaxLayers[i].transform.position;
        }

        Vector3 baseCamPos = cam.transform.position;

        // 1. Capture Title Screen over live Zone 1 backdrop + Twilight Canal
        if (uiMgr != null)
        {
            uiMgr.ShowOnlyPanel(GameState.Title);
        }
        CaptureCameraView(cam, new Vector3(8f, 1.4f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/title_screen.png");

        // 2. Capture Zone 1 Gameplay + HUD + Grounded Rooftops & Twilight Canal Water
        if (uiMgr != null)
        {
            uiMgr.ShowOnlyPanel(GameState.Playing);
            if (uiMgr.bannerContainer != null && uiMgr.bannerText != null)
            {
                uiMgr.bannerText.text = "COURTYARD OF WHISPERS: Move with A/D or Arrows, Jump with SPACE or W!";
                uiMgr.bannerContainer.SetActive(true);
            }
            uiMgr.UpdateShards(2, 25);
        }
        CaptureCameraView(cam, new Vector3(9.5f, 1.2f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/gameplay_preview.png");

        // 3. Capture Signature Shadow-Cloak Glide + Ribbon Trail Particles + Disjoint Safety Floor & Canal Water
        if (player != null && playerSr != null && assets != null)
        {
            player.transform.position = new Vector3(34.5f, 4.1f, 0f);
            playerSr.sprite = assets.vesperGlideFrames != null && assets.vesperGlideFrames.Length > 1 ? assets.vesperGlideFrames[1] : assets.vesperGlide;
            if (player.glideTrailParticles != null)
            {
                player.glideTrailParticles.Play();
                player.glideTrailParticles.Simulate(0.55f, true, true);
            }
            if (uiMgr != null)
            {
                uiMgr.UpdateGlideStatus(true, true);
                if (uiMgr.bannerText != null)
                {
                    uiMgr.bannerText.text = "SIGNATURE MOVE: Hold SPACE or SHIFT in mid-air to open your Shadow-Cloak Glide!";
                }
            }
        }
        CaptureCameraView(cam, new Vector3(35.5f, 1.8f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/glide_and_particles_preview.png");

        // Restore player position/sprite cleanly
        if (player != null && playerSr != null)
        {
            player.transform.position = origPlayerPos;
            playerSr.sprite = origPlayerSprite;
            if (player.glideTrailParticles != null)
            {
                player.glideTrailParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        // 4. Capture Zone 2: Gothic Pillars, Pop-Up Ground Spikes erupting with Spark Trails, and Moving Platform Shuttle
        if (uiMgr != null && uiMgr.bannerContainer != null)
        {
            uiMgr.UpdateGlideStatus(false, false);
            uiMgr.UpdateShards(10, 25);
            uiMgr.bannerText.text = "AMBUSH TRAPS: Watch the stone grates - concealed spikes spark and erupt when crossed!";
        }
        PopUpSpikes[] allTraps = UnityEngine.Object.FindObjectsByType<PopUpSpikes>(FindObjectsSortMode.None);
        PopUpSpikes zone2Trap = null;
        PopUpSpikes zone4Trap = null;
        foreach (PopUpSpikes trap in allTraps)
        {
            if (trap.gameObject.name == "Spikes_2_Ambush") zone2Trap = trap;
            if (trap.gameObject.name == "Spikes_4_FinalAmbush") zone4Trap = trap;
        }
        if (zone2Trap != null)
        {
            zone2Trap.SetPreviewExtension(1.0f);
            if (zone2Trap.sparkParticles != null)
            {
                zone2Trap.sparkParticles.Play();
                zone2Trap.sparkParticles.Simulate(0.18f, true, true);
            }
        }
        CaptureCameraView(cam, new Vector3(71.5f, 2.2f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/zone2_traps_and_moving_preview.png");

        if (zone2Trap != null)
        {
            zone2Trap.SetPreviewExtension(0.0f);
            if (zone2Trap.sparkParticles != null)
            {
                zone2Trap.sparkParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        // 5. Capture Zone 3A: Crumbling Stone Span, Foundry Sentinel & Canal
        if (uiMgr != null)
        {
            uiMgr.UpdateShards(16, 25);
            if (uiMgr.bannerContainer != null) uiMgr.bannerContainer.SetActive(false);
        }
        CaptureCameraView(cam, new Vector3(132.5f, 3.0f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/zone3_crumble_span_preview.png");

        // 6. Capture Zone 3B: Dual Non-Colliding Moving Platforms + Thrusters + Foundry Sentinel
        if (uiMgr != null)
        {
            uiMgr.UpdateShards(18, 25);
        }
        CaptureCameraView(cam, new Vector3(152.0f, 2.4f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/zone3_dynamic_platforms_preview.png");

        // 7. Capture Zone 4: Sanctum Beacon Goal + Animated Light Pillar + Final Sentinel & Concealed Floor Grate
        if (uiMgr != null)
        {
            uiMgr.UpdateShards(25, 25);
        }
        if (zone4Trap != null)
        {
            // Keep final trap retracted in stone grate to verify clean embedded concealment
            zone4Trap.SetPreviewExtension(0.0f);
        }
        CaptureCameraView(cam, new Vector3(217.5f, 10.2f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/level_goal_preview.png");

        // Restore Camera, Parallax, and Initial Title UI state cleanly
        cam.transform.position = baseCamPos;
        for (int i = 0; i < parallaxLayers.Length; i++)
        {
            parallaxLayers[i].transform.position = origLayerPos[i];
        }
        if (uiMgr != null)
        {
            uiMgr.UpdateShards(0, 25);
            uiMgr.ShowOnlyPanel(GameState.Title);
            if (uiMgr.bannerContainer != null) uiMgr.bannerContainer.SetActive(false);
        }
    }

    private static void CaptureCameraView(
        Camera cam,
        Vector3 camPos,
        Vector3 baseCamPos,
        ParallaxLayer[] parallaxLayers,
        Vector3[] baseLayerPos,
        string outputPath)
    {
        cam.transform.position = camPos;

        Vector3 camDelta = camPos - baseCamPos;
        for (int i = 0; i < parallaxLayers.Length; i++)
        {
            ParallaxLayer p = parallaxLayers[i];
            parallaxLayers[i].transform.position = new Vector3(
                baseLayerPos[i].x + camDelta.x * p.parallaxFactorX,
                baseLayerPos[i].y + camDelta.y * p.parallaxFactorY,
                baseLayerPos[i].z
            );
        }

        ParticleSystem[] systems = UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (ParticleSystem ps in systems)
        {
            if (ps.main.playOnAwake)
            {
                ps.Simulate(1.2f, true, true);
            }
        }

        int width = 1280;
        int height = 720;

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        // Warm-up render pass ensures URP 2D shaders, canvas sorting, and parallax layers settle before readback
        cam.Render();
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
