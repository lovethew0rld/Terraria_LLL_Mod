using Terraria;
using Terraria.ModLoader;
using LLL.Systems;

namespace LLL.Buffs
{
    /// <summary>
    /// 在召唤物 / 哨兵的 buff 描述末尾追加一行「当前数量 / 栏位上限（剩余空位）」，例如：
    ///     史莱姆宝宝将为你而战
    ///     当前 4/15（剩余 7 格）
    ///
    /// 这里是从 buff 反查：统计玩家身上“带这个 buff 的召唤物/哨兵”有几只，
    /// 所以原版与模组的召唤 buff 都能自动支持。由 LLLConfig.ShowMinionCount 控制开关。
    /// </summary>
    public class MinionCountGlobalBuff : GlobalBuff
    {
        public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
        {
            if (!ModContent.GetInstance<LLLConfig>().ShowMinionCount) return;

            Player player = Main.LocalPlayer;
            int count = MinionUsage.CountByBuff(player, type, out bool sentry);

            // 没带着这种召唤物就不加这一行（buff 一般也只在带着的时候才显示）
            if (count <= 0) return;

            int capacity = MinionUsage.GetSlotCapacity(player, sentry);
            int free = MinionUsage.GetFreeSlots(player, sentry);
            tip += "\n" + Mod.GetLocalization("UI.MinionCountBuff").Format(count, capacity, free);
        }
    }
}
