using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.Localization;
using Terraria.Map;

namespace LLL.Items
{
    /// <summary>
    /// 宝物魔镜：传送至随机未开启的宝箱位置，并在工具提示中显示全图宝箱统计。
    /// 右键可切换为花之魔镜。
    /// 带有智能缓存，每秒更新一次宝箱数据，极大降低性能开销。
    /// </summary>
    public class TreasureMagicMirror : ModItem
    {
        // ═══════════════════════════════════════════════════════════════
        // 物品基础属性
        // ═══════════════════════════════════════════════════════════════
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.consumable = false;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = SoundID.Item6;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(0, 5, 0, 0);
        }

        // ═══════════════════════════════════════════════════════════════
        // 🔄 形态切换（右键切换为花之魔镜）
        // ═══════════════════════════════════════════════════════════════
        public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                int currentPrefix = Item.prefix;
                bool isFavorited = Item.favorited;
                Item.SetDefaults(ModContent.ItemType<PlanteraMagicMirror>());
                Item.Prefix(currentPrefix);
                Item.favorited = isFavorited;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                return false;
            }
            return true;
        }

        public override bool CanRightClick() => true;
        public override void RightClick(Player player)
        {
            int currentPrefix = Item.prefix;
            bool isFavorited = Item.favorited;
            Item.SetDefaults(ModContent.ItemType<PlanteraMagicMirror>());
            Item.Prefix(currentPrefix);
            Item.favorited = isFavorited;
        }

        public override bool ConsumeItem(Player player) => false;

        // ═══════════════════════════════════════════════════════════════
        // 🗺️ 核心传送逻辑
        // ═══════════════════════════════════════════════════════════════
        public override bool? UseItem(Player player)
        {
            TeleportToRandomChest(player);
            return true;
        }

        // ─── 宝箱有效性过滤器 ──────────────────────────────────────────
        // 📐 边界死区：40格 → 距离世界边缘至少40格（约640像素），
        //    防止传送到地图边界外或生成在不可站立的位置。
        //    该值也可调大（如60）以更安全，但会减少可选宝箱数量。
        private bool IsValidTargetChest(Chest chest)
        {
            if (chest == null) return false;

            if (chest.x < 40 || chest.x > Main.maxTilesX - 40 ||
                chest.y < 40 || chest.y > Main.maxTilesY - 40)
                return false;

            Tile tile = Main.tile[chest.x, chest.y];
            if (tile == null || !tile.HasTile || !TileID.Sets.BasicChest[tile.TileType])
                return false;

            // 注意：不检查 chest.item 是否为空，因为空箱子也可作为传送点
            return true;
        }

        private void TeleportToRandomChest(Player player)
        {
            List<Vector2> chestPositions = new List<Vector2>();

            // 遍历所有宝箱（最多 8000 个）
            for (int i = 0; i < Main.chest.Length; i++)
            {
                if (IsValidTargetChest(Main.chest[i]))
                {
                    chestPositions.Add(new Vector2(Main.chest[i].x * 16f, Main.chest[i].y * 16f));
                }
            }

            if (chestPositions.Count > 0)
            {
                Vector2 targetPosition = Main.rand.Next(chestPositions);

                // ✨ 传送特效（起飞和落地各20个粒子）
                //    粒子类型：金币 + 魔法镜碎片
                //    DustID.GoldCoin → 金色闪光
                //    DustID.MagicMirror → 蓝紫色魔法粒子
                //    📐 数量 20 可调（10~40），影响华丽程度。
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(player.position, player.width, player.height,
                        DustID.GoldCoin, 0f, 0f, 150, default(Color), 1.5f);
                    Dust.NewDust(player.position, player.width, player.height,
                        DustID.MagicMirror, 0f, 0f, 150, default(Color), 1.2f);
                }

                player.Teleport(targetPosition, 1);
                player.velocity = Vector2.Zero;

                // 落地特效
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(player.position, player.width, player.height,
                        DustID.GoldCoin, 0f, 0f, 150, default(Color), 1.5f);
                    Dust.NewDust(player.position, player.width, player.height,
                        DustID.MagicMirror, 0f, 0f, 150, default(Color), 1.2f);
                }
            }
            else
            {
                string failText = Language.GetTextValue("Mods.LLL.Items.TreasureMagicMirror.NoChestsFound");
                Main.NewText(failText, Color.Red);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 🏷️ 获取宝箱名称（支持原版、模组、玩家自定义）
        // ═══════════════════════════════════════════════════════════════
        private string GetChestName(Chest chest)
        {
            // 1. 玩家自定义名字（铁砧命名）优先级最高
            if (!string.IsNullOrEmpty(chest.name))
                return chest.name;

            Tile tile = Main.tile[chest.x, chest.y];
            int type = tile.TileType;
            int style = tile.TileFrameX / 36; // 每个宝箱样式占用36像素宽度

            // 2. 原版宝箱：使用官方本地化数组
            if (type == TileID.Containers && style < Lang.chestType.Length)
                return Lang.chestType[style].Value;
            if (type == TileID.Containers2 && style < Lang.chestType2.Length)
                return Lang.chestType2[style].Value;

            // 3. Mod 宝箱兜底：通过掉落物名称
            int dropItemId = TileLoader.GetItemDropFromTypeAndStyle(type, style);
            if (dropItemId > 0)
                return Lang.GetItemNameValue(dropItemId);

            // 4. Mod 宝箱：直接取 ModTile 名称
            ModTile modTile = TileLoader.GetTile(type);
            if (modTile != null)
                return modTile.Name;

            // 5. 终极兜底
            return Language.GetTextValue("Mods.LLL.Items.TreasureMagicMirror.UnknownChest");
        }

        // ═══════════════════════════════════════════════════════════════
        // 📊 工具提示 —— 显示宝箱统计（带缓存）
        // ═══════════════════════════════════════════════════════════════
        // 缓存策略：每60帧（约1秒）重新扫描一次全图宝箱，
        // 避免每帧遍历8000个宝箱影响性能。
        private static Dictionary<string, int> _cachedChestStats = new Dictionary<string, int>();
        private static int _cachedValidChestCount = 0;
        private static uint _lastUpdateFrame = 0;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (Main.gameMenu) return;

            // 🚀 性能优化：每隔60帧（1秒）更新一次缓存
            //    60帧是性能和实时性的良好平衡点。
            //    可调范围：30~120。值越小更新越频繁，但性能消耗也越大。
            if (Main.GameUpdateCount - _lastUpdateFrame > 60 || _lastUpdateFrame == 0)
            {
                _cachedChestStats.Clear();
                _cachedValidChestCount = 0;

                for (int i = 0; i < Main.chest.Length; i++)
                {
                    Chest chest = Main.chest[i];
                    if (IsValidTargetChest(chest))
                    {
                        _cachedValidChestCount++;

                        string chestName = GetChestName(chest);
                        if (!_cachedChestStats.ContainsKey(chestName))
                            _cachedChestStats[chestName] = 0;
                        _cachedChestStats[chestName]++;
                    }
                }

                _lastUpdateFrame = Main.GameUpdateCount;
            }

            // 替换描述中的 {0} 为有效宝箱总数
            foreach (TooltipLine line in tooltips)
            {
                if (line.Text.Contains("{0}"))
                {
                    line.Text = line.Text.Replace("{0}", _cachedValidChestCount.ToString());
                }
            }

            // 如果有宝箱，显示分类统计列表
            if (_cachedValidChestCount > 0)
            {
                string headerText = Language.GetTextValue("Mods.LLL.Items.TreasureMagicMirror.ChestListHeader");
                tooltips.Add(new TooltipLine(Mod, "ChestStatsHeader", headerText) { OverrideColor = Color.Gray });

                foreach (var kvp in _cachedChestStats)
                {
                    tooltips.Add(new TooltipLine(Mod, $"ChestStat_{kvp.Key}",
                        $"{kvp.Key}: {kvp.Value}") { OverrideColor = Color.LightGoldenrodYellow });
                }
            }
            else
            {
                // 没有宝箱时显示红色警告
                string failText = Language.GetTextValue("Mods.LLL.Items.TreasureMagicMirror.NoChestsFound");
                tooltips.Add(new TooltipLine(Mod, "NoChestsWarning", failText) { OverrideColor = Color.Red });
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 🔧 合成配方
        // ═══════════════════════════════════════════════════════════════
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MagicMirror, 1);
            recipe.AddIngredient(ItemID.FallenStar, 10);
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }
    }
}