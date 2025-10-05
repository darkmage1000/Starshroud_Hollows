using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Claude4_5Terraria.World;

namespace Claude4_5Terraria.Player
{
    public class Player
    {
        public Vector2 Position { get; set; }  // Make sure this is public
        public Vector2 Velocity { get; set; }

        public const int PLAYER_WIDTH = 24;
        public const int PLAYER_HEIGHT = 48;

        private const float GRAVITY = 0.5f;
        private const float MAX_FALL_SPEED = 15f;
        private const float MOVE_SPEED = 4f;
        private const float JUMP_FORCE = -12f;
        private const float FAST_FALL_MULTIPLIER = 2.5f;

        private bool isOnGround;
        private World.World world;

        public Player(World.World world, Vector2 startPosition)
        {
            this.world = world;
            Position = startPosition;
            Velocity = Vector2.Zero;
            isOnGround = false;
        }

        public void Update(GameTime gameTime)
        {
            KeyboardState keyState = Keyboard.GetState();

            float horizontalInput = 0;
            if (keyState.IsKeyDown(Keys.A))
            {
                horizontalInput = -1;
            }
            if (keyState.IsKeyDown(Keys.D))
            {
                horizontalInput = 1;
            }

            Velocity = new Vector2(horizontalInput * MOVE_SPEED, Velocity.Y);

            if (keyState.IsKeyDown(Keys.W) && isOnGround)
            {
                Velocity = new Vector2(Velocity.X, JUMP_FORCE);
                isOnGround = false;
            }

            float gravityMultiplier = 1f;
            if (keyState.IsKeyDown(Keys.S))
            {
                gravityMultiplier = FAST_FALL_MULTIPLIER;
            }

            if (!isOnGround)
            {
                Velocity = new Vector2(Velocity.X, Velocity.Y + (GRAVITY * gravityMultiplier));

                if (Velocity.Y > MAX_FALL_SPEED)
                {
                    Velocity = new Vector2(Velocity.X, MAX_FALL_SPEED);
                }
            }

            ApplyPhysics();
        }

        private void ApplyPhysics()
        {
            Vector2 newPosition = new Vector2(Position.X + Velocity.X, Position.Y);
            if (!CheckCollision(newPosition))
            {
                Position = newPosition;
            }
            else
            {
                Velocity = new Vector2(0, Velocity.Y);
            }

            newPosition = new Vector2(Position.X, Position.Y + Velocity.Y);
            if (!CheckCollision(newPosition))
            {
                Position = newPosition;
                isOnGround = false;
            }
            else
            {
                if (Velocity.Y > 0)
                {
                    isOnGround = true;
                }
                Velocity = new Vector2(Velocity.X, 0);
            }
        }

        private bool CheckCollision(Vector2 position)
        {
            int tileSize = World.World.TILE_SIZE;

            Vector2[] checkPoints = new Vector2[]
            {
                new Vector2(position.X, position.Y),
                new Vector2(position.X + PLAYER_WIDTH, position.Y),
                new Vector2(position.X, position.Y + PLAYER_HEIGHT),
                new Vector2(position.X + PLAYER_WIDTH, position.Y + PLAYER_HEIGHT),
                new Vector2(position.X + PLAYER_WIDTH / 2, position.Y),
                new Vector2(position.X + PLAYER_WIDTH / 2, position.Y + PLAYER_HEIGHT),
                new Vector2(position.X, position.Y + PLAYER_HEIGHT / 2),
                new Vector2(position.X + PLAYER_WIDTH, position.Y + PLAYER_HEIGHT / 2)
            };

            foreach (Vector2 point in checkPoints)
            {
                int tileX = (int)(point.X / tileSize);
                int tileY = (int)(point.Y / tileSize);

                if (world.IsSolidAtPosition(tileX, tileY))
                {
                    return true;
                }
            }

            return false;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            Rectangle playerRect = new Rectangle(
                (int)Position.X,
                (int)Position.Y,
                PLAYER_WIDTH,
                PLAYER_HEIGHT
            );

            spriteBatch.Draw(pixelTexture, playerRect, Color.Yellow);
        }

        public Vector2 GetCenterPosition()
        {
            return new Vector2(Position.X + PLAYER_WIDTH / 2, Position.Y + PLAYER_HEIGHT / 2);
        }
    }
}