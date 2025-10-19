using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StarshroudHollows.Enums;
using System;

namespace StarshroudHollows.Player

{
    public class Player
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Vector2 SpawnPosition { get; set; }
        public Vector2 BedSpawnPosition { get; private set; }
        public bool HasBedSpawn { get; private set; }
        private DateTime lastSpawnSetTime;
        private const float SPAWN_MESSAGE_DURATION = 5.0f;
        public float Scale { get; set; } = 1f;  // Uniform scale factor (or Vector2 for non-uniform)
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        private float lavaDamageTimer = 0f;
        private const float LAVA_DAMAGE_INTERVAL = 0.5f;
        private const float LAVA_DAMAGE_AMOUNT = 15f;  // INCREASED from 10f to make lava more threatening

        public float AirBubbles { get; private set; }
        public float MaxAirBubbles { get; private set; }
        private const float AIR_DEPLETE_RATE = 10f;
        private const float AIR_RESTORE_RATE = 50f;
        private const float DROWN_DAMAGE = 5f;

        public const int PLAYER_WIDTH = 30;
        public const int PLAYER_HEIGHT = 62;

        private const float GRAVITY = 0.5f;
        private const float MAX_FALL_SPEED = 15f;
        private const float MOVE_SPEED = 4f;
        private const float JUMP_FORCE = -12f;
        private const float FAST_FALL_MULTIPLIER = 2.5f;

        private bool debugMode = false;
        private const float DEBUG_FLY_SPEED = 15f;

        private bool isOnGround;
        private StarshroudHollows.World.World world; // Fully qualified World reference

        private Texture2D spriteSheet;
        private bool facingRight = true;
        private bool isMoving = false;

        private int currentFrame = 0;
        private float animationTimer = 0f;
        private const float FRAME_TIME = 0.12f;

        // Single frame sprite - scaled to fit player hitbox properly
        // Sprite is 250x450, we want 2 blocks tall (64 pixels)
        // Scale: 64 / 450 = 0.142
        private const float SPRITE_SCALE_X = 0.23f;  // Player width scale
        private const float SPRITE_SCALE_Y = 0.142f; // Player height scale (2 blocks tall)

        private float itemAnimTimer = 0f;
        private const float ITEM_ANIM_SPEED = 0.15f;
        private int itemAnimFrame = 0;

        private float heldItemAnimationTimer = 0f;
        private int heldItemCurrentFrame = 0;

        // PHASE 2: Health Potion System
        private float healthPotionCooldownTimer = 0f;
        private const float HEALTH_POTION_COOLDOWN = 30f;  // 30 seconds
        private const float HEALTH_POTION_HEAL_AMOUNT = 10f;
        private KeyboardState previousKeyState;


        public Player(StarshroudHollows.World.World world, Vector2 startPosition) // Fully qualified World parameter
        {
            this.world = world;
            Position = startPosition;
            SpawnPosition = startPosition;
            BedSpawnPosition = Vector2.Zero;
            HasBedSpawn = false;
            Velocity = Vector2.Zero;
            isOnGround = false;
            lastSpawnSetTime = DateTime.MinValue;

            MaxHealth = 100f;
            Health = MaxHealth;

            MaxAirBubbles = 100f;
            AirBubbles = MaxAirBubbles;

            // Initialize potion cooldown
            healthPotionCooldownTimer = 0f;
            previousKeyState = Keyboard.GetState();
        }

        public Vector2 GetBedSpawnPosition() => BedSpawnPosition;
        public DateTime GetLastSpawnSetTime() => lastSpawnSetTime;
        public float GetSpawnMessageDuration() => SPAWN_MESSAGE_DURATION;
        public float GetMaxHealth() => MaxHealth;
        public float GetMaxAir() => MaxAirBubbles;

        public void SetBedSpawn(Vector2 bedPosition)
        {
            BedSpawnPosition = bedPosition;
            HasBedSpawn = true;
            lastSpawnSetTime = DateTime.Now;
            StarshroudHollows.Systems.Logger.Log($"[PLAYER] Bed spawn set at: {bedPosition}");
        }

        public void ClearBedSpawn()
        {
            HasBedSpawn = false;
            BedSpawnPosition = Vector2.Zero;
            StarshroudHollows.Systems.Logger.Log("[PLAYER] Bed spawn cleared");
        }

        public void TeleportToSpawn()
        {
            if (HasBedSpawn)
            {
                Position = BedSpawnPosition;
                StarshroudHollows.Systems.Logger.Log($"[PLAYER] Teleported to bed spawn: {BedSpawnPosition}");
            }
            else
            {
                Position = SpawnPosition;
                StarshroudHollows.Systems.Logger.Log($"[PLAYER] Teleported to world spawn: {SpawnPosition}");
            }
            Velocity = Vector2.Zero;
            AirBubbles = MaxAirBubbles;
            lavaDamageTimer = 0f;
        }

        public void SetDebugMode(bool enabled)
        {
            debugMode = enabled;
            if (debugMode)
            {
                StarshroudHollows.Systems.Logger.Log("[PLAYER] Debug mode ENABLED: Fly + Noclip active");
            }
            else
            {
                StarshroudHollows.Systems.Logger.Log("[PLAYER] Debug mode DISABLED");
            }
        }

        public bool IsDebugModeActive() => debugMode;

        public void LoadContent(Texture2D playerSpriteSheet)
        {
            StarshroudHollows.Systems.Logger.Log("===========================================");
            StarshroudHollows.Systems.Logger.Log("PLAYER LOADCONTENT CALLED");

            spriteSheet = playerSpriteSheet;

            if (spriteSheet != null)
            {
                StarshroudHollows.Systems.Logger.Log($"Spritesheet loaded successfully!");
                StarshroudHollows.Systems.Logger.Log($"Full dimensions: {spriteSheet.Width} x {spriteSheet.Height} pixels");
                
                // Calculate scale for 2 blocks tall (64 pixels)
                float calculatedScaleY = 64f / spriteSheet.Height;
                StarshroudHollows.Systems.Logger.Log($"Calculated scale for 2 blocks: Y={calculatedScaleY:F3} (current: {SPRITE_SCALE_Y:F3})");
                StarshroudHollows.Systems.Logger.Log($"Current visual height: {spriteSheet.Height * SPRITE_SCALE_Y:F1} pixels");
            }
            else
            {
                StarshroudHollows.Systems.Logger.Log("ERROR: Spritesheet is NULL!");
            }

            StarshroudHollows.Systems.Logger.Log("===========================================");
        }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            HandleSwimming(deltaTime);
            CheckLavaDamage(deltaTime);
            UpdatePotionCooldowns(deltaTime);

            KeyboardState keyState = Keyboard.GetState();

            if (debugMode)
            {
                Vector2 flyDirection = Vector2.Zero;

                if (keyState.IsKeyDown(Keys.A))
                {
                    flyDirection.X = -1;
                    facingRight = false;
                    isMoving = true;
                }
                if (keyState.IsKeyDown(Keys.D))
                {
                    flyDirection.X = 1;
                    facingRight = true;
                    isMoving = true;
                }
                if (keyState.IsKeyDown(Keys.W) || keyState.IsKeyDown(Keys.Space))
                {
                    flyDirection.Y = -1;
                }
                if (keyState.IsKeyDown(Keys.S))
                {
                    flyDirection.Y = 1;
                }

                if (flyDirection.LengthSquared() > 0)
                {
                    flyDirection.Normalize();
                }

                Position += flyDirection * DEBUG_FLY_SPEED;
                Velocity = Vector2.Zero;
                UpdateAnimation(deltaTime);
                UpdateItemAnimation(deltaTime);
                UpdateHeldItemAnimation(deltaTime);
                return;
            }

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
                Velocity = new Vector2(Velocity.X, Velocity.Y + GRAVITY * gravityMultiplier);

                if (Velocity.Y > MAX_FALL_SPEED)
                {
                    Velocity = new Vector2(Velocity.X, MAX_FALL_SPEED);
                }
            }

            ApplyPhysics();
            UpdateAnimation(deltaTime);
            UpdateItemAnimation(deltaTime);
            UpdateHeldItemAnimation(deltaTime);
        }

        private void CheckLavaDamage(float deltaTime)
        {
            bool inLava = IsInLava();

            if (inLava)
            {
                lavaDamageTimer += deltaTime;

                if (lavaDamageTimer >= LAVA_DAMAGE_INTERVAL)
                {
                    TakeDamage(LAVA_DAMAGE_AMOUNT);
                    lavaDamageTimer -= LAVA_DAMAGE_INTERVAL;
                    StarshroudHollows.Systems.Logger.Log($"[PLAYER] Lava damage! Health: {Health}/{MaxHealth}");
                }
            }
            else
            {
                lavaDamageTimer = 0f;
            }
        }

        private bool IsInLava()
        {
            // Use fully qualified World reference for TILE_SIZE
            int tileSize = StarshroudHollows.World.World.TILE_SIZE;

            Vector2[] checkPoints = new Vector2[]
            {
                new Vector2(Position.X + 5, Position.Y + 5),
                new Vector2(Position.X + Player.PLAYER_WIDTH - 5, Position.Y + 5),
                new Vector2(Position.X + 5, Position.Y + Player.PLAYER_HEIGHT - 5),
                new Vector2(Position.X + Player.PLAYER_WIDTH - 5, Position.Y + Player.PLAYER_HEIGHT - 5),
                new Vector2(Position.X + Player.PLAYER_WIDTH / 2, Position.Y + Player.PLAYER_HEIGHT / 2)
            };

            foreach (Vector2 point in checkPoints)
            {
                int tileX = (int)(point.X / tileSize);
                int tileY = (int)(point.Y / tileSize);

                var tile = world.GetTile(tileX, tileY);
                if (tile != null && tile.Type == TileType.Lava && tile.LiquidVolume > 0.05f)
                {
                    return true;
                }
            }

            return false;
        }

        public void TakeDamage(float amount)
        {
            Health -= amount;
            Health = MathHelper.Clamp(Health, 0, MaxHealth);

            if (Health <= 0)
            {
                StarshroudHollows.Systems.Logger.Log("[PLAYER] Player died! Respawning...");
                TeleportToSpawn();
                Health = MaxHealth;
            }
        }

        public void Heal(float amount)
        {
            Health += amount;
            if (Health > MaxHealth) Health = MaxHealth;
        }

        // PHASE 2: Potion System Methods
        private void UpdatePotionCooldowns(float deltaTime)
        {
            if (healthPotionCooldownTimer > 0)
            {
                healthPotionCooldownTimer -= deltaTime;
                if (healthPotionCooldownTimer < 0) healthPotionCooldownTimer = 0;
            }
        }

        public bool TryUseHealthPotion(StarshroudHollows.Systems.Inventory inventory)
        {
            // Check cooldown
            if (healthPotionCooldownTimer > 0)
            {
                StarshroudHollows.Systems.Logger.Log($"[POTION] Health potion on cooldown: {healthPotionCooldownTimer:F1}s remaining");
                return false;
            }

            // Check if player has health potion
            if (!inventory.HasItem(ItemType.HealthPotion, 1))
            {
                StarshroudHollows.Systems.Logger.Log("[POTION] No health potions in inventory");
                return false;
            }

            // Check if already at max health
            if (Health >= MaxHealth)
            {
                StarshroudHollows.Systems.Logger.Log("[POTION] Already at max health");
                return false;
            }

            // Use the potion
            Heal(HEALTH_POTION_HEAL_AMOUNT);
            inventory.RemoveItem(ItemType.HealthPotion, 1);
            healthPotionCooldownTimer = HEALTH_POTION_COOLDOWN;
            StarshroudHollows.Systems.Logger.Log($"[POTION] Used health potion! Healed {HEALTH_POTION_HEAL_AMOUNT} HP. New health: {Health}/{MaxHealth}");
            return true;
        }

        public float GetHealthPotionCooldown() => healthPotionCooldownTimer;
        public float GetHealthPotionCooldownPercent() => healthPotionCooldownTimer / HEALTH_POTION_COOLDOWN;

        private void HandleSwimming(float deltaTime)
        {
            bool inWater = IsInWater();
            bool headUnderwater = IsHeadUnderwater();
            float WATER_DRAG_FACTOR = 0.5f;

            if (inWater)
            {
                Velocity = new Vector2(Velocity.X * WATER_DRAG_FACTOR, Velocity.Y);

                if (headUnderwater)
                {
                    AirBubbles -= AIR_DEPLETE_RATE * deltaTime;

                    if (AirBubbles <= 0)
                    {
                        AirBubbles = 0;
                        TakeDamage(DROWN_DAMAGE * deltaTime);
                    }
                }
                else
                {
                    AirBubbles += AIR_RESTORE_RATE * deltaTime;
                }

                if (Velocity.Y > 3f)
                {
                    Velocity = new Vector2(Velocity.X, 3f);
                }
            }
            else
            {
                AirBubbles += AIR_RESTORE_RATE * deltaTime;
            }

            AirBubbles = MathHelper.Clamp(AirBubbles, 0, MaxAirBubbles);
        }

        private bool IsHeadUnderwater()
        {
            // Use fully qualified World reference for TILE_SIZE
            int tileSize = StarshroudHollows.World.World.TILE_SIZE;
            int headHeight = Player.PLAYER_HEIGHT / 3;

            Vector2[] checkPoints = new Vector2[]
            {
                new Vector2(Position.X + 5, Position.Y + 5),
                new Vector2(Position.X + Player.PLAYER_WIDTH - 5, Position.Y + 5),
                new Vector2(Position.X + Player.PLAYER_WIDTH / 2, Position.Y + headHeight / 2)
            };

            foreach (Vector2 point in checkPoints)
            {
                int tileX = (int)(point.X / tileSize);
                int tileY = (int)(point.Y / tileSize);

                var tile = world.GetTile(tileX, tileY);
                if (tile != null && tile.Type == TileType.Water && tile.LiquidVolume > 0.9f)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsInWater()
        {
            // Use fully qualified World reference for TILE_SIZE
            int tileSize = StarshroudHollows.World.World.TILE_SIZE;

            Vector2[] checkPoints = new Vector2[]
            {
                new Vector2(Position.X + 5, Position.Y + 5),
                new Vector2(Position.X + Player.PLAYER_WIDTH - 5, Position.Y + 5),
                new Vector2(Position.X + 5, Position.Y + Player.PLAYER_HEIGHT - 5),
                new Vector2(Position.X + Player.PLAYER_WIDTH - 5, Position.Y + Player.PLAYER_HEIGHT - 5),
                new Vector2(Position.X + Player.PLAYER_WIDTH / 2, Position.Y + Player.PLAYER_HEIGHT / 2)
            };

            foreach (Vector2 point in checkPoints)
            {
                int tileX = (int)(point.X / tileSize);
                int tileY = (int)(point.Y / tileSize);

                var tile = world.GetTile(tileX, tileY);
                if (tile != null && tile.Type == TileType.Water && tile.LiquidVolume > 0.05f)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateAnimation(float deltaTime)
        {
            // No animation - single frame sprite
        }

        private void UpdateItemAnimation(float deltaTime)
        {
            itemAnimTimer += deltaTime;
            if (itemAnimTimer >= ITEM_ANIM_SPEED)
            {
                itemAnimTimer = 0f;
                itemAnimFrame = (itemAnimFrame + 1) % 3;
            }
        }

        private void UpdateHeldItemAnimation(float deltaTime)
        {
            heldItemAnimationTimer += deltaTime;
            if (heldItemAnimationTimer >= FRAME_TIME)
            {
                heldItemAnimationTimer -= FRAME_TIME;
                heldItemCurrentFrame++;
            }
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
                    // Use fully qualified World reference for TILE_SIZE
                    int hitTileY = (int)((Position.Y + Velocity.Y + Player.PLAYER_HEIGHT) / StarshroudHollows.World.World.TILE_SIZE);
                    Position = new Vector2(Position.X, hitTileY * StarshroudHollows.World.World.TILE_SIZE - Player.PLAYER_HEIGHT);
                    isOnGround = true;
                }
                Velocity = new Vector2(Velocity.X, 0);
            }

            Vector2 groundCheckPos = new Vector2(Position.X, Position.Y + 1);
            if (CheckCollision(groundCheckPos))
            {
                isOnGround = true;
            }
        }

        private bool CheckCollision(Vector2 position)
        {
            // Use fully qualified World reference for TILE_SIZE
            int tileSize = StarshroudHollows.World.World.TILE_SIZE;

            Vector2[] checkPoints = new Vector2[]
            {
                new Vector2(position.X + 1, position.Y + 0),
                new Vector2(position.X + Player.PLAYER_WIDTH - 1, position.Y + 0),
                new Vector2(position.X + 1, position.Y + Player.PLAYER_HEIGHT - 1),
                new Vector2(position.X + Player.PLAYER_WIDTH - 1, position.Y + Player.PLAYER_HEIGHT - 1),
                new Vector2(position.X + Player.PLAYER_WIDTH / 2, position.Y + 0),
                new Vector2(position.X + Player.PLAYER_WIDTH / 2, position.Y + Player.PLAYER_HEIGHT - 1),
                new Vector2(position.X + 1, position.Y + Player.PLAYER_HEIGHT / 2),
                new Vector2(position.X + Player.PLAYER_WIDTH - 1, position.Y + Player.PLAYER_HEIGHT / 2)
            };

            foreach (Vector2 point in checkPoints)
            {
                int tileX = (int)(point.X / tileSize);
                int tileY = (int)(point.Y / tileSize);

                var tile = world.GetTile(tileX, tileY);
                if (tile != null && (tile.Type == TileType.Water || tile.Type == TileType.Lava))
                {
                    if (tile.LiquidVolume < 0.5f)
                    {
                        continue;
                    }
                }

                if (world.IsSolidAtPosition(tileX, tileY))
                {
                    return true;
                }
            }

            return false;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, ItemType heldItemType, Texture2D itemSpriteSheet, int animationFrame)
        {
            // Draw the player sprite
            if (spriteSheet != null)
            {
                // Use entire sprite as single frame (1028x1028)
                Rectangle playerSourceRect = new Rectangle(0, 0, spriteSheet.Width, spriteSheet.Height);
                
                // Calculate the scaled sprite dimensions to fit player hitbox with non-uniform scaling
                float scaledWidth = spriteSheet.Width * SPRITE_SCALE_X;
                float scaledHeight = spriteSheet.Height * SPRITE_SCALE_Y;
                
                // Center the sprite on the player's hitbox position
                Vector2 playerDrawPosition = new Vector2(
                    Position.X + (Player.PLAYER_WIDTH / 2) - (scaledWidth / 2),
                    Position.Y + Player.PLAYER_HEIGHT - scaledHeight
                );
                
                SpriteEffects playerFlip = facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                
                // Use Vector2 for non-uniform scale
                Vector2 scale = new Vector2(SPRITE_SCALE_X, SPRITE_SCALE_Y);
                spriteBatch.Draw(spriteSheet, playerDrawPosition, playerSourceRect, Color.White, 0f, Vector2.Zero, scale, playerFlip, 0f);
            }
            else // Fallback drawing
            {
                Rectangle playerRect = new Rectangle((int)Position.X, (int)Position.Y, Player.PLAYER_WIDTH, Player.PLAYER_HEIGHT);
                spriteBatch.Draw(pixelTexture, playerRect, Color.Yellow);
            }

            // Draw the held item
            if (heldItemType != ItemType.None && itemSpriteSheet != null)
            {
                // Initialize variables for drawing the item
                int itemFrameWidth = itemSpriteSheet.Width;
                int itemFrameHeight = itemSpriteSheet.Height;
                float rotation = 0f;
                float scale = 0.5f;
                SpriteEffects flip = facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                int sourceX = 0;
                int sourceY = 0;

                // --- LOGIC FOR DIFFERENT ITEM TYPES ---
                if (heldItemType >= ItemType.WoodPickaxe && heldItemType <= ItemType.RunicPickaxe)
                {
                    scale = 0.5f; // Reduced from 1.7f to 0.5f
                    float idleAngle = facingRight ? MathHelper.ToRadians(-45) : MathHelper.ToRadians(45);
                    float backswingAngle = facingRight ? MathHelper.ToRadians(-120) : MathHelper.ToRadians(120);
                    float impactAngle = facingRight ? MathHelper.ToRadians(80) : MathHelper.ToRadians(-80);

                    if (animationFrame == 1) rotation = backswingAngle;
                    else if (animationFrame == 2) rotation = impactAngle;
                    else rotation = idleAngle;

                    if (heldItemType == ItemType.RunicPickaxe)
                    {
                        itemFrameWidth = itemSpriteSheet.Width / 2;
                        itemFrameHeight = itemSpriteSheet.Height / 2;
                        int currentFrame = animationFrame > 0 ? animationFrame : itemAnimFrame;
                        if (currentFrame == 1) sourceX = itemFrameWidth;
                        if (currentFrame == 2) sourceY = itemFrameHeight;
                    }
                }
                else if (heldItemType >= ItemType.WoodSword && heldItemType <= ItemType.RunicSword)
                {
                    scale = 0.35f; // Reduced from 0.6f to 0.35f
                    if (animationFrame == 1) rotation = facingRight ? MathHelper.PiOver4 * 2.8f : -MathHelper.PiOver4 * 1.2f;
                    else if (animationFrame == 2) rotation = facingRight ? MathHelper.PiOver4 * 0.5f : -MathHelper.PiOver4 * 2.5f;
                    else rotation = facingRight ? MathHelper.PiOver4 * 0.5f : -MathHelper.PiOver4 * 0.5f;

                    if (heldItemType == ItemType.RunicSword)
                    {
                        int totalFrames = 9;
                        itemFrameWidth = itemSpriteSheet.Width / 5;
                        itemFrameHeight = itemSpriteSheet.Height / 2;
                        int currentAnimFrame = heldItemCurrentFrame % totalFrames;
                        sourceX = currentAnimFrame % 5 * itemFrameWidth;
                        sourceY = currentAnimFrame / 5 * itemFrameHeight;
                    }
                }
                else if (heldItemType >= ItemType.WoodWand && heldItemType <= ItemType.RunicLaserWand)
                {
                    scale = 0.3f; // Reduced from 0.5f to 0.3f
                    rotation = 0f;
                    if (heldItemType == ItemType.RunicLaserWand)
                    {
                        int totalFrames = 9;
                        itemFrameWidth = itemSpriteSheet.Width / 4;
                        itemFrameHeight = itemSpriteSheet.Height / 3;
                        int currentAnimFrame = heldItemCurrentFrame % totalFrames;
                        sourceX = currentAnimFrame % 4 * itemFrameWidth;
                        sourceY = currentAnimFrame / 4 * itemFrameHeight;
                    }
                }
                else if (heldItemType == ItemType.Torch)
                {
                    scale = 0.4f; // Reduced from 0.6f to 0.4f
                    rotation = 0f;
                    flip = SpriteEffects.None;
                }
                // Default case for other items is already set
                // Default case for other items is already set

                // --- FINAL DRAW CALL FOR THE ITEM ---
                Rectangle sourceRect = new Rectangle(sourceX, sourceY, itemFrameWidth, itemFrameHeight);
                Vector2 origin = new Vector2(itemFrameWidth / 2f, itemFrameHeight / 2f);
                Vector2 drawPosition = new Vector2(Position.X + Player.PLAYER_WIDTH * 0.75f, Position.Y + Player.PLAYER_HEIGHT * 0.35f);

                if (heldItemType >= ItemType.WoodSword && heldItemType <= ItemType.RunicSword)
                {
                    if (animationFrame == 1)
                    {
                        drawPosition.X += facingRight ? 10 : -10;
                        drawPosition.Y -= 5;
                    }
                    else if (animationFrame == 2)
                    {
                        drawPosition.X += facingRight ? 15 : -15;
                        drawPosition.Y += 5;
                    }
                }

                if (heldItemType == ItemType.Torch)
                {
                    drawPosition.Y += 5;
                }

                spriteBatch.Draw(itemSpriteSheet, drawPosition, sourceRect, Color.White, rotation, origin, scale, flip, 0f);
            }
        }

        public Vector2 GetCenterPosition()
        {
            return new Vector2(
                Position.X + Player.PLAYER_WIDTH / 2,
                Position.Y + Player.PLAYER_HEIGHT / 2
            );
        }

        public bool GetFacingRight()
        {
            return facingRight;
        }

        public bool IsOnGround()
        {
            return isOnGround;
        }

        public Rectangle GetHitbox()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, Player.PLAYER_WIDTH, Player.PLAYER_HEIGHT);
        }

        public StarshroudHollows.Systems.Inventory GetInventory()
        {
            // This return type is now correctly fully qualified to resolve the ambiguity.
            return null;
        }
    }
}
