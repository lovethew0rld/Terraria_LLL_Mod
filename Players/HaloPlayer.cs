using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Linq;
using LLL;

namespace LLL.Players
{
    /// <summary>
    /// 彩蛋光环系统：仅当玩家名字全为数字时，才会激活。
    /// 效果：每隔一段时间从玩家身上发射一颗彩虹孢子射弹，
    /// 伤害 1~2，可攻击敌人（但伤害极低，主要作为娱乐彩蛋）。
    /// 由 LLLServerConfig.ShowHaloVisual 控制总开关。
    /// </summary>
    public class HaloPlayer : ModPlayer
    {
        public float nameHue = 0f;        // 用于给名字颜色做哈希（暂未使用）
        public bool hasNumericName = false; // 名字是否全为数字

        private int sporeTimer = 0; // 发射间隔计时器

        /// <summary>
        /// 进入世界时检测玩家名字是否全为数字。
        /// 同时计算名字色相哈希（暂未用于视觉效果，可扩展）。
        /// </summary>
        public override void OnEnterWorld()
        {
            // 检查名字是否全为数字字符
            hasNumericName = !string.IsNullOrEmpty(Player.name) && Player.name.All(char.IsDigit);

            // 简单哈希用于生成独特的色相值（可用于后续扩展）
            int nameHash = 0;
            foreach (char c in Player.name)
            {
                nameHash += c;
            }
            nameHue = (nameHash % 360) / 360f;
        }

        /// <summary>
        /// 每帧更新：若条件满足，每隔一定帧数发射一颗孢子。
        /// </summary>
        public override void PostUpdate()
        {
            // 检查开关和名字条件
            if (!ModContent.GetInstance<LLLServerConfig>().ShowHaloVisual) return;
            if (!hasNumericName) return;

            sporeTimer++;

            // ? 发射间隔：每 15 帧（0.25秒）发射一颗
            //    可调范围 5~60，值越小发射越密集，性能消耗越大。
            if (sporeTimer > 15)
            {
                sporeTimer = 0;

                // 限制场上最大粒子数量为 10，防止积累过多影响性能
                // 类似原版孢子囊的设计
                if (Player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.HaloProjectile>()] < 10)
                {
                    // 随机向外速度（圆形范围，最大 5f）
                    // ? 速度范围 5f：可调 2~8，值越大孢子飞得越远
                    Vector2 randomVelocity = Main.rand.NextVector2Circular(5f, 5f);

                    // ? 伤害 1~2（固定，不可配置）
                    int randomDamage = Main.rand.Next(1, 3);

                    Projectile.NewProjectile(
                        Player.GetSource_Misc("NumericNameHalo"),
                        Player.Center,
                        randomVelocity,
                        ModContent.ProjectileType<Projectiles.HaloProjectile>(),
                        randomDamage,   // 伤害 1 或 2
                        0f,             // 击退为 0
                        Player.whoAmI
                    );
                }
            }
        }
    }
}