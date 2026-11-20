using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using LLL;

namespace LLL.Items
{
    /// <summary>
    /// 全局物品工具提示特效类。
    /// 为物品名称添加稀有度呼吸光环 + 黑白描边；
    /// 为伤害数值添加七彩流光 + 职业专属粒子特效。
    /// 由 LLLServerConfig 中的 EnableCoolItemName 和 EnableCoolDamageTooltip 控制开关。
    /// 
    /// ═══════════════════════════════════════════════════════════════════════
    /// 📐 参数调优速查表（所有数值参数集中说明，方便调整）
    /// ═══════════════════════════════════════════════════════════════════════
    /// 
    /// 【模块1：物品名称 —— 稀有度呼吸光环】
    /// ┌────────────────────────┬────────────┬──────────────────────────────────────┐
    /// │ 参数名                 │ 默认值     │ 说明 / 调优建议                      │
    /// ├────────────────────────┼────────────┼──────────────────────────────────────┤
    /// │ pulse 频率 (sin 内部)  │ 3f         │ 呼吸速度。越大闪得越快，推荐 1~8。   │
    /// │ auraCount              │ 6          │ 光环“花瓣”数量。4~12 效果较好，     │
    /// │                        │            │ 越大光晕越密，8以上可能视觉拥挤。    │
    /// │ auraRadius 基数        │ 3f         │ 光环最小半径（像素）。               │
    /// │ auraRadius 浮动振幅   │ 2.5f       │ 光环脉动幅度。基数+振幅=最大半径。   │
    /// │                        │            │ 推荐：基数3~6，振幅1~4。             │
    /// │ rotationSpeed          │ 1.5f       │ 光环自转速度。越大转得越快。         │
    /// │ 光环透明度             │ 0.6f       │ 光晕不透明度。0.3~0.8 较柔和。       │
    /// │ 描边偏移量             │ 1.5f       │ 轮廓与主体的距离（像素）。            │
    /// │                        │            │ 1.0~2.5 为佳，太大文字会散开。       │
    /// └────────────────────────┴────────────┴──────────────────────────────────────┘
    /// 
    /// 【模块2：伤害数值 —— 七彩流光 + 职业粒子】
    /// ┌────────────────────────┬────────────┬──────────────────────────────────────┐
    /// │ 参数名                 │ 默认值     │ 说明 / 调优建议                      │
    /// ├────────────────────────┼────────────┼──────────────────────────────────────┤
    /// │ 色相变化速度 (hue)     │ 0.5f       │ 彩虹变色速度。0.2~1.5 效果较好，    │
    /// │                        │            │ 越大颜色变化越快。                   │
    /// │ 光环数量               │ 6          │ 同模块1。                            │
    /// │ 光环半径               │ 3.5f       │ 固定，无脉动。2~6 较合适。          │
    /// │ 光环旋转速度           │ 3f         │ 比名称光环稍快。                     │
    /// │ 光环透明度             │ 0.45f      │ 比名称光环略淡，避免喧宾夺主。       │
    /// │ 粒子数量 (per 行)     │ 3          │ 每行伤害数字显示的粒子数。            │
    /// │                        │            │ 2~6 较合适，太多会显得杂乱。         │
    /// │ 粒子运动速度因子       │ 0.8f       │ 粒子整体动画速度。0.3~1.5。          │
    /// │ 粒子扩散幅度           │ 4f / 5f   │ 魔法/召唤粒子的横向摆动幅度。        │
    /// │ 粒子尺寸基数           │ ~5.5f     │ 粒子初始大小。数值越大粒子越粗。     │
    /// └────────────────────────┴────────────┴──────────────────────────────────────┘
    /// </summary>
    public class CoolTooltipFX : GlobalItem
    {
        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            var config = ModContent.GetInstance<LLLServerConfig>();

            float time = Main.GlobalTimeWrappedHourly;
            Vector2 position = new Vector2(line.X, line.Y);

            // ==========================================
            // 模块 1：✨ 物品名称 (ItemName) —— 稀有度专属特效
            // ==========================================
            if (config.EnableCoolItemName && line.Mod == "Terraria" && line.Name == "ItemName")
            {
                // --- 1. 获取稀有度颜色 ---
                Color rarityColor = ItemRarity.GetColor(item.rare);
                if (item.rare > Terraria.ID.ItemRarityID.Purple)
                {
                    var modRarity = RarityLoader.GetRarity(item.rare);
                    if (modRarity != null) rarityColor = modRarity.RarityColor;
                }
                if (rarityColor == Color.White || rarityColor == Color.Gray)
                    rarityColor = Color.LightGray;

                // --- 2. 底层呼吸光环 ---
                // 📐 pulse: sin频率 3f → 每秒约 0.477 个完整呼吸周期 (3/(2π))
                //    值越大呼吸越快。推荐范围 1~8。
                //    设为 1 → 缓慢呼吸；设为 8 → 快速闪烁。
                float pulse = (float)Math.Sin(time * 3f) / 2f + 0.5f; // 0.0 ~ 1.0 循环

                // 📐 auraCount: 光环的“光晕瓣”数量。
                //    6 是六边形分布，视觉上最自然。
                //    4 → 十字星效果；8 → 更密集的花瓣；12 → 接近完整光晕环。
                //    建议范围 4~12，超过 16 可能看不出个体，反而降低性能。
                int auraCount = 6;

                // 📐 auraRadius: 光环半径 = 基数 + 振幅 * pulse
                //    基数 3f → 最小半径（静止时）
                //    振幅 2.5f → 脉动变化幅度
                //    最大半径 = 3 + 2.5 = 5.5 像素
                //    调大基数 → 光环离文字更远；调大振幅 → 呼吸感更强。
                //    推荐：基数 2~6，振幅 1~5。
                float auraRadius = 3f + pulse * 2.5f;

                // 📐 rotationSpeed: 光环自转速度。
                //    1.5f → 每约 4.19 秒转一圈 (2π/1.5)。
                //    值越大转得越快，推荐 0.5~5。
                float rotationSpeed = time * 1.5f;

                // 📐 透明度 0.6f → 半透明光晕。
                //    0.3 → 很淡；0.8 → 较明显；1.0 → 完全不透明（可能遮盖文字）。
                float auraAlpha = 0.6f;

                for (int i = 0; i < auraCount; i++)
                {
                    Vector2 auraOffset = new Vector2(auraRadius, 0)
                        .RotatedBy(rotationSpeed + (MathHelper.TwoPi / auraCount) * i);
                    ChatManager.DrawColorCodedString(
                        Main.spriteBatch, line.Font, line.Text,
                        position + auraOffset,
                        rarityColor * auraAlpha,
                        line.Rotation, line.Origin, line.BaseScale);
                }

                // --- 3. 中间层白色描边 ---
                // 📐 描边偏移量 1.5f → 轮廓与主体文字的距离（像素）
                //    1.0 → 很细的描边（接近无）；2.5 → 较粗的描边；
                //    超过 3.0 可能让文字显得“散架”。
                float outlineOffset = 1.5f;
                for (int i = 0; i < 4; i++)
                {
                    Vector2 offset = new Vector2(0, outlineOffset).RotatedBy(MathHelper.PiOver2 * i);
                    ChatManager.DrawColorCodedString(
                        Main.spriteBatch, line.Font, line.Text,
                        position + offset,
                        Color.White, line.Rotation, line.Origin, line.BaseScale);
                }

                // --- 4. 最顶层黑色主体 ---
                ChatManager.DrawColorCodedString(
                    Main.spriteBatch, line.Font, line.Text,
                    position, Color.Black, line.Rotation, line.Origin, line.BaseScale);

                return false;
            }

            // ==========================================
            // 模块 2：⚔️ 武器伤害 (Damage) —— 七彩流光 + 职业专属粒子
            // ==========================================
            if (config.EnableCoolDamageTooltip && line.Mod == "Terraria" && line.Name == "Damage")
            {
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Vector2 textSize = ChatManager.GetStringSize(line.Font, line.Text, line.BaseScale);

                // --- 1. 七彩流光颜色 ---
                // 📐 色相变化速度 0.5f → 色相环每 2 秒转一圈 (1/0.5)
                //    0.2 → 慢速彩虹（约 5 秒一圈）；1.5 → 快速彩虹（约 0.67 秒一圈）
                //    推荐 0.3~1.2，太快会晃眼，太慢则看不出变化。
                float hue = (time * 0.5f) % 1f;
                Color prismaticColor = Main.hslToRgb(hue, 1f, 0.5f);

                // --- 2. 七彩旋转光环 ---
                // 📐 光环半径 3.5f（固定无脉动，保持稳定彩虹效果）
                //    推荐 2~6。
                float auraRadius = 3.5f;
                // 📐 旋转速度 3f → 比名称光环稍快，增加动态感。
                float rotSpeed = time * 3f;
                // 📐 透明度 0.45f → 比名称光环略淡，避免与文字争抢视觉焦点。
                float auraAlpha = 0.45f;
                int auraCount = 6;

                for (int i = 0; i < auraCount; i++)
                {
                    Vector2 auraOffset = new Vector2(auraRadius, 0)
                        .RotatedBy(rotSpeed + (MathHelper.TwoPi / auraCount) * i);
                    ChatManager.DrawColorCodedString(
                        Main.spriteBatch, line.Font, line.Text,
                        position + auraOffset,
                        prismaticColor * auraAlpha,
                        line.Rotation, line.Origin, line.BaseScale);
                }

                // --- 3. 白色描边（同模块1）---
                float outlineOffset = 1.5f;
                for (int i = 0; i < 4; i++)
                {
                    Vector2 offset = new Vector2(0, outlineOffset).RotatedBy(MathHelper.PiOver2 * i);
                    ChatManager.DrawColorCodedString(
                        Main.spriteBatch, line.Font, line.Text,
                        position + offset,
                        Color.White, line.Rotation, line.Origin, line.BaseScale);
                }

                // --- 4. 黑色主体 ---
                ChatManager.DrawColorCodedString(
                    Main.spriteBatch, line.Font, line.Text,
                    position, Color.Black, line.Rotation, line.Origin, line.BaseScale);

                // --- 5. 职业专属粒子特效 ---
                Color pColorA = Color.White;
                Color pColorB = Color.Gray;
                int fxType = 0;

                if (item.DamageType.CountsAsClass(DamageClass.Melee))
                { pColorA = Color.Crimson; pColorB = Color.Gold; fxType = 1; }
                else if (item.DamageType.CountsAsClass(DamageClass.Magic))
                { pColorA = Color.DeepSkyBlue; pColorB = Color.Magenta; fxType = 2; }
                else if (item.DamageType.CountsAsClass(DamageClass.Ranged))
                { pColorA = Color.LimeGreen; pColorB = Color.Yellow; fxType = 3; }
                else if (item.DamageType.CountsAsClass(DamageClass.Summon))
                { pColorA = Color.Cyan; pColorB = Color.White; fxType = 4; }

                // 📐 wave: 颜色混合进度，在 pColorA 和 pColorB 之间平滑过渡
                //    频率 3f → 每秒约 0.477 次完整渐变，与名称光环呼吸同步。
                float wave = (float)Math.Sin(time * 3f) / 2f + 0.5f;
                Color particleBaseColor = Color.Lerp(pColorA, pColorB, wave);

                // 📐 particleCount: 每行伤害数字显示的粒子数量。
                //    3 → 适中。2 → 简洁；5 → 丰富；超过 8 可能显得杂乱。
                //    注意：粒子数量对性能有线性影响，推荐 2~6。
                int particleCount = 3;

                // 📐 粒子动画速度因子 0.8f → 控制粒子运动快慢。
                //    0.3 → 慢速飘散；1.5 → 快速飘散。
                //    配合各职业的位移计算公式，整体调整。
                float animSpeed = 0.8f;

                for (int i = 0; i < particleCount; i++)
                {
                    float progress = (time * animSpeed + i * 0.33f) % 1f;
                    float alpha = (float)Math.Sin(progress * MathHelper.Pi);
                    Color finalParticleColor = particleBaseColor * alpha;

                    Vector2 pPos = Vector2.Zero;
                    float rotation = 0f;
                    Vector2 scale = new Vector2(2f, 2f);
                    Vector2 origin = new Vector2(0.5f, 0.5f);

                    // 水平起始位置：均匀分布在文字宽度上
                    float startX = position.X + (i + 1) * (textSize.X / (particleCount + 1));

                    // ─── 各职业粒子形态详解 ───
                    // 📐 近战 (fxType=1)：竖向矩形条，向上飘散。
                    //    矩形宽度 6.5f，高度 6.5f，随 alpha 缩放。
                    //    竖直位移 = progress * 20f → 上升 0~20 像素。
                    //    调整 20f → 飘散高度；调整 6.5f → 粒子粗细。
                    if (fxType == 1)
                    {
                        pPos.X = startX;
                        pPos.Y = position.Y + textSize.Y - progress * 20f;
                        rotation = MathHelper.PiOver4;
                        scale = new Vector2(6.5f, 6.5f) * alpha;
                    }
                    // 📐 魔法 (fxType=2)：波浪轨迹 + 收缩消失。
                    //    横向摆动幅度 4f → 左右 ±4 像素摆动。
                    //    竖向上升 25f → 比近战飘得更高。
                    //    尺寸从 5.5f 线性缩小到 0。
                    //    调整 4f → 波浪幅度；25f → 上升高度；5.5f → 初始大小。
                    else if (fxType == 2)
                    {
                        pPos.X = startX + (float)Math.Sin(progress * MathHelper.TwoPi * 3f) * 4f;
                        pPos.Y = position.Y + textSize.Y + 2f - progress * 25f;
                        scale = new Vector2(5.5f, 5.5f) * (1f - progress);
                    }
                    // 📐 远程 (fxType=3)：水平长条，横向扫过。
                    //    矩形宽度 22f，高度 1.8f → 细长的“子弹”效果。
                    //    横向从文字左端扫到右端。
                    //    调整 22f → 粒子长度；1.8f → 粒子粗细。
                    //    透明度额外乘 0.8f 使其更柔和。
                    else if (fxType == 3)
                    {
                        pPos.X = position.X + (textSize.X * progress);
                        pPos.Y = position.Y + textSize.Y + (i % 2 == 0 ? 0f : -6f);
                        scale = new Vector2(22f, 1.8f);
                        finalParticleColor = pColorA * alpha * 0.8f;
                    }
                    // 📐 召唤 (fxType=4)：圆形轨道，旋转运动。
                    //    轨道半径 5f → 围绕文字中心旋转。
                    //    角度 = progress * 2π * 2 → 快速旋转。
                    //    调整 5f → 轨道半径；2f 乘数 → 旋转圈数。
                    else if (fxType == 4)
                    {
                        pPos.X = startX + (float)Math.Cos(progress * MathHelper.TwoPi) * 5f;
                        pPos.Y = position.Y + textSize.Y / 2f + (float)Math.Sin(progress * MathHelper.TwoPi) * 5f;
                        rotation = progress * MathHelper.TwoPi * 2f;
                        scale = new Vector2(4.5f, 4.5f);
                    }
                    // ─── 默认 / 无职业：普通上飘圆点 ───
                    else
                    {
                        pPos.X = startX;
                        pPos.Y = position.Y + textSize.Y - progress * 15f;
                        scale = new Vector2(3.5f, 3.5f);
                    }

                    Main.spriteBatch.Draw(pixel, pPos, new Rectangle(0, 0, 1, 1),
                        finalParticleColor, rotation, origin, scale, SpriteEffects.None, 0f);
                }

                return false;
            }

            return true;
        }
    }
}