using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace LLL.Systems
{
    /// <summary>
    /// 宝箱所处的环境标签。
    /// 目的只是把“原版掉落表本来就不一样”的箱子拆进不同的组，不追求和游戏内生物群系判定完全一致。
    /// </summary>
    public enum ChestEnvironment
    {
        /// <summary>没有任何群系特征（普通的地下/洞穴小屋）。</summary>
        None = 0,
        /// <summary>海洋 / 海滩。</summary>
        Ocean,
        /// <summary>高空 / 空岛。</summary>
        Sky,
        /// <summary>雪原 / 地下冰雪。</summary>
        Snow,
        /// <summary>沙漠 / 地下沙漠 / 金字塔（砂岩建筑）。</summary>
        Desert,
        /// <summary>丛林。</summary>
        Jungle,
        /// <summary>发光蘑菇地。</summary>
        GlowingMushroom,
        /// <summary>腐化。</summary>
        Corruption,
        /// <summary>猩红。</summary>
        Crimson,
        /// <summary>神圣。</summary>
        Hallow,
        /// <summary>地牢。</summary>
        Dungeon,
        /// <summary>神庙。</summary>
        Temple,
        /// <summary>花岗岩。</summary>
        Granite,
        /// <summary>大理石。</summary>
        Marble,
        /// <summary>蜂巢。</summary>
        BeeHive,
        /// <summary>蜘蛛巢。</summary>
        SpiderNest,
    }

    /// <summary>
    /// 深度带。原版金箱的“地下掉落表”和“洞穴掉落表”就是按深度分开的，所以分组时要带上它。
    /// </summary>
    public enum ChestDepthBand
    {
        Surface = 0,
        Underground,
        Cavern,
        Underworld,
    }

    /// <summary>
    /// 判定宝箱所处的环境与深度带。
    ///
    /// 【为什么不用原版生物群系判定】
    /// 我们只需要一个“够用的粗分类”：把图格类型/样式相同、但实际掉落表不同的箱子拆开就够了
    /// （最典型的就是“金字塔里的金箱”和“普通地下金箱”，两者图格完全一样）。
    /// 自己扫描的好处是：不依赖任何 API 细节、结果可解释、能和调试日志对上。
    ///
    /// 【算法】
    /// 以所在 40 格网格的中心为中心，扫描 ±40 格的图格 + 墙壁，按类别累计命中数：
    ///     墙壁对“箱子处在什么建筑里”指示性很强（地牢砖墙 / 神庙砖墙 / 砂岩砖墙 / 蜘蛛巢墙…）
    /// 判定顺序：
    ///     ① 地牢 / 神庙（结构性地标，命中数达标就直接采用，避免被海洋或周围群系抢走）
    ///     ② 海洋 / 海滩（横向靠近世界边缘、不太深，且附近确实有水或已经到最外侧）
    ///     ③ 空岛（云 / 雨云命中数达标）
    ///     ④ 其余环境取命中数最多的一个（必须达到 MinScore），同分时按代码中的先后顺序取
    /// 同一个 40 格网格内的箱子共用一次扫描结果。
    /// </summary>
    public static class ChestEnvironmentClassifier
    {
        /// <summary>扫描半径（格），同时也是环境缓存网格的边长。</summary>
        private const int ScanRadius = 40;

        /// <summary>
        /// 某个环境至少要有这么多“图格 + 墙壁”命中才算数。
        /// 注意：同一格的图格与墙壁会各计一次，所以这个值大致相当于 20 个独立方块。
        /// </summary>
        private const int MinScore = 40;

        /// <summary>游戏内海洋的横向范围（与原版一致）。</summary>
        private const int OceanEdge = 380;

        /// <summary>最外侧这一段无论有没有水都按海洋算（海底箱子常常在洞里）。</summary>
        private const int OceanOuter = 150;

        /// <summary>按 40 格网格缓存扫描结果：同一间小屋里的多个箱子只扫一次，且结果与查询顺序无关。</summary>
        private static readonly Dictionary<(int, int), ChestEnvironment> _cellCache =
            new Dictionary<(int, int), ChestEnvironment>();

        /// <summary>世界卸载时清空缓存。</summary>
        public static void ClearCache() => _cellCache.Clear();

        /// <summary>按 y 坐标判断深度带。</summary>
        public static ChestDepthBand GetDepthBand(int y)
        {
            if (y >= Main.UnderworldLayer) return ChestDepthBand.Underworld;
            if (y > Main.rockLayer) return ChestDepthBand.Cavern;
            if (y > Main.worldSurface) return ChestDepthBand.Underground;
            return ChestDepthBand.Surface;
        }

        /// <summary>
        /// 判断箱子（x, y 为图格坐标）所处的环境。
        /// 以网格中心为扫描中心，保证“同一格的箱子结果一致”，不受 Main.chest 遍历顺序影响。
        /// </summary>
        public static ChestEnvironment GetEnvironment(int x, int y)
        {
            int cellX = x / ScanRadius;
            int cellY = y / ScanRadius;
            (int, int) cell = (cellX, cellY);

            if (_cellCache.TryGetValue(cell, out ChestEnvironment cached))
                return cached;

            ChestEnvironment environment = Scan(cellX * ScanRadius + ScanRadius / 2, cellY * ScanRadius + ScanRadius / 2);
            _cellCache[cell] = environment;
            return environment;
        }

        private static ChestEnvironment Scan(int cx, int cy)
        {
            int left = cx - ScanRadius;
            if (left < 1) left = 1;
            int right = cx + ScanRadius;
            if (right > Main.maxTilesX - 2) right = Main.maxTilesX - 2;
            int top = cy - ScanRadius;
            if (top < 1) top = 1;
            int bottom = cy + ScanRadius;
            if (bottom > Main.maxTilesY - 2) bottom = Main.maxTilesY - 2;

            int dungeon = 0, temple = 0, sky = 0, hive = 0, spider = 0, granite = 0, marble = 0;
            int mushroom = 0, snow = 0, desert = 0, jungle = 0, corrupt = 0, crimson = 0, hallow = 0;
            bool hasWater = false;

            for (int i = left; i <= right; i++)
            {
                for (int j = top; j <= bottom; j++)
                {
                    Tile tile = Main.tile[i, j];
                    if (tile == null) continue;

                    if (!hasWater && tile.LiquidAmount > 0 && tile.LiquidType == 0)
                        hasWater = true;

                    if (tile.HasTile)
                    {
                        int type = tile.TileType;
                        if (IsDungeonBrick(type)) dungeon++;
                        else if (type == TileID.LihzahrdBrick) temple++;
                        else if (type == TileID.Cloud || type == TileID.RainCloud) sky++;
                        else if (type == TileID.Hive) hive++;
                        else if (type == TileID.SnowBlock || type == TileID.IceBlock ||
                                 type == TileID.BreakableIce) snow++;
                        else if (type == TileID.Sand || type == TileID.HardenedSand ||
                                 type == TileID.Sandstone || type == TileID.SandstoneBrick ||
                                 type == TileID.DesertFossil) desert++;
                        else if (type == TileID.Ebonsand || type == TileID.CorruptSandstone ||
                                 type == TileID.CorruptHardenedSand || type == TileID.Ebonstone) corrupt++;
                        else if (type == TileID.Crimsand || type == TileID.CrimsonSandstone ||
                                 type == TileID.CrimsonHardenedSand || type == TileID.Crimstone) crimson++;
                        else if (type == TileID.Pearlsand || type == TileID.HallowSandstone ||
                                 type == TileID.HallowHardenedSand || type == TileID.Pearlstone) hallow++;
                        else if (type == TileID.JungleGrass || type == TileID.Mud) jungle++;
                        else if (type == TileID.MushroomGrass || type == TileID.MushroomBlock) mushroom++;
                        else if (type == TileID.Granite) granite++;
                        else if (type == TileID.Marble) marble++;
                    }

                    int wall = tile.WallType;
                    if (wall <= 0) continue;

                    if (IsDungeonWall(wall)) dungeon++;
                    else if (wall == WallID.LihzahrdBrickUnsafe) temple++;
                    else if (wall == WallID.HiveUnsafe) hive++;
                    else if (wall == WallID.SpiderUnsafe) spider++;
                    else if (wall == WallID.GraniteUnsafe) granite++;
                    else if (wall == WallID.MarbleUnsafe) marble++;
                    else if (wall == WallID.MushroomUnsafe) mushroom++;
                    else if (wall == WallID.SnowWallUnsafe) snow++;
                    else if (wall == WallID.SandstoneBrick) desert++;
                    else if (wall == WallID.JungleUnsafe || (wall >= WallID.JungleUnsafe1 && wall <= WallID.JungleUnsafe4)) jungle++;
                    else if (wall == WallID.EbonstoneUnsafe) corrupt++;
                    else if (wall == WallID.CrimstoneUnsafe) crimson++;
                    else if (wall == WallID.PearlstoneBrickUnsafe) hallow++;
                }
            }

            // ── ① 结构性地标优先：地牢/神庙不可能被误判成海洋或普通群系 ──
            //    注意：原版地牢的位置经常就落在“海洋”的横向范围（x < 380）里，
            //    所以必须先判地牢，否则地牢箱子会被打上海洋标签、被拆成两组。
            if (dungeon >= MinScore) return ChestEnvironment.Dungeon;
            if (temple >= MinScore) return ChestEnvironment.Temple;

            // ── ② 海洋/海滩：靠近世界边缘、不太深，而且要真的靠海（附近有水）或已经在最外侧 ──
            bool nearEdge = cx < OceanEdge || cx > Main.maxTilesX - OceanEdge;
            bool farEdge = cx < OceanOuter || cx > Main.maxTilesX - OceanOuter;
            if (nearEdge && cy < Main.rockLayer && (hasWater || farEdge))
                return ChestEnvironment.Ocean;

            // ── ③ 空岛：云 / 雨云 ──
            if (sky >= MinScore) return ChestEnvironment.Sky;

            // ── ④ 其它环境：取命中数最多的一个（顺序即优先级，同分时取靠前的）──
            var scores = new (ChestEnvironment Environment, int Score)[]
            {
                (ChestEnvironment.SpiderNest, spider),
                (ChestEnvironment.BeeHive, hive),
                (ChestEnvironment.Granite, granite),
                (ChestEnvironment.Marble, marble),
                (ChestEnvironment.GlowingMushroom, mushroom),
                (ChestEnvironment.Snow, snow),
                (ChestEnvironment.Desert, desert),
                (ChestEnvironment.Jungle, jungle),
                (ChestEnvironment.Corruption, corrupt),
                (ChestEnvironment.Crimson, crimson),
                (ChestEnvironment.Hallow, hallow),
            };

            ChestEnvironment best = ChestEnvironment.None;
            int bestScore = MinScore - 1; // 必须 >= MinScore 才算命中
            foreach ((ChestEnvironment environment, int score) in scores)
            {
                if (score > bestScore)
                {
                    best = environment;
                    bestScore = score;
                }
            }

            if (best != ChestEnvironment.None)
                return best;

            // 高空且没有任何群系特征 → 天空
            if (cy < Main.worldSurface * 0.35)
                return ChestEnvironment.Sky;

            return ChestEnvironment.None;
        }

        private static bool IsDungeonBrick(int type)
        {
            return type == TileID.BlueDungeonBrick || type == TileID.GreenDungeonBrick ||
                   type == TileID.PinkDungeonBrick || type == TileID.CrackedBlueDungeonBrick ||
                   type == TileID.CrackedGreenDungeonBrick || type == TileID.CrackedPinkDungeonBrick;
        }

        private static bool IsDungeonWall(int wall)
        {
            return wall == WallID.BlueDungeonUnsafe || wall == WallID.BlueDungeonSlabUnsafe ||
                   wall == WallID.BlueDungeonTileUnsafe || wall == WallID.GreenDungeonUnsafe ||
                   wall == WallID.GreenDungeonSlabUnsafe || wall == WallID.GreenDungeonTileUnsafe ||
                   wall == WallID.PinkDungeonUnsafe || wall == WallID.PinkDungeonSlabUnsafe ||
                   wall == WallID.PinkDungeonTileUnsafe;
        }
    }
}
