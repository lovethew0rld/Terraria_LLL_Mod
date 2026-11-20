namespace LLL
{
    /// <summary>
    /// 三个“布尔加成”的编号（存进人物存档的就是这些数字，别改数值）。
    /// </summary>
    public enum BooleanBonusEffect
    {
        /// <summary>免疫掉落伤害（LLLConfig.EnableNoFallDamage）</summary>
        NoFallDamage = 0,

        /// <summary>免疫击退（LLLConfig.EnableNoKnockback）</summary>
        NoKnockback = 1,

        /// <summary>远程武器不消耗弹药（LLLConfig.EnableAmmoConservation）</summary>
        AmmoConservation = 2,
    }
}
