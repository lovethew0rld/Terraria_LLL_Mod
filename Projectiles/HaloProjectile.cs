using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using LLL.Players;

namespace LLL.Projectiles
{
    /// <summary>
    /// 彩蛋光环发射的追踪孢子射弹。
    /// 仅当玩家名字全为数字且 ShowHaloVisual 开启时才会生成。
    /// 行为类似孢子囊（Spore Sac）的追踪孢子，但伤害极低（1~2），
    /// 且击中非Boss敌人时会将其最大生命值减半（核心彩蛋机制）。
    /// </summary>
    public class HaloProjectile : ModProjectile
    {
        /// <summary>
        /// 使用 1x1 的纯白纹理作为默认贴图（实际绘制已被 PreDraw 禁用，
        /// 全靠粒子特效呈现视觉效果）。
        /// </summary>
        public override string Texture => "LLL/Projectiles/HaloBrush";

        // ═══════════════════════════════════════════════════════════════
        // ?? 射弹基础属性
        // ═══════════════════════════════════════════════════════════════
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;     // 对敌人造成伤害
            Projectile.hostile = false;     // 不伤害玩家
            Projectile.penetrate = 1;       // 穿透一次后消失（碰到敌人就消失）
            Projectile.timeLeft = 300;      // ? 存活 300 帧（5 秒），可调范围 120~600
            Projectile.tileCollide = false; // 穿墙
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Generic; // 泛用伤害类型
        }

        public override bool? CanCutTiles() => false; // 不破坏物块

        // ═══════════════════════════════════════════════════════════════
        // ? AI 行为：追踪最近的敌人
        // ═══════════════════════════════════════════════════════════════
        public override void AI()
        {
            // ─── 1. 粒子特效（无照明彩虹拖尾） ──────────────────────
            Player owner = Main.player[Projectile.owner];
            if (owner.active && !owner.dead)
            {
                // 色相：基于玩家名字哈希 + 时间变化（产生彩虹渐变）
                // ? 时间乘数 2f：颜色变化速度，可调 0.5~5，越大颜色变化越快
                float hue = (owner.GetModPlayer<HaloPlayer>().nameHue + Main.GlobalTimeWrappedHourly * 2f) % 1f;
                Color particleColor = Main.hslToRgb(hue, 1f, 0.7f);

                // 生成一个不发光、无重力的彩色粒子
                int d = Dust.NewDust(
                    Projectile.Center, 0, 0,
                    Terraria.ID.DustID.WhiteTorch, 0f, 0f, 0,
                    particleColor, 1.5f  // ? 粒子大小 1.5f，可调 0.8~3
                );
                Main.dust[d].noGravity = true; // 不坠落
                Main.dust[d].noLight = true;   // ? 核心要求：不产生任何照明
                Main.dust[d].velocity *= 0.1f; // 几乎静止，形成拖尾效果
            }

            // ─── 2. 追踪逻辑（类似孢子囊） ──────────────────────────
            // ? 搜索半径 600f 像素（约 37.5 格），可调范围 300~1200
            //    值越大追踪范围越广，但可能追踪到屏幕外的敌人。
            NPC target = FindClosestNPC(600f);

            if (target != null)
            {
                // 发现敌人 → 转向追踪
                Vector2 direction = target.Center - Projectile.Center;
                direction.Normalize();

                // ? 平滑追踪算法：
                //    (当前速度 × 20 + 目标方向 × 6) / 21
                //    这是一个加权平均，使转向平滑不突兀。
                //    权重比 20:6 意味着"保持当前惯性"占主导，转向较柔和。
                //    若想转向更灵敏，可调整为 (速度×10 + 方向×10)/20
                Projectile.velocity = (Projectile.velocity * 20f + direction * 6f) / 21f;
            }
            else
            {
                // 无目标 → 缓慢减速漂浮（类似孢子）
                // ? 速度衰减系数 0.95f：每帧保留 95% 速度，逐渐停下来
                //    可调 0.9~0.99，值越大滑行越远。
                Projectile.velocity *= 0.95f;
            }
        }

        // ─── 索敌辅助方法 ────────────────────────────────────────────
        /// <summary>
        /// 在指定距离内搜索最近的、可被锁定的敌对NPC。
        /// </summary>
        /// <param name="maxDetectDistance">最大搜索半径（像素）</param>
        private NPC FindClosestNPC(float maxDetectDistance)
        {
            NPC closestNPC = null;
            float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC target = Main.npc[i];
                // 检查是否可被追踪（存活、敌对、可攻击）
                if (target.CanBeChasedBy())
                {
                    float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);
                    if (sqrDistanceToTarget < sqrMaxDetectDistance)
                    {
                        sqrMaxDetectDistance = sqrDistanceToTarget;
                        closestNPC = target;
                    }
                }
            }
            return closestNPC;
        }

        // ═══════════════════════════════════════════════════════════════
        // ? 核心机制：非Boss敌人最大生命值减半
        // ═══════════════════════════════════════════════════════════════
        /// <summary>
        /// 射弹击中NPC时触发。
        /// 若非Boss敌人，将其最大生命值减半（下限为1），
        /// 并播放粉碎特效。
        /// 
        /// ?? 这是一个永久性改变！被砍半的敌人在本次生命周期内
        ///    会保持减半后的生命上限，直到死亡或重生。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 只对非Boss且生命上限 > 1 的敌人生效
            if (!target.boss && target.lifeMax > 1)
            {
                // 强制减半生命上限（至少保留 1）
                target.lifeMax = Math.Max(1, target.lifeMax / 2);

                // 当前生命不能超过新的上限（防止显示异常）
                if (target.life > target.lifeMax)
                {
                    target.life = target.lifeMax;
                }

                // 爆出暗影光束风格的粉碎特效（15个粒子）
                // ? 粒子数量 15，可调 8~30，越多特效越华丽
                // ? 粒子速度乘数 2f，可调 1~4，越大炸开范围越广
                for (int i = 0; i < 15; i++)
                {
                    int d = Dust.NewDust(
                        target.position, target.width, target.height,
                        Terraria.ID.DustID.ShadowbeamStaff,
                        0f, 0f, 0, default, 1.5f
                    );
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 2f;
                }
            }
        }

        /// <summary>
        /// 禁用默认绘制（因为我们用粒子特效呈现视觉效果）。
        /// 返回 false 跳过原版绘制。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}