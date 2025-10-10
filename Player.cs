using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Claude4_5Terraria.World;
using Claude4_5Terraria.Enums; // Added for ItemType

namespace Claude4_5Terraria.Player
{
    public class Player
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Vector2 SpawnPosition { get; set; }  // NEW: Remember spawn point

        public const int PLAYER_WIDTH = 32;   // 1 block wide
        public const int PLAYER_HEIGHT = 64;  // 2 blocks tall (32 * 2)

        private const float GRAVITY = 0.5f;
        private const float MAX_FALL_SPEED = 15f;
        private const float MOVE_SPEED = 4f;
        private const float JUMP_FORCE = -12f;
        private const float FAST_FALL_MULTIPLIER = 2.5f;

        private bool isOnGround;
        private World.World world;

        private Texture2D spriteSheet;
        private bool facingRight = true;
        private bool isMoving = false;

        private int currentFrame = 0;
        private float animationTimer = 0f;
        private const float FRAME_TIME = 0.12f;

        // Try 128x128 - many character creators use this size
        private const int FRAME_WIDTH = 128;
        private const int FRAME_HEIGHT = 128;
        private const int FRAMES_PER_ROW = 6;  // 832 / 128 = 6.5, so 6 full frames

        // With 26 frames per row, the animations are laid out differently
        // Let's use the first row for idle animation
        private const int IDLE_DOWN_ROW = 0;
        private const int IDLE_LEFT_ROW = 1;
        private const int IDLE_RIGHT_ROW = 2;
        private const int IDLE_UP_ROW = 3;

        private int currentAnimationRow = IDLE_DOWN_ROW;
        private const float SPRITE_SCALE = 2.0f;

        public Player(World.World world, Vector2 startPosition)
        {
            this.world = world;
            Position = startPosition;
            SpawnPosition = startPosition;  // NEW: Set spawn position
            Velocity = Vector2.Zero;
            isOnGround = false;
        }

        // NEW: Teleport to spawn
        public void TeleportToSpawn()
        {
            Position = SpawnPosition;
            Velocity = Vector2.Zero;
            Systems.Logger.Log($"[PLAYER] Teleported to spawn: {SpawnPosition}");
        }

        public void LoadContent(Texture2D playerSpriteSheet)
        {
            Systems.Logger.Log("===========================================");
            Systems.Logger.Log("PLAYER LOADCONTENT CALLED");

            spriteSheet = playerSpriteSheet;

            if (spriteSheet != null)
            {
                Systems.Logger.Log($"Spritesheet loaded successfully!");
                Systems.Logger.Log($"Full dimensions: {spriteSheet.Width} x {spriteSheet.Height} pixels");
                Systems.Logger.Log($"Frame size we're using: {FRAME_WIDTH} x {FRAME_HEIGHT}");

                int framesWide = spriteSheet.Width / FRAME_WIDTH;
                int framesTall = spriteSheet.Height / FRAME_HEIGHT;
                Systems.Logger.Log($"Frames that fit: {framesWide} wide x {framesTall} tall = {framesWide * framesTall} total");

                Systems.Logger.Log($"Frame [0,0] will grab: X={0} Y={0} Width={FRAME_WIDTH} Height={FRAME_HEIGHT}");
                Systems.Logger.Log($"This is pixels from top-left corner: (0,0) to ({FRAME_WIDTH},{FRAME_HEIGHT})");
            }
            else
            {
                Systems.Logger.Log("ERROR: Spritesheet is NULL!");
            }

            Systems.Logger.Log("===========================================");
        }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keyState = Keyboard.GetState();

            float horizontalInput = 0;
            isMoving = false;

            if (keyState.IsKeyDown(Keys.A))
            {
                horizontalInput = -1;
                facingRight = false;
                isMoving = true;
            }
            if (keyState.IsKeyDown(Keys.D))
            {
                horizontalInput = 1;
                facingRight = true;
                isMoving = true;
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
            UpdateAnimation(deltaTime);
        }

        private void UpdateAnimation(float deltaTime)
        {
            animationTimer += deltaTime;

            if (animationTimer >= FRAME_TIME)
            {
                animationTimer -= FRAME_TIME;
                currentFrame++;

                // With 13 frames per row, let's use first 6 for animation
                if (currentFrame >= 6)
                {
                    currentFrame = 0;
                }
            }

            currentAnimationRow = IDLE_DOWN_ROW;
        }

        private void ApplyPhysics()
        {
            // Horizontal movement
            Vector2 newPosition = new Vector2(Position.X + Velocity.X, Position.Y);
            if (!CheckCollision(newPosition))
            {
                Position = newPosition;
            }
            else
            {
                // Only stop horizontal velocity if there's actually a collision
                Velocity = new Vector2(0, Velocity.Y);
            }

            // Vertical movement
            newPosition = new Vector2(Position.X, Position.Y + Velocity.Y);
            if (!CheckCollision(newPosition))
            {
                Position = newPosition;
                isOnGround = false;
            }
            else
            {
                // Land on ground
                if (Velocity.Y > 0)
                {
                    isOnGround = true;
                }
                Velocity = new Vector2(Velocity.X, 0);
            }

            // Always check if we're actually on ground (even when stationary)
            Vector2 groundCheckPos = new Vector2(Position.X, Position.Y + 1);
            if (CheckCollision(groundCheckPos))
            {
                isOnGround = true;
            }
        }

        private bool CheckCollision(Vector2 position)
        {
            int tileSize = World.World.TILE_SIZE;

            // Check points with small margin from edges to allow 2-block passage
            // Reduce top margin to allow fitting through 2-block spaces
            Vector2[] checkPoints = new Vector2[]
            {
                new Vector2(position.X + 2, position.Y + 1),  // Top-left (minimal margin)
                new Vector2(position.X + PLAYER_WIDTH - 2, position.Y + 1),  // Top-right (minimal margin)
                new Vector2(position.X + 2, position.Y + PLAYER_HEIGHT - 1),  // Bottom-left
                new Vector2(position.X + PLAYER_WIDTH - 2, position.Y + PLAYER_HEIGHT - 1),  // Bottom-right
                new Vector2(position.X + PLAYER_WIDTH / 2, position.Y + 1),  // Top-center (minimal margin)
                new Vector2(position.X + PLAYER_WIDTH / 2, position.Y + PLAYER_HEIGHT - 1),  // Bottom-center
                new Vector2(position.X + 2, position.Y + PLAYER_HEIGHT / 2),  // Left-center
                new Vector2(position.X + PLAYER_WIDTH - 2, position.Y + PLAYER_HEIGHT / 2)  // Right-center
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

        private bool hasLoggedDraw = false;

        // UPDATED: Logic to correctly read the 3-frame spritesheet
        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, ItemType heldItemType, Texture2D itemSpriteSheet, int animationFrame)
        {
            // --- 1. Draw Player (Currently a yellow box) ---
            Rectangle playerRect = new Rectangle(
                (int)Position.X,
                (int)Position.Y,
                PLAYER_WIDTH,
                PLAYER_HEIGHT
            );
            spriteBatch.Draw(pixelTexture, playerRect, Color.Yellow);

            // --- 2. Draw Held Item (Pickaxe) ---
            if (heldItemType == ItemType.RunicPickaxe && itemSpriteSheet != null)
            {
                // CRITICAL FIX: Calculate frame dimensions based on 3 frames in a 2x2 grid layout.
                // Assuming the spritesheet is roughly 2 frames wide and 2 frames tall (4 total frame slots)
                int cols = 2; // Assuming the pickaxes take up 2 horizontal slots
                int rows = 2; // Assuming the pickaxes take up 2 vertical slots
                int itemFrameWidth = itemSpriteSheet.Width / cols;
                int itemFrameHeight = itemSpriteSheet.Height / rows;

                // Map the 0, 1, 2 animationFrame index to the correct pixel coordinates:
                // Frame 0: (0, 0)
                // Frame 1: (itemFrameWidth, 0)
                // Frame 2: (0, itemFrameHeight)

                int sourceX = 0;
                int sourceY = 0;

                if (animationFrame == 0)
                {
                    sourceX = 0;
                    sourceY = 0;
                }
                else if (animationFrame == 1)
                {
                    sourceX = itemFrameWidth;
                    sourceY = 0;
                }
                else if (animationFrame == 2)
                {
                    sourceX = 0;
                    sourceY = itemFrameHeight;
                }

                Rectangle sourceRect = new Rectangle(
                    sourceX,
                    sourceY,
                    itemFrameWidth,
                    itemFrameHeight
                );

                // Calculate the position for the pickaxe (relative to the player's hand/shoulder)
                Vector2 origin = new Vector2(itemFrameWidth / 2f, itemFrameHeight / 2f);

                // Positioned near the right shoulder/hand, scaled down for pickaxe size
                Vector2 drawPosition = new Vector2(
                    Position.X + (PLAYER_WIDTH * 0.75f),
                    Position.Y + (PLAYER_HEIGHT * 0.35f)
                );

                float rotation = 0f;
                // Adjust rotation to simulate swinging
                if (animationFrame == 1) // Mid-swing frame
                {
                    rotation = facingRight ? MathHelper.PiOver4 * 0.5f : MathHelper.PiOver4 * 1.5f;
                }
                else if (animationFrame == 2) // End-swing frame / contact
                {
                    rotation = facingRight ? MathHelper.PiOver4 * 1.5f : -MathHelper.PiOver4 * 0.5f; // Slight adjustment for left swing visual
                }
                else // Idle/Start frame (Frame 0)
                {
                    rotation = facingRight ? -MathHelper.PiOver4 * 0.25f : MathHelper.PiOver4 * 0.25f;
                }

                SpriteEffects flip = facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                // Draw the pickaxe
                spriteBatch.Draw(
                    itemSpriteSheet,
                    drawPosition,
                    sourceRect,
                    Color.White,
                    rotation,
                    origin,
                    0.5f, // Scale down to a reasonable size (e.g., 50%)
                    flip,
                    0f
                );
            }
        }

        public Vector2 GetCenterPosition()
        {
            return new Vector2(
                Position.X + PLAYER_WIDTH / 2,
                Position.Y + PLAYER_HEIGHT / 2
            );
        }
    }
}