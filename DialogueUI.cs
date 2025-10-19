using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarshroudHollows.Systems.Housing;

namespace StarshroudHollows.UI
{
    public class DialogueUI
    {
        private string currentDialogue = "";
        private string currentNPCName = "";
        private float displayTimer = 0f;
        private const float DISPLAY_DURATION = 5f; // Show for 5 seconds
        private bool isVisible = false;

        public bool IsVisible => isVisible;

        public void ShowDialogue(string npcName, string dialogue)
        {
            currentNPCName = npcName;
            currentDialogue = dialogue;
            displayTimer = DISPLAY_DURATION;
            isVisible = true;
        }

        public void Update(float deltaTime)
        {
            if (isVisible)
            {
                displayTimer -= deltaTime;
                if (displayTimer <= 0)
                {
                    isVisible = false;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight)
        {
            if (!isVisible) return;

            // Draw dialogue box at bottom center of screen
            int boxWidth = 800;
            int boxHeight = 120;
            int boxX = (screenWidth - boxWidth) / 2;
            int boxY = screenHeight - boxHeight - 100; // 100 pixels from bottom

            // Draw semi-transparent background
            Rectangle bgRect = new Rectangle(boxX, boxY, boxWidth, boxHeight);
            spriteBatch.Draw(pixelTexture, bgRect, Color.Black * 0.85f);

            // Draw border
            DrawBorder(spriteBatch, pixelTexture, bgRect, 3, Color.White);

            // Draw NPC name
            Vector2 namePos = new Vector2(boxX + 20, boxY + 10);
            spriteBatch.DrawString(font, currentNPCName, namePos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, currentNPCName, namePos, Color.Cyan);

            // Draw dialogue text (word wrap)
            string wrappedText = WrapText(currentDialogue, font, boxWidth - 40);
            Vector2 textPos = new Vector2(boxX + 20, boxY + 40);
            spriteBatch.DrawString(font, wrappedText, textPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, wrappedText, textPos, Color.White);

            // Draw "Right-click to continue" hint
            string hint = "(Right-click to continue)";
            Vector2 hintSize = font.MeasureString(hint);
            Vector2 hintPos = new Vector2(boxX + boxWidth - hintSize.X - 20, boxY + boxHeight - hintSize.Y - 10);
            spriteBatch.DrawString(font, hint, hintPos, Color.Gray);
        }

        private string WrapText(string text, SpriteFont font, float maxWidth)
        {
            string[] words = text.Split(' ');
            string wrappedText = "";
            string currentLine = "";

            foreach (string word in words)
            {
                string testLine = currentLine + word + " ";
                Vector2 size = font.MeasureString(testLine);

                if (size.X > maxWidth && currentLine != "")
                {
                    wrappedText += currentLine + "\n";
                    currentLine = word + " ";
                }
                else
                {
                    currentLine = testLine;
                }
            }

            wrappedText += currentLine;
            return wrappedText;
        }

        private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle rect, int thickness, Color color)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}
