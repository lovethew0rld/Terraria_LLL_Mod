using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace LLL.NPCs
{
    /// <summary>
    /// 水母类生物特殊掉落：为指定水母类NPC添加放屁坐垫（Whoopie Cushion）的掉落。
    /// 概率为 5%（1/20）。
    /// 涉及：蓝水母、粉水母、绿水母、血水母、蘑菇鱼。
    /// </summary>
    public class GlobalNPCLoot : GlobalNPC
    {
        /// <summary>
        /// 修改每个NPC的战利品表。
        /// 为指定ID的水母添加额外的掉落规则。
        /// </summary>
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // 定义目标NPC类型数组
            int[] jellyfishTypes = {
                NPCID.BlueJellyfish,    // 蓝水母 (ID: 115)
                NPCID.PinkJellyfish,    // 粉水母 (ID: 138)
                NPCID.GreenJellyfish,   // 绿水母 (ID: 121)
                NPCID.BloodJelly,       // 血水母 (ID: 518) - 血月期间在水中生成
                NPCID.FungoFish         // 蘑菇鱼 (ID: 173) - 在发光蘑菇生物群落的水中生成
            };

            // 检查当前NPC是否在目标列表中
            if (System.Array.IndexOf(jellyfishTypes, npc.type) > -1)
            {
                // ? 添加掉落规则：ItemDropRule.Common(物品ID, 掉落分母)
                //    分母 20 → 概率 1/20 = 5%
                //    可调范围：10（10%）、50（2%）、100（1%）
                npcLoot.Add(ItemDropRule.Common(ItemID.WhoopieCushion, 20));
            }
        }
    }
}