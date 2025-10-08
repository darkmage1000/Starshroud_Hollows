using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Claude4_5Terraria.UI
{
    public class HUD
    {
        private bool showBlockOutlines;

        public HUD()
        {
            showBlockOutlines = false;
        }

        public void ToggleBlockOutlines()
        {
            showBlockOutlines = !showBlockOutlines;
        }

        public bool ShowBlockOutlines => showBlockOutlines;

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight, Vector2 playerPosition)
        {
            if (font == null) return;

            // Draw coordinates in top-left
            int tileX = (int)(playerPosition.X / 32); // TILE_SIZE = 32
            int tileY = (int)(playerPosition.Y / 32);
            string coordText = $"X: {tileX}, Y: {tileY}";
            Vector2 coordSize = font.MeasureString(coordText);
            Vector2 coordPosition = new Vector2(10, 10);
            
            // Background for coordinates
            Rectangle coordBgRect = new Rectangle(
                (int)coordPosition.X - 5,
                (int)coordPosition.Y - 5,
                (int)coordSize.X + 10,
                (int)coordSize.Y + 10
            );
            spriteBatch.Draw(pixelTexture, coordBgRect, Color.Black * 0.7f);
            spriteBatch.DrawString(font, coordText, coordPosition, Color.Yellow);

            // Draw block outline hint in top-right
            string hintText = showBlockOutlines ? "T: Hide Outlines" : "T: Show Outlines";
            Vector2 textSize = font.MeasureString(hintText);

            Vector2 textPosition = new Vector2(screenWidth - textSize.X - 20, 10);

            // Background
            Rectangle bgRect = new Rectangle(
                (int)textPosition.X - 10,
                (int)textPosition.Y - 5,
                (int)textSize.X + 20,
                (int)textSize.Y + 10
            );
            spriteBatch.Draw(pixelTexture, bgRect, Color.Black * 0.6f);

            // Text
            spriteBatch.DrawString(font, hintText, textPosition, Color.White);
        }
    }
}