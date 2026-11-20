using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using LLL;

namespace LLL.Items
{
    /// <summary>
    /// 背包火力全开（万剑齐发）功能的玩家类。
    /// 当玩家手持武器攻击时，背包中的其他非召唤武器也会同步发射。
    /// 由 LLLServerConfig.EnableInventoryFire 控制总开关。
    /// </summary>
    public class InventoryWeaponPlayer : ModPlayer
    {
        /// <summary>
        /// 持续引导类武器（悠悠球、泰拉魔刃等）的专用冷却计时器。
        /// 因为这类武器没有 "ItemAnimationJustStarted" 事件，需要手动模拟节奏。
        /// </summary>
        public int inventoryFireCooldown = 0;

        public override void PostItemCheck()
        {
            var config = ModContent.GetInstance<LLLServerConfig>();
            if (!config.EnableInventoryFire) return;
            if (Player.whoAmI != Main.myPlayer) return;

            Item heldItem = Player.inventory[Player.selectedItem];
            // 排除空手、无伤害、工具类（镐/斧/锤）
            if (heldItem.IsAir || heldItem.damage <= 0 || heldItem.pick > 0 || heldItem.axe > 0 || heldItem.hammer > 0)
                return;

            // 如果手持的是鞭子（召唤伤害），需要确保有弹幕才能工作
            if (heldItem.DamageType.CountsAsClass(DamageClass.Summon))
            {
                if (heldItem.shoot <= 0 || !ProjectileID.Sets.IsAWhip[heldItem.shoot]) return;
            }

            // ==========================================
            // 触发逻辑：普通武器 + 引导武器双管齐下
            // ==========================================
            bool shouldFire = false;

            // 1. 普通武器（短剑、弓箭等）：每次开始挥动时触发
            if (Player.ItemAnimationJustStarted)
            {
                shouldFire = true;
                inventoryFireCooldown = heldItem.useTime; // 初始化冷却
            }
            // 2. 持续引导类武器（悠悠球、泰拉魔刃）：按住左键时按攻速循环触发
            else if (Player.channel && heldItem.channel)
            {
                inventoryFireCooldown--;
                if (inventoryFireCooldown <= 0)
                {
                    shouldFire = true;
                    inventoryFireCooldown = heldItem.useTime > 0 ? heldItem.useTime : 10;
                }
            }

            if (!shouldFire) return;

            // ==========================================
            // 遍历背包，发射除当前手持武器以外的其他武器
            // ==========================================
            for (int i = 0; i < 50; i++)
            {
                if (i == Player.selectedItem) continue; // 跳过手持武器

                Item invItem = Player.inventory[i];

                // 关键排除：!invItem.channel —— 坚决排除悠悠球、泰拉魔刃等引导类武器
                // 防止这些武器在背包里也发射，避免产生循环/叠层 Bug
                if (!invItem.IsAir && invItem.damage > 0 && invItem.shoot > 0 &&
                    invItem.pick == 0 && invItem.axe == 0 && invItem.hammer == 0 &&
                    !invItem.channel &&  // 排除引导类武器
                    !invItem.DamageType.CountsAsClass(DamageClass.Summon)) // 排除召唤武器（鞭子除外）
                {
                    int projToShoot = invItem.shoot;
                    float projSpeed = invItem.shootSpeed;
                    int projDamage = Player.GetWeaponDamage(invItem);
                    float projKnockback = Player.GetWeaponKnockback(invItem, invItem.knockBack);
                    int usedAmmoItemId = 0;

                    // 处理弹药消耗（弓/枪需要弹药）
                    if (invItem.useAmmo > 0)
                    {
                        bool canShoot = Player.PickAmmo(invItem, out int ammoProj, out float ammoSpeed,
                            out int ammoDamage, out float ammoKnockback, out usedAmmoItemId,
                            dontConsume: !config.InventoryFireConsumeAmmo); // 根据配置决定是否消耗
                        if (!canShoot) continue;

                        projToShoot = ammoProj;
                        projSpeed += ammoSpeed;
                        projDamage += ammoDamage;
                        projKnockback += ammoKnockback;
                    }

                    if (projSpeed <= 0f) projSpeed = 10f; // 保底速度

                    Vector2 position = Player.Center;
                    Vector2 target = Main.MouseWorld;
                    Vector2 newVelocity = (target - position).SafeNormalize(Vector2.Zero) * projSpeed;

                    var invSource = Player.GetSource_ItemUse_WithPotentialAmmo(invItem, usedAmmoItemId);

                    // 发射射弹，并记录其索引
                    int projIndex = Projectile.NewProjectile(invSource, position, newVelocity,
                        projToShoot, projDamage, projKnockback, Player.whoAmI);

                    // 获取刚生成的射弹实例，打上"来自背包"的标记
                    Projectile newProj = Main.projectile[projIndex];
                    if (newProj.TryGetGlobalProjectile<InventoryProjectile>(out var invProj))
                    {
                        invProj.isFromInventory = true;
                    }
                }
            }
        }
    }
}