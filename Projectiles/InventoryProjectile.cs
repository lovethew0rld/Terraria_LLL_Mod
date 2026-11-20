using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace LLL.Items
{
    /// <summary>
    /// 背包火力全开（万剑齐发）系统的射弹辅助类。
    /// 为"由背包武器发射的射弹"添加加速特性：
    /// 每一帧速度提升 1%，直到达到上限 22f。
    /// 目的是让射弹在飞行过程中逐渐加速，产生"巡航导弹"般的视觉冲击。
    /// </summary>
    public class InventoryProjectile : GlobalProjectile
    {
        /// <summary>
        /// ⚡ 关键属性：确保每个射弹实体都拥有独立的实例变量。
        /// 如果不设为 true，所有射弹会共享同一个 isFromInventory 变量，
        /// 导致互相干扰（例如A射弹加速，B射弹也被标记加速）。
        /// 
        /// 务必保持为 true！
        /// </summary>
        public override bool InstancePerEntity => true;

        /// <summary>
        /// 标记该射弹是否由背包火力系统生成。
        /// 在 InventoryWeaponPlayer 中发射射弹时会被设置为 true。
        /// </summary>
        public bool isFromInventory = false;

        /// <summary>
        /// 每帧调用，对标记为"来自背包"的射弹施加加速效果。
        /// </summary>
        public override void AI(Projectile projectile)
        {
            if (isFromInventory)
            {
                float currentSpeed = projectile.velocity.Length();

                // ─── 速度限制保护 ─────────────────────────────────────
                // ⚠️ 泰拉瑞亚原版碰撞检测在速度 > 20f 时容易失效，
                //    射弹可能直接穿过物块和敌人（俗称"穿墙Bug"）。
                //    因此我们将最大速度限制在 22f，这是安全的极限值。
                //    📐 若想更安全，可设为 18f；若想更激进，可设为 25f（但有穿墙风险）。
                if (currentSpeed > 0.1f && currentSpeed < 22f)
                {
                    // 📐 加速倍率 1.01f = 每帧提速 1%
                    //    效果：指数级加速，越飞越快。
                    //    可调范围 1.005~1.03，值越大加速越快。
                    //    - 1.005 → 缓慢加速（几乎感觉不到）
                    //    - 1.01  → 适中（推荐）
                    //    - 1.03  → 快速加速（几帧内达到极速）
                    projectile.velocity *= 1.01f;
                }
                // 如果已经超过 22f，则不再继续加速（保持限速）
            }
        }
    }
}