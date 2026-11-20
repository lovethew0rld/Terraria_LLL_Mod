using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using LLL;

namespace LLL.Tiles
{
    /// <summary>
    /// 地牢破裂砖连锁破坏系统。
    /// 当玩家破坏任意一块破裂地牢砖（蓝/绿/粉）时，会触发连锁反应，
    /// 自动破坏所有相连的破裂砖（包括斜角相连），最多处理 1000 块。
    /// 
    /// 【设计理念】
    /// 原版地牢中大量分布的破裂砖，手动清理非常繁琐。
    /// 本功能让玩家"敲一块，清一片"，极大提升探索体验。
    /// 
    /// 【技术要点】
    /// 1. 两阶段处理：先 BFS 扫描记录所有目标坐标，再统一销毁。
    ///    避免在遍历过程中直接修改地图导致"漏算"或"索引错乱"。
    /// 2. 防重入锁：_isChainBreaking 防止连锁过程中再次触发连锁（死循环）。
    /// 3. 数量上限：1000 块，防止超大连接区域导致性能崩溃。
    /// 4. 八方向连通：包括斜角，让连锁更彻底。
    /// 
    /// 由 LLLServerConfig.EnableCrackedBrickChain 控制开关（默认开启）。
    /// </summary>
    public class CrackedBrickChain : GlobalTile
    {
        // 🔒 防重入锁：防止连锁过程中再次触发 KillTile 导致递归死循环
        //    当正在执行连锁破坏时，任何新的破裂砖破坏事件都会被忽略。
        private static bool _isChainBreaking = false;

        /// <summary>
        /// 当某块物块被破坏时触发（包括玩家挖掘、爆炸等）。
        /// 参数说明：
        ///   i, j       - 物块的瓦片坐标（以"格"为单位）
        ///   type       - 物块类型（TileID）
        ///   fail       - 是否破坏失败（如镐力不足）
        ///   effectOnly - 是否仅播放特效而不实际破坏
        ///   noItem     - 是否不产生掉落物
        /// </summary>
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            // 开关检查
            if (!ModContent.GetInstance<LLLServerConfig>().EnableCrackedBrickChain) return;

            // 如果破坏失败、仅特效模式、或正在连锁中 → 跳过
            if (fail || effectOnly || _isChainBreaking) return;

            // 只对三种破裂地牢砖生效
            if (type == TileID.CrackedBlueDungeonBrick ||
                type == TileID.CrackedGreenDungeonBrick ||
                type == TileID.CrackedPinkDungeonBrick)
            {
                // ⚡ 锁定，防止递归
                _isChainBreaking = true;

                // ═══════════════════════════════════════════════════════════
                // 📝 阶段一：BFS 扫描，记录所有相连的破裂砖坐标
                // ═══════════════════════════════════════════════════════════
                // 【为什么需要两阶段？】
                // 如果在 BFS 过程中直接调用 WorldGen.KillTile，会立即修改地图，
                // 同时可能触发新的 KillTile 事件，导致：
                // 1. 正在遍历的 Tile 引用失效（被销毁）
                // 2. 连锁触发自身，产生无限递归
                // 3. 漏算某些砖块（因为地图状态在遍历中变化）
                //
                // 所以：先"点名"（记录坐标），再"处决"（统一销毁）。

                List<Point> deathList = new List<Point>(); // 待销毁的坐标列表
                Queue<Point> queue = new Queue<Point>();   // BFS 队列
                HashSet<Point> visited = new HashSet<Point>(); // 已访问集合（防重复）

                Point startPoint = new Point(i, j);
                queue.Enqueue(startPoint);
                visited.Add(startPoint);

                // 📐 八方向搜索（包括斜角）
                //    对角连通让连锁更彻底，更符合"爆破"的直觉。
                Point[] directions = new Point[] {
                    new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1),     // 上下左右
                    new Point(-1, -1), new Point(1, -1), new Point(-1, 1), new Point(1, 1)    // 四个斜角
                };

                // 📐 数量上限 1000 块
                //    防止超大区域（如整个地牢连成一片）导致：
                //    - BFS 队列过大，内存占用飙升
                //    - 销毁 1000+ 块砖导致明显的卡顿
                //    可调范围 500~5000，1000 是安全且实用的值。
                while (queue.Count > 0 && deathList.Count < 1000)
                {
                    Point p = queue.Dequeue();
                    deathList.Add(p); // 将当前点加入"死亡名单"

                    // 检查八个方向的邻居
                    foreach (Point dir in directions)
                    {
                        int nx = p.X + dir.X;
                        int ny = p.Y + dir.Y;
                        Point neighbor = new Point(nx, ny);

                        // 如果该邻居已经访问过，跳过
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            Tile tile = Main.tile[nx, ny];

                            // ⚡ 关键：只检查 Tile 类型，不实际修改！
                            //    这样即使在扫描过程中有其它物块被破坏，也不影响本次扫描的完整性。
                            if (tile != null && tile.HasTile &&
                                (tile.TileType == TileID.CrackedBlueDungeonBrick ||
                                 tile.TileType == TileID.CrackedGreenDungeonBrick ||
                                 tile.TileType == TileID.CrackedPinkDungeonBrick))
                            {
                                queue.Enqueue(neighbor);
                            }
                        }
                    }
                }

                // ═══════════════════════════════════════════════════════════
                // 💥 阶段二：统一销毁所有记录的破裂砖
                // ═══════════════════════════════════════════════════════════
                foreach (Point target in deathList)
                {
                    // 跳过玩家正在敲的那块砖（原版 KillTile 已经处理了它）
                    // 如果这里再次尝试销毁它，可能导致双重掉落或冲突。
                    if (target.X == i && target.Y == j) continue;

                    // 📐 参数说明：
                    //   fail: false     → 强制成功，无视镐力
                    //   effectOnly: false → 实际破坏，不只是特效
                    //   noItem: false   → 正常产生掉落物（让玩家获得砖块材料）
                    WorldGen.KillTile(target.X, target.Y,
                        fail: false,
                        effectOnly: false,
                        noItem: false);
                }

                // 解锁，允许后续的连锁触发
                _isChainBreaking = false;
            }
        }
    }
}