using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using LLL;

namespace LLL.Systems
{
    /// <summary>
    /// 快捷键获取物品：按下快捷键（默认 G）即可直接获取鼠标指向的物品。
    /// 支持三种场景：
    /// 1. 鼠标正"捏着"物品（从背包/容器中拿出来尚未放下）
    /// 2. 鼠标悬停在 UI 上的物品（背包格、合成台输出等）
    /// 3. 鼠标指向世界地面上的掉落物
    /// 
    /// 每次获取一整组（最大堆叠数量）。
    /// 由 LLLServerConfig.EnableQuickItemGrab 控制开关。
    /// </summary>
    public class GetItem : ModPlayer
    {
        /// <summary>
        /// 每帧检查按键触发（ProcessTriggers 在玩家输入处理阶段调用）。
        /// </summary>
        public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet)
        {
            if (!ModContent.GetInstance<LLLServerConfig>().EnableQuickItemGrab)
                return;

            if (KeybindSystem.GrabHoverItemKeybind.JustPressed)
            {
                Item targetItem = null;

                // ─── 优先级1：鼠标正"捏着"物品 ──────────────────────
                // 场景：从背包拖出物品但还没放下，或正在移动物品。
                if (!Main.mouseItem.IsAir && Main.mouseItem.type > ItemID.None)
                {
                    targetItem = Main.mouseItem;
                }
                // ─── 优先级2：鼠标悬停在UI物品上 ────────────────────
                // 场景：鼠标指着背包格、商店、合成台输出等。
                else if (Main.HoverItem != null && !Main.HoverItem.IsAir)
                {
                    targetItem = Main.HoverItem;
                }
                // ─── 优先级3：鼠标指向世界地上的掉落物 ──────────────
                // 场景：鼠标指着一个掉在地上的物品实体。
                else
                {
                    for (int i = 0; i < Main.maxItems; i++)
                    {
                        Item worldItem = Main.item[i];
                        if (worldItem.active && worldItem.Hitbox.Contains(Main.MouseWorld.ToPoint()))
                        {
                            targetItem = worldItem;
                            break;
                        }
                    }
                }

                // 如果找到了有效物品，获取一整组并提示
                if (targetItem != null && !targetItem.IsAir && targetItem.type > ItemID.None)
                {
                    int stack = targetItem.maxStack; // 📐 获取最大堆叠数（一组）

                    // 生成物品给玩家（如果背包满了会掉落在地上）
                    Player.QuickSpawnItem(Player.GetSource_Misc("QuickItemGrab"), targetItem.type, stack);

                    // 显示提示信息（从本地化文件读取）
                    string message = Language.GetTextValue("Mods.LLL.Messages.ItemGrabbed", stack, targetItem.Name);
                    Main.NewText(message, Color.Yellow);
                }
            }
        }
    }
}