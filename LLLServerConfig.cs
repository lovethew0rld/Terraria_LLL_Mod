using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace LLL
{
    /// <summary>
    /// 服务器端配置文件，对应“游戏机制更改”界面，只能由服务器管理员在菜单中调整。
    /// 大部分选项带有 [ReloadRequired] 特性，修改后需要重载模组才能生效。
    /// </summary>
    public class LLLServerConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        // ==================== 合成系统设置 ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.Crafting")]

        /// <summary>
        /// 是否启用全局合成材料调整功能。默认禁用。
        /// </summary>
        [ReloadRequired]
        [DefaultValue(false)]
        public bool EnableCraftingCostTweak;

        /// <summary>
        /// 所有配方所需材料的百分比（100% 为原版消耗，1% 为最低消耗）。默认 100%。
        /// </summary>
        [ReloadRequired]
        [Range(1, 1000)]
        [DefaultValue(100)]
        public int CraftingCostMultiplier;

        // ==================== 战斗机制（斩杀） ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.Combat")]

        /// <summary>
        /// 是否对非 Boss 敌怪启用低血量斩杀。默认禁用。
        /// </summary>
        [ReloadRequired]
        [DefaultValue(false)]
        public bool EnableExecute;

        /// <summary>
        /// 非 Boss 敌怪的斩杀线（生命值百分比），低于此值即被击杀。默认 20%。
        /// </summary>
        [ReloadRequired]
        [Range(1, 100)]
        [DefaultValue(20)]
        public int ExecutePercentage;

        /// <summary>
        /// 是否对 Boss 启用低血量斩杀（单独控制）。默认禁用。
        /// </summary>
        [ReloadRequired]
        [DefaultValue(false)]
        public bool EnableBossExecute;

        /// <summary>
        /// Boss 的斩杀线（生命值百分比）。默认 5%。
        /// </summary>
        [ReloadRequired]
        [Range(1, 100)]
        [DefaultValue(5)]
        public int BossExecutePercentage;

        // ==================== 战利品倍率 ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.LootSettings")]

        /// <summary>
        /// NPC 战利品掉落倍率（整数倍），例如 2 表示掉落数量翻倍。默认 2 倍。
        /// </summary>
        [ReloadRequired]
        [Range(1, 100)]
        [DefaultValue(2)]
        public int LootMultiplier;

        /// <summary>
        /// 是否给多倍掉落“额外生成的那几份”战利品附加修饰语（前缀）。默认禁用。
        /// 只有能附魔的物品生效：武器、带伤害的镐/斧/锤、饰品；鱼竿与时装不附魔。
        /// 原本掉落的那一份不受影响。
        /// </summary>
        [DefaultValue(false)]
        public bool PrefixExtraLoot;

        // ─────────────────────────────────────────────────────────────
        // 下面 5 个是三档模式（下拉框直接显示枚举成员名）：
        //   全部随机           —— 每个额外副本随机取一个修饰语（可负面；25% 概率轮空不加）
        //   全部指定           —— 每个额外副本都用本分类的指定修饰语
        //   首件指定其余随机   —— 额外副本第 1 件用指定修饰语，其余随机
        // 指定修饰语：近战与镐/斧/锤 = 传奇，远程 = 虚幻，魔法 = 神话，召唤 = 无情，饰品 = 护佑。
        // ─────────────────────────────────────────────────────────────

        /// <summary>近战武器与镐/斧/锤的额外战利品修饰语模式。</summary>
        [DefaultValue(ExtraLootPrefixMode.AllRandom)]
        public ExtraLootPrefixMode ExtraLootMeleePrefix;

        /// <summary>远程武器的额外战利品修饰语模式。</summary>
        [DefaultValue(ExtraLootPrefixMode.AllRandom)]
        public ExtraLootPrefixMode ExtraLootRangedPrefix;

        /// <summary>魔法武器的额外战利品修饰语模式。</summary>
        [DefaultValue(ExtraLootPrefixMode.AllRandom)]
        public ExtraLootPrefixMode ExtraLootMagicPrefix;

        /// <summary>召唤武器（含鞭）的额外战利品修饰语模式。</summary>
        [DefaultValue(ExtraLootPrefixMode.AllRandom)]
        public ExtraLootPrefixMode ExtraLootSummonPrefix;

        /// <summary>饰品的额外战利品修饰语模式。</summary>
        [DefaultValue(ExtraLootPrefixMode.AllRandom)]
        public ExtraLootPrefixMode ExtraLootAccessoryPrefix;

        // ==================== 无敌帧设置 ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.ImmunityFrames")]

        /// <summary>
        /// 是否禁用玩家的受伤无敌帧（受伤后闪烁保护）。默认不禁用。
        /// </summary>
        [ReloadRequired]
        [DefaultValue(false)]
        public bool DisablePlayerImmunity;

        /// <summary>
        /// 是否禁用 NPC / 怪物受击后的无敌帧（允许连续攻击）。默认不禁用。
        /// </summary>
        [ReloadRequired]
        [DefaultValue(false)]
        public bool DisableNPCImmunity;

        // ==================== 容器装备生效（背包、银行等） ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.ContainerEquip")]

        /// <summary>
        /// 容器内饰品是否生效的总开关。开启后各子容器生效，关闭则全部失效。默认禁用（代码默认 false，但翻译提示默认开，以代码为准）。
        /// </summary>
        [ReloadRequired]
        [DefaultValue(false)]
        public bool EnableContainerEquip;

        /// <summary>
        /// 主背包内的饰品是否视为已装备。默认禁用（以代码为准）。
        /// </summary>
        [ReloadRequired]
        [DefaultValue(false)]
        public bool EnableInventoryEquip;

        /// <summary>
        /// 虚空保险库内的饰品是否视为已装备。默认禁用。
        /// </summary>
        [ReloadRequired]
        [DefaultValue(false)]
        public bool EnableVoidVaultEquip;

        /// <summary>
        /// 猪猪存钱罐内的饰品是否视为已装备。默认禁用。
        /// </summary>
        [ReloadRequired]
        [DefaultValue(false)]
        public bool EnablePiggyBankEquip;

        /// <summary>
        /// 保险箱内的饰品是否视为已装备。默认禁用。
        /// </summary>
        [ReloadRequired]
        [DefaultValue(false)]
        public bool EnableSafeEquip;

        /// <summary>
        /// 护卫熔炉内的饰品是否视为已装备。默认禁用。
        /// </summary>
        [ReloadRequired]
        [DefaultValue(false)]
        public bool EnableDefenderForgeEquip;

        // ==================== NPC 商店解锁条件调整 ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.ShopUnlock")]

        /// <summary>
        /// 是否解锁 NPC 商店中所有有条件物品（如生物群系晶塔、丛林限定商品）的出售限制。默认禁用。
        /// </summary>
        [ReloadRequired]
        [DefaultValue(false)]
        public bool EnableUnlockShopConditions;

        // ==================== 视觉特效 ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.VisualEffects")]

        /// <summary>
        /// 是否启用伤害类型的文字特效（如彩色伤害数字）。默认启用。
        /// </summary>
        [DefaultValue(true)]
        public bool EnableCoolDamageTooltip;

        /// <summary>
        /// 是否启用物品名字的稀有度特效。默认启用。
        /// </summary>
        [DefaultValue(true)]
        public bool EnableCoolItemName;

        // ==================== 背包火力全开（万剑齐发） ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.InventoryFire")]

        /// <summary>
        /// 是否启用“发射背包其他武器的射弹”功能：使用非召唤杖武器时，同步发射背包内其他武器的射弹。默认禁用。
        /// </summary>
        [DefaultValue(false)]
        public bool EnableInventoryFire;

        /// <summary>
        /// 背包火力全开时，远程武器是否正常消耗弹药。默认启用（消耗弹药）。
        /// </summary>
        [DefaultValue(true)]
        public bool InventoryFireConsumeAmmo;

        // ==================== 世界生成与福利 ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.WorldGen")]

        /// <summary>
        /// 是否在创建世界时于地牢入口生成一个“地牢礼物”上锁金箱，内含一些武器。默认禁用。
        /// </summary>
        [DefaultValue(false)]
        public bool EnableDungeonGift;

        /// <summary>
        /// 是否启用“破裂地牢砖 100% 连锁破坏”：破坏任意一块破裂砖会自动破坏相连的所有破裂砖。默认启用。
        /// </summary>
        [DefaultValue(true)]
        public bool EnableCrackedBrickChain;

        // ==================== 节日 ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.Holiday")]

        /// <summary>
        /// 是否强制开启圣诞节（敌人掉礼物、圣诞老人、圣诞主题外观与音乐）。
        /// 改动即时生效，无需重载模组。
        /// </summary>
        [DefaultValue(false)]
        public bool ForceChristmas;

        /// <summary>
        /// 是否强制开启万圣节（敌人掉福袋、南瓜与幽灵主题内容、NPC 变装）。
        /// 可以和圣诞节同时开启；改动即时生效，无需重载模组。
        /// </summary>
        [DefaultValue(false)]
        public bool ForceHalloween;

        // ==================== 额外宝箱物品 ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.ChestItemChain")]

        /// <summary>
        /// 是否在生成世界时为某些宝箱额外添加物品（只影响新世界）。
        /// 分组按“箱子图格类型+样式 + 环境 + 深度带”，参与物品为“第一格物品 + 所有可装备”。默认禁用。
        /// </summary>
        [DefaultValue(false)]
        public bool EnableChestItemChain;

        /// <summary>
        /// 是否在模组日志里输出“额外宝箱物品”的分组与生成明细（排错用）。默认禁用。
        /// </summary>
        [DefaultValue(false)]
        public bool EnableChestItemChainDebugLog;

        // ==================== 监狱建造器设置 ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.PrisonBuilder")]

        /// <summary>
        /// 是否让 NPC 监狱建造器使用随机材料（花里胡哨）。默认禁用。
        /// </summary>
        [ReloadRequired]
        [DefaultValue(false)]
        public bool RandomizePrisonMaterials;

        /// <summary>
        /// 是否限制监狱建造器只使用肉前材料（防止微光等后期材料提前出现）。默认启用。
        /// </summary>
        [ReloadRequired]
        [DefaultValue(true)]
        public bool RestrictMaterialsToPreHardmode;

        // ==================== 钓鱼系统 ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.Fishing")]

        /// <summary>
        /// 每次挥动鱼竿时额外抛出的鱼漂数量（0 为原版单线）。最多额外 50 条。默认额外 1 条（共 2 条）。
        /// </summary>
        [Range(0, 50)]
        [DefaultValue(1)]
        public int ExtraFishingLines;

        // ==================== 任务鱼调整 ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.QuestFish")]

        /// <summary>
        /// 任务鱼调整总开关。默认禁用。
        /// 开启后可以无视生物群系/日期去钓任务鱼，具体规则见下面两项。
        /// </summary>
        [DefaultValue(false)]
        public bool EnableFishingQuestTweak;

        /// <summary>
        /// 任意水域都能钓到「今天的任务鱼」（不再要求待在对应生物群系/深度）。默认启用。
        /// </summary>
        [DefaultValue(true)]
        public bool QuestFishAnyWater;

        /// <summary>
        /// 任意时间都能钓到「当前环境对应的任务鱼」（不再要求今天是这条鱼）。默认启用。
        /// </summary>
        [DefaultValue(true)]
        public bool QuestFishAnyDay;

        /// <summary>
        /// 是否无视困难模式限定（开启后，肉前也能钓到鱼龙、独角兽鱼等肉后专属任务鱼）。默认禁用。
        /// </summary>
        [DefaultValue(false)]
        public bool QuestFishIgnoreHardmode;

        // ==================== 大量获得物品快捷键 ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.QuickItemGrab")]

        /// <summary>
        /// 是否启用快捷键获取鼠标悬停物品的最大堆叠数量。默认禁用。
        /// </summary>
        [DefaultValue(false)]
        public bool EnableQuickItemGrab;

        // ==================== 彩蛋设置 ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.EasterEggs")]

        /// <summary>
        /// 是否启用彩蛋效果：发散 1~2 伤害的彩虹粒子攻击敌人，且被攻击的非 Boss 敌人最大生命值减半。默认禁用。
        /// </summary>
        [DefaultValue(false)]
        public bool ShowHaloVisual;

        // ==================== 饰品词条同步 ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.AutoReforge")]

        /// <summary>
        /// 饰品词条同步总开关。默认禁用。
        /// 开启后以「第一个饰品栏」为模板：只要它的词条发生变化（放入带词条的饰品、
        /// 取下再放回、直接换另一件、重铸它），就把该词条覆盖到其余所有饰品栏与装饰栏。
        /// 源没有词条（素饰品）时整体跳过，不会清洗别人的词条。
        /// </summary>
        [DefaultValue(false)]
        public bool EnableAutoReforge;

        /// <summary>
        /// 同步范围是否包含所有饰品栏：原版 7 格 + 其它模组添加的额外饰品栏。默认启用。
        /// </summary>
        [DefaultValue(true)]
        public bool AutoReforgeIncludeAccessories;

        /// <summary>
        /// 同步范围是否包含所有装饰（社交）栏：原版 7 格 + 其它模组添加的额外装饰栏。默认启用。
        /// </summary>
        [DefaultValue(true)]
        public bool AutoReforgeIncludeVanity;

        // ==================== 开发者测试功能 ====================
        [Header("$Mods.LLL.Configs.LLLServerConfig.Headers.DeveloperTesting")]

        /// <summary>
        /// 是否在所有敌怪头顶显示实时生命值（当前/最大），方便测试生命值减半等机制。默认禁用。
        /// </summary>
        [DefaultValue(false)]
        public bool ShowEnemyHealth;
    }
}