using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Terraria.GameContent.Generation;
using Terraria.IO;
using System.Collections.Generic;
using LLL;

namespace LLL.WorldGeneration
{
    /// <summary>
    /// 地牢礼物宝箱生成系统：在世界生成时，于地牢入口处放置一个上锁金箱，
    /// 内含强力武器（地牢钥匙武器系列），帮助玩家快速起步。
    /// 
    /// 【包含物品】
    /// - 食人鱼枪（PiranhaGun）
    /// - 彩虹枪（RainbowGun）
    /// - 寒霜九头蛇法杖（StaffOfTheFrostHydra）
    /// - 暴风雨虎杖（StormTigerStaff）
    /// - 吸血鬼刀（VampireKnives）/ 腐化灾兵（ScourgeOfTheCorruptor）—— 根据世界猩红/腐化决定
    /// 
    /// 【时机】
    /// 在地牢生成完成后、Final Cleanup 之后执行，确保地牢区域已完全形成。
    /// 
    /// 由 LLLServerConfig.EnableDungeonGift 控制开关（默认关闭，这是超模福利）。
    /// ⚠️ 仅影响新生成的世界，对已存在的世界无效。
    /// </summary>
    public class DungeonGiftSystem : ModSystem
    {
        /// <summary>
        /// 修改世界生成任务列表，在 Final Cleanup 之后插入地牢礼物生成任务。
        /// </summary>
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            var config = ModContent.GetInstance<LLLServerConfig>();

            // 功能未开启则跳过
            if (!config.EnableDungeonGift) return;

            // 找到 Final Cleanup 任务的索引（世界生成中最后一个清理阶段）
            // 在这个之后插入，可以确保：
            // 1. 地牢已经完整生成
            // 2. 不会与其他世界生成任务冲突
            // 3. 玩家进入世界时宝箱已经就绪
            int finalCleanupIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Final Cleanup"));

            if (finalCleanupIndex != -1)
            {
                // 插入新任务：在 Final Cleanup 之后执行
                tasks.Insert(finalCleanupIndex + 1, new PassLegacy("Dungeon Gift", PlaceDungeonChest));
            }
        }

        /// <summary>
        /// 实际放置地牢礼物的核心逻辑。
        /// 尝试在地牢入口附近找一个合适的位置放置宝箱，若找不到则强制在入口坐标放置。
        /// </summary>
        private void PlaceDungeonChest(GenerationProgress progress, GameConfiguration configuration)
        {
            // 地牢入口坐标（由原版世界生成确定）
            int startX = Main.dungeonX;
            int startY = Main.dungeonY;

            bool chestPlaced = false;

            // ─── 尝试 1000 次找到一个合适的放置位置 ──────────────────
            // 条件：有实体地面（SolidTile），上方至少 2 格为空（放置宝箱需要空间）
            for (int attempt = 0; attempt < 1000; attempt++)
            {
                // 在地牢入口 ±20 格水平、±10 格垂直范围内随机搜索
                int x = startX + WorldGen.genRand.Next(-20, 21);
                int y = startY + WorldGen.genRand.Next(-10, 11);

                // 边界检查（确保在有效地图范围内）
                if (!WorldGen.InWorld(x, y - 2, 5)) continue;

                // 检查地面：当前位置和右侧一格必须是实心物块
                if (WorldGen.SolidTile(x, y) && WorldGen.SolidTile(x + 1, y) &&
                    // 上方一格和上方两格必须为空（宝箱占 2×2 空间）
                    !Main.tile[x, y - 1].HasTile && !Main.tile[x + 1, y - 1].HasTile &&
                    !Main.tile[x, y - 2].HasTile && !Main.tile[x + 1, y - 2].HasTile)
                {
                    // 放置宝箱（类型：上锁金箱，样式 21）
                    // 📐 style: 21 = 上锁金箱（Golden Lock Box 风格）
                    //    可根据需要改为其他宝箱样式。
                    int chestIndex = WorldGen.PlaceChest(x, y - 1, TileID.Containers,
                        notNearOtherChests: false, style: 21);

                    if (chestIndex != -1)
                    {
                        Chest chest = Main.chest[chestIndex];
                        int itemSlot = 0;

                        // ─── 填充宝箱物品 ──────────────────────────────
                        // 地牢钥匙武器系列（全部放入）
                        chest.item[itemSlot++].SetDefaults(ItemID.PiranhaGun);           // 食人鱼枪
                        chest.item[itemSlot++].SetDefaults(ItemID.RainbowGun);           // 彩虹枪
                        chest.item[itemSlot++].SetDefaults(ItemID.StaffoftheFrostHydra); // 寒霜九头蛇法杖
                        chest.item[itemSlot++].SetDefaults(ItemID.StormTigerStaff);      // 暴风雨虎杖

                        // 根据世界类型决定放吸血鬼刀还是腐化灾兵
                        if (WorldGen.crimson)
                        {
                            chest.item[itemSlot++].SetDefaults(ItemID.VampireKnives);
                        }
                        else
                        {
                            chest.item[itemSlot++].SetDefaults(ItemID.ScourgeoftheCorruptor);
                        }

                        chestPlaced = true;
                        break; // 成功放置，退出搜索
                    }
                }
            }

            // ─── 如果搜索 1000 次都找不到合适位置，强制在地牢入口放置 ──
            // 这是最后的保底方案，确保玩家一定能拿到礼物。
            if (!chestPlaced)
            {
                // 在地牢入口坐标强制放置两块地牢砖作为"地基"
                WorldGen.PlaceTile(startX, startY, TileID.GreenDungeonBrick, forced: true);
                WorldGen.PlaceTile(startX + 1, startY, TileID.GreenDungeonBrick, forced: true);

                // 在地基上方强制放置宝箱
                int forcedChestIndex = WorldGen.PlaceChest(startX, startY - 1,
                    TileID.Containers, notNearOtherChests: false, style: 21);

                if (forcedChestIndex != -1)
                {
                    Chest chest = Main.chest[forcedChestIndex];
                    chest.item[0].SetDefaults(ItemID.PiranhaGun);
                    chest.item[1].SetDefaults(ItemID.RainbowGun);
                    chest.item[2].SetDefaults(ItemID.StaffoftheFrostHydra);
                    chest.item[3].SetDefaults(ItemID.StormTigerStaff);
                    chest.item[4].SetDefaults(WorldGen.crimson ? ItemID.VampireKnives : ItemID.ScourgeoftheCorruptor);
                }
            }
        }
    }
}