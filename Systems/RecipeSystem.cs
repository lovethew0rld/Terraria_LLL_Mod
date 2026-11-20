using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using LLL;

namespace LLL.Systems
{
    /// <summary>
    /// 合成系统：定义配方组（RecipeGroups）和大量自定义配方。
    /// 同时支持"合成材料倍率调整"功能（由 LLLServerConfig 控制）。
    /// 
    /// 【配方组的作用】
    /// 将多个同类物品归为一组，在配方中可通用。
    /// 例如："IronBar" 组包含铁锭和铅锭，配方中用 AddRecipeGroup("IronBar", 10) 即可。
    /// 
    /// 【自定义配方】
    /// 本模组添加了大量原版物品的可合成配方，让原本只能通过探索/掉落获得的物品
    /// 变为可合成，降低随机性依赖。
    /// 
    /// 【合成材料倍率调整】
    /// 如果 EnableCraftingCostTweak 为 true，所有配方的材料消耗会乘以
    /// CraftingCostMultiplier / 100 的倍率（向上取整，最少为1）。
    /// </summary>
    public class RecipeSystem : ModSystem
    {
        // ═══════════════════════════════════════════════════════════════
        // 📦 配方组注册（AddRecipeGroups）
        // ═══════════════════════════════════════════════════════════════
        public override void AddRecipeGroups()
        {
            // ─── 草种组 ──────────────────────────────────────────────
            // 包含所有类型的草种/花种，用于合成草药袋等。
            var grassSeedGroup = new RecipeGroup(() => "任意草种",
                ItemID.Seed,
                ItemID.MushroomGrassSeeds,
                ItemID.JungleGrassSeeds,
                ItemID.GrassSeeds,
                ItemID.CrimsonSeeds,
                ItemID.CorruptSeeds,
                ItemID.AshGrassSeeds,
                ItemID.ShiverthornSeeds,
                ItemID.HallowedSeeds,
                ItemID.DaybloomSeeds,
                ItemID.MoonglowSeeds,
                ItemID.BlinkrootSeeds,
                ItemID.DeathweedSeeds,
                ItemID.WaterleafSeeds,
                ItemID.FireblossomSeeds
            );
            RecipeGroup.RegisterGroup("AnyGrassSeed", grassSeedGroup);

            // ─── 金属锭组（铁/铅、金/铂金、银/钨） ────────────────
            // 原版世界中，铁和金对应两种矿石，用配方组可以自动适配。
            // 检查是否已存在，避免重复注册。
            if (!RecipeGroup.recipeGroupIDs.ContainsKey("IronBar"))
            {
                var ironBarGroup = new RecipeGroup(() => Lang.misc[37] + " " + Lang.GetItemNameValue(ItemID.IronBar),
                    ItemID.IronBar, ItemID.LeadBar);
                RecipeGroup.RegisterGroup("IronBar", ironBarGroup);
            }

            if (!RecipeGroup.recipeGroupIDs.ContainsKey("GoldBar"))
            {
                var goldBarGroup = new RecipeGroup(() => Lang.misc[37] + " " + Lang.GetItemNameValue(ItemID.GoldBar),
                    ItemID.GoldBar, ItemID.PlatinumBar);
                RecipeGroup.RegisterGroup("GoldBar", goldBarGroup);
            }

            if (!RecipeGroup.recipeGroupIDs.ContainsKey("SilverBar"))
            {
                var silverBarGroup = new RecipeGroup(() => Lang.misc[37] + " " + Lang.GetItemNameValue(ItemID.SilverBar),
                    ItemID.SilverBar, ItemID.TungstenBar);
                RecipeGroup.RegisterGroup("SilverBar", silverBarGroup);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 🛠️ 自定义配方注册（AddRecipes）
        // ═══════════════════════════════════════════════════════════════
        // 注：这里添加了大量配方，将原本只能靠探索/掉落获得的物品变为可合成。
        // 每个配方都使用了 DisableDecraft() 防止玩家反向拆解（避免破坏平衡）。
        //
        // 【配方格式说明】
        // Recipe.Create(目标物品ID)
        //   .AddIngredient(原料ID, 数量)    // 添加原料
        //   .AddRecipeGroup("组名", 数量)   // 添加配方组
        //   .AddTile(工作台ID)              // 所需合成站
        //   .AddCondition(条件)             // 可选条件（如 DownedPlantera）
        //   .DisableDecraft()               // 禁止反向拆解
        //   .Register();                    // 注册到游戏
        //
        // 【新人提示】每个配方末尾的 .DisableDecraft() 是可选的，
        // 如果不加，玩家可以在"微光"中反向拆解物品（可能破坏游戏平衡）。
        // ================================================================
        public override void AddRecipes()
        {
            // ─── 探索辅助饰品 ───────────────────────────────────────
            Recipe.Create(ItemID.Compass)
                .AddRecipeGroup("GoldBar", 6)
                .AddRecipeGroup("IronBar", 10)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.DepthMeter)
                .AddRecipeGroup("SilverBar", 6)
                .AddRecipeGroup("IronBar", 10)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.Radar)
                .AddRecipeGroup("IronBar", 10)
                .AddIngredient(ItemID.Glass, 5)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.LifeformAnalyzer)
                .AddRecipeGroup("IronBar", 10)
                .AddIngredient(ItemID.Glass, 2)
                .AddIngredient(ItemID.Wire, 5)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.TallyCounter)
                .AddRecipeGroup("IronBar", 10)
                .AddIngredient(ItemID.Glass, 2)
                .AddIngredient(ItemID.Bone, 5)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.MetalDetector)
                .AddRecipeGroup("IronBar", 5)
                .AddRecipeGroup("GoldBar", 2)
                .AddIngredient(ItemID.Wire, 5)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            // ─── 移动类饰品 ──────────────────────────────────────────
            Recipe.Create(ItemID.IceSkates)
                .AddRecipeGroup("SilverBar", 6)
                .AddRecipeGroup("IronBar", 10)
                .AddIngredient(ItemID.SnowBlock, 10)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.Aglet)
                .AddRecipeGroup("IronBar", 4)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.AnkletoftheWind)
                .AddIngredient(ItemID.Vine, 2)
                .AddIngredient(ItemID.JungleSpores, 8)
                .AddTile(TileID.WorkBenches)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.LuckyHorseshoe)
                .AddRecipeGroup("GoldBar", 5)
                .AddIngredient(ItemID.Cloud, 10)
                .AddIngredient(ItemID.Feather, 5)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.ShinyRedBalloon)
                .AddIngredient(ItemID.Gel, 10)
                .AddIngredient(ItemID.Cloud, 10)
                .AddIngredient(ItemID.WhiteString, 5)
                .AddTile(TileID.WorkBenches)
                .DisableDecraft()
                .Register();

            // ─── 瓶子类饰品（云朵瓶循环合成） ──────────────────────
            Recipe.Create(ItemID.LavaCharm)
                .AddIngredient(ItemID.MagmaStone, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.CloudinaBottle)
                .AddIngredient(ItemID.SandstorminaBottle, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.BlizzardinaBottle)
                .AddIngredient(ItemID.CloudinaBottle, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.SandstorminaBottle)
                .AddIngredient(ItemID.BlizzardinaBottle, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            // ─── 生命/魔力类 ──────────────────────────────────────────
            Recipe.Create(ItemID.BandofRegeneration)
                .AddRecipeGroup("IronBar", 2)
                .AddIngredient(ItemID.LifeCrystal, 1)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.ManaRegenerationBand)
                .AddRecipeGroup("IronBar", 2)
                .AddIngredient(ItemID.ManaCrystal, 1)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            // ─── DPS/信息类饰品 ──────────────────────────────────────
            Recipe.Create(ItemID.Stopwatch)
                .AddRecipeGroup("IronBar", 5)
                .AddIngredient(ItemID.PlatinumWatch, 1)
                .AddIngredient(ItemID.Wire, 5)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.Stopwatch)
                .AddRecipeGroup("IronBar", 5)
                .AddIngredient(ItemID.GoldWatch, 1)
                .AddIngredient(ItemID.Wire, 5)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.DPSMeter)
                .AddRecipeGroup("IronBar", 5)
                .AddIngredient(ItemID.Glass, 2)
                .AddIngredient(ItemID.Wire, 5)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            // ─── 钓鱼信息饰品（三件循环） ──────────────────────────
            Recipe.Create(ItemID.FishermansGuide)
                .AddIngredient(ItemID.Sextant, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.WeatherRadio)
                .AddIngredient(ItemID.FishermansGuide, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.Sextant)
                .AddIngredient(ItemID.WeatherRadio, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            // ─── 盾牌类 ──────────────────────────────────────────────
            Recipe.Create(ItemID.CobaltShield)
                .AddIngredient(ItemID.CobaltBar, 10)
                .AddTile(TileID.MythrilAnvil)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.CobaltShield)
                .AddIngredient(ItemID.PalladiumBar, 10)
                .AddTile(TileID.MythrilAnvil)
                .DisableDecraft()
                .Register();

            // ─── 其他饰品 ────────────────────────────────────────────
            Recipe.Create(ItemID.SharkToothNecklace)
                .AddRecipeGroup("IronBar", 5)
                .AddIngredient(ItemID.Chain, 2)
                .AddIngredient(ItemID.SharkFin, 3)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.Leather)
                .AddIngredient(ItemID.Gel, 1)
                .AddIngredient(ItemID.Silk, 1)
                .AddTile(TileID.WorkBenches)
                .DisableDecraft()
                .Register();

            // ─── 钓鱼饰品循环 ──────────────────────────────────────
            Recipe.Create(ItemID.AnglerEarring)
                .AddIngredient(ItemID.HighTestFishingLine, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.TackleBox)
                .AddIngredient(ItemID.AnglerEarring, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.HighTestFishingLine)
                .AddIngredient(ItemID.TackleBox, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.LavaFishingHook)
                .AddIngredient(ItemID.HellstoneBar, 2)
                .AddIngredient(ItemID.Chain, 1)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            // ─── 建筑/材料类 ──────────────────────────────────────
            Recipe.Create(ItemID.MushroomGrassSeeds)
                .AddIngredient(ItemID.GlowingMushroom, 5)
                .AddTile(TileID.Bottles)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.PocketMirror)
                .AddIngredient(ItemID.Marble, 20)
                .AddIngredient(ItemID.Glass, 15)
                .AddTile(TileID.WorkBenches)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.PaintSprayer)
                .AddRecipeGroup("IronBar", 5)
                .AddIngredient(ItemID.Gel, 5)
                .AddIngredient(ItemID.Bottle, 2)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.ExtendoGrip)
                .AddRecipeGroup("IronBar", 5)
                .AddRecipeGroup("Wood", 5)
                .AddIngredient(ItemID.Chain, 2)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.PortableCementMixer)
                .AddRecipeGroup("IronBar", 15)
                .AddRecipeGroup("Wood", 5)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.BrickLayer)
                .AddRecipeGroup("IronBar", 3)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            // ─── Boss召唤物 / 事件触发 ──────────────────────────────
            Recipe.Create(ItemID.QueenSlimeCrystal)
                .AddIngredient(ItemID.PinkGel, 5)
                .AddIngredient(ItemID.CrystalShard, 5)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.BloodMoonStarter)
                .AddIngredient(ItemID.LifeCrystal, 1)
                .AddIngredient(ItemID.HealingPotion, 2)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.GuideVoodooDoll)
                .AddIngredient(ItemID.Silk, 5)
                .AddIngredient(ItemID.Bone, 5)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            // ─── 地牢钥匙武器（可用钥匙合成） ──────────────────────
            Recipe.Create(ItemID.ScourgeoftheCorruptor)
                .AddIngredient(ItemID.CorruptionKey, 1)
                .AddIngredient(ItemID.Ectoplasm, 20)
                .AddIngredient(ItemID.Bone, 20)
                .AddTile(TileID.MythrilAnvil)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.VampireKnives)
                .AddIngredient(ItemID.CrimsonKey, 1)
                .AddIngredient(ItemID.Ectoplasm, 20)
                .AddIngredient(ItemID.Bone, 20)
                .AddTile(TileID.MythrilAnvil)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.RainbowGun)
                .AddIngredient(ItemID.HallowedKey, 1)
                .AddIngredient(ItemID.Ectoplasm, 20)
                .AddIngredient(ItemID.Bone, 20)
                .AddTile(TileID.MythrilAnvil)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.StaffoftheFrostHydra)
                .AddIngredient(ItemID.FrozenKey, 1)
                .AddIngredient(ItemID.Ectoplasm, 20)
                .AddIngredient(ItemID.Bone, 20)
                .AddTile(TileID.MythrilAnvil)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.StormTigerStaff)
                .AddIngredient(ItemID.DungeonDesertKey, 1)
                .AddIngredient(ItemID.Ectoplasm, 20)
                .AddIngredient(ItemID.Bone, 20)
                .AddTile(TileID.MythrilAnvil)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.PiranhaGun)
                .AddIngredient(ItemID.JungleKey, 1)
                .AddIngredient(ItemID.Ectoplasm, 20)
                .AddIngredient(ItemID.Bone, 20)
                .AddTile(TileID.MythrilAnvil)
                .DisableDecraft()
                .Register();

            // ─── 腐化/猩红互相转化 ──────────────────────────────────
            Recipe.Create(ItemID.DemoniteOre)
                .AddIngredient(ItemID.CrimtaneOre, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.CrimtaneOre)
                .AddIngredient(ItemID.DemoniteOre, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.DemoniteBar)
                .AddIngredient(ItemID.CrimtaneBar, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.CrimtaneBar)
                .AddIngredient(ItemID.DemoniteBar, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            // ─── 腐化/猩红武器互相转换 ──────────────────────────────
            Recipe.Create(ItemID.VampireKnives)
                .AddIngredient(ItemID.ScourgeoftheCorruptor, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.ScourgeoftheCorruptor)
                .AddIngredient(ItemID.VampireKnives, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            // ─── 材料互相转化 ──────────────────────────────────────
            Recipe.Create(ItemID.TissueSample)
                .AddIngredient(ItemID.ShadowScale, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.ShadowScale)
                .AddIngredient(ItemID.TissueSample, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            // ─── 特殊材料合成 ──────────────────────────────────────
            Recipe.Create(ItemID.BlackLens)
                .AddIngredient(ItemID.Lens, 5)
                .AddTile(TileID.WorkBenches)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.PinkGel)
                .AddIngredient(ItemID.Gel, 5)
                .AddTile(TileID.WorkBenches)
                .DisableDecraft()
                .Register();

            // ─── 松露虫合成（多种鱼饵配方） ────────────────────────
            Recipe.Create(ItemID.TruffleWorm)
                .AddIngredient(ItemID.GlowingMushroom, 5)
                .AddIngredient(ItemID.MasterBait, 5)
                .AddTile(TileID.Bottles)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.TruffleWorm)
                .AddIngredient(ItemID.GlowingMushroom, 5)
                .AddIngredient(ItemID.JourneymanBait, 10)
                .AddTile(TileID.Bottles)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.TruffleWorm)
                .AddIngredient(ItemID.GlowingMushroom, 5)
                .AddIngredient(ItemID.ApprenticeBait, 20)
                .AddTile(TileID.Bottles)
                .DisableDecraft()
                .Register();

            // ─── 光魂/暗魂互相转化 ──────────────────────────────────
            Recipe.Create(ItemID.SoulofLight)
                .AddIngredient(ItemID.SoulofNight, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.SoulofNight)
                .AddIngredient(ItemID.SoulofLight, 1)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();

            // ─── 灵液与三魂互相转化（需击败世纪之花） ──────────────
            // AddCondition(Condition.DownedPlantera) 确保只有击败世纪之花后可用
            Recipe.Create(ItemID.Ectoplasm)
                .AddIngredient(ItemID.SoulofFright, 1)
                .AddTile(TileID.DemonAltar)
                .AddCondition(Condition.DownedPlantera)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.Ectoplasm)
                .AddIngredient(ItemID.SoulofSight, 1)
                .AddTile(TileID.DemonAltar)
                .AddCondition(Condition.DownedPlantera)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.Ectoplasm)
                .AddIngredient(ItemID.SoulofMight, 1)
                .AddTile(TileID.DemonAltar)
                .AddCondition(Condition.DownedPlantera)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.SoulofFright)
                .AddIngredient(ItemID.Ectoplasm, 1)
                .AddTile(TileID.DemonAltar)
                .AddCondition(Condition.DownedPlantera)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.SoulofSight)
                .AddIngredient(ItemID.Ectoplasm, 1)
                .AddTile(TileID.DemonAltar)
                .AddCondition(Condition.DownedPlantera)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.SoulofMight)
                .AddIngredient(ItemID.Ectoplasm, 1)
                .AddTile(TileID.DemonAltar)
                .AddCondition(Condition.DownedPlantera)
                .DisableDecraft()
                .Register();

            // ─── 杂项配方 ────────────────────────────────────────────
            Recipe.Create(ItemID.SliceOfCake)
                .AddIngredient(ItemID.Pumpkin, 5)
                .AddIngredient(ItemID.IceBlock, 5)
                .AddIngredient(ItemID.SnowBlock, 5)
                .AddRecipeGroup("Fruit", 5)
                .AddTile(TileID.WorkBenches)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.HerbBag)
                .AddIngredient(ItemID.Silk, 5)
                .AddRecipeGroup("AnyGrassSeed", 5)
                .AddTile(TileID.WorkBenches)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.CanOfWorms)
                .AddIngredient(ItemID.Worm, 5)
                .AddRecipeGroup("IronBar", 3)
                .AddIngredient(ItemID.DirtBlock, 5)
                .AddTile(TileID.WorkBenches)
                .DisableDecraft()
                .Register();

            // ─── 镜子合成 ────────────────────────────────────────────
            Recipe.Create(ItemID.MagicMirror)
                .AddRecipeGroup("IronBar", 10)
                .AddRecipeGroup("SilverBar", 5)
                .AddIngredient(ItemID.Glass, 15)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.IceMirror)
                .AddIngredient(ItemID.IceBlock, 15)
                .AddIngredient(ItemID.SnowBlock, 15)
                .AddIngredient(ItemID.Glass, 15)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.IceMirror)
                .AddIngredient(ItemID.IceBlock, 5)
                .AddIngredient(ItemID.MagicMirror, 1)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            // ─── 宝石互相转化（钻石 ↔ 其他宝石） ──────────────────
            // 任何宝石 → 钻石
            Recipe.Create(ItemID.Diamond)
                .AddIngredient(ItemID.Sapphire, 1)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();
            Recipe.Create(ItemID.Diamond)
                .AddIngredient(ItemID.Ruby, 1)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();
            Recipe.Create(ItemID.Diamond)
                .AddIngredient(ItemID.Emerald, 1)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();
            Recipe.Create(ItemID.Diamond)
                .AddIngredient(ItemID.Topaz, 1)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();
            Recipe.Create(ItemID.Diamond)
                .AddIngredient(ItemID.Amethyst, 1)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();
            Recipe.Create(ItemID.Diamond)
                .AddIngredient(ItemID.Amber, 1)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            // 钻石 → 其他宝石
            Recipe.Create(ItemID.Sapphire)
                .AddIngredient(ItemID.Diamond, 1)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();
            Recipe.Create(ItemID.Ruby)
                .AddIngredient(ItemID.Diamond, 1)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();
            Recipe.Create(ItemID.Emerald)
                .AddIngredient(ItemID.Diamond, 1)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();
            Recipe.Create(ItemID.Topaz)
                .AddIngredient(ItemID.Diamond, 1)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();
            Recipe.Create(ItemID.Amethyst)
                .AddIngredient(ItemID.Diamond, 1)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();
            Recipe.Create(ItemID.Amber)
                .AddIngredient(ItemID.Diamond, 1)
                .AddTile(TileID.Anvils)
                .DisableDecraft()
                .Register();

            // ─── 书（用竹子代替木头） ──────────────────────────────
            Recipe.Create(ItemID.Book)
                .AddIngredient(ItemID.BambooBlock, 6)
                .AddIngredient(ItemID.Silk, 5)
                .AddTile(TileID.Tables)
                .DisableDecraft()
                .Register();

            Recipe.Create(ItemID.Book)
                .AddIngredient(ItemID.LargeBambooBlock, 2)
                .AddIngredient(ItemID.Silk, 5)
                .AddTile(TileID.Tables)
                .DisableDecraft()
                .Register();

            // ─── 其他杂项 ────────────────────────────────────────────
            Recipe.Create(ItemID.EmpressButterfly, 2)
                .AddIngredient(ItemID.Ectoplasm, 5)
                .AddIngredient(ItemID.PixieDust, 5)
                .AddRecipeGroup("Butterflies", 5)
                .AddTile(TileID.DemonAltar)
                .DisableDecraft()
                .Register();
        }

        // ═══════════════════════════════════════════════════════════════
        // 📊 合成材料倍率调整（PostAddRecipes）
        // ═══════════════════════════════════════════════════════════════
        /// <summary>
        /// 在所有配方注册完成后调用。
        /// 如果 EnableCraftingCostTweak 开启，遍历所有配方，
        /// 将每个配方的材料数量乘以倍率（向上取整，最少为1）。
        /// 
        /// 【计算示例】
        /// CraftingCostMultiplier = 50（表示50%）
        /// 原配方需要10个铁锭 → 10 × 0.5 = 5 → 向上取整 = 5
        /// 
        /// CraftingCostMultiplier = 150（表示150%）
        /// 原配方需要10个铁锭 → 10 × 1.5 = 15 → 向上取整 = 15
        /// 
        /// 📐 建议范围：1~1000，100为原价。
        /// </summary>
        public override void PostAddRecipes()
        {
            var config = ModContent.GetInstance<LLLServerConfig>();

            // 功能未开启或倍率为100%（无变化）则跳过
            if (!config.EnableCraftingCostTweak) return;
            if (config.CraftingCostMultiplier == 100) return;

            double multiplier = config.CraftingCostMultiplier / 100.0;

            // 遍历所有已注册的配方
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe recipe = Main.recipe[i];

                // 遍历该配方的所有原料
                foreach (Item ingredient in recipe.requiredItem)
                {
                    if (ingredient.type > 0)
                    {
                        int originalStack = ingredient.stack;
                        // 📐 向上取整（Ceiling），确保即使倍率很小也至少保留1个
                        int newStack = (int)Math.Ceiling(originalStack * multiplier);

                        if (newStack < 1)
                            newStack = 1;

                        ingredient.stack = newStack;
                    }
                }
            }
        }
    }
}