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

        // UPDATED: Added specific logic for drawing the Torch
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

            // --- 2. Draw Held Item (Tool) ---
            if (heldItemType != ItemType.None && itemSpriteSheet != null)
            {
                int totalFrames = 1; // Default to single frame icon
                int itemFrameWidth = itemSpriteSheet.Width;
                int itemFrameHeight = itemSpriteSheet.Height;
                int currentFrame = 0;
                float rotation = 0f;
                float scale = 0.5f;
                SpriteEffects flip = facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                // === A) Special Case: Runic Pickaxe Animation ===
                if (heldItemType == ItemType.RunicPickaxe)
                {
                    // Based on your vertical 3-frame spritesheet (2 columns x 2 rows)
                    totalFrames = 3;
                    itemFrameWidth = itemSpriteSheet.Width / 2;
                    itemFrameHeight = itemSpriteSheet.Height / 2;
                    currentFrame = animationFrame; // Use progress-based frame
                    scale = 0.5f; // Scale down for pickaxe size

                    // Rotation to simulate swinging
                    if (animationFrame == 1) // Mid-swing frame
                    {
                        rotation = facingRight ? MathHelper.PiOver4 * 0.5f : MathHelper.PiOver4 * 1.5f;
                    }
                    else if (animationFrame == 2) // End-swing frame / contact
                    {
                        rotation = facingRight ? MathHelper.PiOver4 * 1.5f : -MathHelper.PiOver4 * 0.5f;
                    }
                    else // Idle/Start frame (Frame 0)
                    {
                        rotation = facingRight ? -MathHelper.PiOver4 * 0.25f : MathHelper.PiOver4 * 0.25f;
                    }
                }
                // === B) Special Case: Torch (Held upright) ===
                else if (heldItemType == ItemType.Torch)
                {
                    // For static items, itemFrameWidth/Height remains the full texture size 
                    // unless your torch texture is also a sheet. Assuming single 32x32 texture is loaded.
                    scale = 0.6f;
                    rotation = 0f;
                    flip = SpriteEffects.None; // Torches shouldn't flip
                }
                // === C) General Item Drawing ===
                else
                {
                    // Default static item settings (using default scale=0.5f)
                    scale = 0.5f;
                    rotation = 0f;
                }

                // Map the frame index to the correct pixel coordinates (Handles Runic pickaxe and defaults others to (0,0))
                int sourceX = 0;
                int sourceY = 0;

                if (heldItemType == ItemType.RunicPickaxe)
                {
                    if (currentFrame == 1) sourceX = itemFrameWidth;
                    if (currentFrame == 2) sourceY = itemFrameHeight;
                }
                // For other items, sourceX and sourceY remain 0.

                Rectangle sourceRect = new Rectangle(
                    sourceX,
                    sourceY,
                    itemFrameWidth,
                    itemFrameHeight
                );

                // Calculate the position for the tool (relative to the player's hand/shoulder)
                Vector2 origin = new Vector2(itemFrameWidth / 2f, itemFrameHeight / 2f);

                // Base position is near the right shoulder/hand
                Vector2 drawPosition = new Vector2(
                    Position.X + (PLAYER_WIDTH * 0.75f),
                    Position.Y + (PLAYER_HEIGHT * 0.35f)
                );

                // Adjust vertical position slightly for a smaller torch
                if (heldItemType == ItemType.Torch)
                {
                    drawPosition.Y += 5; // Move torch down slightly into the hand
                }


                // Draw the tool
                spriteBatch.Draw(
                    itemSpriteSheet,
                    drawPosition,
                    sourceRect,
                    Color.White,
                    rotation,
                    origin,
                    scale,
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