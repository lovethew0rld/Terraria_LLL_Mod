using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace LLL
{
    /// <summary>
    /// 客户端配置文件，对应游戏内“饰品配置”界面。
    /// 玩家可独立调整各项属性加成，支持进度阶段限制（由 EnableProgression 控制）。
    /// </summary>
    public class LLLConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // ==================== 进攻属性 ====================
        [Header("$Mods.LLL.Configs.LLLConfig.Headers.Offense")]

        /// <summary>
        /// 额外全局暴击率百分比。默认 100%。
        /// </summary>
        [Range(0, 100)]
        [DefaultValue(100)]
        public int BonusCritChance;

        /// <summary>
        /// 额外全局护甲穿透值。默认 35。
        /// </summary>
        [Range(0, 200)]
        [DefaultValue(35)]
        public int BonusArmorPen;

        /// <summary>
        /// 额外最大召唤物（仆从）栏位。默认 66。
        /// </summary>
        [Range(0, 100)]
        [DefaultValue(66)]
        public int BonusMaxMinions;

        /// <summary>
        /// 额外最大哨兵（炮台）栏位。默认 66。
        /// </summary>
        [Range(0, 100)]
        [DefaultValue(66)]
        public int BonusMaxTurrets;

        /// <summary>
        /// 最终伤害倍率（近战伤害乘数）。默认 5 倍。
        /// 根据翻译文本，此值为“伤害倍率”，但字段名为 MeleeDamageMultiplier，实际可能应用于所有伤害，请以游戏内表现或后续代码为准。
        /// </summary>
        [Range(1f, 100f)]
        [Increment(1f)]
        [DefaultValue(5f)]
        public float MeleeDamageMultiplier;

        // ==================== 防御属性 ====================
        [Header("$Mods.LLL.Configs.LLLConfig.Headers.Defense")]

        /// <summary>
        /// 是否免疫击退。默认启用。
        /// </summary>
        [DefaultValue(true)]
        public bool EnableNoKnockback;

        /// <summary>
        /// 是否免疫掉落伤害。默认启用。
        /// </summary>
        [DefaultValue(true)]
        public bool EnableNoFallDamage;

        /// <summary>
        /// 额外伤害减免百分比。默认 40%。
        /// </summary>
        [Range(0, 100)]
        [DefaultValue(40)]
        public int DamageReduction;

        /// <summary>
        /// 额外最大生命值上限。默认 +100。
        /// </summary>
        [Range(0, 1000)]
        [DefaultValue(100)]
        public int BonusMaxLife;

        // ==================== 通用 / 实用属性 ====================
        [Header("$Mods.LLL.Configs.LLLConfig.Headers.Utility")]

        /// <summary>
        /// 所有物品的法力消耗减免百分比。默认 80%。
        /// </summary>
        [Range(0, 100)]
        [DefaultValue(80)]
        public int ManaCostReduction;

        /// <summary>
        /// 额外最大法力值上限。默认 +100。
        /// </summary>
        [Range(0, 1000)]
        [DefaultValue(100)]
        public int BonusMaxMana;

        /// <summary>
        /// 挖掘速度加成百分比。默认 30%。
        /// </summary>
        [Range(0, 100)]
        [DefaultValue(30)]
        public int MiningSpeedIncrease;

        /// <summary>
        /// 攻击速度（物品使用速度）加成。默认 +40%（数值越大使用速度越快，实际效果为使用时间减少）。
        /// </summary>
        [Range(0, 500)]
        [DefaultValue(40)]
        public int BonusAttackSpeed;

        /// <summary>
        /// 额外运气值。范围 0~1，默认 0。
        /// </summary>
        [Range(0f, 1f)]
        [Increment(0.1f)]
        [DefaultValue(0f)]
        public float BonusLuck;

        /// <summary>
        /// 是否为远程武器提供 100% 不消耗弹药的效果。默认启用。
        /// </summary>
        [DefaultValue(true)]
        public bool EnableAmmoConservation;

        // ==================== 进度阶段限制 ====================
        [Header("$Mods.LLL.Configs.LLLConfig.Headers.Progression")]

        /// <summary>
        /// 是否启用阶段限制。开启后，部分加成会随游戏进度（肉前、肉后、月后）逐步增强；
        /// 关闭则立即获得全部力量。
        /// </summary>
        [DefaultValue(true)]
        public bool EnableProgression;

        /// <summary>
        /// 开局阶段（击败肉山前）的加成倍率。范围 0.1~1.0，默认 0.2。
        /// </summary>
        [Range(0.1f, 1f)]
        [Increment(0.1f)]
        [DefaultValue(0.2f)]
        public float Phase1Coefficient;

        /// <summary>
        /// 困难模式（肉山后）的加成倍率。默认 0.5。
        /// </summary>
        [Range(0.1f, 1f)]
        [Increment(0.1f)]
        [DefaultValue(0.5f)]
        public float Phase2Coefficient;

        /// <summary>
        /// 击败月亮领主后的加成倍率。默认 1.0（全效）。
        /// </summary>
        [Range(0.1f, 1f)]
        [Increment(0.1f)]
        [DefaultValue(1f)]
        public float Phase3Coefficient;

        /// <summary>
        /// 是否在开局时即解锁免疫掉落伤害、免疫击退、不消耗弹药这三个效果。
        /// 关闭则需要击败任意一个 Boss 后才全部解锁。
        /// </summary>
        [DefaultValue(false)]
        public bool UnlockBooleanBonusesAtStart;

        /// <summary>
        /// 是否启用磁力拾取快捷键（默认 Z 键），按下后可将全图掉落物传送至玩家身边。
        /// </summary>
        [DefaultValue(true)]
        public bool EnableMagnetKey;

        /// <summary>
        /// 是否在召唤法杖的物品说明、以及召唤物 buff 的描述里，显示「当前数量 / 栏位上限」。
        /// 默认启用；关闭后这两个地方都不再显示这一行。
        /// </summary>
        [Header("$Mods.LLL.Configs.LLLConfig.Headers.Display")]
        [DefaultValue(true)]
        public bool ShowMinionCount;
    }
}