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

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight)
        {
            if (font == null) return;

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