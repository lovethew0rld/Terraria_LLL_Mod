using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.Localization;

namespace LLL.Items
{
    /// <summary>
    /// 花之魔镜：传送至随机的世纪之花苞位置。
    /// 右键可切换回宝物魔镜（形态切换）。
    /// 带缓存优化，避免每帧扫描全图。
    /// </summary>
    public class PlanteraMagicMirror : ModItem
    {
        // ═══════════════════════════════════════════════════════════════
        // 📦 缓存变量（性能优化）
        // ═══════════════════════════════════════════════════════════════
        private static List<Vector2> _cachedBulbPositions = new List<Vector2>(); // 缓存的花苞位置（世界坐标）
        private static bool _hasScanned = false;
        private static uint _lastScanFrame = 0; // 上次扫描时的游戏帧数

        // ═══════════════════════════════════════════════════════════════
        // 物品基础属性
        // ═══════════════════════════════════════════════════════════════
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.consumable = false;
            Item.useTime = 30;          // 使用时间0.5秒（30帧）
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = SoundID.Item6; // 魔法镜音效
            Item.rare = ItemRarityID.Lime; // 青柠色（世纪之花级别）
            Item.value = Item.sellPrice(0, 5, 0, 0);
        }

        // ═══════════════════════════════════════════════════════════════
        // 🔄 形态切换（右键切换为宝物魔镜）
        // ═══════════════════════════════════════════════════════════════
        public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2) // 右键（快捷栏外）
            {
                int currentPrefix = Item.prefix;
                bool isFavorited = Item.favorited;
                Item.SetDefaults(ModContent.ItemType<TreasureMagicMirror>());
                Item.Prefix(currentPrefix);
                Item.favorited = isFavorited;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                return false;
            }
            return true;
        }

        // 背包内右键切换（与快捷栏外逻辑一致）
        public override bool CanRightClick() => true;
        public override void RightClick(Player player)
        {
            int currentPrefix = Item.prefix;
            bool isFavorited = Item.favorited;
            Item.SetDefaults(ModContent.ItemType<TreasureMagicMirror>());
            Item.Prefix(currentPrefix);
            Item.favorited = isFavorited;
        }

        public override bool ConsumeItem(Player player) => false;

        // ═══════════════════════════════════════════════════════════════
        // 🔍 全图扫描花苞（性能敏感）
        // ═══════════════════════════════════════════════════════════════
        private void ScanForBulbs()
        {
            _cachedBulbPositions.Clear();

            // 遍历整个地图的每个格子（最多 4200×2400 ≈ 1000万格）
            for (int x = 0; x < Main.maxTilesX; x++)
            {
                for (int y = 0; y < Main.maxTilesY; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (tile.HasTile && tile.TileType == TileID.PlanteraBulb)
                    {
                        // 花苞是2x2多块结构，只抓取左上角（FrameX=0且FrameY=0）避免重复计数
                        if (tile.TileFrameX == 0 && tile.TileFrameY == 0)
                        {
                            // 存储世界坐标（像素）
                            _cachedBulbPositions.Add(new Vector2(x * 16f, y * 16f));
                        }
                    }
                }
            }
            _hasScanned = true;
            _lastScanFrame = Main.GameUpdateCount;
        }

        // ═══════════════════════════════════════════════════════════════
        // 🚀 使用物品 —— 传送至随机花苞
        // ═══════════════════════════════════════════════════════════════
        public override bool? UseItem(Player player)
        {
            ScanForBulbs();

            if (_cachedBulbPositions.Count > 0)
            {
                Vector2 targetPosition = Main.rand.Next(_cachedBulbPositions);
                targetPosition.X += 16f; // 调整到花苞中心
                targetPosition.Y -= 16f;

                // 🌸 特效生成（起飞前和落地后各一次）
                // 定义一个复用方法，避免重复代码
                Action<Vector2> spawnPlanteraDust = (pos) =>
                {
                    // 📐 粒子数量：35个（因为消失快，所以多一些）
                    //    可调范围 15~60，越多越华丽但性能消耗也大。
                    for (int i = 0; i < 35; i++)
                    {
                        int d = Dust.NewDust(pos, player.width, player.height,
                            DustID.Plantera_Pink, 0f, 0f, 0, default(Color), 2f);
                        Dust dust = Main.dust[d];

                        // 🔍 关键参数调优：
                        // 📐 noGravity = true → 粒子不坠落，呈云雾飘散状。
                        //    设为 false 则会受重力下落，形成“花瓣雨”效果。
                        dust.noGravity = true;

                        // 📐 fadeIn = 1.4f → 粒子快速缩小消失。
                        //    当 fadeIn > 1 时，粒子会迅速淡出（生命周期缩短）。
                        //    值越大消失越快。范围 0.5~3.0。
                        //    1.0 为正常消退；1.4 让粒子在约0.5秒内消失。
                        dust.fadeIn = 1.4f;

                        // 📐 alpha = 50 → 初始透明度（0~255），50为半透明，产生朦胧感。
                        dust.alpha = 50;

                        // 📐 初始速度：随机圆形方向，速度幅度 3f。
                        //    范围 1~6，越大炸开范围越广。
                        dust.velocity = Main.rand.NextVector2Circular(3f, 3f);
                        dust.velocity.Y -= 0.3f; // 轻微向上飘，增加灵动感
                    }
                };

                // 起飞特效
                spawnPlanteraDust(player.position);

                // 传送
                player.Teleport(targetPosition, 1);
                player.velocity = Vector2.Zero;

                // 落地特效
                spawnPlanteraDust(player.position);
            }
            else
            {
                string failText = Language.GetTextValue("Mods.LLL.Items.PlanteraMagicMirror.NoBulbsFound");
                Main.NewText(failText, Color.Pink);
            }
            return true;
        }

        // ═══════════════════════════════════════════════════════════════
        // 📋 工具提示 —— 显示剩余花苞数量（带缓存刷新）
        // ═══════════════════════════════════════════════════════════════
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (Main.gameMenu) return;

            // 🚀 性能保护：每30帧（0.5秒）最多扫描一次全图
            //    30帧是平衡实时性和性能的好选择。
            //    可调范围：15~120帧。值越小越实时，但性能消耗更大。
            if (!_hasScanned || Main.GameUpdateCount - _lastScanFrame > 30)
            {
                ScanForBulbs();
            }

            // 替换描述中的 {0} 为花苞数量
            foreach (TooltipLine line in tooltips)
            {
                if (line.Text.Contains("{0}"))
                {
                    line.Text = line.Text.Replace("{0}", _cachedBulbPositions.Count.ToString());
                }
            }

            // 如果没有花苞，额外显示红色警告
            if (_cachedBulbPositions.Count == 0)
            {
                string failText = Language.GetTextValue("Mods.LLL.Items.PlanteraMagicMirror.NoBulbsFound");
                tooltips.Add(new TooltipLine(Mod, "NoBulbsWarning", failText) { OverrideColor = Color.Red });
            }
        }
    }
}