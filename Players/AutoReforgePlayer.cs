using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using LLL;
using LLL.Systems;

namespace LLL.Players
{
    /// <summary>
    /// 饰品词条同步（俗称“自动重铸”）。
    ///
    /// 【它做什么】
    /// 以「第一个饰品栏」（Player.armor[3]）为模板，把它的词条（Item.prefix）覆盖到
    /// 其余所有饰品栏与装饰栏：
    ///   · 原版 7 个饰品栏（armor[3..9]）与 7 个装饰栏（armor[13..19]）
    ///   · 其它模组注册的额外饰品栏（ModAccessorySlot 的 FunctionalItem / VanityItem）
    ///
    /// 【什么时候触发】
    ///   1. 总开关从「关」变「开」的那一帧；
    ///   2. 第一格的签名（物品 ID + 词条）发生变化：
    ///        - 空格子放入带词条的饰品
    ///        - 取下来再放回（哪怕还是同一件）
    ///        - 直接换成另一件饰品
    ///        - 用哥布林等途径重铸了第一格
    ///   3. 进入世界（功能已开启时）先对齐一次。
    ///
    /// 【什么时候什么都不做】
    /// 第一格是空的 / 不是饰品 / 词条为 0（素饰品）→ 整体跳过，
    /// 绝不会把别的格子上的词条“洗白”。
    ///
    /// 【为什么只改 prefix 就够】
    /// 饰品词条的加成是 Player.ApplyEquipFunctional 每帧按 item.prefix 现算的，
    /// 所以改完 prefix 后，下一帧的装备结算就会用上新词条，不需要重新穿脱装备。
    /// </summary>
    public class AutoReforgePlayer : ModPlayer
    {
        // ═══════════════════════════════════════════════════════════════
        // 📐 原版槽位常量（armor 数组布局）
        //    0..2   装备（头/身/腿）
        //    3..9   饰品栏（7 格）
        //    10..12 时装（头/身/腿）
        //    13..19 装饰栏（7 格）
        // ═══════════════════════════════════════════════════════════════
        private const int FirstAccessorySlot = 3;
        private const int FirstVanitySlot = 13;
        private const int VanillaSlotCount = 7;

        /// <summary>
        /// 上一帧看到的第一格签名。(type, prefix) = (-1, 0) 表示空格或不是饰品。
        /// 进世界 / 新建人物时重置，这样第一帧就会被当作“变化”而触发一次对齐。
        /// </summary>
        private (int type, int prefix) _lastSource = (-1, 0);

        /// <summary>上一帧的总开关状态，用来识别“刚刚被打开”。</summary>
        private bool _wasEnabled;

        // ═══════════════════════════════════════════════════════════════
        // 🔄 生命周期：重置状态
        // ═══════════════════════════════════════════════════════════════

        public override void Initialize()
        {
            _lastSource = (-1, 0);
            _wasEnabled = false;
        }

        public override void OnEnterWorld()
        {
            // 进世界时清掉上一份世界的记录，让这一帧按“变化”处理 → 自动对齐一次
            _lastSource = (-1, 0);
            _wasEnabled = false;
        }

        // ═══════════════════════════════════════════════════════════════
        // ⚙️ 每帧：检测触发条件
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 放在 PostUpdateEquips：每帧装备结算结束后检查一次。
        /// 改完词条最迟在下一帧的装备结算里生效（1/60 秒，实际无感）。
        /// </summary>
        public override void PostUpdateEquips()
        {
            // 只处理本地玩家（模组的其余功能同样只考虑单人）
            if (Player.whoAmI != Main.myPlayer) return;

            LLLServerConfig config = ModContent.GetInstance<LLLServerConfig>();

            // 关掉时清空“刚打开”的标记，下次打开能再次触发
            if (!config.EnableAutoReforge)
            {
                _wasEnabled = false;
                return;
            }

            bool justEnabled = !_wasEnabled;
            _wasEnabled = true;

            Item source = Player.armor[FirstAccessorySlot];
            (int type, int prefix) signature = DescribeSource(source);

            bool sourceChanged = signature != _lastSource;
            _lastSource = signature;

            if (!justEnabled && !sourceChanged) return;

            // 源无效（空 / 非饰品 / 素饰品）→ 整体跳过，不动任何格子
            if (signature.type < 0 || signature.prefix <= 0) return;

            int changed = SyncAll(signature.prefix, config);
            if (changed <= 0) return;

            Mod.Logger.Info($"[饰品词条同步] 以第一格为模板，已同步 {changed} 件饰品（词条 {signature.prefix}）。");

            string message = Language.GetTextValue("Mods.LLL.Messages.AutoReforgeSynced", changed);
            Main.NewText(message, new Color(120, 200, 255));
        }

        // ═══════════════════════════════════════════════════════════════
        // 🔁 同步本体
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 把指定词条覆盖到所有目标格子，返回真正被改动的件数。
        /// </summary>
        private int SyncAll(int prefix, LLLServerConfig config)
        {
            int changed = 0;

            // ─── 原版饰品栏（跳过第一格本身） ───
            if (config.AutoReforgeIncludeAccessories)
            {
                for (int i = FirstAccessorySlot + 1; i < FirstAccessorySlot + VanillaSlotCount; i++)
                    changed += TryApplyPrefix(Player.armor[i], prefix);
            }

            // ─── 原版装饰栏 ───
            if (config.AutoReforgeIncludeVanity)
            {
                for (int i = FirstVanitySlot; i < FirstVanitySlot + VanillaSlotCount; i++)
                    changed += TryApplyPrefix(Player.armor[i], prefix);
            }

            // ─── 其它模组的额外饰品栏 ───
            // ModAccessorySlot 是每个额外栏一个实例，FunctionalItem / VanityItem 直接给物品。
            // 注意：ModAccessorySlot.Player 是静态属性（内部就是 Main.CurrentPlayer），
            // 所以它不是“这个槽属于谁”，而是“当前正在结算谁”——单人下永远等于本地玩家。
            if (ModAccessorySlot.Player == Player)
            {
                foreach (ModAccessorySlot slot in ModContent.GetContent<ModAccessorySlot>())
                {
                    if (slot == null) continue;

                    if (config.AutoReforgeIncludeAccessories)
                        changed += TryApplyPrefix(slot.FunctionalItem, prefix);

                    if (config.AutoReforgeIncludeVanity)
                        changed += TryApplyPrefix(slot.VanityItem, prefix);
                }
            }

            return changed;
        }

        /// <summary>
        /// 给单个物品套用词条。返回 1 表示真的改了，0 表示跳过。
        /// </summary>
        private static int TryApplyPrefix(Item item, int prefix)
        {
            if (item == null || item.IsAir || !item.accessory) return 0;
            if (item.prefix == prefix) return 0;                 // 已经是目标词条
            if (!item.IsAPrefixableAccessory()) return 0;        // 这饰品天生不能附魔
            if (!PrefixRegistry.IsValid(item, prefix)) return 0; // 该词条对这件物品不合法（含模组词条）

            // Item.Prefix 内部还会再校验一次，并重算 damage / crit / value 等数值
            return item.Prefix(prefix) ? 1 : 0;
        }

        // ═══════════════════════════════════════════════════════════════
        // 🏷️ 取第一格的签名
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 把物品压缩成 (type, prefix)。空格 / 非饰品统一返回 (-1, 0)。
        /// 用“物品 ID + 词条”当签名，可以同时识别“换了物品”和“换了词条”；
        /// 取出再放回会经过 (-1, 0)，所以同一件物品重新放上去也算一次变化。
        /// </summary>
        private static (int type, int prefix) DescribeSource(Item item)
        {
            if (item == null || item.IsAir || !item.accessory) return (-1, 0);
            return (item.type, item.prefix);
        }
    }
}
