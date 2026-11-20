using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using LLL;

namespace LLL.NPCs
{
    /// <summary>
    /// 显示敌怪实时血量：在所有活着的NPC头顶显示
    /// "当前生命 / 最大生命" 的文本。
    /// 主要用于调试/测试，或满足玩家查看精确血量的需求。
    /// 由 LLLServerConfig.ShowEnemyHealth 控制开关。
    /// </summary>
    public class ShowHealthNPC : GlobalNPC
    {
        /// <summary>
        /// 在NPC绘制完成后调用（PostDraw），用于在NPC上层叠加UI文本。
        /// </summary>
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!ModContent.GetInstance<LLLServerConfig>().ShowEnemyHealth)
                return;

            // 只对存活且生命上限 > 1 的生物显示（排除小动物、物品等）
            if (npc.active && npc.lifeMax > 1)
            {
                string hpText = $"{npc.life} / {npc.lifeMax}";

                // 计算文本位置：NPC头顶上方 20 像素（世界坐标 → 屏幕坐标）
                // 📐 偏移量 -20f：可调，值越大文字离头顶越远。
                //    建议范围 -10 ~ -40，取决于NPC大小和UI布局。
                Vector2 textPos = npc.Top - Main.screenPosition - new Vector2(0, 20f);

                // 使用 ChatManager 绘制带阴影的文字，确保在任何背景下都可读
                Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(
                    spriteBatch,
                    FontAssets.MouseText.Value,      // 使用游戏默认鼠标文本字体
                    hpText,
                    textPos,
                    Color.LimeGreen,                 // 亮绿色，显眼且代表生命值
                    0f,                              // 无旋转
                    FontAssets.MouseText.Value.MeasureString(hpText) / 2f, // 居中对齐
                    Vector2.One                      // 默认缩放
                );
            }
        }
    }
}