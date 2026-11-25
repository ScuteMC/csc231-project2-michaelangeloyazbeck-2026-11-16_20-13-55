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
    private const string UiFolder = "Assets/Art/UI";
    private const string AudioFolder = "Assets/Audio";
    private const string ArtFolder = "Assets/Art";
    private const string SettingsFolder = "Assets/Settings";

    private class BuiltAssets
    {
        public Sprite vesperIdle, vesperRun1, vesperRun2, vesperJump, vesperFall, vesperGlide, vesperHurt;
        public Sprite[] vesperIdleFrames, vesperRunFrames, vesperJumpFrames, vesperFallFrames, vesperGlideFrames, vesperHurtFrames;
        public Sprite[] sentinelFrames, shardFrames, checkpointFrames, goalBeaconFrames, waterFrames;
        public Sprite[] bossWanderFrames, bossAttackFrames, bossDashFrames, bossCastFrames, bossStunnedFrames;
        public Sprite[] gargoyleFrames, casterFrames;
        public Sprite propEclipseSeal, vfxBossShockwave, vfxBossSkySpear, vfxPlayerLightningBolt, vfxEclipseOrb;
        public Sprite propIronChain, hazardPendulumBlade, propSoulLifeRelic;
        public Sprite tileRooftop, tileRooftopBody, hazardSpikes, enemySentinel, collectibleShard, propCheckpoint, propGoalBeacon;
        public Sprite tileSpikeSlot, propGothicPillar, tileMovingPlatform, tileCrumblePlatform;
        public Sprite propUpdraftVent, propSteamChimney, propChimneyCap, propBrokenArch, propTrackPylon, propGothicRailing, propDistrictArch, propStreetLamp;
        public Sprite bgFarSky, bgMidCity, bgNearSilhouettes;
        public Sprite uiWhiteSprite, uiHeartSprite, softParticleSprite;
        public Sprite uiBtnPrimary, uiBtnSecondary, uiBtnDanger, uiPanelOrnate, uiHudBar, uiKeycap, uiTitleCrest, uiDivider;
        public Sprite uiPortraitVesper, uiHpBarFrame, uiHpBarFill, uiLifeSoulIcon, uiWardDiamond, uiLightningIcon;
        public Sprite[] storyCardSprites;
        public Material particleMaterial;
        public PhysicsMaterial2D frictionlessMaterial;
        public RuntimeAnimatorController vesperAnimator, bossAnimator, gargoyleAnimator, casterAnimator;
        public RuntimeAnimatorController sentinelAnimator, shardAnimator, checkpointAnimator, goalBeaconAnimator, waterAnimator;
        public AudioClip bgmClip, jumpClip, landClip, collectClip, hurtClip, checkpointClip, glideClip, winClip, zapClip, bossRoarClip;
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
            "Assets/Art/UI",
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
        ConfigureSingleSprite($"{SpriteFolder}/Prop_UpdraftVent.png", 128f, new Vector2(0.5f, 0.72f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_SteamChimney.png", 128f, new Vector2(0.5f, 0.5f), TextureWrapMode.Repeat, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_ChimneyCap.png", 128f, new Vector2(0.5f, 0.0f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_BrokenArch.png", 128f, new Vector2(0.0f, 1.0f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_TrackPylon.png", 128f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_GothicRailing.png", 128f, new Vector2(0.5f, 0.0f), TextureWrapMode.Repeat, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_DistrictArch.png", 80f, new Vector2(0.5f, 0.0f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_StreetLamp.png", 96f, new Vector2(0.5f, 0.0f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Hazard_Spikes.png", 128f, new Vector2(0.5f, 0.0f), TextureWrapMode.Repeat, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Enemy_Sentinel.png", 160f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSingleSprite($"{SpriteFolder}/Collectible_Shard.png", 180f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_Checkpoint.png", 110f, new Vector2(0.5f, 0.0f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_GoalBeacon.png", 95f, new Vector2(0.5f, 0.0f), TextureWrapMode.Clamp, SpriteMeshType.Tight);

        ConfigureSpriteSequence("Vesper_Idle", 6, 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Vesper_Run", 8, 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Vesper_Jump", 4, 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Vesper_Fall", 4, 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Vesper_Glide", 4, 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Vesper_Hurt", 4, 128f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Enemy_Sentinel", 6, 160f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Collectible_Shard", 6, 180f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Prop_Checkpoint", 4, 110f, new Vector2(0.5f, 0.0f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Prop_GoalBeacon", 6, 95f, new Vector2(0.5f, 0.0f), TextureWrapMode.Clamp, SpriteMeshType.Tight);
        ConfigureSpriteSequence("Water", 6, 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Repeat, SpriteMeshType.FullRect);

        ConfigureSpriteSequence("Boss_Warden_Wander", 4, 85f, new Vector2(0.5f, 0.01f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSpriteSequence("Boss_Warden_Attack", 4, 85f, new Vector2(0.5f, 0.01f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSpriteSequence("Boss_Warden_Dash", 4, 85f, new Vector2(0.5f, 0.01f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSpriteSequence("Boss_Warden_Cast", 4, 85f, new Vector2(0.5f, 0.01f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSpriteSequence("Boss_Warden_Stunned", 4, 85f, new Vector2(0.5f, 0.01f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSpriteSequence("Enemy_Gargoyle", 4, 96f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSpriteSequence("Enemy_Caster", 4, 96f, new Vector2(0.5f, 0.02f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_EclipseSeal.png", 95f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/VFX_BossShockwave.png", 95f, new Vector2(0.5f, 0.04f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/VFX_BossSkySpear.png", 48f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/VFX_PlayerLightningBolt.png", 96f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/VFX_EclipseOrb.png", 96f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_IronChain.png", 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Repeat, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Hazard_PendulumBlade.png", 85f, new Vector2(0.5f, 0.65f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/Prop_SoulLifeRelic.png", 110f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.Tight);

        ConfigureSlicedUiSprite($"{UiFolder}/UI_Btn_Primary.png", 100f, new Vector4(24f, 24f, 24f, 24f));
        ConfigureSlicedUiSprite($"{UiFolder}/UI_Btn_Secondary.png", 100f, new Vector4(24f, 24f, 24f, 24f));
        ConfigureSlicedUiSprite($"{UiFolder}/UI_Btn_Danger.png", 100f, new Vector4(24f, 24f, 24f, 24f));
        ConfigureSlicedUiSprite($"{UiFolder}/UI_Panel_Ornate.png", 100f, new Vector4(32f, 32f, 32f, 32f));
        ConfigureSlicedUiSprite($"{UiFolder}/UI_Hud_Bar.png", 100f, new Vector4(18f, 18f, 18f, 18f));
        ConfigureSlicedUiSprite($"{UiFolder}/UI_Keycap.png", 100f, new Vector4(12f, 12f, 12f, 12f));
        ConfigureSingleSprite($"{UiFolder}/UI_TitleCrest.png", 100f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{UiFolder}/UI_Divider.png", 100f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{UiFolder}/UI_Portrait_Vesper.png", 100f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{UiFolder}/UI_HpBar_Frame.png", 100f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{UiFolder}/UI_HpBar_Fill.png", 100f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{UiFolder}/UI_LifeSoulIcon.png", 100f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{UiFolder}/UI_WardDiamond.png", 100f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{UiFolder}/UI_LightningIcon.png", 100f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{UiFolder}/UI_StoryCard_1.png", 100f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{UiFolder}/UI_StoryCard_2.png", 100f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{UiFolder}/UI_StoryCard_3.png", 100f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);

        ConfigureSingleSprite($"{SpriteFolder}/BG_FarSky.png", 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/BG_MidCity.png", 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{SpriteFolder}/BG_NearSilhouettes.png", 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);

        ConfigureSingleSprite($"{ArtFolder}/VFX_SoftParticle.png", 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{ArtFolder}/UI_SolidWhite.png", 16f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        ConfigureSingleSprite($"{ArtFolder}/UI_Heart.png", 64f, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
    }

    private static void ConfigureSlicedUiSprite(string assetPath, float ppu, Vector4 border)
    {
        ConfigureSingleSprite(assetPath, ppu, new Vector2(0.5f, 0.5f), TextureWrapMode.Clamp, SpriteMeshType.FullRect);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }
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
        a.propUpdraftVent = LoadSprite($"{SpriteFolder}/Prop_UpdraftVent.png");
        a.propSteamChimney = LoadSprite($"{SpriteFolder}/Prop_SteamChimney.png");
        a.propChimneyCap = LoadSprite($"{SpriteFolder}/Prop_ChimneyCap.png");
        a.propBrokenArch = LoadSprite($"{SpriteFolder}/Prop_BrokenArch.png");
        a.propTrackPylon = LoadSprite($"{SpriteFolder}/Prop_TrackPylon.png");
        a.propGothicRailing = LoadSprite($"{SpriteFolder}/Prop_GothicRailing.png");
        a.propDistrictArch = LoadSprite($"{SpriteFolder}/Prop_DistrictArch.png");
        a.propStreetLamp = LoadSprite($"{SpriteFolder}/Prop_StreetLamp.png");
        a.hazardSpikes = LoadSprite($"{SpriteFolder}/Hazard_Spikes.png");
        a.enemySentinel = LoadSprite($"{SpriteFolder}/Enemy_Sentinel.png");
        a.collectibleShard = LoadSprite($"{SpriteFolder}/Collectible_Shard.png");
        a.propCheckpoint = LoadSprite($"{SpriteFolder}/Prop_Checkpoint.png");
        a.propGoalBeacon = LoadSprite($"{SpriteFolder}/Prop_GoalBeacon.png");

        a.vesperIdleFrames = LoadSpriteSequence("Vesper_Idle", 6);
        a.vesperRunFrames = LoadSpriteSequence("Vesper_Run", 8);
        a.vesperJumpFrames = LoadSpriteSequence("Vesper_Jump", 4);
        a.vesperFallFrames = LoadSpriteSequence("Vesper_Fall", 4);
        a.vesperGlideFrames = LoadSpriteSequence("Vesper_Glide", 4);
        a.vesperHurtFrames = LoadSpriteSequence("Vesper_Hurt", 4);
        a.sentinelFrames = LoadSpriteSequence("Enemy_Sentinel", 6);
        a.shardFrames = LoadSpriteSequence("Collectible_Shard", 6);
        a.checkpointFrames = LoadSpriteSequence("Prop_Checkpoint", 4);
        a.goalBeaconFrames = LoadSpriteSequence("Prop_GoalBeacon", 6);
        a.waterFrames = LoadSpriteSequence("Water", 6);
        a.bossWanderFrames = LoadSpriteSequence("Boss_Warden_Wander", 4);
        a.bossAttackFrames = LoadSpriteSequence("Boss_Warden_Attack", 4);
        a.bossDashFrames = LoadSpriteSequence("Boss_Warden_Dash", 4);
        a.bossCastFrames = LoadSpriteSequence("Boss_Warden_Cast", 4);
        a.bossStunnedFrames = LoadSpriteSequence("Boss_Warden_Stunned", 4);
        a.gargoyleFrames = LoadSpriteSequence("Enemy_Gargoyle", 4);
        a.casterFrames = LoadSpriteSequence("Enemy_Caster", 4);
        a.propEclipseSeal = LoadSprite($"{SpriteFolder}/Prop_EclipseSeal.png");
        a.vfxBossShockwave = LoadSprite($"{SpriteFolder}/VFX_BossShockwave.png");
        a.vfxBossSkySpear = LoadSprite($"{SpriteFolder}/VFX_BossSkySpear.png");
        a.vfxPlayerLightningBolt = LoadSprite($"{SpriteFolder}/VFX_PlayerLightningBolt.png");
        a.vfxEclipseOrb = LoadSprite($"{SpriteFolder}/VFX_EclipseOrb.png");
        a.propIronChain = LoadSprite($"{SpriteFolder}/Prop_IronChain.png");
        a.hazardPendulumBlade = LoadSprite($"{SpriteFolder}/Hazard_PendulumBlade.png");
        a.propSoulLifeRelic = LoadSprite($"{SpriteFolder}/Prop_SoulLifeRelic.png");

        a.bgFarSky = LoadSprite($"{SpriteFolder}/BG_FarSky.png");
        a.bgMidCity = LoadSprite($"{SpriteFolder}/BG_MidCity.png");
        a.bgNearSilhouettes = LoadSprite($"{SpriteFolder}/BG_NearSilhouettes.png");

        a.softParticleSprite = LoadSprite($"{ArtFolder}/VFX_SoftParticle.png");
        a.uiWhiteSprite = LoadSprite($"{ArtFolder}/UI_SolidWhite.png");
        a.uiHeartSprite = LoadSprite($"{ArtFolder}/UI_Heart.png");
        a.uiBtnPrimary = LoadSprite($"{UiFolder}/UI_Btn_Primary.png");
        a.uiBtnSecondary = LoadSprite($"{UiFolder}/UI_Btn_Secondary.png");
        a.uiBtnDanger = LoadSprite($"{UiFolder}/UI_Btn_Danger.png");
        a.uiPanelOrnate = LoadSprite($"{UiFolder}/UI_Panel_Ornate.png");
        a.uiHudBar = LoadSprite($"{UiFolder}/UI_Hud_Bar.png");
        a.uiKeycap = LoadSprite($"{UiFolder}/UI_Keycap.png");
        a.uiTitleCrest = LoadSprite($"{UiFolder}/UI_TitleCrest.png");
        a.uiDivider = LoadSprite($"{UiFolder}/UI_Divider.png");
        a.uiPortraitVesper = LoadSprite($"{UiFolder}/UI_Portrait_Vesper.png");
        a.uiHpBarFrame = LoadSprite($"{UiFolder}/UI_HpBar_Frame.png");
        a.uiHpBarFill = LoadSprite($"{UiFolder}/UI_HpBar_Fill.png");
        a.uiLifeSoulIcon = LoadSprite($"{UiFolder}/UI_LifeSoulIcon.png");
        a.uiWardDiamond = LoadSprite($"{UiFolder}/UI_WardDiamond.png");
        a.uiLightningIcon = LoadSprite($"{UiFolder}/UI_LightningIcon.png");
        a.storyCardSprites = new[]
        {
            LoadSprite($"{UiFolder}/UI_StoryCard_1.png"),
            LoadSprite($"{UiFolder}/UI_StoryCard_2.png"),
            LoadSprite($"{UiFolder}/UI_StoryCard_3.png")
        };

        a.bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/bgm_twilight_rooftops.wav");
        a.jumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_jump.wav");
        a.landClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_land.wav");
        a.collectClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_collect.wav");
        a.hurtClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_hurt.wav");
        a.checkpointClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_checkpoint.wav");
        a.glideClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_glide.wav");
        a.winClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_win.wav");
        a.zapClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_zap.wav");
        a.bossRoarClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/sfx_boss_roar.wav");

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
        a.bossAnimator = BuildBossAnimatorController(a);
        a.gargoyleAnimator = BuildLoopingPropAnimator("GargoyleAnimator", "Enemy_Gargoyle_Swoop", a.gargoyleFrames, 0.12f);
        a.casterAnimator = BuildLoopingPropAnimator("CasterAnimator", "Enemy_Caster_Idle", a.casterFrames, 0.16f);
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
        AnimationClip runClip = CreateSpriteClip("Vesper_Run", a.vesperRunFrames, 0.068f, true);
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

    private static RuntimeAnimatorController BuildBossAnimatorController(BuiltAssets a)
    {
        string path = $"{AnimationFolder}/BossWardenAnimator.controller";
        if (File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("State", AnimatorControllerParameterType.Int);

        AnimationClip wanderClip = CreateSpriteClip("Boss_Warden_Wander", a.bossWanderFrames, 0.16f, true);
        AnimationClip attackClip = CreateSpriteClip("Boss_Warden_Attack", a.bossAttackFrames, 0.20f, false);
        AnimationClip stunnedClip = CreateSpriteClip("Boss_Warden_Stunned", a.bossStunnedFrames, 0.14f, true);
        AnimationClip dashClip = CreateSpriteClip("Boss_Warden_Dash", a.bossDashFrames, 0.09f, true);
        AnimationClip castClip = CreateSpriteClip("Boss_Warden_Cast", a.bossCastFrames, 0.16f, false);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState wanderState = sm.AddState("Wandering");
        wanderState.motion = wanderClip;
        AnimatorState attackState = sm.AddState("Attacking");
        attackState.motion = attackClip;
        AnimatorState stunnedState = sm.AddState("Stunned");
        stunnedState.motion = stunnedClip;
        AnimatorState dashState = sm.AddState("Dashing");
        dashState.motion = dashClip;
        AnimatorState castState = sm.AddState("Casting");
        castState.motion = castClip;
        sm.defaultState = wanderState;

        AnimatorStateTransition toWander = sm.AddAnyStateTransition(wanderState);
        toWander.hasExitTime = false;
        toWander.duration = 0f;
        toWander.canTransitionToSelf = false;
        toWander.AddCondition(AnimatorConditionMode.Equals, 0, "State");

        AnimatorStateTransition toAttack = sm.AddAnyStateTransition(attackState);
        toAttack.hasExitTime = false;
        toAttack.duration = 0f;
        toAttack.canTransitionToSelf = false;
        toAttack.AddCondition(AnimatorConditionMode.Equals, 1, "State");

        AnimatorStateTransition toStunned = sm.AddAnyStateTransition(stunnedState);
        toStunned.hasExitTime = false;
        toStunned.duration = 0f;
        toStunned.canTransitionToSelf = false;
        toStunned.AddCondition(AnimatorConditionMode.Equals, 2, "State");

        AnimatorStateTransition toDash = sm.AddAnyStateTransition(dashState);
        toDash.hasExitTime = false;
        toDash.duration = 0f;
        toDash.canTransitionToSelf = false;
        toDash.AddCondition(AnimatorConditionMode.Equals, 3, "State");

        AnimatorStateTransition toCast = sm.AddAnyStateTransition(castState);
        toCast.hasExitTime = false;
        toCast.duration = 0f;
        toCast.canTransitionToSelf = false;
        toCast.AddCondition(AnimatorConditionMode.Equals, 4, "State");

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
        camController.maxX = 236f;

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
        audioMgr.zapClip = a.zapClip;
        audioMgr.bossRoarClip = a.bossRoarClip;

        GameObject gmGo = new GameObject("GameManager");
        GameManager gm = gmGo.AddComponent<GameManager>();
        gm.totalShards = 30;

        // 5. Player (Vesper)
        GameObject playerGo = CreatePlayer(a);
        gm.playerController = playerGo.GetComponent<PlayerController>();
        gm.playerHealth = playerGo.GetComponent<PlayerHealth>();
        camController.target = playerGo.transform;

        // 6. Level Geometry, Hazards, Enemies, Updrafts, Collectibles, Checkpoints, Goal
        BuildLevelGeometryAndProps(a);
        gm.levelGoalX = 239.5f;
        gm.finalBoss = UnityEngine.Object.FindFirstObjectByType<FinalBossAI>();

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
        for (int i = -1; i <= 14; i++)
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
        pc.lightningBoltSprite = a.vfxPlayerLightningBolt;
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
        ph.aetherHealParticles = CreateOneShotBurstParticles(
            "AetherHealParticles", playerGo.transform, new Vector3(0f, 0.9f, 0f),
            a.particleMaterial, new Color(0.42f, 0.98f, 1f, 0.98f), 22, 0.14f, 0.30f, 4.8f, 180f, 12,
            enableTrails: true
        );
        ph.aetherShieldParticles = CreateLoopingTrailParticles(
            "AetherShieldParticles", playerGo.transform, new Vector3(0f, 0.85f, 0f),
            a.particleMaterial, new Color(0.38f, 0.96f, 1f, 0.72f), new Color(1f, 0.90f, 0.45f, 0.68f), 11,
            enableRibbons: false, noiseStrength: 0.75f
        );
        var shieldMain = ph.aetherShieldParticles.main;
        shieldMain.playOnAwake = false;
        shieldMain.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.22f);
        var shieldShape = ph.aetherShieldParticles.shape;
        shieldShape.shapeType = ParticleSystemShapeType.Circle;
        shieldShape.radius = 0.72f;
        ph.aetherShieldParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        pc.aetherFlashParticles = CreateOneShotBurstParticles(
            "AetherFlashParticles", playerGo.transform, new Vector3(0f, 0.9f, 0f),
            a.particleMaterial, new Color(0.55f, 0.98f, 1f, 1f), 42, 0.22f, 0.52f, 11.5f, 180f, 14,
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
        // TWILIGHT CANAL WATER (x = -14 .. 260, surfaceY = -4.6)
        // Animated 6-frame moonlit canal water + mist + splash particles
        // ============================================================
        CreateWaterCanal(-14f, 260f, -4.6f, a, levelRoot.transform);

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

        // Environmental storytelling on Zone 1 Courtyard (district gatehouse, iron railings, streetlamps)
        CreateDistrictArch("Arch_1_CourtyardGate", new Vector2(0.4f, 0f), a, levelRoot.transform);
        CreateGothicRailing("Railing_1A", -1.4f, 12.8f, 0f, a, levelRoot.transform);
        CreateStreetLamp("Lamp_1A", new Vector2(5.5f, 0f), a, levelRoot.transform);
        CreateGothicRailing("Railing_1D", 42.6f, 48.2f, 1.2f, a, levelRoot.transform);
        CreateStreetLamp("Lamp_1D", new Vector2(49.0f, 1.2f), a, levelRoot.transform);

        CreateTutorialTrigger("Tut_1_Controls", new Vector2(3.5f, 1.5f), new Vector2(4f, 4f),
            string.Empty,
            "I · COURTYARDS", string.Empty,
            levelRoot.transform);
        CreateSentinel("Sentinel_0_Courtyard", new Vector2(18.5f, 2.25f), -2.2f, 2.2f, 2.6f, a, levelRoot.transform);
        CreateGargoyle("Gargoyle_1_Chasm", new Vector2(35.5f, 5.4f), 3.2f, 2.8f, a, levelRoot.transform);
        CreatePlagueCaster("Caster_1_Landing", new Vector2(49.5f, 1.2f), a, levelRoot.transform);
        CreateTutorialTrigger("Tut_2_Glide", new Vector2(28.5f, 4.5f), new Vector2(4f, 4f),
            "HOLD SHIFT TO GLIDE",
            string.Empty, string.Empty,
            levelRoot.transform);

        // Shards 1..7 + bonus Shard 26
        CreateShard(1, new Vector2(8f, 1.1f), a, levelRoot.transform);
        CreateShard(2, new Vector2(12f, 1.1f), a, levelRoot.transform);
        CreateShard(3, new Vector2(19.8f, 2.5f), a, levelRoot.transform);
        CreateShard(26, new Vector2(25.8f, 4.4f), a, levelRoot.transform);
        CreateShard(4, new Vector2(28.5f, 4.1f), a, levelRoot.transform);
        CreateShard(5, new Vector2(34.0f, 3.9f), a, levelRoot.transform);
        CreateShard(6, new Vector2(36.8f, 3.3f), a, levelRoot.transform);
        CreateShard(7, new Vector2(39.6f, 2.6f), a, levelRoot.transform);

        // ============================================================
        // ZONE 2: Gale-Draft Aqueduct (x = 45 .. 127)
        // Grounded Rooftop Thermal Vent & Canal Steam Chimney Updrafts,
        // Gothic Pillar Aqueducts, Pop-Up Floor Grates, Pendulum Scythe & Relics
        // ============================================================
        CreateCheckpoint(1, "Gale-Draft Overlook", new Vector2(45.5f, 1.2f), a, levelRoot.transform);
        CreateTutorialTrigger("Tut_3_Updraft", new Vector2(50.0f, 2.8f), new Vector2(4f, 4f),
            string.Empty,
            "II · AQUEDUCT", string.Empty,
            levelRoot.transform);

        CreateRooftopUpdraft("Updraft_1_Intro", new Vector2(53.5f, 1.2f), 3.4f, 8.2f, 9.8f, a, levelRoot.transform);

        // Elevated aqueduct bridge visibly supported by grounded Gothic stone pillars down into the canal
        CreateElevatedPlatform("Roof_2B_Upper", 58.0f, 74.0f, 5.4f, 2.0f, a, levelRoot.transform, footprints);
        CreateGothicPillar("Pillar_2B_Left", 60.5f, 3.45f, GroundBottomY, 1.6f, a, levelRoot.transform);
        CreateGothicPillar("Pillar_2B_Right", 71.5f, 3.45f, GroundBottomY, 1.6f, a, levelRoot.transform);
        CreateGothicRailing("Railing_2B_L", 58.6f, 61.5f, 5.4f, a, levelRoot.transform);
        CreateGothicRailing("Railing_2B_R", 68.6f, 73.0f, 5.4f, a, levelRoot.transform);
        CreateStreetLamp("Lamp_2B", new Vector2(60.2f, 5.4f), a, levelRoot.transform);
        CreatePendulumScythe("Pendulum_2_Aqueduct", new Vector2(63.2f, 10.6f), 3.65f, 44f, 2.4f, 0f, a, levelRoot.transform);
        CreateElevatedPlatform("Roof_2_RelicPerch", 70.6f, 74.0f, 9.0f, 2.0f, a, levelRoot.transform, footprints);
        CreateSoulLifeRelic("SoulRelic_1_Aqueduct", new Vector2(72.3f, 9.9f), a, levelRoot.transform);

        CreateTutorialTrigger("Tut_4_Spikes", new Vector2(61.0f, 7.0f), new Vector2(3.5f, 4f),
            string.Empty,
            string.Empty, string.Empty,
            levelRoot.transform);
        CreatePopUpSpikes("Spikes_2_Ambush", new Vector2(66.2f, 5.4f), 2.0f, PopUpSpikes.TrapMode.ProximityAmbush, 0f, a, levelRoot.transform);

        // Horizontal Moving Gothic Bridge spanning 74.0 .. 87.0 with >= 1.2u clearance on both ends & chained canal piers
        CreateMovingPlatform("MovingPlat_2A_Shuttle", new Vector3(77.0f, 3.72f, 0f), new Vector3(84.0f, 3.72f, 0f),
            3.6f, 0.95f, 2.4f, 0.45f, 0.0f, true, GroundBottomY, a, levelRoot.transform, footprints);
        CreateGargoyle("Gargoyle_2_Shuttle", new Vector2(80.5f, 7.6f), 3.4f, 3.1f, a, levelRoot.transform);

        CreateGroundedPlatform("Roof_2C_Mid", 87.0f, 99.0f, 3.2f, GroundBottomY, a, levelRoot.transform, footprints);
        CreateStreetLamp("Lamp_2C", new Vector2(89.2f, 3.2f), a, levelRoot.transform);
        CreateGothicRailing("Railing_2C", 95.0f, 98.0f, 3.2f, a, levelRoot.transform);
        CreatePopUpSpikes("Spikes_2C_Timed", new Vector2(92.6f, 3.2f), 2.2f, PopUpSpikes.TrapMode.TimedCycle, 0.5f, a, levelRoot.transform);
        CreatePlagueCaster("Caster_2_Mid", new Vector2(96.6f, 3.2f), a, levelRoot.transform);

        // Grounded Canal Steam Chimney rising from canal water to launch Vesper up to Roof_2D_High
        CreateCanalChimneyUpdraft("Updraft_2_Chasm", 102.2f, -1.8f, GroundBottomY, 3.8f, 11.6f, 10.6f, a, levelRoot.transform, footprints);
        CreateGroundedPlatform("Roof_2D_High", 105.5f, 127.0f, 6.0f, GroundBottomY, a, levelRoot.transform, footprints);
        CreatePopUpSpikes("Spikes_2D_Ambush", new Vector2(109.8f, 6.0f), 2.0f, PopUpSpikes.TrapMode.ProximityAmbush, 0f, a, levelRoot.transform);
        CreateStreetLamp("Lamp_2D", new Vector2(111.8f, 6.0f), a, levelRoot.transform);

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
        // Sentinels, Plague Casters, Gargoyles, Pendulum Scythe & Dual Gothic Lifts
        // ============================================================
        CreateCheckpoint(2, "Clockwork Foundry Watch", new Vector2(113.5f, 6.0f), a, levelRoot.transform);
        CreateDistrictArch("Arch_3_FoundryGate", new Vector2(115.6f, 6.0f), a, levelRoot.transform);
        CreateTutorialTrigger("Tut_5_Sentinels", new Vector2(116.5f, 7.5f), new Vector2(3.5f, 4f),
            string.Empty,
            "III · FOUNDRY", string.Empty,
            levelRoot.transform);

        CreateSentinel("Sentinel_1_Intro", new Vector2(121.5f, 6.95f), -3.0f, 3.0f, 2.5f, a, levelRoot.transform);

        // Fragile crumbling bridge inside canal gap 127.0 .. 136.0 framed by weathered stone arch corbels on both cliffs
        CreateBrokenArchCorbel("Corbel_3A_Left", 127.0f, 6.0f, flipX: false, a, levelRoot.transform);
        CreateBrokenArchCorbel("Corbel_3A_Right", 136.0f, 4.6f, flipX: true, a, levelRoot.transform);
        CreateCrumblingPlatform("Crumble_3A_Span", 129.5f, 133.5f, 5.2f, 1.0f, a, levelRoot.transform, footprints);

        CreateGroundedPlatform("Roof_3A_Foundry", 136.0f, 148.5f, 4.6f, GroundBottomY, a, levelRoot.transform, footprints);
        CreatePlagueCaster("Caster_3A_CrumbleGuard", new Vector2(137.0f, 4.6f), a, levelRoot.transform);
        CreatePopUpSpikes("Spikes_3A_Ambush", new Vector2(139.8f, 4.6f), 1.8f, PopUpSpikes.TrapMode.ProximityAmbush, 0f, a, levelRoot.transform);
        CreateStreetLamp("Lamp_3A", new Vector2(141.4f, 4.6f), a, levelRoot.transform);
        CreateSentinel("Sentinel_2_Foundry", new Vector2(144.6f, 5.55f), -2.4f, 2.4f, 2.8f, a, levelRoot.transform);
        CreateGargoyle("Gargoyle_3A_Foundry", new Vector2(144.6f, 8.6f), 3.4f, 3.2f, a, levelRoot.transform);

        // Wide Foundry Canal (148.5 .. 164.5): Dual strictly non-colliding Gothic Stone Platforms on chain piers + high Soul Relic perch
        CreateMovingPlatform("MovingPlat_3A_VerticalLift", new Vector3(152.5f, 2.8f, 0f), new Vector3(152.5f, 6.2f, 0f),
            3.4f, 0.95f, 2.3f, 0.45f, 0.0f, true, GroundBottomY, a, levelRoot.transform, footprints);
        CreatePendulumScythe("Pendulum_3_Gantry", new Vector2(155.4f, 10.2f), 4.1f, 46f, 2.5f, 0.6f, a, levelRoot.transform);
        CreateMovingPlatform("MovingPlat_3B_Shuttle", new Vector3(157.8f, 4.2f, 0f), new Vector3(161.2f, 4.2f, 0f),
            3.4f, 0.95f, 2.1f, 0.40f, 0.35f, true, GroundBottomY, a, levelRoot.transform, footprints);
        CreateElevatedPlatform("Roof_3_RelicPerch", 158.0f, 162.0f, 8.6f, 2.0f, a, levelRoot.transform, footprints);
        CreateSoulLifeRelic("SoulRelic_2_Foundry", new Vector2(160.0f, 9.5f), a, levelRoot.transform);

        // Twin out-of-phase Pop-Up Spike Gauntlet leading to Zone 4 Checkpoint
        CreateGroundedPlatform("Roof_3C_Gauntlet", 164.5f, 178.5f, 3.6f, GroundBottomY, a, levelRoot.transform, footprints);
        CreateGothicRailing("Railing_3C", 165.0f, 166.6f, 3.6f, a, levelRoot.transform);
        CreatePopUpSpikes("Spikes_3B_TimedA", new Vector2(168.0f, 3.6f), 1.8f, PopUpSpikes.TrapMode.TimedCycle, 0.0f, a, levelRoot.transform);
        CreatePopUpSpikes("Spikes_3C_TimedB", new Vector2(172.2f, 3.6f), 1.8f, PopUpSpikes.TrapMode.TimedCycle, 1.55f, a, levelRoot.transform);
        CreatePlagueCaster("Caster_3B_Gauntlet", new Vector2(174.5f, 3.6f), a, levelRoot.transform);
        CreateStreetLamp("Lamp_3C", new Vector2(174.4f, 3.6f), a, levelRoot.transform);

        // Shards 15..20 + 27..28
        CreateShard(15, new Vector2(121.5f, 9.2f), a, levelRoot.transform);
        CreateShard(16, new Vector2(131.5f, 6.8f), a, levelRoot.transform);
        CreateShard(27, new Vector2(140.5f, 6.2f), a, levelRoot.transform);
        CreateShard(17, new Vector2(144.6f, 7.9f), a, levelRoot.transform);
        CreateShard(18, new Vector2(152.5f, 7.6f), a, levelRoot.transform);
        CreateShard(19, new Vector2(159.5f, 6.0f), a, levelRoot.transform);
        CreateShard(28, new Vector2(166.0f, 5.8f), a, levelRoot.transform);
        CreateShard(20, new Vector2(170.1f, 5.7f), a, levelRoot.transform);

        // ============================================================
        // ZONE 4: Twilight Sanctum Ascent (x = 176 .. 244)
        // Gargoyle-Guarded Chimneys, Bastion Pendulum/Caster, and Multi-Phase Final Boss Arena
        // ============================================================
        CreateCheckpoint(3, "Sanctum Stair Gate", new Vector2(176.2f, 3.6f), a, levelRoot.transform);
        CreateTutorialTrigger("Tut_6_Final", new Vector2(176.5f, 5.2f), new Vector2(3.5f, 4f),
            string.Empty,
            "IV · SANCTUM", string.Empty,
            levelRoot.transform);

        CreateGroundedPlatform("Roof_4A_Bastion", 181.0f, 190.5f, 4.6f, GroundBottomY, a, levelRoot.transform, footprints);
        CreateStreetLamp("Lamp_4A", new Vector2(182.0f, 4.6f), a, levelRoot.transform);
        CreateSentinel("Sentinel_4_Bastion", new Vector2(185.8f, 5.55f), -2.8f, 2.8f, 3.1f, a, levelRoot.transform);
        CreatePendulumScythe("Pendulum_4_Bastion", new Vector2(185.8f, 10.4f), 3.85f, 44f, 2.6f, 1.2f, a, levelRoot.transform);
        CreatePlagueCaster("Caster_4_Bastion", new Vector2(188.6f, 4.6f), a, levelRoot.transform);

        // Twin Grounded Canal Steam Chimneys flanked by Abyssal Gargoyle Swoopers
        CreateCanalChimneyUpdraft("Updraft_4_AscentA", 193.8f, -1.6f, GroundBottomY, 3.6f, 12.1f, 10.6f, a, levelRoot.transform, footprints);
        CreateGargoyle("Gargoyle_4A_Chimney", new Vector2(193.8f, 8.8f), 2.8f, 3.0f, a, levelRoot.transform);
        CreateGothicPillar("Pillar_4B_RuinBase", 199.75f, 6.45f, GroundBottomY, 2.0f, a, levelRoot.transform, new Color(0.68f, 0.66f, 0.76f, 1f));
        CreateBrokenArchCorbel("Corbel_4B_Left", 198.75f, 6.45f, flipX: true, a, levelRoot.transform);
        CreateBrokenArchCorbel("Corbel_4B_Right", 200.75f, 6.45f, flipX: false, a, levelRoot.transform);
        CreateCrumblingPlatform("Crumble_4B_Perch", 197.5f, 202.0f, 7.4f, 1.0f, a, levelRoot.transform, footprints);
        CreateCanalChimneyUpdraft("Updraft_5_AscentB", 205.5f, 0.8f, GroundBottomY, 3.6f, 11.7f, 10.8f, a, levelRoot.transform, footprints);
        CreateGargoyle("Gargoyle_4B_Chimney", new Vector2(205.5f, 11.2f), 2.8f, 3.2f, a, levelRoot.transform);

        // Final Sanctum Terrace, Multi-Phase Final Boss Arena (Malakor) & Sealed Sanctum Beacon
        CreateGroundedPlatform("Roof_4C_Sanctum", 209.0f, 244.0f, 9.0f, GroundBottomY, a, levelRoot.transform, footprints);
        CreateDistrictArch("Arch_4_SanctumApproach", new Vector2(210.0f, 9.0f), a, levelRoot.transform);
        CreateCheckpoint(4, "Sanctum Threshold", new Vector2(211.6f, 9.0f), a, levelRoot.transform);
        CreateStreetLamp("Lamp_4_ArenaL", new Vector2(213.2f, 9.0f), a, levelRoot.transform);
        CreateRooftopUpdraft("Updraft_6_BossArenaLeft", new Vector2(214.8f, 9.0f), 2.8f, 5.8f, 9.4f, a, levelRoot.transform);
        CreateArenaAmmoSpawner("ArenaLightning_Left", new Vector2(214.8f, 13.8f), a, levelRoot.transform);
        CreateRooftopUpdraft("Updraft_7_BossArenaRight", new Vector2(233.2f, 9.0f), 2.8f, 5.8f, 9.4f, a, levelRoot.transform);
        CreateArenaAmmoSpawner("ArenaLightning_Right", new Vector2(233.2f, 13.8f), a, levelRoot.transform);
        CreateStreetLamp("Lamp_4_BeaconL", new Vector2(236.2f, 9.0f), a, levelRoot.transform);
        CreateStreetLamp("Lamp_4_BeaconR", new Vector2(242.4f, 9.0f), a, levelRoot.transform);
        CreateGothicRailing("Railing_4C_End", 241.0f, 243.6f, 9.0f, a, levelRoot.transform);

        // Shards 21..25 + 29..30
        CreateShard(29, new Vector2(183.2f, 6.4f), a, levelRoot.transform);
        CreateShard(21, new Vector2(185.8f, 7.9f), a, levelRoot.transform);
        CreateShard(22, new Vector2(193.8f, 7.4f), a, levelRoot.transform);
        CreateShard(23, new Vector2(199.8f, 9.0f), a, levelRoot.transform);
        CreateShard(30, new Vector2(202.0f, 10.4f), a, levelRoot.transform);
        CreateShard(24, new Vector2(219.5f, 12.6f), a, levelRoot.transform);
        CreateShard(25, new Vector2(228.5f, 12.6f), a, levelRoot.transform);

        LevelGoal sanctumGoal = CreateLevelGoal(new Vector2(239.5f, 9.0f), a, levelRoot.transform);
        CreateFinalBoss(new Vector2(225.0f, 9.0f), sanctumGoal, a, levelRoot.transform);

        // Verify mathematically that zero static, chimney, crumbling, or moving platform swept envelopes overlap!
        ValidateNoPlatformOverlaps(footprints);

        // Invisible Solid World Boundaries (Left & Right) + Deep Backup Abyss Killzone
        CreateBoundaryWall("LeftWorldWall", new Vector2(-2.5f, 10f), new Vector2(1f, 40f), levelRoot.transform);
        CreateBoundaryWall("RightWorldWall", new Vector2(244.5f, 10f), new Vector2(1f, 40f), levelRoot.transform);
        CreateAbyssKillzone(new Vector2(120f, -11.5f), new Vector2(290f, 4f), levelRoot.transform);
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

    private static void CreateGothicPillar(string name, float centerX, float topY, float bottomY, float width, BuiltAssets a, Transform parent, Color? tint = null)
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
        sr.color = tint ?? Color.white;
        sr.sortingOrder = -2;
    }

    private static void CreateBrokenArchCorbel(string name, float wallX, float topY, bool flipX, BuiltAssets a, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(wallX, topY, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.propBrokenArch;
        sr.flipX = flipX;
        sr.sortingOrder = 1;
    }

    private static void CreateGothicRailing(string name, float startX, float endX, float topY, BuiltAssets a, Transform parent)
    {
        float width = Mathf.Max(0.5f, endX - startX);
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3((startX + endX) * 0.5f, topY - 0.02f, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.propGothicRailing;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(width, 0.5f);
        sr.sortingOrder = -1;
    }

    private static void CreateStreetLamp(string name, Vector2 basePos, BuiltAssets a, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(basePos.x, basePos.y - 0.02f, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.propStreetLamp;
        sr.sortingOrder = -1;
    }

    private static void CreateDistrictArch(string name, Vector2 basePos, BuiltAssets a, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(basePos.x, basePos.y - 0.03f, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.propDistrictArch;
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
        bool groundSupportStanchions,
        float groundBottomY,
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

        // Wrought-iron gothic chain guide rail showing the platform's exact travel path
        GameObject railGo = new GameObject($"{name}_GuideRail");
        railGo.transform.SetParent(parent, false);
        Vector3 mid = (startPos + endPos) * 0.5f;
        Vector3 diff = endPos - startPos;
        float railLen = diff.magnitude + 0.5f;
        float railAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        railGo.transform.position = mid;
        railGo.transform.rotation = Quaternion.Euler(0f, 0f, railAngle - 90f);
        SpriteRenderer railSr = railGo.AddComponent<SpriteRenderer>();
        railSr.sprite = a.propIronChain;
        railSr.drawMode = SpriteDrawMode.Tiled;
        railSr.tileMode = SpriteTileMode.Continuous;
        railSr.size = new Vector2(0.45f, railLen);
        railSr.color = new Color(0.76f, 0.80f, 0.92f, 0.78f);
        railSr.sortingOrder = -1;

        // Brass clockwork gear pylons capping every endpoint + forged wrought-iron suspension chains into canal
        List<Vector3> anchors = new List<Vector3> { startPos };
        if ((endPos - startPos).sqrMagnitude > 0.01f)
        {
            anchors.Add(endPos);
        }
        for (int i = 0; i < anchors.Count; i++)
        {
            Vector3 endPt = anchors[i];
            if (groundSupportStanchions)
            {
                bool skipDuplicateColumn = Mathf.Abs(startPos.x - endPos.x) < 0.05f && i == 0;
                if (!skipDuplicateColumn)
                {
                    float colH = Mathf.Max(1f, endPt.y - groundBottomY);
                    GameObject colGo = new GameObject($"{name}_GantryPier_{i + 1}");
                    colGo.transform.SetParent(parent, false);
                    colGo.transform.position = new Vector3(endPt.x, (endPt.y + groundBottomY) * 0.5f, 0f);
                    SpriteRenderer colSr = colGo.AddComponent<SpriteRenderer>();
                    colSr.sprite = a.propIronChain;
                    colSr.drawMode = SpriteDrawMode.Tiled;
                    colSr.tileMode = SpriteTileMode.Continuous;
                    colSr.size = new Vector2(0.48f, colH);
                    colSr.color = new Color(0.85f, 0.88f, 0.96f, 0.95f);
                    colSr.sortingOrder = -2;
                }
            }

            GameObject pylon = new GameObject($"{name}_TrackPylon_{i + 1}");
            pylon.transform.SetParent(parent, false);
            pylon.transform.position = endPt;
            SpriteRenderer capSr = pylon.AddComponent<SpriteRenderer>();
            capSr.sprite = a.propTrackPylon;
            capSr.sortingOrder = 0;
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

    private static void CreateRooftopUpdraft(string name, Vector2 baseCenter, float width, float height, float speed, BuiltAssets a, Transform parent)
    {
        // Flush brass-and-iron rooftop thermal turbine grate right under the updraft column
        GameObject ventGo = new GameObject($"{name}_FloorVent");
        ventGo.transform.SetParent(parent, false);
        ventGo.transform.position = new Vector3(baseCenter.x, baseCenter.y, 0f);
        SpriteRenderer ventSr = ventGo.AddComponent<SpriteRenderer>();
        ventSr.sprite = a.propUpdraftVent;
        ventSr.sortingOrder = 2;

        CreateUpdraftColumn(name, baseCenter, width, height, speed, a.particleMaterial, parent);
    }

    private static void CreateCanalChimneyUpdraft(
        string name,
        float centerX,
        float chimneyTopY,
        float groundBottomY,
        float updraftWidth,
        float columnHeight,
        float speed,
        BuiltAssets a,
        Transform parent,
        List<PlatformFootprint> footprints)
    {
        const float chimneyWidth = 2.0f;
        const float capSolidHeight = 0.84f;
        float capTopY = chimneyTopY + capSolidHeight;
        float stackHeight = Mathf.Max(1.0f, chimneyTopY - groundBottomY);

        footprints.Add(new PlatformFootprint
        {
            name = $"{name}_Chimney",
            minX = centerX - chimneyWidth * 0.5f,
            maxX = centerX + chimneyWidth * 0.5f,
            minY = groundBottomY,
            maxY = capTopY
        });

        // Tiled vertical steam chimney shaft plunging into the Twilight Canal
        GameObject chimneyGo = new GameObject($"{name}_Chimney");
        chimneyGo.transform.SetParent(parent, false);
        chimneyGo.transform.position = new Vector3(centerX, (chimneyTopY + groundBottomY) * 0.5f, 0f);
        SpriteRenderer chimSr = chimneyGo.AddComponent<SpriteRenderer>();
        chimSr.sprite = a.propSteamChimney;
        chimSr.drawMode = SpriteDrawMode.Tiled;
        chimSr.tileMode = SpriteTileMode.Continuous;
        chimSr.size = new Vector2(chimneyWidth, stackHeight);
        chimSr.sortingOrder = -1;

        // Solid chimney stack collider so players never fall through a solid brick smokestack
        BoxCollider2D stackCol = chimneyGo.AddComponent<BoxCollider2D>();
        stackCol.size = new Vector2(chimneyWidth * 0.92f, stackHeight);

        // Flared brass turbine crown atop chimney
        GameObject capGo = new GameObject($"{name}_ChimneyCap");
        capGo.transform.SetParent(parent, false);
        capGo.transform.position = new Vector3(centerX, chimneyTopY - 0.02f, 0f);
        SpriteRenderer capSr = capGo.AddComponent<SpriteRenderer>();
        capSr.sprite = a.propChimneyCap;
        capSr.sortingOrder = 2;
        BoxCollider2D capCol = capGo.AddComponent<BoxCollider2D>();
        capCol.offset = new Vector2(0f, capSolidHeight * 0.42f);
        capCol.size = new Vector2(chimneyWidth * 0.96f, capSolidHeight * 0.84f);

        CreateUpdraftColumn(name, new Vector2(centerX, capTopY - 0.1f), updraftWidth, columnHeight, speed, a.particleMaterial, parent);
    }

    private static void CreateUpdraftColumn(string name, Vector2 baseCenter, float width, float height, float speed, Material particleMat, Transform parent)
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
        shape.scale = new Vector3(width * 0.52f, 0.25f, 0.25f);

        ConfigureParticleNoise(ps, 0.85f, 0.85f, 1.1f);
        ConfigureParticleTrails(ps, particleMat, 0.55f, 0.20f, 0.55f);

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMat;
        renderer.sortingOrder = 1;
        ConfigureParticleFade(ps, 0.88f, true);
    }

    private static void CreateTutorialTrigger(
        string name,
        Vector2 center,
        Vector2 size,
        string message,
        string districtLabel,
        string objectiveText,
        Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, center.y, 0f);

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = size;

        TutorialTrigger trigger = go.AddComponent<TutorialTrigger>();
        trigger.tutorialMessage = message;
        trigger.districtLabel = districtLabel;
        trigger.objectiveText = objectiveText;
        trigger.displayDuration = 5.0f;
        trigger.triggerOnce = true;
    }

    private static void CreateGargoyle(string name, Vector2 pos, float range, float speed, BuiltAssets a, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.gargoyleFrames[0];
        sr.sortingOrder = 7;

        Animator anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = a.gargoyleAnimator;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.72f;

        SwoopEnemy swoop = go.AddComponent<SwoopEnemy>();
        swoop.patrolRange = range;
        swoop.patrolSpeed = speed;
        swoop.defeatParticles = CreateOneShotBurstParticles(
            $"{name}_DefeatParticles", go.transform, Vector3.zero,
            a.particleMaterial, new Color(0.85f, 0.35f, 0.65f, 0.96f), 24, 0.14f, 0.34f, 6.2f, 180f, 11,
            enableTrails: true
        );
    }

    private static void CreatePlagueCaster(string name, Vector2 basePos, BuiltAssets a, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(basePos.x, basePos.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.casterFrames[0];
        sr.sortingOrder = 6;

        Animator anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = a.casterAnimator;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.offset = new Vector2(0f, 0.92f);
        col.size = new Vector2(1.05f, 1.85f);

        AetherShooterEnemy caster = go.AddComponent<AetherShooterEnemy>();
        caster.projectileSprite = a.vfxEclipseOrb;
        caster.defeatParticles = CreateOneShotBurstParticles(
            $"{name}_DefeatParticles", go.transform, new Vector3(0f, 0.9f, 0f),
            a.particleMaterial, new Color(0.92f, 0.32f, 0.75f, 0.96f), 26, 0.14f, 0.34f, 6.2f, 180f, 11,
            enableTrails: true
        );
    }

    private static void CreatePendulumScythe(
        string name,
        Vector2 pivotPos,
        float chainLength,
        float maxAngle,
        float swingSpeed,
        float phaseOffset,
        BuiltAssets a,
        Transform parent)
    {
        GameObject pivot = new GameObject(name);
        pivot.transform.SetParent(parent, false);
        pivot.transform.position = new Vector3(pivotPos.x, pivotPos.y, 0f);

        PendulumHazard pend = pivot.AddComponent<PendulumHazard>();
        pend.maxAngle = maxAngle;
        pend.swingSpeed = swingSpeed;
        pend.phaseOffset = phaseOffset;

        // Ornate brass clockwork ceiling anchor hub
        GameObject hub = new GameObject("CeilingAnchorHub");
        hub.transform.SetParent(pivot.transform, false);
        hub.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        SpriteRenderer hubSr = hub.AddComponent<SpriteRenderer>();
        hubSr.sprite = a.propTrackPylon;
        hubSr.sortingOrder = 4;

        // Forged iron chain arm down to blade
        GameObject chain = new GameObject("PendulumChain");
        chain.transform.SetParent(pivot.transform, false);
        chain.transform.localPosition = new Vector3(0f, -chainLength * 0.5f, 0f);
        SpriteRenderer chSr = chain.AddComponent<SpriteRenderer>();
        chSr.sprite = a.propIronChain;
        chSr.drawMode = SpriteDrawMode.Tiled;
        chSr.tileMode = SpriteTileMode.Continuous;
        chSr.size = new Vector2(0.42f, chainLength);
        chSr.sortingOrder = 3;

        // Crescent executioner blade at bottom of chain
        GameObject blade = new GameObject("ScytheBlade");
        blade.transform.SetParent(pivot.transform, false);
        blade.transform.localPosition = new Vector3(0f, -chainLength, 0f);
        SpriteRenderer blSr = blade.AddComponent<SpriteRenderer>();
        blSr.sprite = a.hazardPendulumBlade;
        blSr.sortingOrder = 5;

        BoxCollider2D col = blade.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.offset = new Vector2(0f, -0.18f);
        col.size = new Vector2(1.75f, 0.52f);

        Hazard hz = blade.AddComponent<Hazard>();
        hz.isKillzone = false;
        hz.damageAmount = 25;
    }

    private static void CreateSoulLifeRelic(string name, Vector2 pos, BuiltAssets a, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.propSoulLifeRelic;
        sr.sortingOrder = 6;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.62f;

        CreateLoopingTrailParticles(
            $"{name}_Glow", go.transform, Vector3.zero,
            a.particleMaterial, new Color(1f, 0.90f, 0.42f, 0.85f), new Color(0.45f, 0.98f, 1f, 0.80f), 5,
            enableRibbons: false, noiseStrength: 0.45f
        );

        SoulLifePickup pickup = go.AddComponent<SoulLifePickup>();
        pickup.pickupParticles = CreateOneShotBurstParticles(
            $"{name}_Burst", go.transform, Vector3.zero,
            a.particleMaterial, new Color(1f, 0.90f, 0.45f, 1f), 28, 0.16f, 0.34f, 6.0f, 180f, 12,
            enableTrails: true
        );
    }

    private static void CreateArenaAmmoSpawner(string name, Vector2 pos, BuiltAssets a, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.shardFrames[0];
        sr.sortingOrder = 6;

        Animator anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = a.shardAnimator;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.65f;

        ArenaAmmoSpawner spawner = go.AddComponent<ArenaAmmoSpawner>();
        spawner.idleParticles = CreateLoopingTrailParticles(
            $"{name}_Idle", go.transform, Vector3.zero,
            a.particleMaterial, new Color(0.45f, 0.98f, 1f, 0.85f), new Color(1f, 0.94f, 0.55f, 0.75f), 5,
            enableRibbons: false, noiseStrength: 0.45f
        );
        spawner.collectBurstParticles = CreateOneShotBurstParticles(
            $"{name}_CollectBurst", go.transform, Vector3.zero,
            a.particleMaterial, new Color(0.45f, 0.98f, 1f, 1f), 22, 0.16f, 0.34f, 6.2f, 180f, 12,
            enableTrails: true
        );
    }

    private static void CreateFinalBoss(Vector2 basePos, LevelGoal linkedGoal, BuiltAssets a, Transform parent)
    {
        GameObject go = new GameObject("FinalBoss_EclipseWarden");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(basePos.x, basePos.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = a.bossWanderFrames[0];
        sr.sortingOrder = 7;

        Animator anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = a.bossAnimator;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.offset = new Vector2(0f, 1.38f);
        col.size = new Vector2(1.75f, 2.75f);

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        GameObject wardRingGo = new GameObject("EclipseWardRing");
        wardRingGo.transform.SetParent(go.transform, false);
        wardRingGo.transform.localPosition = new Vector3(0f, 1.45f, 0f);
        wardRingGo.transform.localScale = new Vector3(1.42f, 1.42f, 1f);
        SpriteRenderer wardSr = wardRingGo.AddComponent<SpriteRenderer>();
        wardSr.sprite = a.propEclipseSeal;
        wardSr.color = new Color(1f, 0.38f, 0.52f, 0.44f);
        wardSr.sortingOrder = 6;

        FinalBossAI boss = go.AddComponent<FinalBossAI>();
        boss.animator = anim;
        boss.spriteRenderer = sr;
        boss.linkedGoal = linkedGoal;
        boss.wardRingVisual = wardRingGo;
        boss.shockwaveSprite = a.vfxBossShockwave;
        boss.skySpearSprite = a.vfxBossSkySpear;
        boss.skyBeamWhiteSprite = a.uiWhiteSprite;
        boss.eclipseOrbSprite = a.vfxEclipseOrb;
        boss.wanderPreviewSprite = a.bossWanderFrames[0];
        boss.attackPreviewSprite = a.bossAttackFrames[2];
        boss.dashPreviewSprite = a.bossDashFrames[1];
        boss.castPreviewSprite = a.bossCastFrames[1];
        boss.stunnedPreviewSprite = a.bossStunnedFrames[1];
        boss.arenaActivationX = 212.5f;
        boss.minPatrolX = 215.5f;
        boss.maxPatrolX = 232.8f;

        boss.slamParticles = CreateOneShotBurstParticles(
            "BossSlamParticles", go.transform, new Vector3(-1.45f, 0.15f, 0f),
            a.particleMaterial, new Color(1f, 0.34f, 0.22f, 0.96f), 32, 0.16f, 0.42f, 7.2f, 130f, 12,
            enableTrails: true
        );
        boss.hitSparkParticles = CreateOneShotBurstParticles(
            "BossHitSparkParticles", go.transform, new Vector3(0f, 2.0f, 0f),
            a.particleMaterial, new Color(0.45f, 0.98f, 1f, 0.98f), 28, 0.14f, 0.32f, 6.8f, 180f, 13,
            enableTrails: true
        );
        boss.stunLoopParticles = CreateLoopingTrailParticles(
            "BossStunLoopParticles", go.transform, new Vector3(0f, 2.4f, 0f),
            a.particleMaterial, new Color(0.45f, 0.98f, 1f, 0.85f), new Color(1f, 0.86f, 0.35f, 0.8f), 8,
            enableRibbons: false, noiseStrength: 0.7f
        );
        boss.stunLoopParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var stunMain = boss.stunLoopParticles.main;
        stunMain.playOnAwake = false;

        boss.rageAuraParticles = CreateLoopingTrailParticles(
            "BossRageAuraParticles", go.transform, new Vector3(0f, 1.2f, 0f),
            a.particleMaterial, new Color(1f, 0.24f, 0.42f, 0.82f), new Color(1f, 0.58f, 0.20f, 0.75f), 5,
            enableRibbons: true, noiseStrength: 0.85f
        );
        boss.rageAuraParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var rageMain = boss.rageAuraParticles.main;
        rageMain.playOnAwake = false;

        boss.defeatExplosionParticles = CreateOneShotBurstParticles(
            "BossDefeatExplosionParticles", go.transform, new Vector3(0f, 1.4f, 0f),
            a.particleMaterial, new Color(1f, 0.72f, 0.32f, 1f), 60, 0.22f, 0.54f, 9.2f, 180f, 14,
            enableTrails: true
        );
    }

    private static LevelGoal CreateLevelGoal(Vector2 basePos, BuiltAssets a, Transform parent)
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

        GameObject sealGo = new GameObject("EclipseSealVisual");
        sealGo.transform.SetParent(go.transform, false);
        sealGo.transform.localPosition = new Vector3(0f, 1.85f, 0f);
        sealGo.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
        SpriteRenderer sealSr = sealGo.AddComponent<SpriteRenderer>();
        sealSr.sprite = a.propEclipseSeal;
        sealSr.sortingOrder = 6;

        LevelGoal goal = go.AddComponent<LevelGoal>();
        goal.isLockedByBoss = true;
        goal.eclipseSealVisual = sealGo;
        goal.sealBreakParticles = CreateOneShotBurstParticles(
            "GoalSealBreakParticles", go.transform, new Vector3(0f, 1.85f, 0f),
            a.particleMaterial, new Color(1f, 0.35f, 0.45f, 1f), 36, 0.16f, 0.38f, 7.0f, 180f, 14,
            enableTrails: true
        );
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
        return goal;
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
        Color cyanGlow = new Color(0.45f, 0.96f, 1f, 1f);
        Color goldTrim = new Color(1f, 0.86f, 0.44f, 1f);
        Color warmWhite = new Color(0.92f, 0.94f, 1f, 1f);

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
        uiMgr.storyCardSprites = a.storyCardSprites;

        // EventSystem configured cleanly for Unity InputSystem (or Legacy fallback)
        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        esGo.AddComponent<InputSystemUIInputModule>();
#else
        esGo.AddComponent<StandaloneInputModule>();
#endif

        // ============================================================
        // 1. TITLE SCREEN PANEL (Ornate Gothic modal over live Courtyard)
        // ============================================================
        GameObject titlePanel = CreateFullScreenPanel("TitlePanel", canvasGo.transform, a.uiWhiteSprite, new Color(0.04f, 0.05f, 0.13f, 0.48f));
        uiMgr.titlePanel = titlePanel;

        GameObject titleModal = CreateOrnatePanel("TitleModal", titlePanel.transform, a.uiPanelOrnate, Vector2.zero, new Vector2(640f, 520f));
        CreateUiImage("TitleCrest", titleModal.transform, a.uiTitleCrest, new Vector2(0.5f, 0.5f), new Vector2(0f, 198f), new Vector2(440f, 74f));

        Text titleLogo = CreateText("TitleLogo", titleModal.transform, uiFont, "VESPER", 42, FontStyle.Bold,
            cyanGlow, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 134f), new Vector2(560f, 52f));
        AddUiShadow(titleLogo, new Color(0f, 0f, 0f, 0.85f), new Vector2(2f, -2f));

        CreateText("TitleSubtitle", titleModal.transform, uiFont, "TWILIGHT COURIER", 18, FontStyle.Bold,
            goldTrim, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 96f), new Vector2(480f, 28f));

        CreateUiImage("TitleDivider", titleModal.transform, a.uiDivider, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(400f, 18f));

        CreateText("TitlePitch", titleModal.transform, uiFont,
            "Outfly the eclipse. Break the Warden.",
            16, FontStyle.Normal, warmWhite, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 36f), new Vector2(520f, 32f));

        Button startBtn = CreateOrnateButton("StartRunButton", titleModal.transform, a.uiBtnPrimary, uiFont,
            "PLAY", new Vector2(0f, -24f), new Vector2(290f, 56f), 22, Color.white);
        uiMgr.startRunButton = startBtn;
        UnityEventTools.AddPersistentListener(startBtn.onClick, gm.BeginDeliveryFromTitle);

        Button titleStoryBtn = CreateOrnateButton("TitleStoryButton", titleModal.transform, a.uiBtnSecondary, uiFont,
            "STORY", new Vector2(0f, -86f), new Vector2(260f, 46f), 18, goldTrim);
        uiMgr.titleStoryButton = titleStoryBtn;
        UnityEventTools.AddPersistentListener(titleStoryBtn.onClick, gm.ShowLoreScreen);

        Button titleTutBtn = CreateOrnateButton("TitleTutorialButton", titleModal.transform, a.uiBtnSecondary, uiFont,
            "GUIDE", new Vector2(0f, -142f), new Vector2(260f, 46f), 18, cyanGlow);
        uiMgr.titleTutorialButton = titleTutBtn;
        UnityEventTools.AddPersistentListener(titleTutBtn.onClick, gm.OpenTutorialFromTitle);

        Button titleQuitBtn = CreateOrnateButton("TitleQuitButton", titleModal.transform, a.uiBtnDanger, uiFont,
            "QUIT", new Vector2(0f, -198f), new Vector2(200f, 42f), 17, warmWhite);
        uiMgr.titleQuitButton = titleQuitBtn;
        UnityEventTools.AddPersistentListener(titleQuitBtn.onClick, gm.QuitGame);

        // ============================================================
        // 2. INTERACTIVE 3-CARD STORY INTRO PROLOGUE PANEL
        // ============================================================
        GameObject storyIntroPanel = CreateFullScreenPanel("StoryIntroPanel", canvasGo.transform, a.uiWhiteSprite, new Color(0.03f, 0.04f, 0.11f, 0.78f));
        uiMgr.storyIntroPanel = storyIntroPanel;

        GameObject storyModal = CreateOrnatePanel("StoryIntroModal", storyIntroPanel.transform, a.uiPanelOrnate, Vector2.zero, new Vector2(840f, 540f));
        Text storyHeader = CreateText("StoryHeader", storyModal.transform, uiFont, "NOCTIS REACH", 28, FontStyle.Bold,
            goldTrim, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 224f), new Vector2(680f, 42f));
        AddUiShadow(storyHeader, new Color(0f, 0f, 0f, 0.85f), new Vector2(2f, -2f));
        CreateUiImage("StoryDividerTop", storyModal.transform, a.uiDivider, new Vector2(0.5f, 0.5f), new Vector2(0f, 194f), new Vector2(460f, 16f));

        uiMgr.storyCardImage = CreateUiImage("StoryCardImage", storyModal.transform,
            a.storyCardSprites != null && a.storyCardSprites.Length > 0 ? a.storyCardSprites[0] : a.uiWhiteSprite,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 64f), new Vector2(520f, 226f));

        uiMgr.storyStepTitleText = CreateText("StoryStepTitleText", storyModal.transform, uiFont, "I · ECLIPSE", 23, FontStyle.Bold,
            cyanGlow, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -74f), new Vector2(700f, 34f));

        uiMgr.storyStepBodyText = CreateText("StoryStepBodyText", storyModal.transform, uiFont,
            "The Eternal Eclipse freezes Noctis Reach.",
            18, FontStyle.Normal, warmWhite, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -118f), new Vector2(720f, 44f));

        Button storyBackBtn = CreateOrnateButton("StoryBackButton", storyModal.transform, a.uiBtnSecondary, uiFont,
            "BACK", new Vector2(-260f, -206f), new Vector2(150f, 46f), 16, warmWhite);
        uiMgr.storyBackButton = storyBackBtn;
        UnityEventTools.AddPersistentListener(storyBackBtn.onClick, uiMgr.PreviousStoryIntro);

        uiMgr.storyPageIndicatorText = CreateText("StoryPageIndicatorText", storyModal.transform, uiFont, "1 / 3", 18, FontStyle.Bold,
            goldTrim, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(-115f, -206f), new Vector2(86f, 34f));

        Button storyNextBtn = CreateOrnateButton("StoryNextButton", storyModal.transform, a.uiBtnPrimary, uiFont,
            "NEXT", new Vector2(75f, -206f), new Vector2(220f, 52f), 19, Color.white);
        uiMgr.storyNextButton = storyNextBtn;
        uiMgr.storyNextButtonText = storyNextBtn.GetComponentInChildren<Text>();
        UnityEventTools.AddPersistentListener(storyNextBtn.onClick, uiMgr.AdvanceStoryIntro);

        Button storySkipBtn = CreateOrnateButton("StorySkipButton", storyModal.transform, a.uiBtnSecondary, uiFont,
            "SKIP", new Vector2(280f, -206f), new Vector2(145f, 46f), 16, warmWhite);
        uiMgr.storySkipButton = storySkipBtn;
        UnityEventTools.AddPersistentListener(storySkipBtn.onClick, gm.StartGame);

        // ============================================================
        // 3. STORY & MISSION LORE SCREEN
        // ============================================================
        GameObject lorePanel = CreateFullScreenPanel("LorePanel", canvasGo.transform, a.uiWhiteSprite, new Color(0.03f, 0.04f, 0.11f, 0.78f));
        uiMgr.lorePanel = lorePanel;

        GameObject loreModal = CreateOrnatePanel("LoreModal", lorePanel.transform, a.uiPanelOrnate, Vector2.zero, new Vector2(780f, 460f));
        Text loreTitle = CreateText("LoreTitle", loreModal.transform, uiFont, "CHRONICLE", 30, FontStyle.Bold,
            goldTrim, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 178f), new Vector2(680f, 44f));
        AddUiShadow(loreTitle, new Color(0f, 0f, 0f, 0.85f), new Vector2(2f, -2f));
        CreateUiImage("LoreDivider", loreModal.transform, a.uiDivider, new Vector2(0.5f, 0.5f), new Vector2(0f, 146f), new Vector2(460f, 16f));

        CreateText("LoreContent", loreModal.transform, uiFont,
            "NOCTIS REACH — Locked in Eternal Eclipse.\n\n" +
            "VESPER — Shadow-Stitcher flying the dusk-woven cloak.\n\n" +
            "MISSION — Gather Storm-Lightnings, shatter Malakor's Ward, ignite Dawn.",
            19, FontStyle.Normal, warmWhite, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(700f, 220f));

        Button loreStartBtn = CreateOrnateButton("LoreStartButton", loreModal.transform, a.uiBtnPrimary, uiFont,
            "PLAY", new Vector2(-140f, -158f), new Vector2(220f, 52f), 19, Color.white);
        uiMgr.loreStartButton = loreStartBtn;
        UnityEventTools.AddPersistentListener(loreStartBtn.onClick, gm.StartGame);

        Button loreBackBtn = CreateOrnateButton("LoreBackButton", loreModal.transform, a.uiBtnSecondary, uiFont,
            "BACK", new Vector2(140f, -158f), new Vector2(220f, 48f), 18, warmWhite);
        uiMgr.loreBackButton = loreBackBtn;
        UnityEventTools.AddPersistentListener(loreBackBtn.onClick, gm.CloseSubScreen);

        // ============================================================
        // 4. ANIMATED VISUAL TUTORIAL SCREEN ("FIELD GUIDE")
        // ============================================================
        GameObject tutorialPanel = CreateFullScreenPanel("TutorialPanel", canvasGo.transform, a.uiWhiteSprite, new Color(0.03f, 0.04f, 0.11f, 0.78f));
        uiMgr.tutorialPanel = tutorialPanel;

        GameObject tutModal = CreateOrnatePanel("TutorialModal", tutorialPanel.transform, a.uiPanelOrnate, Vector2.zero, new Vector2(1020f, 585f));
        Text tutTitle = CreateText("TutorialTitle", tutModal.transform, uiFont, "FIELD GUIDE", 30, FontStyle.Bold,
            goldTrim, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 246f), new Vector2(760f, 44f));
        AddUiShadow(tutTitle, new Color(0f, 0f, 0f, 0.85f), new Vector2(2f, -2f));
        CreateUiImage("TutorialDivider", tutModal.transform, a.uiDivider, new Vector2(0.5f, 0.5f), new Vector2(0f, 216f), new Vector2(520f, 16f));

        CreateVisualTutorialCard(tutModal.transform, a, uiFont, "Card_MoveJump",
            new Vector2(-325f, 106f), a.vesperRunFrames[0], new Vector2(78f, 78f),
            "MOVE & JUMP", "A/D + SPACE", "Hold Jump for extra height.", cyanGlow,
            a.vesperRunFrames, 10f, 0f, 5f);
        CreateVisualTutorialCard(tutModal.transform, a, uiFont, "Card_Glide",
            new Vector2(0f, 106f), a.vesperGlideFrames[1], new Vector2(78f, 78f),
            "CLOAK GLIDE", "HOLD SHIFT", "Soar cleanly across chasms.", cyanGlow,
            a.vesperGlideFrames, 8f, 5f, 0f);
        CreateVisualTutorialCard(tutModal.transform, a, uiFont, "Card_Updraft",
            new Vector2(325f, 106f), a.propUpdraftVent, new Vector2(88f, 36f),
            "UPDRAFTS", "GLIDE IN", "Glide into vents & steam.", cyanGlow,
            null, 6f, 7f, 0f);
        CreateVisualTutorialCard(tutModal.transform, a, uiFont, "Card_Hazards",
            new Vector2(-325f, -80f), a.hazardPendulumBlade != null ? a.hazardPendulumBlade : a.hazardSpikes, new Vector2(76f, 64f),
            "TRAPS & SCYTHES", "TIMED DODGE", "Time scythes & spark grates.", new Color(1f, 0.45f, 0.58f, 1f),
            new[] { a.hazardPendulumBlade, a.hazardSpikes }, 1.2f, 3f, 8f);
        CreateVisualTutorialCard(tutModal.transform, a, uiFont, "Card_Lightning",
            new Vector2(0f, -80f), a.shardFrames[0], new Vector2(74f, 74f),
            "STORM BOLT", "CLICK / E / F", "Blast foes & Boss Shield!", cyanGlow,
            a.shardFrames, 9f, 4f, 0f);
        CreateVisualTutorialCard(tutModal.transform, a, uiFont, "Card_Stomp",
            new Vector2(325f, -80f), a.gargoyleFrames != null && a.gargoyleFrames.Length > 0 ? a.gargoyleFrames[0] : a.sentinelFrames[0], new Vector2(76f, 76f),
            "STOMP FOES", "LAND ON TOP", "Crush drones & Stunned Boss.", goldTrim,
            a.gargoyleFrames != null && a.gargoyleFrames.Length > 0 ? a.gargoyleFrames : a.sentinelFrames, 9f, 6f, 0f);

        Button tutBackBtn = CreateOrnateButton("TutorialBackButton", tutModal.transform, a.uiBtnSecondary, uiFont,
            "BACK", new Vector2(0f, -234f), new Vector2(220f, 48f), 18, warmWhite);
        uiMgr.tutorialBackButton = tutBackBtn;
        UnityEventTools.AddPersistentListener(tutBackBtn.onClick, gm.CloseSubScreen);

        // ============================================================
        // 5. HUD PANEL (Ornate Plaques + Objective + Progress Gauge)
        // ============================================================
        GameObject hudPanel = CreateFullScreenPanel("HUDPanel", canvasGo.transform, a.uiWhiteSprite, new Color(0f, 0f, 0f, 0f));
        uiMgr.hudPanel = hudPanel;

        // Top-left Combat Stats Plaque (Portrait + 5 Life Souls + 100 HP Bar + Lightning Ammo + Shards + Timer)
        GameObject leftPlaque = CreateHudPlaque("HudStatsPlaque", hudPanel.transform, a.uiHudBar,
            new Vector2(0f, 1f), new Vector2(216f, -64f), new Vector2(412f, 112f));

        Image portraitImg = CreateUiImage("VesperPortrait", leftPlaque.transform,
            a.uiPortraitVesper != null ? a.uiPortraitVesper : a.vesperIdle,
            new Vector2(0.5f, 0.5f), new Vector2(-152f, 0f), new Vector2(78f, 78f));
        portraitImg.preserveAspect = true;

        uiMgr.lifeIcons = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject lifeGo = new GameObject($"LifeIcon_{i + 1}");
            lifeGo.transform.SetParent(leftPlaque.transform, false);
            Image lImg = lifeGo.AddComponent<Image>();
            lImg.sprite = a.uiLifeSoulIcon != null ? a.uiLifeSoulIcon : a.uiHeartSprite;
            lImg.preserveAspect = true;
            lImg.color = i < 3 ? uiMgr.fullLifeColor : uiMgr.emptyLifeColor;
            lImg.raycastTarget = false;

            RectTransform lRt = lifeGo.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0.5f, 0.5f);
            lRt.anchorMax = new Vector2(0.5f, 0.5f);
            lRt.anchoredPosition = new Vector2(-88f + i * 26f, 32f);
            lRt.sizeDelta = new Vector2(22f, 22f);
            uiMgr.lifeIcons[i] = lImg;
        }

        uiMgr.timerText = CreateText("TimerText", leftPlaque.transform, uiFont, "00:00", 14, FontStyle.Bold,
            goldTrim, TextAnchor.MiddleRight,
            new Vector2(0.5f, 0.5f), new Vector2(148f, 32f), new Vector2(96f, 24f));

        // 100 HP Bar with delayed white-gold drain trail and ornate Gothic frame
        GameObject hpTrack = new GameObject("HpBarTrack");
        hpTrack.transform.SetParent(leftPlaque.transform, false);
        Image hpTrackImg = hpTrack.AddComponent<Image>();
        hpTrackImg.sprite = a.uiWhiteSprite;
        hpTrackImg.color = new Color(0.06f, 0.05f, 0.12f, 0.96f);
        hpTrackImg.raycastTarget = false;
        RectTransform hpTrackRt = hpTrack.GetComponent<RectTransform>();
        hpTrackRt.anchorMin = new Vector2(0.5f, 0.5f);
        hpTrackRt.anchorMax = new Vector2(0.5f, 0.5f);
        hpTrackRt.anchoredPosition = new Vector2(44f, 3f);
        hpTrackRt.sizeDelta = new Vector2(276f, 18f);

        GameObject hpTrailGo = new GameObject("HpTrailFill");
        hpTrailGo.transform.SetParent(hpTrack.transform, false);
        Image hpTrailImg = hpTrailGo.AddComponent<Image>();
        hpTrailImg.sprite = a.uiHpBarFill != null ? a.uiHpBarFill : a.uiWhiteSprite;
        hpTrailImg.type = Image.Type.Filled;
        hpTrailImg.fillMethod = Image.FillMethod.Horizontal;
        hpTrailImg.fillAmount = 1f;
        hpTrailImg.color = new Color(1f, 0.88f, 0.52f, 0.9f);
        hpTrailImg.raycastTarget = false;
        RectTransform hpTrailRt = hpTrailGo.GetComponent<RectTransform>();
        hpTrailRt.anchorMin = Vector2.zero;
        hpTrailRt.anchorMax = Vector2.one;
        hpTrailRt.offsetMin = Vector2.zero;
        hpTrailRt.offsetMax = Vector2.zero;
        uiMgr.hpTrailFillImage = hpTrailImg;

        GameObject hpFillGo = new GameObject("HpFill");
        hpFillGo.transform.SetParent(hpTrack.transform, false);
        Image hpFillImg = hpFillGo.AddComponent<Image>();
        hpFillImg.sprite = a.uiHpBarFill != null ? a.uiHpBarFill : a.uiWhiteSprite;
        hpFillImg.type = Image.Type.Filled;
        hpFillImg.fillMethod = Image.FillMethod.Horizontal;
        hpFillImg.fillAmount = 1f;
        hpFillImg.color = new Color(0.28f, 0.92f, 0.68f, 1f);
        hpFillImg.raycastTarget = false;
        RectTransform hpFillRt = hpFillGo.GetComponent<RectTransform>();
        hpFillRt.anchorMin = Vector2.zero;
        hpFillRt.anchorMax = Vector2.one;
        hpFillRt.offsetMin = Vector2.zero;
        hpFillRt.offsetMax = Vector2.zero;
        uiMgr.hpFillImage = hpFillImg;

        if (a.uiHpBarFrame != null)
        {
            Image hpFrameImg = CreateUiImage("HpBarFrame", leftPlaque.transform, a.uiHpBarFrame,
                new Vector2(0.5f, 0.5f), new Vector2(44f, 3f), new Vector2(288f, 26f));
            hpFrameImg.type = Image.Type.Sliced;
        }

        uiMgr.hpValueText = CreateText("HpValueText", leftPlaque.transform, uiFont, "100 / 100", 13, FontStyle.Bold,
            Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(44f, 3f), new Vector2(180f, 22f));
        AddUiShadow(uiMgr.hpValueText, new Color(0f, 0f, 0f, 0.92f), new Vector2(1f, -1f));

        // Lightning Ammo Pill + Shard Counter
        GameObject lightPillGo = new GameObject("LightningAmmoPill");
        lightPillGo.transform.SetParent(leftPlaque.transform, false);
        Image lightPillImg = lightPillGo.AddComponent<Image>();
        lightPillImg.sprite = a.uiKeycap;
        lightPillImg.type = Image.Type.Sliced;
        lightPillImg.color = new Color(0.32f, 0.88f, 1f, 0.95f);
        lightPillImg.raycastTarget = false;
        RectTransform lightPillRt = lightPillGo.GetComponent<RectTransform>();
        lightPillRt.anchorMin = new Vector2(0.5f, 0.5f);
        lightPillRt.anchorMax = new Vector2(0.5f, 0.5f);
        lightPillRt.anchoredPosition = new Vector2(-12f, -29f);
        lightPillRt.sizeDelta = new Vector2(166f, 28f);
        uiMgr.lightningBadgeBackground = lightPillImg;

        Image boltIcon = CreateUiImage("HudLightningIcon", lightPillGo.transform,
            a.uiLightningIcon != null ? a.uiLightningIcon : a.shardFrames[0],
            new Vector2(0.5f, 0.5f), new Vector2(-62f, 0f), new Vector2(22f, 22f));
        boltIcon.preserveAspect = true;

        uiMgr.lightningAmmoText = CreateText("LightningAmmoText", lightPillGo.transform, uiFont, "3 BOLTS  [E/CLICK]", 12, FontStyle.Bold,
            Color.white, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(14f, 0f), new Vector2(128f, 24f));

        Image shardMini = CreateUiImage("HudShardIcon", leftPlaque.transform, a.shardFrames[0],
            new Vector2(0.5f, 0.5f), new Vector2(96f, -29f), new Vector2(22f, 22f));
        shardMini.preserveAspect = true;
        uiMgr.shardCounterText = CreateText("ShardCounterText", leftPlaque.transform, uiFont, "0/30", 15, FontStyle.Bold,
            goldTrim, TextAnchor.MiddleLeft,
            new Vector2(0.5f, 0.5f), new Vector2(148f, -29f), new Vector2(76f, 24f));

        // Top-center Minimal Zone & Sanctum Progress Plaque
        GameObject missionPlaque = CreateHudPlaque("HudMissionPlaque", hudPanel.transform, a.uiHudBar,
            new Vector2(0.5f, 1f), new Vector2(-6f, -38f), new Vector2(360f, 58f));

        uiMgr.districtObjectiveText = CreateText("DistrictObjectiveText", missionPlaque.transform, uiFont,
            "I · COURTYARDS", 14, FontStyle.Bold, warmWhite, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 11f), new Vector2(340f, 22f));

        GameObject progTrack = new GameObject("SanctumProgressTrack");
        progTrack.transform.SetParent(missionPlaque.transform, false);
        Image trackImg = progTrack.AddComponent<Image>();
        trackImg.sprite = a.uiWhiteSprite;
        trackImg.color = new Color(0.06f, 0.08f, 0.18f, 0.95f);
        trackImg.raycastTarget = false;
        RectTransform trackRt = progTrack.GetComponent<RectTransform>();
        trackRt.anchorMin = new Vector2(0.5f, 0.5f);
        trackRt.anchorMax = new Vector2(0.5f, 0.5f);
        trackRt.anchoredPosition = new Vector2(-20f, -12f);
        trackRt.sizeDelta = new Vector2(246f, 10f);

        GameObject progFill = new GameObject("SanctumProgressFill");
        progFill.transform.SetParent(progTrack.transform, false);
        Image fillImg = progFill.AddComponent<Image>();
        fillImg.sprite = a.uiWhiteSprite;
        fillImg.color = cyanGlow;
        fillImg.raycastTarget = false;
        RectTransform fillRt = progFill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        uiMgr.sanctumProgressFill = fillImg;

        CreateUiImage("BeaconGoalMiniIcon", missionPlaque.transform, a.goalBeaconFrames[0], new Vector2(0.5f, 0.5f), new Vector2(116f, -12f), new Vector2(18f, 18f));
        uiMgr.sanctumProgressPercentText = CreateText("SanctumProgressPercentText", missionPlaque.transform, uiFont,
            "0%", 13, FontStyle.Bold, goldTrim, TextAnchor.MiddleLeft,
            new Vector2(0.5f, 0.5f), new Vector2(148f, -12f), new Vector2(42f, 20f));

        // Top-center Final Boss Multi-Phase Health & Eclipse Ward Shield Plaque
        GameObject bossPlaque = CreateHudPlaque("BossHudPlaque", hudPanel.transform, a.uiHudBar,
            new Vector2(0.5f, 1f), new Vector2(-6f, -114f), new Vector2(468f, 82f));
        uiMgr.bossHudContainer = bossPlaque;
        uiMgr.bossNameText = CreateText("BossNameText", bossPlaque.transform, uiFont,
            "MALAKOR  (120/120)", 14, FontStyle.Bold, new Color(1f, 0.86f, 0.65f, 1f), TextAnchor.MiddleLeft,
            new Vector2(0.5f, 0.5f), new Vector2(-128f, 22f), new Vector2(180f, 22f));

        uiMgr.bossPhaseText = CreateText("BossPhaseText", bossPlaque.transform, uiFont,
            "PHASE I", 12, FontStyle.Bold, goldTrim, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(18f, 22f), new Vector2(142f, 22f));

        uiMgr.bossWardIcons = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject wGo = new GameObject($"WardIcon_{i + 1}");
            wGo.transform.SetParent(bossPlaque.transform, false);
            Image wImg = wGo.AddComponent<Image>();
            wImg.sprite = a.uiWardDiamond != null ? a.uiWardDiamond : a.uiWhiteSprite;
            wImg.preserveAspect = true;
            wImg.color = uiMgr.activeWardColor;
            wImg.raycastTarget = false;
            RectTransform wRt = wGo.GetComponent<RectTransform>();
            wRt.anchorMin = new Vector2(0.5f, 0.5f);
            wRt.anchorMax = new Vector2(0.5f, 0.5f);
            wRt.anchoredPosition = new Vector2(114f + i * 24f, 22f);
            wRt.sizeDelta = new Vector2(20f, 20f);
            uiMgr.bossWardIcons[i] = wImg;
        }

        GameObject bossTrack = new GameObject("BossHealthTrack");
        bossTrack.transform.SetParent(bossPlaque.transform, false);
        Image bTrackImg = bossTrack.AddComponent<Image>();
        bTrackImg.sprite = a.uiWhiteSprite;
        bTrackImg.color = new Color(0.08f, 0.05f, 0.12f, 0.96f);
        bTrackImg.raycastTarget = false;
        RectTransform bTrackRt = bossTrack.GetComponent<RectTransform>();
        bTrackRt.anchorMin = new Vector2(0.5f, 0.5f);
        bTrackRt.anchorMax = new Vector2(0.5f, 0.5f);
        bTrackRt.anchoredPosition = new Vector2(0f, 0f);
        bTrackRt.sizeDelta = new Vector2(414f, 14f);

        GameObject bossTrailGo = new GameObject("BossHealthTrailFill");
        bossTrailGo.transform.SetParent(bossTrack.transform, false);
        Image bTrailImg = bossTrailGo.AddComponent<Image>();
        bTrailImg.sprite = a.uiHpBarFill != null ? a.uiHpBarFill : a.uiWhiteSprite;
        bTrailImg.type = Image.Type.Filled;
        bTrailImg.fillMethod = Image.FillMethod.Horizontal;
        bTrailImg.fillAmount = 1f;
        bTrailImg.color = new Color(1f, 0.86f, 0.54f, 0.90f);
        bTrailImg.raycastTarget = false;
        RectTransform bTrailRt = bossTrailGo.GetComponent<RectTransform>();
        bTrailRt.anchorMin = Vector2.zero;
        bTrailRt.anchorMax = Vector2.one;
        bTrailRt.offsetMin = Vector2.zero;
        bTrailRt.offsetMax = Vector2.zero;
        uiMgr.bossHealthTrailFill = bTrailImg;

        GameObject bossFill = new GameObject("BossHealthFill");
        bossFill.transform.SetParent(bossTrack.transform, false);
        Image bFillImg = bossFill.AddComponent<Image>();
        bFillImg.sprite = a.uiHpBarFill != null ? a.uiHpBarFill : a.uiWhiteSprite;
        bFillImg.type = Image.Type.Filled;
        bFillImg.fillMethod = Image.FillMethod.Horizontal;
        bFillImg.fillAmount = 1f;
        bFillImg.color = new Color(0.86f, 0.16f, 0.28f, 1f);
        bFillImg.raycastTarget = false;
        RectTransform bFillRt = bossFill.GetComponent<RectTransform>();
        bFillRt.anchorMin = Vector2.zero;
        bFillRt.anchorMax = Vector2.one;
        bFillRt.offsetMin = Vector2.zero;
        bFillRt.offsetMax = Vector2.zero;
        uiMgr.bossHealthFill = bFillImg;

        uiMgr.bossStateText = CreateText("BossStateText", bossPlaque.transform, uiFont,
            "BREAK WARD WITH ⚡", 12, FontStyle.Bold, new Color(0.95f, 0.86f, 0.78f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(436f, 20f));
        bossPlaque.SetActive(false);

        // Top-right Audio & Pause Plaque
        GameObject rightPlaque = CreateHudPlaque("HudAudioPlaque", hudPanel.transform, a.uiHudBar,
            new Vector2(1f, 1f), new Vector2(-190f, -38f), new Vector2(356f, 54f));

        uiMgr.hudVolumeSlider = CreateSlider("HudVolumeSlider", rightPlaque.transform, a.uiWhiteSprite,
            new Vector2(0f, 0.5f), new Vector2(68f, 0f), new Vector2(114f, 20f),
            out _);

        Button hudMuteBtn = CreateOrnateButton("HudMuteButton", rightPlaque.transform, a.uiBtnSecondary, uiFont,
            "MUTE", Vector2.zero, new Vector2(98f, 36f), 13, warmWhite);
        RectTransform hudMuteRt = hudMuteBtn.GetComponent<RectTransform>();
        hudMuteRt.anchorMin = new Vector2(1f, 0.5f);
        hudMuteRt.anchorMax = new Vector2(1f, 0.5f);
        hudMuteRt.anchoredPosition = new Vector2(-152f, 0f);
        uiMgr.hudMuteButton = hudMuteBtn;
        uiMgr.hudMuteButtonText = hudMuteBtn.GetComponentInChildren<Text>();
        UnityEventTools.AddPersistentListener(hudMuteBtn.onClick, uiMgr.OnToggleMuteClicked);

        Button pauseBtn = CreateOrnateButton("HudPauseButton", rightPlaque.transform, a.uiBtnPrimary, uiFont,
            "PAUSE", Vector2.zero, new Vector2(90f, 36f), 14, Color.white);
        RectTransform pauseBtnRt = pauseBtn.GetComponent<RectTransform>();
        pauseBtnRt.anchorMin = new Vector2(1f, 0.5f);
        pauseBtnRt.anchorMax = new Vector2(1f, 0.5f);
        pauseBtnRt.anchoredPosition = new Vector2(-54f, 0f);
        uiMgr.hudPauseButton = pauseBtn;
        UnityEventTools.AddPersistentListener(pauseBtn.onClick, gm.TogglePause);

        // Compact Glide badge at bottom-left
        GameObject badgeGo = new GameObject("GlideStatusBadge");
        badgeGo.transform.SetParent(hudPanel.transform, false);
        Image badgeImg = badgeGo.AddComponent<Image>();
        badgeImg.sprite = a.uiHudBar;
        badgeImg.type = Image.Type.Sliced;
        badgeImg.color = Color.white;
        badgeImg.raycastTarget = false;
        RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0f, 0f);
        badgeRt.anchorMax = new Vector2(0f, 0f);
        badgeRt.anchoredPosition = new Vector2(106f, 34f);
        badgeRt.sizeDelta = new Vector2(180f, 38f);
        uiMgr.glideBadgeBackground = badgeImg;

        uiMgr.glideBadgeText = CreateText("GlideBadgeText", badgeGo.transform, uiFont, "CLOAK: READY", 14, FontStyle.Bold,
            Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(168f, 30f));

        // Center-bottom Minimal Whisper Banner
        GameObject bannerGo = new GameObject("BannerContainer");
        bannerGo.transform.SetParent(hudPanel.transform, false);
        Image bannerImg = bannerGo.AddComponent<Image>();
        bannerImg.sprite = a.uiHudBar;
        bannerImg.type = Image.Type.Sliced;
        bannerImg.color = Color.white;
        bannerImg.raycastTarget = false;
        RectTransform bannerRt = bannerGo.GetComponent<RectTransform>();
        bannerRt.anchorMin = new Vector2(0.5f, 0f);
        bannerRt.anchorMax = new Vector2(0.5f, 0f);
        bannerRt.anchoredPosition = new Vector2(80f, 34f);
        bannerRt.sizeDelta = new Vector2(540f, 44f);
        uiMgr.bannerContainer = bannerGo;
        uiMgr.bannerText = CreateText("BannerText", bannerGo.transform, uiFont, "", 15, FontStyle.Bold,
            warmWhite, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(510f, 36f));
        bannerGo.SetActive(false);

        // ============================================================
        // 6. PAUSE PANEL
        // ============================================================
        GameObject pausePanel = CreateFullScreenPanel("PausePanel", canvasGo.transform, a.uiWhiteSprite, new Color(0.03f, 0.04f, 0.11f, 0.78f));
        uiMgr.pausePanel = pausePanel;

        GameObject pauseModal = CreateOrnatePanel("PauseModal", pausePanel.transform, a.uiPanelOrnate, Vector2.zero, new Vector2(490f, 520f));
        Text pauseTitle = CreateText("PauseTitle", pauseModal.transform, uiFont, "PAUSED", 34, FontStyle.Bold,
            cyanGlow, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(440f, 46f));
        AddUiShadow(pauseTitle, new Color(0f, 0f, 0f, 0.85f), new Vector2(2f, -2f));
        CreateUiImage("PauseDivider", pauseModal.transform, a.uiDivider, new Vector2(0.5f, 0.5f), new Vector2(0f, 178f), new Vector2(360f, 16f));

        CreateText("PauseVolLabel", pauseModal.transform, uiFont, "VOLUME", 16, FontStyle.Bold,
            goldTrim, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 146f), new Vector2(260f, 26f));

        uiMgr.pauseVolumeSlider = CreateSlider("PauseVolumeSlider", pauseModal.transform, a.uiWhiteSprite,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 114f), new Vector2(260f, 24f),
            out _);

        Button pauseMuteBtn = CreateOrnateButton("PauseMuteButton", pauseModal.transform, a.uiBtnSecondary, uiFont,
            "MUTE", new Vector2(0f, 64f), new Vector2(240f, 42f), 16, warmWhite);
        uiMgr.pauseMuteButton = pauseMuteBtn;
        uiMgr.pauseMuteButtonText = pauseMuteBtn.GetComponentInChildren<Text>();
        UnityEventTools.AddPersistentListener(pauseMuteBtn.onClick, uiMgr.OnToggleMuteClicked);

        Button resumeBtn = CreateOrnateButton("ResumeButton", pauseModal.transform, a.uiBtnPrimary, uiFont,
            "RESUME", new Vector2(0f, 4f), new Vector2(260f, 52f), 20, Color.white);
        uiMgr.resumeButton = resumeBtn;
        UnityEventTools.AddPersistentListener(resumeBtn.onClick, gm.ResumeGame);

        Button pauseTutBtn = CreateOrnateButton("PauseTutorialButton", pauseModal.transform, a.uiBtnSecondary, uiFont,
            "GUIDE", new Vector2(0f, -56f), new Vector2(260f, 44f), 17, cyanGlow);
        uiMgr.pauseTutorialButton = pauseTutBtn;
        UnityEventTools.AddPersistentListener(pauseTutBtn.onClick, gm.OpenTutorialFromPause);

        Button pauseRestartBtn = CreateOrnateButton("PauseRestartButton", pauseModal.transform, a.uiBtnSecondary, uiFont,
            "RESTART", new Vector2(0f, -110f), new Vector2(260f, 44f), 17, warmWhite);
        uiMgr.pauseRestartButton = pauseRestartBtn;
        UnityEventTools.AddPersistentListener(pauseRestartBtn.onClick, gm.RestartLevel);

        Button pauseTitleBtn = CreateOrnateButton("PauseTitleButton", pauseModal.transform, a.uiBtnSecondary, uiFont,
            "MENU", new Vector2(0f, -164f), new Vector2(260f, 42f), 17, warmWhite);
        uiMgr.pauseTitleButton = pauseTitleBtn;
        UnityEventTools.AddPersistentListener(pauseTitleBtn.onClick, gm.ReturnToTitle);

        Button pauseQuitBtn = CreateOrnateButton("PauseQuitButton", pauseModal.transform, a.uiBtnDanger, uiFont,
            "QUIT", new Vector2(0f, -216f), new Vector2(220f, 40f), 16, warmWhite);
        uiMgr.pauseQuitButton = pauseQuitBtn;
        UnityEventTools.AddPersistentListener(pauseQuitBtn.onClick, gm.QuitGame);

        // ============================================================
        // 7. WIN SCREEN PANEL ("DAWN RESTORED")
        // ============================================================
        GameObject winPanel = CreateFullScreenPanel("WinPanel", canvasGo.transform, a.uiWhiteSprite, new Color(0.03f, 0.05f, 0.14f, 0.80f));
        uiMgr.winPanel = winPanel;

        GameObject winModal = CreateOrnatePanel("WinModal", winPanel.transform, a.uiPanelOrnate, Vector2.zero, new Vector2(680f, 420f));
        Text winTitle = CreateText("WinTitle", winModal.transform, uiFont, "DAWN RESTORED", 40, FontStyle.Bold,
            goldTrim, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 148f), new Vector2(600f, 54f));
        AddUiShadow(winTitle, new Color(0f, 0f, 0f, 0.85f), new Vector2(2f, -2f));
        CreateUiImage("WinDivider", winModal.transform, a.uiDivider, new Vector2(0.5f, 0.5f), new Vector2(0f, 108f), new Vector2(460f, 18f));

        uiMgr.winSummaryText = CreateText("WinSummaryText", winModal.transform, uiFont,
            "RANK S  ·  TWILIGHT LEGEND\n\n⚡ 30/30   ·   ⏱ 00:00   ·   ♥ ×0 RETRIES",
            21, FontStyle.Bold, warmWhite, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(620f, 110f));

        Button winPlayAgainBtn = CreateOrnateButton("WinPlayAgainButton", winModal.transform, a.uiBtnPrimary, uiFont,
            "PLAY AGAIN", new Vector2(0f, -84f), new Vector2(260f, 54f), 20, Color.white);
        uiMgr.winPlayAgainButton = winPlayAgainBtn;
        UnityEventTools.AddPersistentListener(winPlayAgainBtn.onClick, gm.RestartLevel);

        Button winMenuBtn = CreateOrnateButton("WinMenuButton", winModal.transform, a.uiBtnSecondary, uiFont,
            "MENU", new Vector2(0f, -150f), new Vector2(240f, 46f), 18, warmWhite);
        uiMgr.winMenuButton = winMenuBtn;
        UnityEventTools.AddPersistentListener(winMenuBtn.onClick, gm.ReturnToTitle);

        // ============================================================
        // 8. GAME OVER PANEL ("ECLIPSE FALLS")
        // ============================================================
        GameObject gameOverPanel = CreateFullScreenPanel("GameOverPanel", canvasGo.transform, a.uiWhiteSprite, new Color(0.09f, 0.02f, 0.06f, 0.82f));
        uiMgr.gameOverPanel = gameOverPanel;

        GameObject goModal = CreateOrnatePanel("GameOverModal", gameOverPanel.transform, a.uiPanelOrnate, Vector2.zero, new Vector2(580f, 400f));
        Text goTitle = CreateText("GameOverTitle", goModal.transform, uiFont, "ECLIPSE FALLS", 38, FontStyle.Bold,
            new Color(1f, 0.40f, 0.50f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 134f), new Vector2(520f, 52f));
        AddUiShadow(goTitle, new Color(0f, 0f, 0f, 0.85f), new Vector2(2f, -2f));
        CreateUiImage("GameOverDivider", goModal.transform, a.uiDivider, new Vector2(0.5f, 0.5f), new Vector2(0f, 96f), new Vector2(400f, 16f));

        CreateText("GameOverHint", goModal.transform, uiFont,
            "Rise at the last Watchtower.", 18, FontStyle.Normal,
            warmWhite, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 56f), new Vector2(480f, 34f));

        Button retryCpBtn = CreateOrnateButton("RetryCheckpointButton", goModal.transform, a.uiBtnPrimary, uiFont,
            "RETRY", new Vector2(0f, -14f), new Vector2(260f, 54f), 20, Color.white);
        uiMgr.retryCheckpointButton = retryCpBtn;
        UnityEventTools.AddPersistentListener(retryCpBtn.onClick, gm.RetryFromCheckpoint);

        Button goRestartBtn = CreateOrnateButton("GameOverRestartButton", goModal.transform, a.uiBtnSecondary, uiFont,
            "RESTART", new Vector2(0f, -80f), new Vector2(240f, 46f), 18, warmWhite);
        uiMgr.gameOverRestartButton = goRestartBtn;
        UnityEventTools.AddPersistentListener(goRestartBtn.onClick, gm.RestartLevel);

        Button goTitleBtn = CreateOrnateButton("GameOverTitleButton", goModal.transform, a.uiBtnDanger, uiFont,
            "MENU", new Vector2(0f, -140f), new Vector2(220f, 42f), 17, warmWhite);
        uiMgr.gameOverTitleButton = goTitleBtn;
        UnityEventTools.AddPersistentListener(goTitleBtn.onClick, gm.ReturnToTitle);

        // Initial visible panel in Scene (Title active, others inactive)
        titlePanel.SetActive(true);
        storyIntroPanel.SetActive(false);
        lorePanel.SetActive(false);
        tutorialPanel.SetActive(false);
        hudPanel.SetActive(false);
        pausePanel.SetActive(false);
        winPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }

    private static GameObject CreateOrnatePanel(string name, Transform parent, Sprite slicedSprite, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.sprite = slicedSprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        img.raycastTarget = true;

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return panel;
    }

    private static GameObject CreateHudPlaque(string name, Transform parent, Sprite slicedSprite, Vector2 anchor, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.sprite = slicedSprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        img.raycastTarget = false;

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return panel;
    }

    private static Image CreateUiImage(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return img;
    }

    private static Button CreateOrnateButton(
        string name,
        Transform parent,
        Sprite slicedSprite,
        Font font,
        string label,
        Vector2 anchoredPos,
        Vector2 sizeDelta,
        int fontSize,
        Color textColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.sprite = slicedSprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.16f, 1.14f, 1.08f, 1f);
        cb.pressedColor = new Color(0.76f, 0.78f, 0.86f, 1f);
        cb.disabledColor = new Color(0.45f, 0.45f, 0.55f, 0.65f);
        btn.colors = cb;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Text labelTxt = CreateText("Label", go.transform, font, label, fontSize, FontStyle.Bold, textColor,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, sizeDelta);
        AddUiShadow(labelTxt, new Color(0f, 0f, 0f, 0.85f), new Vector2(1.5f, -1.5f));

        return btn;
    }

    private static void CreateVisualTutorialCard(
        Transform parent,
        BuiltAssets a,
        Font uiFont,
        string name,
        Vector2 anchoredPos,
        Sprite iconSprite,
        Vector2 iconSize,
        string title,
        string keyLabel,
        string description,
        Color accentColor,
        Sprite[] animFrames = null,
        float fps = 8f,
        float bobY = 0f,
        float slideX = 0f)
    {
        GameObject card = CreateHudPlaque(name, parent, a.uiHudBar, new Vector2(0.5f, 0.5f), anchoredPos, new Vector2(308f, 174f));

        Image iconImg = CreateUiImage("Icon", card.transform, iconSprite, new Vector2(0.5f, 0.5f), new Vector2(-92f, 18f), iconSize);
        iconImg.preserveAspect = true;
        if ((animFrames != null && animFrames.Length > 0) || bobY > 0.01f || slideX > 0.01f)
        {
            AnimatedUiSprite anim = iconImg.gameObject.AddComponent<AnimatedUiSprite>();
            anim.frames = animFrames;
            anim.fps = fps;
            anim.bobAmplitudeY = bobY;
            anim.bobFrequency = 2.6f;
            anim.slideAmplitudeX = slideX;
            anim.slideFrequency = 2.1f;
        }

        // Keycap pill at top-right of card
        GameObject capGo = new GameObject("KeycapBadge");
        capGo.transform.SetParent(card.transform, false);
        Image capImg = capGo.AddComponent<Image>();
        capImg.sprite = a.uiKeycap;
        capImg.type = Image.Type.Sliced;
        capImg.raycastTarget = false;
        RectTransform capRt = capGo.GetComponent<RectTransform>();
        capRt.anchorMin = new Vector2(0.5f, 0.5f);
        capRt.anchorMax = new Vector2(0.5f, 0.5f);
        capRt.anchoredPosition = new Vector2(46f, 46f);
        capRt.sizeDelta = new Vector2(158f, 34f);

        CreateText("KeyLabel", capGo.transform, uiFont, keyLabel, 13, FontStyle.Bold,
            new Color(1f, 0.90f, 0.52f, 1f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(148f, 28f));

        CreateText("CardTitle", card.transform, uiFont, title, 16, FontStyle.Bold,
            accentColor, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(54f, 6f), new Vector2(174f, 30f));

        CreateText("CardDesc", card.transform, uiFont, description, 14, FontStyle.Normal,
            new Color(0.90f, 0.93f, 1f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -48f), new Vector2(276f, 46f));
    }

    private static void AddUiShadow(Text txt, Color shadowColor, Vector2 distance)
    {
        if (txt == null) return;
        Shadow s = txt.gameObject.AddComponent<Shadow>();
        s.effectColor = shadowColor;
        s.effectDistance = distance;
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

        // 1. Capture Title Screen over live Zone 1 Courtyard (District Arch, Lamps, Railings, Canal)
        if (uiMgr != null)
        {
            uiMgr.ShowOnlyPanel(GameState.Title);
        }
        CaptureCameraView(cam, new Vector3(7.0f, 1.4f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/title_screen.png");

        // 2. Capture Interactive Story Prologue Card 1
        if (uiMgr != null)
        {
            uiMgr.OpenStoryIntro();
        }
        CaptureCameraView(cam, new Vector3(7.0f, 1.4f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/story_intro_preview.png");

        // 3. Capture Dedicated 6-Card Visual Tutorial Screen ("Courier Field Guide")
        if (uiMgr != null)
        {
            uiMgr.ShowOnlyPanel(GameState.Tutorial);
        }
        CaptureCameraView(cam, new Vector3(7.0f, 1.4f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/tutorial_screen_preview.png");

        // 4. Capture Zone 1 Gameplay + Cool Portrait/HP/Lives/Lightning HUD
        if (uiMgr != null)
        {
            uiMgr.ShowOnlyPanel(GameState.Playing);
            uiMgr.UpdateObjective("I · COURTYARDS", string.Empty);
            uiMgr.UpdateSanctumProgress(0.06f);
            uiMgr.UpdateHealth(100, 100);
            uiMgr.UpdateLives(3, 5);
            uiMgr.UpdateLightningAmmo(5, 2, 30);
            if (uiMgr.bannerContainer != null)
            {
                uiMgr.bannerContainer.SetActive(false);
            }
        }
        CaptureCameraView(cam, new Vector3(8.5f, 1.2f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/gameplay_preview.png");

        // 5. Capture Zone 2: Rooftop Thermal Vent Updraft, Gothic Pillars, Pop-Up Ground Spikes erupting, & Pylon-Anchored Moving Lift
        if (uiMgr != null)
        {
            uiMgr.UpdateGlideStatus(false, false);
            uiMgr.UpdateHealth(85, 100);
            uiMgr.UpdateLives(3, 5);
            uiMgr.UpdateLightningAmmo(6, 10, 30);
            uiMgr.UpdateObjective("II · AQUEDUCT", string.Empty);
            uiMgr.UpdateSanctumProgress(0.32f);
            if (uiMgr.bannerContainer != null) uiMgr.bannerContainer.SetActive(false);
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
        CaptureCameraView(cam, new Vector3(64.5f, 2.4f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/zone2_traps_and_moving_preview.png");

        if (zone2Trap != null)
        {
            zone2Trap.SetPreviewExtension(0.0f);
            if (zone2Trap.sparkParticles != null)
            {
                zone2Trap.sparkParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        // 6. Capture Zone 3A: Broken Arch Corbels, Crumbling Stone Span, Pendulums & Canal
        if (uiMgr != null)
        {
            uiMgr.UpdateHealth(90, 100);
            uiMgr.UpdateLives(4, 5);
            uiMgr.UpdateLightningAmmo(8, 18, 30);
            uiMgr.UpdateObjective("III · FOUNDRY", string.Empty);
            uiMgr.UpdateSanctumProgress(0.58f);
            if (uiMgr.bannerContainer != null) uiMgr.bannerContainer.SetActive(false);
        }
        CaptureCameraView(cam, new Vector3(131.5f, 2.6f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/zone3_crumble_span_preview.png");

        // 7. Capture Zone 3B: Dual Gothic Chain-Anchored Moving Platforms + Relic Perch
        if (uiMgr != null)
        {
            uiMgr.UpdateLightningAmmo(9, 21, 30);
            uiMgr.UpdateSanctumProgress(0.70f);
        }
        CaptureCameraView(cam, new Vector3(154.5f, 2.2f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/zone3_dynamic_platforms_preview.png");

        // 8A. Capture Zone 4 Chasm: Twin Grounded Canal Steam Chimneys & Ruined Watchtower Crumbling Perch
        if (uiMgr != null)
        {
            uiMgr.UpdateLightningAmmo(7, 26, 30);
            uiMgr.UpdateObjective("IV · SANCTUM", string.Empty);
            uiMgr.UpdateSanctumProgress(0.88f);
        }
        CaptureCameraView(cam, new Vector3(200.5f, 3.8f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/zone4_chimney_ascent_preview.png");

        // 8B. Capture Zone 4 Summit: Malakor Guarding Sealed Sanctum Beacon & Arena Thermal Vents
        FinalBossAI boss = UnityEngine.Object.FindFirstObjectByType<FinalBossAI>();
        Vector3 origBossPos = boss != null ? boss.transform.position : Vector3.zero;
        if (uiMgr != null)
        {
            uiMgr.UpdateHealth(100, 100);
            uiMgr.UpdateLives(4, 5);
            uiMgr.UpdateLightningAmmo(9, 30, 30);
            uiMgr.UpdateObjective("IV · SANCTUM", string.Empty);
            uiMgr.UpdateSanctumProgress(0.96f);
            uiMgr.UpdateBossHud(true, 120, 120, 4, 4, 1, "BREAK WARD WITH ⚡");
        }
        if (zone4Trap != null)
        {
            zone4Trap.SetPreviewExtension(0.0f);
        }
        CaptureCameraView(cam, new Vector3(231.5f, 11.2f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/level_goal_preview.png");

        // 8C. Capture Final Boss Phase II Enraged Attack + Sky Spears + Vesper Shooting Lightning Bolt
        if (boss != null && player != null && playerSr != null && assets != null)
        {
            boss.SetEditorPreviewPose(FinalBossAI.BossState.Casting, -1f, new Vector3(227.5f, 9.0f, 0f), true);
            if (boss.slamParticles != null)
            {
                boss.slamParticles.Play();
                boss.slamParticles.Simulate(0.18f, true, true);
            }

            GameObject previewWave = new GameObject("PreviewBossShockwave");
            previewWave.transform.position = new Vector3(222.0f, 9.05f, 0f);
            previewWave.transform.localScale = new Vector3(-1f, 1f, 1f);
            SpriteRenderer waveSr = previewWave.AddComponent<SpriteRenderer>();
            waveSr.sprite = assets.vfxBossShockwave;
            waveSr.sortingOrder = 6;

            GameObject previewSpear1 = new GameObject("PreviewSkySpear1");
            previewSpear1.transform.position = new Vector3(219.5f, 12.8f, 0f);
            SpriteRenderer sp1Sr = previewSpear1.AddComponent<SpriteRenderer>();
            sp1Sr.sprite = assets.vfxBossSkySpear;
            sp1Sr.sortingOrder = 6;

            GameObject previewSpear2 = new GameObject("PreviewSkySpear2");
            previewSpear2.transform.position = new Vector3(231.0f, 11.9f, 0f);
            SpriteRenderer sp2Sr = previewSpear2.AddComponent<SpriteRenderer>();
            sp2Sr.sprite = assets.vfxBossSkySpear;
            sp2Sr.sortingOrder = 6;

            GameObject previewBolt = new GameObject("PreviewPlayerBolt");
            previewBolt.transform.position = new Vector3(222.6f, 11.8f, 0f);
            SpriteRenderer boltSr = previewBolt.AddComponent<SpriteRenderer>();
            boltSr.sprite = assets.vfxPlayerLightningBolt;
            boltSr.sortingOrder = 7;

            player.transform.position = new Vector3(218.2f, 11.8f, 0f);
            playerSr.sprite = assets.vesperGlideFrames != null && assets.vesperGlideFrames.Length > 1 ? assets.vesperGlideFrames[1] : assets.vesperGlide;

            if (uiMgr != null)
            {
                uiMgr.UpdateHealth(72, 100);
                uiMgr.UpdateLightningAmmo(7, 30, 30);
                uiMgr.UpdateBossHud(true, 62, 120, 2, 4, 2, "ENRAGED · SKY SPEARS!");
                if (uiMgr.bannerContainer != null) uiMgr.bannerContainer.SetActive(false);
            }
            CaptureCameraView(cam, new Vector3(224.0f, 11.2f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/boss_attack_preview.png");

            UnityEngine.Object.DestroyImmediate(previewWave);
            UnityEngine.Object.DestroyImmediate(previewSpear1);
            UnityEngine.Object.DestroyImmediate(previewSpear2);
            UnityEngine.Object.DestroyImmediate(previewBolt);
            if (boss.slamParticles != null)
            {
                boss.slamParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            // 8D. Capture Final Boss Phase III Stunned State + Exposed Crown + Vesper Diving for Stomp
            boss.SetEditorPreviewPose(FinalBossAI.BossState.Stunned, -1f, new Vector3(225.0f, 9.0f, 0f), false);
            if (boss.stunLoopParticles != null)
            {
                boss.stunLoopParticles.Play();
                boss.stunLoopParticles.Simulate(0.45f, true, true);
            }
            player.transform.position = new Vector3(225.0f, 12.6f, 0f);
            playerSr.sprite = assets.vesperFallFrames != null && assets.vesperFallFrames.Length > 1 ? assets.vesperFallFrames[1] : assets.vesperFall;

            if (uiMgr != null)
            {
                uiMgr.UpdateHealth(65, 100);
                uiMgr.UpdateLightningAmmo(4, 30, 30);
                uiMgr.UpdateBossHud(true, 34, 120, 0, 4, 3, "STUNNED! STOMP CROWN!");
                if (uiMgr.bannerContainer != null) uiMgr.bannerContainer.SetActive(false);
            }
            CaptureCameraView(cam, new Vector3(224.0f, 11.2f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/boss_stunned_preview.png");

            if (boss.stunLoopParticles != null)
            {
                boss.stunLoopParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            boss.RestoreEditorPreviewPose(origBossPos);
            player.transform.position = origPlayerPos;
            playerSr.sprite = origPlayerSprite;
        }
        if (uiMgr != null)
        {
            uiMgr.UpdateBossHud(false, 120, 120, 4, 4, 1, string.Empty);
        }

        // 9. Capture Dawn Restored Victory Screen Epilogue
        if (uiMgr != null)
        {
            uiMgr.PopulateWinStats(30, 30, 142f, 0);
            uiMgr.ShowOnlyPanel(GameState.Victory);
        }
        CaptureCameraView(cam, new Vector3(236.0f, 10.2f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/win_screen_preview.png");

        // Also recapture Zone 1 Glide action with Vesper_Glide ribbon trail
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
                uiMgr.ShowOnlyPanel(GameState.Playing);
                uiMgr.UpdateObjective("I · COURTYARDS", string.Empty);
                uiMgr.UpdateSanctumProgress(0.14f);
                uiMgr.UpdateHealth(100, 100);
                uiMgr.UpdateLives(3, 5);
                uiMgr.UpdateGlideStatus(true, true);
                uiMgr.UpdateLightningAmmo(5, 5, 30);
                if (uiMgr.bannerContainer != null) uiMgr.bannerContainer.SetActive(false);
            }
        }
        CaptureCameraView(cam, new Vector3(35.5f, 1.8f, -10f), baseCamPos, parallaxLayers, origLayerPos, "Screenshots/glide_and_particles_preview.png");

        // Restore player, camera, parallax, and Initial Title UI state cleanly
        if (player != null && playerSr != null)
        {
            player.transform.position = origPlayerPos;
            playerSr.sprite = origPlayerSprite;
            if (player.glideTrailParticles != null)
            {
                player.glideTrailParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        cam.transform.position = baseCamPos;
        for (int i = 0; i < parallaxLayers.Length; i++)
        {
            parallaxLayers[i].transform.position = origLayerPos[i];
        }
        if (uiMgr != null)
        {
            uiMgr.UpdateHealth(100, 100);
            uiMgr.UpdateLives(3, 5);
            uiMgr.UpdateLightningAmmo(3, 0, 30);
            uiMgr.UpdateSanctumProgress(0f);
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

        byte[] pngBytes = tex.EncodeToPNG();
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
                File.WriteAllBytes(outputPath, pngBytes);
                break;
            }
            catch (IOException) when (attempt < 4)
            {
                System.Threading.Thread.Sleep(150);
            }
        }

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
