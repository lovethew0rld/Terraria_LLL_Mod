using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using LLL;

namespace LLL.Items
{
    /// <summary>
    /// 多重钓鱼线功能（全局物品类）。
    /// 钓鱼时额外抛出多条鱼线，数量由 LLLServerConfig.ExtraFishingLines 控制。
    /// </summary>
    public class MultiFishingPole : GlobalItem
    {
        /// <summary>
        /// 拦截物品发射弹幕的瞬间（包括钓鱼竿抛线）。
        /// 在原有鱼线基础上，额外生成多条带有角度和速度随机偏移的鱼线。
        /// </summary>
        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 判断当前使用的物品是否为钓鱼竿（fishingPole > 0）
            if (item.fishingPole > 0)
            {
                int extraLines = ModContent.GetInstance<LLLServerConfig>().ExtraFishingLines;

                if (extraLines > 0)
                {
                    // 循环生成额外的鱼漂弹幕
                    for (int i = 0; i < extraLines; i++)
                    {
                        // 核心优化：每条鱼线随机偏移 ±15° 并略微减速，
                        // 使鱼漂落水位置错落有致，视觉效果更自然（霰弹状扩散）
                        Vector2 perturbedSpeed = velocity.RotatedByRandom(MathHelper.ToRadians(15));
                        perturbedSpeed *= 1f - Main.rand.NextFloat(0.1f, 0.3f); // 速度随机衰减10%~30%

                        Projectile.NewProjectile(source, position, perturbedSpeed,
                            type, damage, knockback, player.whoAmI);
                    }
                }
            }

            // 必须返回 true，让原版的第一根鱼线正常抛出去
            return true;
        }
    }
}