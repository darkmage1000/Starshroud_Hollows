using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Claude4_5Terraria
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

        public Rectangle GetVisibleArea(int tileSize)
        {
            // Calculate world bounds that are visible
            // Camera position is the CENTER of the view
            float halfScreenWidth = (viewport.Width / Zoom) * 0.5f;
            float halfScreenHeight = (viewport.Height / Zoom) * 0.5f;

            // Calculate visible world coordinates
            int leftTile = (int)((Position.X - halfScreenWidth) / tileSize) - 2;
            int topTile = (int)((Position.Y - halfScreenHeight) / tileSize) - 2;
            int rightTile = (int)((Position.X + halfScreenWidth) / tileSize) + 2;
            int bottomTile = (int)((Position.Y + halfScreenHeight) / tileSize) + 2;

            int visibleWidth = rightTile - leftTile;
            int visibleHeight = bottomTile - topTile;

            return new Rectangle(leftTile, topTile, visibleWidth, visibleHeight);
        }
    }
}