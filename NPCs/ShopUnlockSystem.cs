using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using LLL;

namespace LLL.NPCs
{
    /// <summary>
    /// NPC商店解锁系统：移除所有商店物品的购买条件（Condition）。
    /// 开启后，NPC商店中所有原本需要特定条件（如特定生物群系、打败Boss等）
    /// 才能购买的物品将直接出售。
    /// 由 LLLServerConfig.EnableUnlockShopConditions 控制。
    /// 
    /// ⚠️ 技术说明：使用反射清空 Condition 列表，
    /// 因为 tModLoader 的 NPCShop.Entry 未提供公开的 ClearConditions 方法。
    /// 该操作在 Shop 初始化后执行，不影响商店本身结构。
    /// </summary>
    public class ShopUnlockSystem : GlobalNPC
    {
        /// <summary>
        /// 在 NPC 商店被构建并即将显示给玩家时调用。
        /// 遍历商店所有条目，清空每个条目的条件列表。
        /// </summary>
        public override void ModifyShop(NPCShop shop)
        {
            if (!ModContent.GetInstance<LLLServerConfig>().EnableUnlockShopConditions)
                return;

            foreach (NPCShop.Entry entry in shop.Entries)
            {
                ClearConditions(entry);
            }
        }

        /// <summary>
        /// 通过反射清空 NPCShop.Entry 内部的 Condition 列表。
        /// 
        /// 实现细节：
        /// - Entry 类内部有一个类型为 List&lt;Condition&gt; 的私有字段，
        ///   用于存储所有购买前置条件。
        /// - 我们枚举所有字段（包括私有），找到该列表并调用 Clear()。
        /// - 这不会影响 Entry 的其他属性（如物品、价格、自定义回调等）。
        /// 
        /// ⚠️ 风险提示：如果未来 tModLoader 更改了 Entry 的内部结构，
        /// 此方法可能失效，需要同步更新反射逻辑。
        /// </summary>
        private static void ClearConditions(NPCShop.Entry entry)
        {
            // 获取所有字段（实例字段，包括非公开和公开）
            FieldInfo[] fields = entry.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (FieldInfo field in fields)
            {
                // 找到类型为 List<Condition> 的字段
                if (field.FieldType == typeof(List<Condition>))
                {
                    if (field.GetValue(entry) is List<Condition> conditions)
                    {
                        conditions.Clear(); // 清空条件列表
                    }
                }
            }
        }
    }
}