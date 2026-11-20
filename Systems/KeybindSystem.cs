using Terraria.ModLoader;

namespace LLL.Systems
{
    /// <summary>
    /// 快捷键注册中心：集中管理模组的所有快捷键。
    /// 所有快捷键都在这里注册，其他类通过静态引用访问。
    /// 
    /// 【当前注册的快捷键】
    /// ┌─────────────┬──────────┬─────────────────────────────────────┐
    /// │ 键位        │ 内部键名 │ 功能说明                           │
    /// ├─────────────┼──────────┼─────────────────────────────────────┤
    /// │ Z           │ ItemMagnet         │ 磁力拾取：将全图掉落物传送至玩家身边 │
    /// │ G           │ Get Hovered Item   │ 获取鼠标悬停物品的最大堆叠数量        │
    /// └─────────────┴──────────┴─────────────────────────────────────┘
    /// 
    /// 玩家可以在游戏内"设置 → 控制"中自由修改键位。
    /// 对应的本地化显示名称位于 .hjson 翻译文件中：
    ///   Mods.LLL.Keybinds.ItemMagnet.DisplayName
    ///   Mods.LLL.Keybinds.Get Hovered Item.DisplayName
    /// 
    /// 【设计说明】
    /// 曾经 Z 键（磁力拾取）的注册分散在 CorePlayer.cs 中，
    /// 现统一迁移至此，便于集中管理和维护。
    /// </summary>
    public class KeybindSystem : ModSystem
    {
        // ═══════════════════════════════════════════════════════════════
        // 📌 快捷键实例（公开静态，供其他类访问）
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 【Z键】磁力拾取：将当前世界中所有活跃的掉落物瞬间传送到玩家位置。
        /// 配合 LLLConfig.EnableMagnetKey 开关使用。
        /// 
        /// 📐 默认键位 "Z" —— 可改为任何键，但建议避开常用键。
        /// 使用场景：在刷怪场或探索后，一键收集散落四处的战利品。
        /// 冷却时间：30帧（0.5秒），防止频繁触发。
        /// </summary>
        public static ModKeybind MagnetKeybind { get; private set; }

        /// <summary>
        /// 【G键】获取鼠标悬停物品：一键获取当前鼠标指向的物品一整组。
        /// 配合 LLLServerConfig.EnableQuickItemGrab 开关使用。
        /// 
        /// 📐 默认键位 "G" —— 可改为任何键。
        /// 使用场景：在合成界面、商店、或地上掉落物处，快速拿走目标物品。
        /// 支持三种目标检测优先级：
        ///   1. 鼠标正"捏着"的物品（从背包中拖出但未放下）
        ///   2. 鼠标悬停在UI上的物品（背包格、合成台输出等）
        ///   3. 鼠标指向世界地面上的掉落物
        /// </summary>
        public static ModKeybind GrabHoverItemKeybind { get; private set; }

        // ═══════════════════════════════════════════════════════════════
        // 🔄 生命周期：加载 / 卸载
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 模组加载时注册所有快捷键。
        /// KeybindLoader.RegisterKeybind(Mod, "内部键名", "默认键位")
        /// 
        /// 内部键名必须与 .hjson 翻译文件中的键名完全匹配，
        /// 否则本地化显示名称不会生效。
        /// </summary>
        public override void Load()
        {
            // Z键：磁力拾取
            MagnetKeybind = KeybindLoader.RegisterKeybind(Mod, "ItemMagnet", "Z");

            // G键：获取悬停物品
            GrabHoverItemKeybind = KeybindLoader.RegisterKeybind(Mod, "Get Hovered Item", "G");
        }

        /// <summary>
        /// 模组卸载时清空所有快捷键引用，防止内存泄漏。
        /// </summary>
        public override void Unload()
        {
            MagnetKeybind = null;
            GrabHoverItemKeybind = null;
        }
    }
}