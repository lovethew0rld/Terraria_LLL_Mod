using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Linq;

namespace LLL.Players
{
    /// <summary>
    /// 合成奖励系统：当玩家首次获得某些特定物品时，
    /// 自动获得一个对应的奖励物品（如合成关键素材后获得辅助工具）。
    /// 奖励每个玩家/每个世界只能触发一次，数据持久化保存。
    /// </summary>
    public class CraftingRewardPlayer : ModPlayer
    {
        // 已获得的奖励字典（Key=触发物品ID，Value=是否已领取）
        private Dictionary<int, bool> receivedRewards = new Dictionary<int, bool>();

        // 上一帧是否持有触发物品（用于检测“刚刚获得”的瞬间）
        private Dictionary<int, bool> hadItemLastFrame = new Dictionary<int, bool>();

        // ─── 奖励配置表 ────────────────────────────────────────────────
        // 格式：new RewardConfig(触发物品ID, 奖励物品ID, 最小数量, 最大数量)
        // 如果最小=最大，则固定数量；否则在范围内随机。
        private List<RewardConfig> rewardConfigs = new List<RewardConfig>
        {
            new RewardConfig(ItemID.FallenStar, ItemID.TeleportationPotion, 10, 30),      // 落星 → 传送药水
            new RewardConfig(ItemID.Toolbelt, ItemID.ArchitectGizmoPack, 1, 1),           // 工具腰带 → 建筑师发明背包
            new RewardConfig(ItemID.LightningBoots, ItemID.Stopwatch, 1, 1),              // 闪电靴 → 秒表
            new RewardConfig(ItemID.FeralClaws, ItemID.DPSMeter, 1, 1)                   // 猛爪手套 → DPS计数器
        };

        /// <summary>
        /// 每帧更新：检测玩家是否刚获得触发物品，若是则发放奖励并标记。
        /// </summary>
        public override void PostUpdate()
        {
            foreach (var config in rewardConfigs)
            {
                int triggerId = config.TriggerItemId;

                // 初始化字典条目
                if (!hadItemLastFrame.ContainsKey(triggerId))
                    hadItemLastFrame[triggerId] = false;
                if (!receivedRewards.ContainsKey(triggerId))
                    receivedRewards[triggerId] = false;

                bool hasItemNow = Player.HasItem(triggerId);

                // 检测条件：上一帧没有 → 这一帧有了 → 且尚未领取奖励
                if (!hadItemLastFrame[triggerId] && hasItemNow && !receivedRewards[triggerId])
                {
                    // 计算奖励数量（如果最大>最小则随机）
                    int amount = config.MinAmount;
                    if (config.MaxAmount > config.MinAmount)
                    {
                        amount = Main.rand.Next(config.MinAmount, config.MaxAmount + 1);
                    }

                    Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), config.RewardItemId, amount);
                    receivedRewards[triggerId] = true;
                }

                // 更新“上一帧持有状态”
                hadItemLastFrame[triggerId] = hasItemNow;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ? 数据持久化
        // ═══════════════════════════════════════════════════════════════
        public override void LoadData(TagCompound tag)
        {
            receivedRewards = new Dictionary<int, bool>();

            if (tag.ContainsKey("CraftingRewards"))
            {
                // 从保存的ID列表恢复
                List<int> receivedIds = tag.GetList<int>("CraftingRewards").ToList();
                foreach (int id in receivedIds)
                {
                    receivedRewards[id] = true;
                }
            }
        }

        public override void SaveData(TagCompound tag)
        {
            List<int> receivedIds = new List<int>();
            foreach (var kvp in receivedRewards)
            {
                if (kvp.Value) // 只保存已获得的
                    receivedIds.Add(kvp.Key);
            }
            tag["CraftingRewards"] = receivedIds;
        }

        // ================================================================
        // ? 奖励配置结构体 —— 合成奖励系统的"数据蓝图"
        // ================================================================
        // 
        // 【结构体是什么？】
        // 结构体（struct）是一种轻量级的数据容器，类似于"一张填写好的表格"。
        // 它本身不包含逻辑，只负责把相关的数据打包在一起。
        // 在这里，我们用 RewardConfig 来定义一条"合成奖励规则"：
        //   当我获得物品 A 时 → 系统自动给我物品 B，数量为 N 个。
        //
        // 【为什么不用类（class）？】
        // 结构体是值类型，存储在栈上，访问速度更快，适合存储少量简单数据。
        // 而且我们只需要存储数据，不需要继承或多态，用结构体更轻便。
        //
        // 【新人必看：如何使用？】
        // 要添加一条新的奖励规则，就在 rewardConfigs 列表里新增一行：
        //   new RewardConfig(触发物品ID, 奖励物品ID, 最小数量, 最大数量)
        //
        // 举个实际例子：
        //   玩家获得"传送晶塔"时，奖励 1 个"魔镜"。
        //   new RewardConfig(ItemID.TeleportationPotion, ItemID.MagicMirror, 1, 1)
        //
        //   玩家获得"金锭"时，随机奖励 5~10 个"药水"。
        //   new RewardConfig(ItemID.GoldBar, ItemID.HealingPotion, 5, 10)
        // ================================================================

        private struct RewardConfig
        {
            // ─── 字段说明 ──────────────────────────────────────────────────────
            // 这些是结构体的"数据列"，每一条规则都由这 4 个值定义。

            /// <summary>
            /// 【触发物品ID】：玩家获得哪个物品时触发奖励？
            /// 这个 ID 对应 Terraria.ID.ItemID 中的常量。
            /// 例如：ItemID.FallenStar 就是落星（ID: 75）。
            /// 
            /// ?? 注意：系统检测的是"玩家首次获得"这个物品，
            /// 而不是"合成"这个物品。所以即使这个物品是捡来的、买的、
            /// 或者从宝箱里开出来的，同样会触发奖励！
            /// </summary>
            public int TriggerItemId;

            /// <summary>
            /// 【奖励物品ID】：触发后给玩家什么物品？
            /// 同样是 ItemID 中的常量。
            /// 例如：ItemID.TeleportationPotion 就是传送药水（ID: 3093）。
            /// 
            /// ? 奖励物品会直接进入玩家背包，如果背包满了会自动掉落在地上。
            /// </summary>
            public int RewardItemId;

            /// <summary>
            /// 【最小数量】：奖励物品的最少数量。
            /// 如果 MinAmount == MaxAmount，则固定给这个数量的物品。
            /// 如果 MinAmount < MaxAmount，则在 Min~Max 之间随机取一个整数。
            /// 
            /// ? 范围建议：1~999，但通常不要给太多，以免破坏游戏平衡。
            /// 
            /// 示例场景：
            /// - 给"传送药水" 10~30 瓶 → 既不会太少，又有些随机惊喜感。
            /// - 给"建筑发明包" 1 个 → 只给一个就够了，多了也没用。
            /// </summary>
            public int MinAmount;

            /// <summary>
            /// 【最大数量】：奖励物品的最多数量。
            /// 
            /// ?? 重要：如果 MinAmount < MaxAmount，系统会调用：
            ///   Main.rand.Next(MinAmount, MaxAmount + 1)
            /// 注意这里 +1 是为了包含 MaxAmount 本身。
            /// 
            /// 如果不需要随机数量，直接将 MinAmount 和 MaxAmount 设为相同值。
            /// 例如：new RewardConfig(..., 1, 1) 固定给 1 个。
            /// </summary>
            public int MaxAmount;

            // ─── 构造函数 ──────────────────────────────────────────────────────
            // 构造函数（Constructor）：相当于"填写表格的快捷方式"。
            // 当你写 new RewardConfig(参数1, 参数2, 参数3, 参数4) 时，
            // 构造函数会把 4 个参数按顺序赋给对应的 4 个字段。

            /// <summary>
            /// 创建一条奖励规则。
            /// 
            /// 【调用示例】：
            ///   new RewardConfig(ItemID.FallenStar, ItemID.TeleportationPotion, 10, 30)
            /// 
            /// 【参数映射】：
            ///   triggerItemId  → 传入的是"触发物"的ID（如落星）
            ///   rewardItemId   → 传入的是"奖励物"的ID（如传送药水）
            ///   minAmount      → 传入的是最小数量（如 10）
            ///   maxAmount      → 传入的是最大数量（如 30）
            /// 
            /// 【新人避坑】：
            ///   不要把参数的顺序搞反了！
            ///   ? 错误：new RewardConfig(10, 30, ItemID.FallenStar, ItemID.TeleportationPotion)
            ///   ? 正确：new RewardConfig(ItemID.FallenStar, ItemID.TeleportationPotion, 10, 30)
            /// 
            ///   参数类型不对也会报错：
            ///   - 前两个参数必须是 int 类型的物品ID
            ///   - 后两个参数必须是 int 类型的数量
            /// </summary>
            /// <param name="triggerItemId">触发奖励的原料物品ID</param>
            /// <param name="rewardItemId">给予玩家的奖励物品ID</param>
            /// <param name="minAmount">奖励数量的下限（包含）</param>
            /// <param name="maxAmount">奖励数量的上限（包含）</param>
            public RewardConfig(int triggerItemId, int rewardItemId, int minAmount, int maxAmount)
            {
                // 将传入的参数赋值给结构体的公共字段
                // 注意：这里的 this.TriggerItemId 是字段，右边的 triggerItemId 是参数
                TriggerItemId = triggerItemId;
                RewardItemId = rewardItemId;
                MinAmount = minAmount;
                MaxAmount = maxAmount;
            }
        }

        // ================================================================
        // ? 给维护者的扩展指南
        // ================================================================
        // 
        // 【如何添加新的奖励规则？】
        // 在 rewardConfigs 列表（位于本文件上方约第 20 行）中添加一行即可。
        // 
        // 现有配置：
        //   new RewardConfig(ItemID.FallenStar, ItemID.TeleportationPotion, 10, 30),
        //   new RewardConfig(ItemID.Toolbelt, ItemID.ArchitectGizmoPack, 1, 1),
        //   new RewardConfig(ItemID.LightningBoots, ItemID.Stopwatch, 1, 1),
        //   new RewardConfig(ItemID.FeralClaws, ItemID.DPSMeter, 1, 1)
        // 
        // 【添加新规则的步骤】：
        // 1. 想清楚"获得什么"→"奖励什么"→"奖励多少"。
        // 2. 在 ItemID 类中查找对应的物品ID常量（或使用 ModContent.ItemType<>()）。
        // 3. 按格式追加一行，注意末尾的逗号。
        // 
        // 【示例：添加一条新规则】
        // 场景：玩家获得"生命水晶"时，奖励 3 个"恢复药水"。
        // 代码：new RewardConfig(ItemID.LifeCrystal, ItemID.HealingPotion, 3, 3),
        // 
        // 【示例：添加一条带随机数量的规则】
        // 场景：玩家获得"铁锭"时，奖励 3~8 个"炸弹"。
        // 代码：new RewardConfig(ItemID.IronBar, ItemID.Bomb, 3, 8),
        // 
        // ?? 注意事项：
        // - 同一个触发物品只能配置一次，否则后面的会覆盖前面的行为。
        // - 奖励数量不要设置太大（>999），否则可能超过物品堆叠上限。
        // - 如果奖励物品本身也是"触发物品"，不会形成无限循环，因为每个触发物品只会奖励一次。
        // ================================================================
    }
}