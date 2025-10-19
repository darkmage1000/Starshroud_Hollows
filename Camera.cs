using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StarshroudHollows
{
    public class Camera
    {
        public Vector2 Position { get; set; }
        public float Zoom { get; set; }

        private Viewport viewport;

        public int ViewWidth => viewport.Width;
        public int ViewHeight => viewport.Height;

        public Camera(Viewport viewport)
        {
            this.viewport = viewport;
            Position = Vector2.Zero;
            Zoom = 1.0f;
        }

        public Matrix GetTransformMatrix()
        {
            return Matrix.CreateTranslation(new Vector3(-Position.X, -Position.Y, 0)) *
                   Matrix.CreateScale(Zoom) *
                   Matrix.CreateTranslation(new Vector3(viewport.Width * 0.5f, viewport.Height * 0.5f, 0));
        }

        public void Follow(Vector2 targetPosition, float smoothing = 0.1f)
        {
            Position = Vector2.Lerp(Position, targetPosition, smoothing);
        }

        // NEW: Convert screen coordinates to world coordinates
        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            Matrix inverseTransform = Matrix.Invert(GetTransformMatrix());
            return Vector2.Transform(screenPosition, inverseTransform);
        }

        // ✅ FIXED: Returns world coordinates in PIXELS, not tiles
        public Rectangle GetVisibleArea(int tileSize)
        {
            // Camera position is the CENTER of the view in world pixels
            float halfScreenWidth = (viewport.Width / Zoom) * 0.5f;
            float halfScreenHeight = (viewport.Height / Zoom) * 0.5f;

            // Calculate visible world coordinates IN PIXELS with buffer
            int bufferPixels = tileSize * 2;  // 2-tile buffer on each side

            int left = (int)(Position.X - halfScreenWidth) - bufferPixels;
            int top = (int)(Position.Y - halfScreenHeight) - bufferPixels;
            int right = (int)(Position.X + halfScreenWidth) + bufferPixels;
            int bottom = (int)(Position.Y + halfScreenHeight) + bufferPixels;

            int width = right - left;
            int height = bottom - top;

            // ✅ Return rectangle in WORLD PIXEL coordinates
            return new Rectangle(left, top, width, height);
        }
    }
}