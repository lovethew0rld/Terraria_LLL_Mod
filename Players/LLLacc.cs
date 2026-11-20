using Terraria;
using Terraria.ModLoader;
using LLL;

namespace LLL.Players
{
    /// <summary>
    /// 容器装备生效系统：让玩家背包、猪猪存钱罐、保险箱、
    /// 护卫熔炉、虚空保险库中的饰品（Accessory）也能提供属性加成。
    /// 由 LLLServerConfig 中多个开关分别控制每个容器。
    /// 总开关 EnableContainerEquip 必须开启。
    /// </summary>
    public class LLLacc : ModPlayer
    {
        /// <summary>
        /// 在玩家装备更新时触发（每帧都会调用）。
        /// 遍历各个容器，将其中每个饰品应用到玩家身上。
        /// </summary>
        public override void UpdateEquips()
        {
            var config = ModContent.GetInstance<LLLServerConfig>();

            // 总开关关闭则跳过
            if (!config.EnableContainerEquip) return;

            // 1. 主背包（50格）
            if (config.EnableInventoryEquip)
            {
                for (int i = 0; i < 50; i++)
                {
                    ApplyItemEffect(Player.inventory[i]);
                }
            }

            // 2. 猪猪存钱罐
            if (config.EnablePiggyBankEquip)
            {
                for (int i = 0; i < Player.bank.item.Length; i++)
                {
                    ApplyItemEffect(Player.bank.item[i]);
                }
            }

            // 3. 保险箱
            if (config.EnableSafeEquip)
            {
                for (int i = 0; i < Player.bank2.item.Length; i++)
                {
                    ApplyItemEffect(Player.bank2.item[i]);
                }
            }

            // 4. 护卫熔炉
            if (config.EnableDefenderForgeEquip)
            {
                for (int i = 0; i < Player.bank3.item.Length; i++)
                {
                    ApplyItemEffect(Player.bank3.item[i]);
                }
            }

            // 5. 虚空保险库
            if (config.EnableVoidVaultEquip)
            {
                for (int i = 0; i < Player.bank4.item.Length; i++)
                {
                    ApplyItemEffect(Player.bank4.item[i]);
                }
            }
        }

        /// <summary>
        /// 应用单个物品的效果。
        /// 条件：非空且为饰品（accessory == true）。
        /// 使用 Player.ApplyEquipFunctional 应用装备功能（属性、套装等），
        /// 并隐藏视觉效果（hideVisual: true），避免显示多余配饰。
        /// 同时应用物品前缀（修饰语）效果。
        /// </summary>
        private void ApplyItemEffect(Item item)
        {
            if (!item.IsAir && item.accessory)
            {
                // 应用装备的功能效果（属性、套装、特殊能力等）
                // hideVisual: true 阻止饰品在玩家身上显示（避免视觉混乱）
                Player.ApplyEquipFunctional(item, hideVisual: true);

                // 应用物品前缀（如“险恶”、“幸运”等附加属性）
                Player.GrantPrefixBenefits(item);
            }
        }
    }
}