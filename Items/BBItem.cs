using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace LLL.Items
{
    /// <summary>
    /// 初始物品袋（BBItem）—— 右键打开可获得大量基础资源。
    /// 角色第一次进入任何世界时可以获得一次，用于快速起步。
    /// </summary>
    public class BBItem : ModItem
    {
        /// <summary>
        /// 设置物品的基础属性。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 32;          // 物品图标宽度（像素）
            Item.height = 32;         // 物品图标高度（像素）
            Item.maxStack = 1;        // 最大堆叠数量为1，不可堆叠
            Item.consumable = true;   // 使用后物品消失（消耗品）
            Item.UseSound = SoundID.Item1;  // 使用音效：默认金属声
            Item.rare = 3;            // 物品稀有度（蓝色）
            Item.value = Item.sellPrice(0, 5, 0, 0);  // 售价5金币
        }

        /// <summary>
        /// 允许右键点击使用此物品。
        /// </summary>
        public override bool CanRightClick() => true;

        /// <summary>
        /// 右键点击时的具体逻辑：生成随机数量的石头、水晶和各种药水。
        /// </summary>
        public override void RightClick(Player player)
        {
            // 药水数量：10~30瓶
            int potionCount = Main.rand.Next(10, 31);
            // 生命水晶/魔力水晶数量：1~100个
            int crystalCount = Main.rand.Next(1, 101);
            // 石头数量：10~100个
            int stoneCount = Main.rand.Next(10, 101);

            // 基础材料
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.StoneBlock, stoneCount);

            // 生命与魔力水晶
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.LifeCrystal, crystalCount);
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.ManaCrystal, crystalCount);

            // ---- 各种药水（每种随机数量 10~30 瓶） ----
            // 防御/生存类
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.ObsidianSkinPotion, potionCount); // 黑曜石皮肤
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.RegenerationPotion, potionCount); // 再生
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.IronskinPotion, potionCount);     // 铁皮
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.EndurancePotion, potionCount);   // 耐力
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.LifeforcePotion, potionCount);   // 生命力
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.ThornsPotion, potionCount);      // 荆棘

            // 移动/探索类
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.SwiftnessPotion, potionCount);   // 敏捷
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.GillsPotion, potionCount);       // 鳃
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.WaterWalkingPotion, potionCount);// 水上行走
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.FlipperPotion, potionCount);     // 脚蹼
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.ShinePotion, potionCount);       // 光芒
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.NightOwlPotion, potionCount);    // 夜猫子
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.SpelunkerPotion, potionCount);   // 洞穴探险
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.HunterPotion, potionCount);      // 狩猎
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.TrapsightPotion, potionCount);   // 陷阱感应
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.WarmthPotion, potionCount);      // 温暖

            // 战斗/增益类
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.BattlePotion, potionCount);      // 战斗
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.ArcheryPotion, potionCount);     // 箭术
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.MagicPowerPotion, potionCount);  // 魔法力量
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.ManaRegenerationPotion, potionCount); // 魔力再生
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.RagePotion, potionCount);        // 暴怒
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.WrathPotion, potionCount);       // 怒气
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.InfernoPotion, potionCount);     // 狱火

            // 生产/采集类
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.MiningPotion, potionCount);      // 采矿
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.BuilderPotion, potionCount);     // 建筑

            // 钓鱼/运气类
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.FishingPotion, potionCount);     // 钓鱼
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.SonarPotion, potionCount);       // 声呐
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.CratePotion, potionCount);       // 宝箱
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.LuckPotionGreater, potionCount); // 幸运（高级）
        }
    }
}