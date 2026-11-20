using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LLL.Items;

namespace LLL
{
    public class PrisonPreviewSystem : ModSystem
    {
        private const int PrisonWidth = 6;
        private const int PrisonHeight = 10;

        // 绘制用资源
        internal static Texture2D PixelTexture;
        internal static SpriteBatch IndependentSpriteBatch;

        public override void Unload()
        {
            // 安全释放纹理
            if (PixelTexture != null)
            {
                var tex = PixelTexture;
                PixelTexture = null;
                Main.QueueMainThreadAction(() => tex?.Dispose());
            }

            // 安全释放画笔
            if (IndependentSpriteBatch != null)
            {
                var sb = IndependentSpriteBatch;
                IndependentSpriteBatch = null;
                Main.QueueMainThreadAction(() => sb?.Dispose());
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int cursorIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Cursor"));
            if (cursorIndex < 0)
                cursorIndex = layers.Count;

            var previewLayer = new LegacyGameInterfaceLayer(
                "LLL: PrisonPreview",
                delegate
                {
                    if (!Main.dedServ)
                        DrawPrisonPreview(Main.spriteBatch);
                    return true;
                },
                InterfaceScaleType.Game);

            layers.Insert(cursorIndex, previewLayer);
        }

        private void DrawPrisonPreview(SpriteBatch sb)
        {
            Player player = Main.LocalPlayer;
            if (player?.HeldItem == null) return;
            if (player.HeldItem.type != ModContent.ItemType<NPCPrison>()) return;

            Vector2 mouse = Main.MouseWorld;
            int anchorX = (int)(mouse.X / 16f);
            int anchorY = (int)(mouse.Y / 16f);

            if (anchorX < 5 || anchorX > Main.maxTilesX - 6 ||
                anchorY < 10 || anchorY > Main.maxTilesY - 1)
                return;

            int left = anchorX;
            int right = anchorX + PrisonWidth - 1;
            int top = anchorY - (PrisonHeight - 1);
            int bottom = anchorY;

            EnsureResources();
            if (PixelTexture == null) return;

            // 四个角
            DrawTileHighlight(sb, left, bottom);
            DrawTileHighlight(sb, left, top);
            DrawTileHighlight(sb, right, top);
            DrawTileHighlight(sb, right, bottom);
        }

        private void DrawTileHighlight(SpriteBatch sb, int tileX, int tileY)
        {
            Vector2 screenPos = new Vector2(tileX * 16, tileY * 16) - Main.screenPosition;
            Rectangle rect = new Rectangle((int)screenPos.X, (int)screenPos.Y, 16, 16);

            Texture2D pixel = PixelTexture;

            // 半透明黄底
            sb.Draw(pixel, rect, null, Color.Yellow * 0.6f);

            // 白色边框（2像素）
            Color border = Color.White * 0.9f;
            const int thick = 2;
            sb.Draw(pixel, new Rectangle(rect.Left, rect.Top, rect.Width, thick), null, border);
            sb.Draw(pixel, new Rectangle(rect.Left, rect.Bottom - thick, rect.Width, thick), null, border);
            sb.Draw(pixel, new Rectangle(rect.Left, rect.Top, thick, rect.Height), null, border);
            sb.Draw(pixel, new Rectangle(rect.Right - thick, rect.Top, thick, rect.Height), null, border);
        }

        private void EnsureResources()
        {
            if (PixelTexture == null || PixelTexture.IsDisposed)
            {
                if (Main.graphics.GraphicsDevice != null)
                {
                    PixelTexture = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                    PixelTexture.SetData(new[] { Color.White });
                }
            }

            if (IndependentSpriteBatch == null || IndependentSpriteBatch.IsDisposed)
            {
                if (Main.graphics.GraphicsDevice != null)
                    IndependentSpriteBatch = new SpriteBatch(Main.graphics.GraphicsDevice);
            }
        }
    }
}