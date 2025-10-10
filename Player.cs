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
        public Vector2 SpawnPosition { get; set; }  // Spawn point (can be bed or world spawn)
        public Vector2 BedSpawnPosition { get; set; }  // NEW: Bed spawn location
        public bool HasBedSpawn { get; set; }  // NEW: Whether player has set a bed spawn

        // Health system
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        private float lavaDamageTimer = 0f;
        private const float LAVA_DAMAGE_INTERVAL = 0.5f; // Damage every 0.5 seconds
        private const float LAVA_DAMAGE_AMOUNT = 10f; // 10 damage per tick
        
        // NEW: Swimming and breathing
        public float AirBubbles { get; private set; }
        public float MaxAirBubbles { get; private set; }
        private const float AIR_DEPLETE_RATE = 10f; // Air per second underwater
        private const float AIR_RESTORE_RATE = 50f; // Air per second in air
        private const float DROWN_DAMAGE = 5f; // Damage per second when no air

        public const int PLAYER_WIDTH = 30;   // Slightly less than 1 block for smoother movement
        public const int PLAYER_HEIGHT = 62;  // Slightly less than 2 blocks to fit in 64px spaces

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
            SpawnPosition = startPosition;  // World spawn
            BedSpawnPosition = Vector2.Zero;  // No bed spawn yet
            HasBedSpawn = false;
            Velocity = Vector2.Zero;
            isOnGround = false;
            
            // Initialize health
            MaxHealth = 100f;
            Health = MaxHealth;
            
            // Initialize air
            MaxAirBubbles = 100f;
            AirBubbles = MaxAirBubbles;
        }

        // NEW: Set bed as spawn point
        public void SetBedSpawn(Vector2 bedPosition)
        {
            BedSpawnPosition = bedPosition;
            HasBedSpawn = true;
            Systems.Logger.Log($"[PLAYER] Bed spawn set at: {bedPosition}");
        }

        // NEW: Clear bed spawn (when bed destroyed)
        public void ClearBedSpawn()
        {
            HasBedSpawn = false;
            Systems.Logger.Log("[PLAYER] Bed spawn cleared");
        }

        // Teleport to spawn (bed spawn if available, otherwise world spawn)
        public void TeleportToSpawn()
        {
            if (HasBedSpawn)
            {
                Position = BedSpawnPosition;
                Systems.Logger.Log($"[PLAYER] Teleported to bed spawn: {BedSpawnPosition}");
            }
            else
            {
                Position = SpawnPosition;
                Systems.Logger.Log($"[PLAYER] Teleported to world spawn: {SpawnPosition}");
            }
            Velocity = Vector2.Zero;
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
            
            // Check for lava damage
            CheckLavaDamage(deltaTime);
            
            // NEW: Handle swimming and drowning
            HandleSwimming(deltaTime);
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

        private void CheckLavaDamage(float deltaTime)
        {
            // Check if player is touching lava
            bool inLava = IsInLava();
            
            if (inLava)
            {
                lavaDamageTimer += deltaTime;
                
                if (lavaDamageTimer >= LAVA_DAMAGE_INTERVAL)
                {
                    TakeDamage(LAVA_DAMAGE_AMOUNT);
                    lavaDamageTimer = 0f;
                    Systems.Logger.Log($"[PLAYER] Lava damage! Health: {Health}/{MaxHealth}");
                }
            }
            else
            {
                lavaDamageTimer = 0f;
            }
        }

        private bool IsInLava()
        {
            int tileSize = World.World.TILE_SIZE;
            
            // Check multiple points on the player
            Vector2[] checkPoints = new Vector2[]
            {
                new Vector2(Position.X + 5, Position.Y + 5),
                new Vector2(Position.X + PLAYER_WIDTH - 5, Position.Y + 5),
                new Vector2(Position.X + 5, Position.Y + PLAYER_HEIGHT - 5),
                new Vector2(Position.X + PLAYER_WIDTH - 5, Position.Y + PLAYER_HEIGHT - 5),
                new Vector2(Position.X + PLAYER_WIDTH / 2, Position.Y + PLAYER_HEIGHT / 2)
            };
            
            foreach (Vector2 point in checkPoints)
            {
                int tileX = (int)(point.X / tileSize);
                int tileY = (int)(point.Y / tileSize);
                
                var tile = world.GetTile(tileX, tileY);
                if (tile != null && tile.IsActive && tile.Type == TileType.Lava)
                {
                    return true;
                }
            }
            
            return false;
        }

        public void TakeDamage(float amount)
        {
            Health -= amount;
            if (Health < 0) Health = 0;
            
            // If health reaches 0, respawn at spawn point
            if (Health <= 0)
            {
                Systems.Logger.Log("[PLAYER] Player died! Respawning...");
                TeleportToSpawn();
                Health = MaxHealth;
            }
        }

        public void Heal(float amount)
        {
            Health += amount;
            if (Health > MaxHealth) Health = MaxHealth;
        }
        
        // NEW: Handle swimming, drowning, and slow fall in water
        private void HandleSwimming(float deltaTime)
        {
            bool inWater = IsInWater();
            bool inLava = IsInLava();
            
            // Check if player's HEAD is underwater (top 1/3 of player)
            bool headUnderwater = IsHeadUnderwater();
            
            // In water: deplete air only if HEAD is underwater, slow fall
            if (inWater)
            {
                // Only deplete air if head is underwater
                if (headUnderwater)
                {
                    AirBubbles -= AIR_DEPLETE_RATE * deltaTime;
                    
                    if (AirBubbles <= 0)
                    {
                        AirBubbles = 0;
                        // Drown damage when out of air
                        TakeDamage(DROWN_DAMAGE * deltaTime);
                    }
                }
                else
                {
                    // Head above water - restore air
                    AirBubbles += AIR_RESTORE_RATE * deltaTime;
                    if (AirBubbles > MaxAirBubbles) AirBubbles = MaxAirBubbles;
                }
                
                // Slow fall in water
                if (Velocity.Y > 3f)
                {
                    Velocity = new Vector2(Velocity.X, 3f);
                }
            }
            // In lava: slow fall (no air bubbles, just burn)
            else if (inLava)
            {
                // Slow fall in lava
                if (Velocity.Y > 3f)
                {
                    Velocity = new Vector2(Velocity.X, 3f);
                }
                
                // Restore air when in lava (not underwater)
                AirBubbles += AIR_RESTORE_RATE * deltaTime;
                if (AirBubbles > MaxAirBubbles) AirBubbles = MaxAirBubbles;
            }
            // Not in liquid: restore air
            else
            {
                AirBubbles += AIR_RESTORE_RATE * deltaTime;
                if (AirBubbles > MaxAirBubbles) AirBubbles = MaxAirBubbles;
            }
        }
        
        private bool IsHeadUnderwater()
        {
            int tileSize = World.World.TILE_SIZE;
            
            // Check if the top 1/3 of the player (head area) is in water
            int headHeight = PLAYER_HEIGHT / 3;
            
            Vector2[] checkPoints = new Vector2[]
            {
                new Vector2(Position.X + 5, Position.Y + 5), // Top-left of head
                new Vector2(Position.X + PLAYER_WIDTH - 5, Position.Y + 5), // Top-right of head
                new Vector2(Position.X + PLAYER_WIDTH / 2, Position.Y + headHeight / 2) // Center of head
            };
            
            foreach (Vector2 point in checkPoints)
            {
                int tileX = (int)(point.X / tileSize);
                int tileY = (int)(point.Y / tileSize);
                
                var tile = world.GetTile(tileX, tileY);
                if (tile != null && tile.IsActive && tile.Type == TileType.Water)
                {
                    return true; // Head is underwater
                }
            }
            
            return false; // Head is above water
        }
        
        private bool IsInWater()
        {
            int tileSize = World.World.TILE_SIZE;
            
            // Check multiple points on the player
            Vector2[] checkPoints = new Vector2[]
            {
                new Vector2(Position.X + 5, Position.Y + 5),
                new Vector2(Position.X + PLAYER_WIDTH - 5, Position.Y + 5),
                new Vector2(Position.X + 5, Position.Y + PLAYER_HEIGHT - 5),
                new Vector2(Position.X + PLAYER_WIDTH - 5, Position.Y + PLAYER_HEIGHT - 5),
                new Vector2(Position.X + PLAYER_WIDTH / 2, Position.Y + PLAYER_HEIGHT / 2)
            };
            
            foreach (Vector2 point in checkPoints)
            {
                int tileX = (int)(point.X / tileSize);
                int tileY = (int)(point.Y / tileSize);
                
                var tile = world.GetTile(tileX, tileY);
                if (tile != null && tile.IsActive && tile.Type == TileType.Water)
                {
                    return true;
                }
            }
            
            return false;
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

            // FIXED: Use tighter margins so player fits exactly in 2-block (64px) spaces
            // Player is 32x64, tiles are 32x32, so we need minimal padding
            Vector2[] checkPoints = new Vector2[]
            {
                new Vector2(position.X + 1, position.Y + 0),  // Top-left (almost no margin)
                new Vector2(position.X + PLAYER_WIDTH - 1, position.Y + 0),  // Top-right (almost no margin)
                new Vector2(position.X + 1, position.Y + PLAYER_HEIGHT - 1),  // Bottom-left
                new Vector2(position.X + PLAYER_WIDTH - 1, position.Y + PLAYER_HEIGHT - 1),  // Bottom-right
                new Vector2(position.X + PLAYER_WIDTH / 2, position.Y + 0),  // Top-center (almost no margin)
                new Vector2(position.X + PLAYER_WIDTH / 2, position.Y + PLAYER_HEIGHT - 1),  // Bottom-center
                new Vector2(position.X + 1, position.Y + PLAYER_HEIGHT / 2),  // Left-center
                new Vector2(position.X + PLAYER_WIDTH - 1, position.Y + PLAYER_HEIGHT / 2)  // Right-center
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