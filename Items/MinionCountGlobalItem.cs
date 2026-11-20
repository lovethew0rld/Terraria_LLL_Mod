using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using LLL.Systems;

namespace LLL.Items
{
    /// <summary>
    /// 在召唤法杖 / 哨兵法杖的物品说明里加一行「当前数量 / 栏位上限（剩余空位）」，例如：
    ///     召唤物：4/15（剩余 7 格）
    ///     哨兵：1/2（剩余 1 格）
    ///
    /// 分子是**这件武器**召唤出来的数量；括号里是**全局**剩余空位（所有召唤物共用同一批栏位）。
    /// 判定是“这件武器的 shoot 是不是召唤物弹幕”，所以**其它模组加的召唤武器也自动生效**。
    /// 由 LLLConfig.ShowMinionCount 控制开关。
    /// </summary>
    public class MinionCountGlobalItem : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!ModContent.GetInstance<LLLConfig>().ShowMinionCount) return;
            if (!MinionUsage.IsSummonStaff(item, out bool sentry)) return;

            Player player = Main.LocalPlayer;
            int count = MinionUsage.CountByProjectile(player, item.shoot, sentry);
            int capacity = MinionUsage.GetSlotCapacity(player, sentry);
            int free = MinionUsage.GetFreeSlots(player, sentry);

            string key = sentry ? "UI.MinionCountSentry" : "UI.MinionCount";
            tooltips.Add(new TooltipLine(Mod, "LLLMinionCount",
                Mod.GetLocalization(key).Format(count, capacity, free)));
        }
    }
}
