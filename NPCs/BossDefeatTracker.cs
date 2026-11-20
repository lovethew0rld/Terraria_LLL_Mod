using Terraria;
using Terraria.ModLoader;
using LLL.Systems;

namespace LLL.NPCs
{
    /// <summary>
    /// Boss 击败追踪器：监听所有 NPC 的击杀事件，转发给 BossProgressDetector。
    /// 具体判定（是不是 Boss、是不是部位、要不要记进度）都在检测器里，这里保持 GlobalNPC 干净。
    /// </summary>
    public class BossDefeatTracker : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            BossProgressDetector.RegisterKill(npc);
        }
    }
}
