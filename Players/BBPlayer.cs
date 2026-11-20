using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace LLL.Players
{
    /// <summary>
    /// 新手礼包（BBItem）领取逻辑。
    /// 每个玩家在每个世界只能领取一次，记录在玩家数据中。
    /// 进入世界时自动发放。
    /// </summary>
    public class BBPlayer : ModPlayer
    {
        /// <summary>
        /// 是否已领取过新手礼包（每个世界独立）
        /// </summary>
        public bool HasReceivedStarterGift;

        /// <summary>
        /// 玩家进入世界时触发。
        /// 如果尚未领取，则生成一个 BBItem 并标记已领取。
        /// </summary>
        public override void OnEnterWorld()
        {
            if (!HasReceivedStarterGift)
            {
                int giftBagType = ModContent.ItemType<Items.BBItem>();
                Player.QuickSpawnItem(Player.GetSource_Misc("PlayerEnterWorld"), giftBagType, 1);
                HasReceivedStarterGift = true;
            }
        }

        /// <summary>
        /// 保存玩家数据时记录领取状态。
        /// </summary>
        public override void SaveData(TagCompound tag)
        {
            tag["HasReceivedStarterGift"] = HasReceivedStarterGift;
        }

        /// <summary>
        /// 加载玩家数据时恢复领取状态。
        /// </summary>
        public override void LoadData(TagCompound tag)
        {
            HasReceivedStarterGift = tag.GetBool("HasReceivedStarterGift");
        }
    }
}