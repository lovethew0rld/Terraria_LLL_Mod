using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using LLL.Buffs;
using LLL.Systems;
using LLL;

namespace LLL
{
    /// <summary>
    /// 核心玩家类 —— 模组最核心的增益逻辑。
    /// 负责：永久施加宇宙增幅Buff、阶段系数计算、属性加成、
    /// 磁力拾取快捷键、弹药节省、伤害倍率等。
    /// 
    /// 【快捷键引用说明】
    /// 本类不再自行注册快捷键，所有快捷键统一从 KeybindSystem 中读取。
    /// 这样可以集中管理，避免快捷键注册分散在多个文件中。
    /// </summary>
    public class CorePlayer : ModPlayer
    {
        // ═══════════════════════════════════════════════════════════════
        // 🌍 Boss 进度与布尔加成解锁（**存在人物存档里**，一个字符一套进度）
        // ═══════════════════════════════════════════════════════════════
        // 规则：
        //   击败第 1 个不同 Boss → 从未解锁且已启用的效果里随机解锁 1 个
        //   击败第 2 个不同 Boss → 再随机解锁 1 个
        //   击败第 3 个不同 Boss → 全部解锁
        // 不追究历史进度：启用模组（进世界）之前打过的 Boss 不算，完结存档也要重新打 3 个。
        // 检测工作由 Systems/BossProgressDetector 负责，这里只管记录与解锁。

        /// <summary>解锁全部效果所需的“不同 Boss”数量。</summary>
        public const int FullUnlockBossCount = 3;

        private readonly HashSet<int> _defeatedBossTypes = new HashSet<int>();
        private readonly HashSet<int> _unlockedEffects = new HashSet<int>();

        /// <summary>已击败的不同 Boss 数量（界面进度用）。</summary>
        public int DefeatedBossCount => _defeatedBossTypes.Count;

        /// <summary>是否已处于“全解锁”状态（配置里勾了“开局即解锁”也算）。</summary>
        public bool AllUnlocked =>
            ModContent.GetInstance<LLLConfig>().UnlockBooleanBonusesAtStart
            || _defeatedBossTypes.Count >= FullUnlockBossCount;

        /// <summary>已解锁的效果编号（日志用）。</summary>
        public IEnumerable<int> UnlockedEffectIds => _unlockedEffects;

        /// <summary>新建人物时清空（随后 LoadData 会从存档读回）。</summary>
        public override void Initialize()
        {
            _defeatedBossTypes.Clear();
            _unlockedEffects.Clear();
        }

        public override void SaveData(TagCompound tag)
        {
            tag["LLLDefeatedBossTypes"] = new List<int>(_defeatedBossTypes);
            tag["LLLUnlockedEffects"] = new List<int>(_unlockedEffects);
        }

        public override void LoadData(TagCompound tag)
        {
            _defeatedBossTypes.Clear();
            _unlockedEffects.Clear();

            foreach (int type in tag.GetList<int>("LLLDefeatedBossTypes"))
                _defeatedBossTypes.Add(type);

            foreach (int effect in tag.GetList<int>("LLLUnlockedEffects"))
                _unlockedEffects.Add(effect);
        }

        /// <summary>
        /// 登记一个“不同 Boss”（由 BossProgressDetector 调用）。
        /// 返回 true 表示这是新进度（已顺带完成解锁计算）。
        /// </summary>
        public bool RecordBoss(int bossType)
        {
            if (!_defeatedBossTypes.Add(bossType))
                return false;   // 同一只 Boss 刷多次 → 自动去重

            ReconcileUnlocks();
            return true;
        }

        /// <summary>某个布尔加成此刻是否生效 = 配置里启用了它 **且** 已解锁它。</summary>
        public bool IsEffectActive(BooleanBonusEffect effect)
        {
            if (!IsEnabledInConfig(effect)) return false;
            if (AllUnlocked) return true;
            return _unlockedEffects.Contains((int)effect);
        }

        /// <summary>配置里是否启用了这个效果（玩家可以单独关掉某一个）。</summary>
        public static bool IsEnabledInConfig(BooleanBonusEffect effect)
        {
            LLLConfig config = ModContent.GetInstance<LLLConfig>();
            switch (effect)
            {
                case BooleanBonusEffect.NoFallDamage: return config.EnableNoFallDamage;
                case BooleanBonusEffect.NoKnockback: return config.EnableNoKnockback;
                case BooleanBonusEffect.AmmoConservation: return config.EnableAmmoConservation;
                default: return false;
            }
        }

        /// <summary>按当前 Boss 进度把解锁数量补齐（幂等）。</summary>
        private void ReconcileUnlocks()
        {
            var enabled = new List<BooleanBonusEffect>();
            foreach (BooleanBonusEffect effect in System.Enum.GetValues<BooleanBonusEffect>())
            {
                if (IsEnabledInConfig(effect))
                    enabled.Add(effect);
            }

            if (enabled.Count == 0) return;

            int target = System.Math.Min(_defeatedBossTypes.Count, FullUnlockBossCount);

            if (target >= FullUnlockBossCount)
            {
                foreach (BooleanBonusEffect effect in enabled)
                    _unlockedEffects.Add((int)effect);
                return;
            }

            int unlockedEnabled = 0;
            foreach (BooleanBonusEffect effect in enabled)
            {
                if (_unlockedEffects.Contains((int)effect))
                    unlockedEnabled++;
            }

            for (int i = unlockedEnabled; i < target && i < enabled.Count; i++)
            {
                var candidates = new List<BooleanBonusEffect>();
                foreach (BooleanBonusEffect effect in enabled)
                {
                    if (!_unlockedEffects.Contains((int)effect))
                        candidates.Add(effect);
                }

                if (candidates.Count == 0) break;
                _unlockedEffects.Add((int)candidates[Main.rand.Next(candidates.Count)]);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 📐 阶段系数计算
        // ═══════════════════════════════════════════════════════════════
        /// <summary>
        /// 根据游戏进度返回当前阶段的加成系数。
        /// 如果 EnableProgression 为 false，则始终返回 1.0（满效）。
        /// 否则：月后→Phase3，肉后→Phase2，肉前→Phase1。
        /// </summary>
        private float GetCurrentCoefficient()
        {
            var config = ModContent.GetInstance<LLLConfig>();
            if (!config.EnableProgression) return 1f;
            if (NPC.downedMoonlord) return config.Phase3Coefficient;
            if (Main.hardMode) return config.Phase2Coefficient;
            return config.Phase1Coefficient;
        }

        // ═══════════════════════════════════════════════════════════════
        // ⌨️ 磁力拾取快捷键（统一从 KeybindSystem 引用）
        // ═══════════════════════════════════════════════════════════════
        // 💡 原代码在此处注册了 MagnetKey，现已迁移至 KeybindSystem.cs。
        //    现在通过静态属性访问，保持功能不变，代码更整洁。

        /// <summary>
        /// 磁力拾取快捷键的引用（从 KeybindSystem 获取）。
        /// 这是一个便捷属性，避免每次写 KeybindSystem.MagnetKeybind。
        /// </summary>
        private static ModKeybind MagnetKey => KeybindSystem.MagnetKeybind;

        // ⚠️ 注意：Load() 和 Unload() 方法已被移除，
        //    因为快捷键注册现在由 KeybindSystem 统一管理。

        // 冷却计时器（防止频繁触发导致性能问题）
        private static int cooldown = 0;

        // ═══════════════════════════════════════════════════════════════
        // 🔄 每帧更新：Buff 维持 + 快捷键检测
        // ═══════════════════════════════════════════════════════════════
        public override void PostUpdateMiscEffects()
        {
            // 永久施加宇宙增幅 Buff（每帧刷新持续时间，确保不断）
            // 📐 持续时间 2 帧：每帧重新添加，保证 Buff 图标常驻
            Player.AddBuff(ModContent.BuffType<Buffs.AmplifierBuff>(), 2);

            // 冷却递减
            if (cooldown > 0)
                cooldown--;

            // 快捷键检测：按下 Z 且冷却为0
            var config = ModContent.GetInstance<LLLConfig>();
            if (config.EnableMagnetKey && MagnetKey.JustPressed && cooldown == 0)
            {
                PullItemsToPlayer();
                cooldown = 30; // 📐 冷却 30 帧（0.5 秒），防止疯狂触发
                // 可调范围 10~60，值越小响应越快但性能开销越大。
            }
        }

        /// <summary>
        /// 磁力拾取核心：遍历所有掉落物，将其位置设到玩家中心。
        /// 简单粗暴但有效，省去复杂的物理模拟。
        /// </summary>
        private void PullItemsToPlayer()
        {
            for (int i = 0; i < Main.maxItems; i++)
            {
                Item drop = Main.item[i];
                if (drop.active && !drop.favorited) // 只取活跃且未收藏的物品
                {
                    drop.position = Player.Center; // 直接传送至玩家中心
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 📊 属性加成应用（在装备更新后触发）
        // ═══════════════════════════════════════════════════════════════
        public override void PostUpdateEquips()
        {
            var config = ModContent.GetInstance<LLLConfig>();
            float coeff = GetCurrentCoefficient(); // 当前阶段系数

            // ─── 数值加成（均乘以系数） ───
            // 📐 暴击率：直接加算（百分比）
            Player.GetCritChance(DamageClass.Generic) += (int)(config.BonusCritChance * coeff);

            // 📐 魔力消耗减免：乘法叠加，1.0 - 减免百分比
            Player.manaCost *= (1.0f - config.ManaCostReduction / 100f * coeff);

            // 📐 挖掘速度：乘法叠加，数值越大速度越快（此处为速度加成百分比）
            Player.pickSpeed *= (1.0f - config.MiningSpeedIncrease / 100f * coeff);

            // 📐 召唤栏位和哨兵栏位：直接加算
            Player.maxMinions += (int)(config.BonusMaxMinions * coeff);
            Player.maxTurrets += (int)(config.BonusMaxTurrets * coeff);

            // 📐 护甲穿透：直接加算
            Player.GetArmorPenetration(DamageClass.Generic) += (int)(config.BonusArmorPen * coeff);

            // ─── 布尔加成（逐个判定：配置启用 + 已解锁） ───
            // 解锁进度存在本人物存档里（击败 1/2/3 个不同 Boss 逐级解锁）
            if (IsEffectActive(BooleanBonusEffect.NoFallDamage))
                Player.noFallDmg = true;
            if (IsEffectActive(BooleanBonusEffect.NoKnockback))
                Player.noKnockback = true;

            // ─── 其他防御 / 资源加成 ───
            // 📐 伤害减免：直接加算（上限 90%，由游戏本身限制）
            Player.endurance += config.DamageReduction / 100f * coeff;

            // 📐 最大生命、魔力：直接加算
            Player.statLifeMax2 += (int)(config.BonusMaxLife * coeff);
            Player.statManaMax2 += (int)(config.BonusMaxMana * coeff);

            // 📐 攻击速度：加算，数值为百分比（如40% → 使用时间减少40%）
            Player.GetAttackSpeed(DamageClass.Generic) += config.BonusAttackSpeed / 100f * coeff;
        }

        // ═══════════════════════════════════════════════════════════════
        // 🍀 运气加成
        // ═══════════════════════════════════════════════════════════════
        public override void ModifyLuck(ref float luck)
        {
            luck += ModContent.GetInstance<LLLConfig>().BonusLuck;
        }

        // ═══════════════════════════════════════════════════════════════
        // ⚔️ 伤害倍率（应用于所有伤害）
        // ═══════════════════════════════════════════════════════════════
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // 📐 最终伤害倍率 = 配置值 × 阶段系数
            modifiers.FinalDamage *= ModContent.GetInstance<LLLConfig>().MeleeDamageMultiplier * GetCurrentCoefficient();
        }

        // ═══════════════════════════════════════════════════════════════
        // 🏹 弹药节省（100% 不消耗）
        // ═══════════════════════════════════════════════════════════════
        public override bool CanConsumeAmmo(Item weapon, Item ammo)
        {
            // 条件：不消耗弹药已解锁（且配置启用）+ 当前武器为远程
            if (IsEffectActive(BooleanBonusEffect.AmmoConservation) && weapon.CountsAsClass(DamageClass.Ranged))
            {
                return false; // 返回 false 表示不消耗弹药
            }
            return base.CanConsumeAmmo(weapon, ammo);
        }
    }
}