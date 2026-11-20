using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using LLL.Systems;
using LLL;

namespace LLL.Buffs
{
    /// <summary>
    /// 宇宙增幅Buff —— 模组核心增益的"显示层"。
    /// 不提供实际属性加成（由 CorePlayer 负责），
    /// 仅负责动态生成Buff描述文本，让玩家实时看到当前生效的数值。
    /// 
    /// 【工作机制】
    /// 1. 每帧由 CorePlayer.PostUpdateMiscEffects() 主动添加（持续2帧）。
    /// 2. ModifyBuffText 在鼠标悬停到Buff图标时调用，动态生成文本。
    /// 3. 文本中所有数值都乘以当前阶段系数，反映实际生效值。
    /// 
    /// 【新人必看：本地化键名说明】
    /// 所有文本都通过 Mod.GetLocalization() 读取 .hjson 翻译文件。
    /// 对应的翻译键名位于 Localization/en-US/Mods.LLL.hjson 或 zh-Hans/ 中。
    /// 例如：Mods.LLL.Buffs.AmplifierBuff.CritChance 对应暴击率显示行。
    /// </summary>
    public class AmplifierBuff : ModBuff
    {
        /// <summary>
        /// Buff图标纹理路径（指向 Assets 目录下的图片）。
        /// </summary>
        public override string Texture => "LLL/Buffs/AmplifierBuff";

        /// <summary>
        /// 设置Buff的静态属性。
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;          // 不是减益效果
            Main.buffNoTimeDisplay[Type] = true; // 不显示剩余时间（永久）
        }

        /// <summary>
        /// 【核心方法】动态生成Buff的显示名称和描述文本。
        /// 每次玩家将鼠标悬停到Buff图标时都会调用。
        /// 
        /// 文本结构：
        ///   - 第一行：Buff名称（固定为"[c/FF1493:宇宙增幅--无穷伟力已尽归汝身]"）
        ///   - 后续每行：一个属性项，格式为 "属性名: 数值"
        ///   - 最后：如果有磁力拾取功能，显示快捷键提示
        /// 
        /// 📐 注意：所有数值都是"配置值 × 当前阶段系数"，反映实际生效量。
        /// </summary>
        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            // 获取配置和当前玩家（LocalPlayer 在 ModBuff 中可用）
            var config = ModContent.GetInstance<LLLConfig>();
            var player = Main.LocalPlayer;

            // 计算当前阶段系数（与 CorePlayer 保持一致）
            float coeff = GetCurrentCoefficient(config);

            // 从翻译文件读取Buff名称
            buffName = Mod.GetLocalization("Buffs.AmplifierBuff.DisplayName").Value;

            // 定义一个辅助方法，简化本地化字符串的读取和格式化
            // 用法：GetText("CritChance", crit) → 读取 CritChance 键，并替换 {0}
            string GetText(string key, params object[] args) =>
                Mod.GetLocalization($"Buffs.AmplifierBuff.{key}").Format(args);

            // 使用 StringBuilder 高效拼接多行文本
            var sb = new StringBuilder();

            // ─── 1. 数值属性（均乘以阶段系数后取整或保留浮点） ────
            // 📐 暴击率：直接取整显示
            int crit = (int)(config.BonusCritChance * coeff);
            // 📐 护甲穿透：直接取整
            int ap = (int)(config.BonusArmorPen * coeff);
            // 📐 伤害倍率：保留1位小数（如 5.0x）
            float dmgMult = config.MeleeDamageMultiplier * coeff;
            // 📐 伤害减免：取整百分比
            int dmgRed = (int)(config.DamageReduction * coeff);
            // 📐 最大生命：取整
            int life = (int)(config.BonusMaxLife * coeff);
            // 📐 最大魔力：取整
            int mana = (int)(config.BonusMaxMana * coeff);
            // 📐 魔力消耗减免：取整百分比
            int manaCost = (int)(config.ManaCostReduction * coeff);
            // 📐 挖掘速度：取整百分比
            int pick = (int)(config.MiningSpeedIncrease * coeff);
            // 📐 攻击速度：取整百分比
            int atkSpeed = (int)(config.BonusAttackSpeed * coeff);
            // 📐 召唤栏位：取整
            int minions = (int)(config.BonusMaxMinions * coeff);
            // 📐 哨兵栏位：取整
            int turrets = (int)(config.BonusMaxTurrets * coeff);
            // 📐 运气值：保留1位小数
            float luck = config.BonusLuck * coeff;

            // 按顺序添加每一行（顺序与翻译文件中的键名对应）
            sb.AppendLine(GetText("CritChance", crit));
            sb.AppendLine(GetText("ArmorPen", ap));
            sb.AppendLine(GetText("MeleeMult", dmgMult));
            sb.AppendLine(GetText("DamageReduction", dmgRed));
            sb.AppendLine(GetText("MaxLife", life));
            sb.AppendLine(GetText("MaxMana", mana));
            sb.AppendLine(GetText("ManaCost", manaCost));
            sb.AppendLine(GetText("PickSpeed", pick));
            sb.AppendLine(GetText("AttackSpeed", atkSpeed));
            sb.AppendLine(GetText("MaxMinions", minions));
            sb.AppendLine(GetText("MaxTurrets", turrets));
            sb.AppendLine(GetText("Luck", luck));

            // ─── 2. 布尔加成：三态显示 + Boss 解锁进度 ──────────────
            // 判定统一走本人物存档里的 CorePlayer（配置启用 + 是否已解锁）
            CorePlayer core = Main.LocalPlayer.GetModPlayer<CorePlayer>();

            bool anyEffectEnabled = false;
            foreach (BooleanBonusEffect effect in System.Enum.GetValues<BooleanBonusEffect>())
            {
                if (!CorePlayer.IsEnabledInConfig(effect)) continue;

                anyEffectEnabled = true;
                bool active = core.IsEffectActive(effect);
                sb.AppendLine(GetText(active ? ActiveKeyOf(effect) : LockedKeyOf(effect)));
            }

            if (anyEffectEnabled && !core.AllUnlocked)
            {
                // 进度行：击败 1 / 2 / 3 个不同 Boss 逐级解锁
                sb.AppendLine(GetText("BossProgress",
                    System.Math.Min(core.DefeatedBossCount, CorePlayer.FullUnlockBossCount),
                    CorePlayer.FullUnlockBossCount));
            }

            // ─── 3. 磁力拾取快捷键提示（始终显示，不由布尔加成控制） ──
            if (config.EnableMagnetKey)
            {
                // 读取玩家当前为“磁力拾取”绑定的键。
                // 玩家在“设置 → 控制”里改键后会自动跟着变，因为本方法每次悬停 Buff 图标都会重新执行。
                var magnetKeys = KeybindSystem.MagnetKeybind?.GetAssignedKeys();

                sb.Append(magnetKeys != null && magnetKeys.Count > 0
                    ? GetText("MagnetTip", string.Join(" / ", magnetKeys)) // 绑了键 → “按下 Z 传送掉落物”
                    : GetText("MagnetTipUnbound"));                        // 没绑键 → 单独一句提示
            }

            // 将构建好的文本赋值给 tip 参数
            tip = sb.ToString();

            // 稀有度颜色（由配置决定，这里保持默认即可）
            rare = 0;
        }

        // ═══════════════════════════════════════════════════════════════
        // 🔄 辅助方法（与 CorePlayer 保持逻辑一致）
        // ═══════════════════════════════════════════════════════════════
        // 由于无法直接引用 CorePlayer 中的方法（会引起循环依赖），
        // 这里复制了一份相同的逻辑。如果 CorePlayer 中的逻辑有变化，
        // 需要同步更新这里。

        /// <summary>
        /// 计算当前阶段系数（与 CorePlayer.GetCurrentCoefficient 逻辑一致）
        /// </summary>
        private static float GetCurrentCoefficient(LLLConfig config)
        {
            if (!config.EnableProgression) return 1f;
            if (NPC.downedMoonlord) return config.Phase3Coefficient;
            if (Main.hardMode) return config.Phase2Coefficient;
            return config.Phase1Coefficient;
        }

        /// <summary>已解锁时显示的翻译键名。</summary>
        private static string ActiveKeyOf(BooleanBonusEffect effect)
        {
            switch (effect)
            {
                case BooleanBonusEffect.NoKnockback: return "NoKnockback";
                case BooleanBonusEffect.AmmoConservation: return "AmmoSave";
                default: return "NoFallDmg";
            }
        }

        /// <summary>尚未解锁时显示的翻译键名（灰色 ✗ 那种）。</summary>
        private static string LockedKeyOf(BooleanBonusEffect effect)
        {
            switch (effect)
            {
                case BooleanBonusEffect.NoKnockback: return "NoKnockbackLocked";
                case BooleanBonusEffect.AmmoConservation: return "AmmoSaveLocked";
                default: return "NoFallDmgLocked";
            }
        }
    }
}