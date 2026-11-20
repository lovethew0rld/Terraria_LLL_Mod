using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using LLL;

namespace LLL.Systems
{
    /// <summary>
    /// 宝箱额外物品系统（世界生成后执行，每个世界只执行一次）。
    ///
    /// ═══════════════════════════════════════════════════════════════════
    /// 【为什么不再用“箱子显示名”分组】
    /// 旧实现按 chest.name / Lang.chestType 这类“显示名字符串”分池，
    /// 于是“金字塔里的金箱”（装着沙暴瓶、飞毯、法老套）和普通地下金箱被当成同一类，
    /// 沙暴瓶因此扩散到了全世界的金箱。
    ///
    /// 现在改为结构性分组：
    ///     分组键 = (箱子图格类型, 图格样式) + 环境标签 + 深度带
    /// 图格类型 + 样式是箱子的“结构性身份”，不受语言、chest.name、模组改名影响；
    /// 环境与深度则用来区分原版掉落表本来就不同的同种箱子。
    ///
    /// 【已知边界】
    /// 金字塔金箱和“沙漠里同深度带的普通金箱”图格完全一样，只靠环境+深度无法再细分；
    /// 打开 EnableChestItemChainDebugLog 后日志里会打印每个分组的物品池与每个箱子的分组键，
    /// 如果实测发现它们仍混在一起，再加一条“金字塔结构”判定即可。
    /// ═══════════════════════════════════════════════════════════════════
    ///
    /// 【参与物品】
    ///     第一格的物品（空格则忽略这一条） + 所有“可装备”物品
    ///     可装备 = 护甲/时装（头、身、腿）+ 饰品 + 坐骑/矿车 + 宠物/照明宠物 + 抓钩
    ///     其他一律不参与（武器、工具、材料、药水、方块、钱币、弹药…）
    ///
    /// 【额外生成】
    ///     对箱子里每个参与物品，取同组物品池中“下一个”物品，放进空槽位（允许与已有物品重复）。
    ///     若某个组的池子里只有 1 件物品（例如小世界里唯一的生命木宝箱 + 雀杖），该组整体跳过。
    ///
    /// 由 LLLServerConfig.EnableChestItemChain 控制；只影响新生成的世界，且带有世界级“已处理”标记，不会重复加。
    /// </summary>
    public class ChestItemEnhancerSystem : ModSystem
    {
        /// <summary>世界存档里的“已处理”标记。</summary>
        private const string SaveKeyApplied = "ChestItemEnhancerApplied";

        /// <summary>延迟处理标志：PostWorldGen 触发，等世界生成彻底结束后再执行。</summary>
        private bool _pendingProcess;
        private int _delayCounter;

        /// <summary>触发时所在世界的 ID，用于防止“换了世界还把旧任务执行到新世界”。</summary>
        private int _pendingWorldId;

        /// <summary>本世界是否已经执行过（随世界存档保存）。</summary>
        private bool _applied;

        public override void ClearWorld()
        {
            _applied = false;
            ChestEnvironmentClassifier.ClearCache();

            // 刻意不在 ClearWorld 里清掉 _pendingProcess：
            // tModLoader 可能在“世界生成完成 → 真正进入世界”的加载过程中调用 ClearWorld，
            // 如果在这里清掉待处理标志，整个功能会被静默取消（日志里什么都看不到）。
            // 万一真的换了世界，由 _pendingWorldId 兜底放弃。
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (_applied)
                tag[SaveKeyApplied] = true;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            _applied = tag.GetBool(SaveKeyApplied);
        }

        /// <summary>
        /// 世界生成完成后触发。此时宝箱已生成，但可能还有其它模组在 PostWorldGen 里补箱子，
        /// 所以延迟 120 帧（约 2 秒）再执行。
        /// </summary>
        public override void PostWorldGen()
        {
            if (_applied) return;
            if (!ModContent.GetInstance<LLLServerConfig>().EnableChestItemChain) return;

            _pendingProcess = true;
            _delayCounter = 120;
            _pendingWorldId = Main.worldID;
        }

        public override void PostUpdateEverything()
        {
            if (!_pendingProcess) return;

            // 世界生成只发生在服务器/单人端；联机客户端万一残留了标志也直接丢掉。
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                _pendingProcess = false;
                return;
            }

            if (_delayCounter > 0)
            {
                _delayCounter--;
                return;
            }

            _pendingProcess = false;

            if (Main.worldID != _pendingWorldId)
            {
                if (ModContent.GetInstance<LLLServerConfig>().EnableChestItemChainDebugLog)
                    Mod.Logger.Info($"[宝箱额外物品] 世界已切换（{_pendingWorldId} → {Main.worldID}），放弃本次待处理任务。");
                return;
            }

            try
            {
                Apply();
                _applied = true;

                // 把“额外物品 + 已处理标记”立刻一起落盘：
                // 世界创建完成时游戏已经存过一次档，不补存的话这份改动要等下一次自动存档才写进文件。
                // 保存本身单独 try：物品已经加进内存了，不能因为保存失败就把整个流程当成失败。
                try
                {
                    WorldFile.SaveWorld();
                }
                catch (System.Exception saveException)
                {
                    Mod.Logger.Error("[宝箱额外物品] 世界保存失败（物品已加入内存，会在下次自动存档时落盘）：" + saveException);
                }
            }
            catch (System.Exception exception)
            {
                Mod.Logger.Error("[宝箱额外物品] 执行失败：" + exception);
            }
        }

        /// <summary>核心处理逻辑。</summary>
        private int Apply()
        {
            LLLServerConfig config = ModContent.GetInstance<LLLServerConfig>();
            bool debug = config.EnableChestItemChainDebugLog;

            // ═══════════════════════════════════════════════════════════
            // 第一遍：按分组键收集物品池
            // ═══════════════════════════════════════════════════════════
            var pools = new Dictionary<ChestGroupKey, HashSet<int>>();
            // 第一遍就把“箱子 → 分组键”固化下来，第三遍直接复用，
            // 避免将来两遍的判定规则改得不一致时静默用错分组。
            var targets = new List<(Chest Chest, int Index, ChestGroupKey Key)>();
            int chestCount = 0;

            for (int i = 0; i < Main.chest.Length; i++)
            {
                Chest chest = Main.chest[i];
                if (!IsEnhanceableChest(chest)) continue;

                ChestGroupKey key = GetGroupKey(chest);
                targets.Add((chest, i, key));
                chestCount++;

                if (!pools.TryGetValue(key, out HashSet<int> pool))
                {
                    pool = new HashSet<int>();
                    pools[key] = pool;
                }

                for (int slot = 0; slot < chest.item.Length; slot++)
                {
                    Item item = chest.item[slot];
                    if (item == null || item.IsAir) continue;

                    // 参与规则：第一格物品（默认）+ 所有可装备
                    if (slot == 0 || IsEquippable(item))
                        pool.Add(item.type);
                }
            }

            // ═══════════════════════════════════════════════════════════
            // 第二遍：排序成池，并丢掉“只有 1 件物品”的组
            // ═══════════════════════════════════════════════════════════
            var sortedPools = new Dictionary<ChestGroupKey, int[]>();
            int skippedGroups = 0;

            foreach (KeyValuePair<ChestGroupKey, HashSet<int>> pair in pools)
            {
                if (pair.Value.Count < 2)
                {
                    skippedGroups++;
                    if (debug)
                        Mod.Logger.Info($"[宝箱额外物品] 跳过分组 {pair.Key}：池中只有 {pair.Value.Count} 件物品");
                    continue;
                }

                int[] sorted = pair.Value.ToArray();
                System.Array.Sort(sorted);
                sortedPools[pair.Key] = sorted;

                if (debug)
                {
                    string list = string.Join(", ", sorted.Select(type => $"{Lang.GetItemNameValue(type)}({type})"));
                    Mod.Logger.Info($"[宝箱额外物品] 分组 {pair.Key}：池({sorted.Length})=[{list}]");
                }
            }

            // ═══════════════════════════════════════════════════════════
            // 第三遍：给每个箱子补物品
            // ═══════════════════════════════════════════════════════════
            var toAdd = new List<int>();
            int touchedChests = 0;
            int addedTotal = 0;

            foreach ((Chest chest, int chestIndex, ChestGroupKey key) in targets)
            {
                if (!sortedPools.TryGetValue(key, out int[] pool)) continue;

                toAdd.Clear();
                for (int slot = 0; slot < chest.item.Length; slot++)
                {
                    Item item = chest.item[slot];
                    if (item == null || item.IsAir) continue;
                    if (slot != 0 && !IsEquippable(item)) continue;

                    int index = System.Array.BinarySearch(pool, item.type);
                    if (index < 0) continue;

                    // 取池中“下一个”（越界则回到开头）；允许与箱内已有物品重复
                    toAdd.Add(pool[(index + 1) % pool.Length]);
                }

                if (toAdd.Count == 0) continue;

                int addedHere = 0;
                foreach (int addType in toAdd)
                {
                    int emptySlot = FindEmptySlot(chest);
                    if (emptySlot == -1) break; // 箱子满了

                    Item newItem = chest.item[emptySlot];
                    if (newItem == null)
                    {
                        newItem = new Item();
                        chest.item[emptySlot] = newItem;
                    }
                    newItem.SetDefaults(addType);

                    // 消耗性投掷武器给一叠，其余（装备等）给 1 件；注意不要超过物品自身的堆叠上限
                    newItem.stack = newItem.consumable && newItem.damage > 0
                        ? System.Math.Min(Main.rand.Next(20, 100), newItem.maxStack)
                        : 1;

                    addedHere++;
                    addedTotal++;
                }

                if (addedHere <= 0) continue;

                touchedChests++;

                if (debug)
                {
                    string names = string.Join(", ",
                        toAdd.Take(addedHere).Select(type => $"{Lang.GetItemNameValue(type)}({type})"));
                    Mod.Logger.Info($"[宝箱额外物品] 箱子#{chestIndex} ({chest.x},{chest.y}) {key} 额外生成 {addedHere} 件：[{names}]");
                }
            }

            Mod.Logger.Info($"[宝箱额外物品] 完成：可处理箱子 {chestCount} 个，分组 {pools.Count} 个（跳过池<2 的 {skippedGroups} 个），" +
                            $"实际修改 {touchedChests} 个箱子，新增物品 {addedTotal} 件。");
            return addedTotal;
        }

        // ═══════════════════════════════════════════════════════════
        // 分组键：箱子图格身份 + 环境 + 深度带
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 分组键。用它取代旧的“显示名字符串”，不同箱子种类/环境/深度的池子互不干扰。
        /// </summary>
        private readonly struct ChestGroupKey : System.IEquatable<ChestGroupKey>
        {
            public readonly int TileType;
            public readonly int Style;
            public readonly ChestEnvironment Environment;
            public readonly ChestDepthBand Depth;

            public ChestGroupKey(int tileType, int style, ChestEnvironment environment, ChestDepthBand depth)
            {
                TileType = tileType;
                Style = style;
                Environment = environment;
                Depth = depth;
            }

            public bool Equals(ChestGroupKey other)
            {
                return TileType == other.TileType && Style == other.Style &&
                       Environment == other.Environment && Depth == other.Depth;
            }

            public override bool Equals(object obj) => obj is ChestGroupKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = TileType * 397;
                    hash = (hash ^ Style) * 397;
                    hash = (hash ^ (int)Environment) * 397;
                    return hash ^ (int)Depth;
                }
            }

            public override string ToString()
            {
                return $"tile={TileType}/style={Style}/env={Environment}/depth={Depth}";
            }
        }

        private static ChestGroupKey GetGroupKey(Chest chest)
        {
            Tile tile = Main.tile[chest.x, chest.y];
            return new ChestGroupKey(
                tile.TileType,
                tile.TileFrameX / 36,
                ChestEnvironmentClassifier.GetEnvironment(chest.x, chest.y),
                ChestEnvironmentClassifier.GetDepthBand(chest.y));
        }

        // ═══════════════════════════════════════════════════════════
        // 参与规则
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 是否是“可装备”物品：
        /// 护甲/时装（头、身、腿）、饰品（含翅膀、气球、鞋、盾…）、坐骑/矿车、宠物/照明宠物、抓钩。
        /// </summary>
        private static bool IsEquippable(Item item)
        {
            if (item == null || item.IsAir) return false;

            // 饰品（翅膀、气球、鞋、盾等都在这里）
            if (item.accessory) return true;

            // 护甲 / 时装
            if (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0) return true;

            // 坐骑 / 矿车
            if (item.mountType >= 0) return true;

            // 宠物 / 照明宠物（两个数组长度取小值，避免索引越界）
            int petBuffCount = System.Math.Min(Main.vanityPet.Length, Main.lightPet.Length);
            if (item.buffType > 0 && item.buffType < petBuffCount &&
                (Main.vanityPet[item.buffType] || Main.lightPet[item.buffType]))
            {
                return true;
            }

            // 抓钩
            if (item.shoot > 0 && item.shoot < Main.projHook.Length && Main.projHook[item.shoot]) return true;

            return false;
        }

        /// <summary>是否是本系统要处理的箱子（原版风格的普通箱子）。</summary>
        private static bool IsEnhanceableChest(Chest chest)
        {
            if (chest == null || chest.item == null) return false;
            if (chest.x < 0 || chest.y < 0 || chest.x >= Main.maxTilesX || chest.y >= Main.maxTilesY) return false;

            Tile tile = Main.tile[chest.x, chest.y];
            if (tile == null || !tile.HasTile) return false;

            int type = tile.TileType;
            if (type < 0 || type >= TileID.Sets.BasicChest.Length) return false;

            return TileID.Sets.BasicChest[type];
        }

        /// <summary>查找宝箱中的第一个空槽位，找不到返回 -1。</summary>
        private static int FindEmptySlot(Chest chest)
        {
            for (int i = 0; i < chest.item.Length; i++)
            {
                if (chest.item[i] == null || chest.item[i].IsAir)
                    return i;
            }
            return -1;
        }
    }
}
