namespace LLL
{
    /// <summary>
    /// 额外战利品的修饰语（前缀）使用模式。
    ///
    /// 【为什么成员名用英文】
    /// 成员名会直接进入本地化 key（Mods.LLL.Configs.ExtraLootPrefixMode.&lt;成员名&gt;.Label），
    /// 用英文可以避免中文标识符可能带来的编码风险；界面文字全部交给本地化文件。
    /// en-US 不写也有默认文字（tModLoader 会自动把成员名拆成 "All Random" 这样的可读文本）。
    /// 对应的翻译条目位于 Configs → ExtraLootPrefixMode → &lt;成员名&gt; → Label / Tooltip。
    /// </summary>
    public enum ExtraLootPrefixMode
    {
        /// <summary>每份额外战利品都随机取一个修饰语：可能正面、可能负面，另有 25% 概率轮空（不带修饰语）。</summary>
        AllRandom = 0,

        /// <summary>
        /// 每份额外战利品都带本分类的指定修饰语：
        /// 近战与镐/斧/锤 = 传奇，远程 = 虚幻，魔法 = 神话，召唤 = 无情，饰品 = 护佑。
        /// 指定修饰语万一用不了（例如长矛拿不到传奇）会自动退化成随机。
        /// </summary>
        AlwaysSpecified = 1,

        /// <summary>只给第 1 份额外战利品带指定修饰语，其余随机（同样可能负面、可能轮空）。</summary>
        FirstSpecifiedRestRandom = 2,
    }
}
