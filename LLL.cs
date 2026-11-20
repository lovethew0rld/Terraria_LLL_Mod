using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using LLL.Players;
using Terraria; // 包含 Main、NPCID 等
using Terraria.ID; // 包含 ItemID、NPCID
using Terraria.ModLoader.IO; // 包含 TagCompound

namespace LLL
{
    /// <summary>
    /// LLL 模组的主类，继承自 Mod。
    /// 这个类是模组的入口点，tModLoader 会自动加载并初始化它。
    /// 你可以在里面重写各种生命周期方法，比如 Load、Unload、AddRecipes 等。
    /// 目前这个类还没有添加任何功能逻辑，后续可以根据需要扩展。
    /// </summary>
    public class LLL : Mod
    {
        // 注意：这里没有重写任何方法，所有配置和功能都通过其他类（如玩家类、配置类）实现。
        // 未来如果需要在模组加载时执行初始化，可以重写 Load() 方法。
    }
}