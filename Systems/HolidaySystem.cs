using Terraria;
using Terraria.ModLoader;
using LLL;

namespace LLL.Systems
{
    /// <summary>
    /// 节日系统：强制把游戏变成圣诞节 / 万圣节（两个可以同时开）。
    ///
    /// 【原理】
    /// 原版用两个静态开关判断当前是不是节日：
    ///     Main.xMas      —— 圣诞节：敌人掉礼物、圣诞老人可以入住、圣诞主题外观与音乐…
    ///     Main.halloween —— 万圣节：敌人掉福袋、南瓜与幽灵主题内容、NPC 换节日装扮…
    /// 平时它们由游戏启动时按系统日期算出来，这里改成每帧强制成你选的值，
    /// 所以开关一改就立刻生效，不需要重载模组，也不需要重进世界。
    ///
    /// 【关掉开关时怎么办】
    /// 第一次强制之前先把原版的真实值记下来，等你把开关关掉再写回去，
    /// 这样“系统日期真的是圣诞节/万圣节”的原版行为不会被破坏。
    /// </summary>
    public class HolidaySystem : ModSystem
    {
        private bool _xMasForced;
        private bool _savedXMas;

        private bool _halloweenForced;
        private bool _savedHalloween;

        public override void PostUpdateEverything()
        {
            LLLServerConfig config = ModContent.GetInstance<LLLServerConfig>();

            // ── 圣诞节 ──
            if (config.ForceChristmas)
            {
                if (!_xMasForced)
                {
                    _savedXMas = Main.xMas; // 记下原版按系统日期算出来的值
                    _xMasForced = true;
                }

                Main.xMas = true;
            }
            else if (_xMasForced)
            {
                Main.xMas = _savedXMas;
                _xMasForced = false;
            }

            // ── 万圣节 ──
            if (config.ForceHalloween)
            {
                if (!_halloweenForced)
                {
                    _savedHalloween = Main.halloween;
                    _halloweenForced = true;
                }

                Main.halloween = true;
            }
            else if (_halloweenForced)
            {
                Main.halloween = _savedHalloween;
                _halloweenForced = false;
            }
        }
    }
}