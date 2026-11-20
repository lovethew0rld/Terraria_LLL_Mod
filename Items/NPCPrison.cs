using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using LLL;

namespace LLL.Items
{
    /// <summary>
    /// 监狱建造器：在光标位置一键生成一个符合NPC入住条件的完整房间（带家具）。
    /// 使用随机材料主题（可配置），支持肉前/肉后材料限制。
    /// 售价5金币，合成需要66个木头。
    /// 
    /// 【材料主题说明】
    /// 共15种材料主题，每种材料都拥有完整的专属配套：
    /// - 主方块（墙壁/天花板/地板）
    /// - 背景墙
    /// - 平台 / 椅子 / 工作台（三者共享同一个家具样式编号）
    /// - 专属火把（只允许本材料的火把，排除无关火把）
    /// 
    /// 【随机逻辑】
    /// - 随机关闭 → 固定使用普通木材（AllThemes[0]）
    /// - 随机开启 → 从全部15种中随机选择（木材只是选项之一，不是默认）
    /// - 肉后筛选开启 → 仅过滤珍珠木（保留其余14种）
    /// </summary>
    public class NPCPrison : ModItem
    {
        // ═══════════════════════════════════════════════════════════════
        // 📐 建筑尺寸常量（以"格"为单位，1格 = 16像素）
        // ═══════════════════════════════════════════════════════════════
        private const int PrisonWidth = 6;      // 总宽度：6格（内部空间4格，左右墙各1格）
        private const int PrisonHeight = 10;    // 总高度：10格（顶部1格天花板，底部1格地板，
                                                //          中间8格为房间内部高度）

        // 边界安全距离（防止在世界边缘建造导致异常）
        private const int AnchorMinX = 5;               // 光标X最小必须 ≥ 5
        private const int AnchorMaxXOffset = 6;         // 光标X最大不能超过 maxTilesX - 6
        private const int AnchorMinY = 10;              // 光标Y最小必须 ≥ 10（防止建在地图顶部之外）
        private const int AnchorMaxYOffset = 1;         // 光标Y最大不能超过 maxTilesY - 1

        // 建造冷却（防止连续点击导致冲突/性能问题）
        private const int CooldownFrames = 15;          // 两次建造之间的最小间隔帧数（15帧 ≈ 0.25秒）
        private static int? lastBuildFrameRecord = null;

        /// <summary>
        /// WoodTheme 描述一种材料所需的全部建筑/家具参数。
        /// 
        /// 【字段说明】
        /// - TileType：主方块类型（墙砖、天花板、地板内部填充使用）
        /// - WallType：首选背景墙类型（房间内部背景墙）
        /// - AltWallType：备选背景墙类型。
        ///   0 表示无备选；大于0时，建造时在 WallType 和 AltWallType 之间随机二选一。
        ///   目前仅王朝木使用（白墙/蓝墙两种都保留，随机选用）。
        /// - FurnitureStyle：家具样式编号（平台/椅子/工作台三者共享）。
        ///   原版中平台、椅子、工作台的样式顺序完全一致，同编号即同材料。
        ///   例如 0=木材，1=红木，……，17=仙人掌，19=蘑菇，20=竹子。
        /// 
        /// 【兜底规则】
        /// FurnitureStyle 为 -1 表示"无专属"，建造时兜底为木材样式 0。
        /// 
        /// 【火把说明】
        /// 所有主题统一使用经典火把（TorchID.Torch）。
        /// 木火把/宝石火把等专属火把的图格样式编号在原版中不通用，
        /// 实测无法可靠放置，已回退为经典火把。
        /// </summary>
        private struct WoodTheme
        {
            public ushort TileType;      // 主方块（墙壁/天花板/地板填充）类型
            public ushort WallType;      // 首选背景墙类型
            public ushort AltWallType;   // 备选背景墙类型（0=无备选，>0=随机二选一）
            public int FurnitureStyle;   // 平台/椅子/工作台共享样式（-1=兜底木材样式0）
        }
        
        // ═══════════════════════════════════════════════════════════════
        // 📦 全部材料主题列表（共15种，每种带专属方块/墙/家具套件）
        // ═══════════════════════════════════════════════════════════════
        // 【样式编号速查】
        // 平台/椅子/工作台共享编号：
        //   0=木材  1=红木  2=针叶木  3=棕榈木  4=乌木  5=暗影木
        //   6=珍珠木 7=灰烬木 8=王朝木 9=阴森木
        //   17=仙人掌 18=南瓜 19=蘑菇 20=竹子 21=大竹子
        //
        // 火把：全部统一使用经典火把，不再区分材料。
        //
        // 索引顺序中，珍珠木（索引6）是唯一会被"肉后筛选"过滤的材料。
        private static readonly WoodTheme[] AllThemes = new WoodTheme[]
        {
            // ─── 标准木材组（8种，样式编号与原版一一对应） ──────────────
            new WoodTheme { TileType = TileID.WoodBlock,        WallType = WallID.Wood,              AltWallType = 0, FurnitureStyle = 0 },
            new WoodTheme { TileType = TileID.RichMahogany,     WallType = WallID.RichMaogany,       AltWallType = 0, FurnitureStyle = 1 },
            new WoodTheme { TileType = TileID.BorealWood,       WallType = WallID.BorealWood,        AltWallType = 0, FurnitureStyle = 2 },
            new WoodTheme { TileType = TileID.PalmWood,         WallType = WallID.PalmWood,          AltWallType = 0, FurnitureStyle = 3 },
            new WoodTheme { TileType = TileID.Ebonwood,         WallType = WallID.Ebonwood,          AltWallType = 0, FurnitureStyle = 4 },
            new WoodTheme { TileType = TileID.Shadewood,        WallType = WallID.Shadewood,         AltWallType = 0, FurnitureStyle = 5 },
            new WoodTheme { TileType = TileID.Pearlwood,        WallType = WallID.Pearlwood,         AltWallType = 0, FurnitureStyle = 6 },
            new WoodTheme { TileType = TileID.AshWood,          WallType = WallID.AshWood,           AltWallType = 0, FurnitureStyle = 7 },

            // ─── 广义木材组（7种，方块+墙+家具配套，火把统一经典火把） ──
            // 王朝木：白墙/蓝墙随机二选一（两种墙都保留，不忽略）
            new WoodTheme { TileType = TileID.DynastyWood,      WallType = WallID.WhiteDynasty,      AltWallType = WallID.BlueDynasty, FurnitureStyle = 8 },
            new WoodTheme { TileType = TileID.SpookyWood,       WallType = WallID.SpookyWood,        AltWallType = 0, FurnitureStyle = 9 },
            // 仙人掌方块：正确ID是 CactusBlock（不是 Cactus，Cactus是植物图格）
            new WoodTheme { TileType = TileID.CactusBlock,      WallType = WallID.Cactus,            AltWallType = 0, FurnitureStyle = 17 },
            new WoodTheme { TileType = TileID.BambooBlock,      WallType = WallID.BambooBlockWall,   AltWallType = 0, FurnitureStyle = 20 },
            new WoodTheme { TileType = TileID.LargeBambooBlock, WallType = WallID.LargeBambooBlockWall, AltWallType = 0, FurnitureStyle = 21 },
            new WoodTheme { TileType = TileID.PumpkinBlock,     WallType = WallID.Pumpkin,           AltWallType = 0, FurnitureStyle = 18 },
            new WoodTheme { TileType = TileID.MushroomBlock,    WallType = WallID.Mushroom,          AltWallType = 0, FurnitureStyle = 19 },
        };

        // ═══════════════════════════════════════════════════════════════
        // 物品基础属性
        // ═══════════════════════════════════════════════════════════════
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.useTime = 15;          // 使用时间（帧数），15帧 ≈ 0.25秒
            Item.useAnimation = 15;     // 与useTime一致，保证动画完整
            Item.useStyle = ItemUseStyleID.HoldUp;  // 举起动作
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item1;
            Item.consumable = false;    // 不消耗，可重复使用
            Item.maxStack = 1;
            Item.autoReuse = false;     // 不自动连发，防止误触
            Item.createTile = -1;       // 不放置任何方块（我们完全自定义建造）
            Item.value = Item.sellPrice(0, 5, 0, 0); // 5金币
        }

        // 合成配方：任意木材 × 66，在工作台处合成
        public override void AddRecipes()
        {
            var recipe = CreateRecipe();
            recipe.AddRecipeGroup(RecipeGroupID.Wood, 66);
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }

        // ═══════════════════════════════════════════════════════════════
        // 🏗️ 使用物品 —— 建造触发
        // ═══════════════════════════════════════════════════════════════
        public override bool? UseItem(Player player)
        {
            // 冷却检查：防止短时间内连续建造
            if (lastBuildFrameRecord.HasValue && Main.GameUpdateCount - lastBuildFrameRecord.Value < CooldownFrames)
                return true;

            // 获取光标所在的世界坐标（以"格"为单位）
            int anchorX = (int)(Main.MouseWorld.X / 16f);
            int anchorY = (int)(Main.MouseWorld.Y / 16f);

            // 检查是否在世界边界内（留出安全距离）
            if (anchorX < AnchorMinX || anchorX > Main.maxTilesX - AnchorMaxXOffset ||
                anchorY < AnchorMinY || anchorY > Main.maxTilesY - AnchorMaxYOffset)
            {
                Main.NewText("Not enough space for prison here!", Color.Red);
                return true;
            }

            // 执行建造
            BuildPrison(anchorX, anchorY);
            lastBuildFrameRecord = (int)Main.GameUpdateCount; // 更新冷却标记
            return true;
        }

        // ═══════════════════════════════════════════════════════════════
        // 🧱 核心建造逻辑
        // ═══════════════════════════════════════════════════════════════
        /// <summary>
        /// 在指定锚点位置构建完整的NPC监狱房间。
        /// 
        /// 【建造流程】
        /// 1. 确定房间范围（left/right/top/bottom）
        /// 2. 根据配置选择材料主题
        /// 3. 清空区域（移除所有原有物块和墙壁）
        /// 4. 放置方块和平台（天花板、左右墙、地板、内部结构）
        /// 5. 解析并填充背景墙（支持备选墙随机二选一）
        /// 6. 放置家具（椅子、工作台）——样式与材料配套
        /// 7. 服务器同步（多人模式）
        /// 
        /// 【样式配套规则】
        /// 平台/椅子/工作台共用主题的 FurnitureStyle；
        /// 火把使用主题的 TorchStyle（每个材料只配自己的火把，排除无关火把）。
        /// </summary>
        /// <param name="baseX">光标所在格坐标X（锚点）</param>
        /// <param name="baseY">光标所在格坐标Y（锚点，即地板所在行）</param>
        private void BuildPrison(int baseX, int baseY)
        {
            // 光标位置作为房间的**底部**（锚点在地板行）
            // 布局：天花板在 top，地板在 bottom（即 baseY）
            int left = baseX;
            int right = baseX + PrisonWidth - 1;
            int top = baseY - (PrisonHeight - 1);  // 向上延伸 PrisonHeight-1 格
            int bottom = baseY;

            // 读取配置
            var config = ModContent.GetInstance<LLLServerConfig>();
            bool randomize = config.RandomizePrisonMaterials;      // 是否随机主题
            bool restrict = config.RestrictMaterialsToPreHardmode; // 是否限制肉后材料

            // 选择材料主题（详见 GetTheme 方法）
            WoodTheme theme = GetTheme(randomize, restrict);

            // ─── 解析家具样式（与材料配套，含兜底） ───
            // 平台/椅子/工作台共用 FurnitureStyle；-1 时兜底为木材样式 0
            int furnitureStyle = theme.FurnitureStyle >= 0 ? theme.FurnitureStyle : 0;
            // 火把：在经典火把与白火把之间随机二选一（各50%概率）。
            // 白火把（TorchID.White）无特殊颜色渲染，是原版最亮的火把，
            // 随机到白火把当作小惊喜；经典火把（TorchID.Torch）为常规结果。
            int torchStyle = Main.rand.NextBool() ? TorchID.Torch : TorchID.White;

            // ─── 步骤1：清空区域（移除所有物块和墙壁） ───
            for (int x = left; x <= right; x++)
                for (int y = top; y <= bottom; y++)
                {
                    WorldGen.KillTile(x, y, false, false, false); // 移除物块（不丢掉落）
                    WorldGen.KillWall(x, y, false);               // 移除墙壁
                }

            // ─── 步骤2：放置方块和平台 ───
            for (int y = top; y <= bottom; y++)
            {
                for (int x = left; x <= right; x++)
                {
                    int row = y - top; // 行索引（0 = 天花板，9 = 地板）

                    // 天花板 (row == 0) ：实心方块
                    if (y == top)
                    {
                        WorldGen.PlaceTile(x, y, theme.TileType, false, true);
                    }
                    // 地板 (row == PrisonHeight-1) ：平台（用于进出，样式与材料配套）
                    else if (y == bottom)
                    {
                        WorldGen.PlaceTile(x, y, TileID.Platforms, false, true, -1, furnitureStyle);
                    }
                    // 左右墙壁 (x == left 或 x == right)
                    else if (x == left || x == right)
                    {
                        // 第1~5行（row 1~5）：实心墙
                        if (row >= 1 && row <= 5)
                            WorldGen.PlaceTile(x, y, theme.TileType, false, true);
                        // 第6~8行（row 6~8）：平台（作为窗户或装饰缺口，样式与材料配套）
                        else if (row >= 6 && row <= 8)
                            WorldGen.PlaceTile(x, y, TileID.Platforms, false, true, -1, furnitureStyle);
                    }
                    // 内部空间 (x 在 left+1 到 right-1 之间)
                    else
                    {
                        // 在第5行（即 row == 5）放置一个火把（在右侧第二格位置）
                        // 其余位置用主方块填充，形成内部结构支撑
                        if (row == 5)
                        {
                            if (x == left + PrisonWidth - 2) // 右侧倒数第二格
                                WorldGen.PlaceTile(x, y, TileID.Torches, false, true, -1, torchStyle);
                            else
                                WorldGen.PlaceTile(x, y, theme.TileType, false, true);
                        }
                    }
                }
            }

            // ─── 步骤3：解析背景墙类型（支持备选墙随机二选一） ───
            // 如果主题定义了备选墙（AltWallType > 0），在两种墙之间随机选择。
            // 目前仅王朝木使用此机制：白色王朝墙与蓝色王朝墙各 50% 概率。
            ushort wallType = theme.WallType;
            if (theme.AltWallType > 0 && Main.rand.NextBool())
            {
                wallType = theme.AltWallType;
            }

            // ─── 步骤4：背景墙（内部填充） ───
            for (int x = left + 1; x <= right - 1; x++)
                for (int y = top + 1; y <= bottom - 1; y++)
                    WorldGen.PlaceWall(x, y, wallType, true);

            // ─── 步骤5：放置家具（样式与材料配套） ───
            // 椅子：位于 left+1, top+4 （面向右，即朝向工作台）
            PlaceFurniture(left + 1, top + 4, TileID.Chairs, furnitureStyle, direction: 1);
            // 工作台：位于 left+2, top+4 （方向无关紧要）
            PlaceFurniture(left + 2, top + 4, TileID.WorkBenches, furnitureStyle, direction: -1);

            // 如果是服务器，同步给所有客户端
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendTileSquare(-1, left, top, PrisonWidth, PrisonHeight);
        }

        // ═══════════════════════════════════════════════════════════════
        // 🎲 主题选择逻辑
        // ═══════════════════════════════════════════════════════════════
        /// <summary>
        /// 从全部15种材料主题中选择本次建造使用的主题。
        /// 
        /// 【选择规则】
        /// - 随机关闭 → 固定返回普通木材（AllThemes[0]）
        /// - 随机开启 → 从池中随机选取，木材只是15个选项之一
        /// - 肉后筛选开启 → 仅过滤珍珠木（索引6），保留其余14种
        /// 
        /// 【为什么珍珠木是唯一被过滤的？】
        /// 按照设计需求，珍珠木是唯一被视为"肉后材料"的木材。
        /// 灰烬木虽在原版中也属于肉后，但需求明确保留它，因此不过滤。
        /// </summary>
        /// <param name="randomize">是否启用随机材料</param>
        /// <param name="restrictToPreHM">是否过滤肉后材料（仅过滤珍珠木）</param>
        /// <returns>本次建造使用的材料主题</returns>
        private WoodTheme GetTheme(bool randomize, bool restrictToPreHM)
        {
            // 随机关闭 → 始终使用普通木材
            if (!randomize)
                return AllThemes[0];

            // 构建随机池
            List<WoodTheme> pool = new List<WoodTheme>(AllThemes);

            // 肉后筛选：仅移除珍珠木
            if (restrictToPreHM)
                pool.RemoveAll(t => t.TileType == TileID.Pearlwood);

            // 从池中随机选一个
            return pool[Main.rand.Next(pool.Count)];
        }

        // ═══════════════════════════════════════════════════════════════
        // 🪑 放置家具（带方向参数）
        // ═══════════════════════════════════════════════════════════════
        /// <summary>
        /// 在指定坐标放置家具。
        /// 
        /// 【参数说明】
        /// - x, y：目标坐标（格）
        /// - type：家具物块类型（TileID.Chairs / TileID.WorkBenches 等）
        /// - style：家具样式编号（必须是合法样式，已与材料配套）
        /// - direction：家具朝向，1 = 朝右，-1 = 无方向/自动
        /// 
        /// 【失败处理】
        /// 如果放置失败，记录警告日志但不中断建造流程。
        /// </summary>
        private void PlaceFurniture(int x, int y, int type, int style, int direction = -1)
        {
            bool ok = WorldGen.PlaceObject(x, y, type, false, style, 0, -1, direction);
            if (!ok)
            {
                // 失败时记录日志（用于调试）
                Mod.Logger.Warn($"Failed to place furniture {type} (style {style}, dir {direction}) at ({x},{y})");
            }
        }
    }
}