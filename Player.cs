using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Claude4_5Terraria.World;
using Claude4_5Terraria.Enums;
using System;

namespace Claude4_5Terraria.Player
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

        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        private float lavaDamageTimer = 0f;
        private const float LAVA_DAMAGE_INTERVAL = 0.5f;
        private const float LAVA_DAMAGE_AMOUNT = 10f;

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

        // Debug mode
        private bool debugMode = false;
        private const float DEBUG_FLY_SPEED = 15f;

        private bool isOnGround;
        private World.World world;

        private Texture2D spriteSheet;
        private bool facingRight = true;
        private bool isMoving = false;

        private int currentFrame = 0;
        private float animationTimer = 0f;
        private const float FRAME_TIME = 0.12f;

        private const int FRAME_WIDTH = 128;
        private const int FRAME_HEIGHT = 128;
        private const int FRAMES_PER_ROW = 6;

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
            Systems.Logger.Log($"[PLAYER] Bed spawn set at: {bedPosition}");
        }

        public void ClearBedSpawn()
        {
            HasBedSpawn = false;
            BedSpawnPosition = Vector2.Zero;
            Systems.Logger.Log("[PLAYER] Bed spawn cleared");
        }

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
            AirBubbles = MaxAirBubbles;
            lavaDamageTimer = 0f;
        }

        public void SetDebugMode(bool enabled)
        {
            debugMode = enabled;
            if (debugMode)
            {
                Systems.Logger.Log("[PLAYER] Debug mode ENABLED: Fly + Noclip active");
            }
            else
            {
                Systems.Logger.Log("[PLAYER] Debug mode DISABLED");
            }
        }

        public bool IsDebugModeActive() => debugMode;

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

            HandleSwimming(deltaTime);
            CheckLavaDamage(deltaTime);

            KeyboardState keyState = Keyboard.GetState();

            // DEBUG MODE: Free fly in all directions, noclip
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
                return;
            }

            // NORMAL MODE
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
            bool inLava = IsInLava();

            if (inLava)
            {
                lavaDamageTimer += deltaTime;

                if (lavaDamageTimer >= LAVA_DAMAGE_INTERVAL)
                {
                    TakeDamage(LAVA_DAMAGE_AMOUNT);
                    lavaDamageTimer -= LAVA_DAMAGE_INTERVAL;
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
            int tileSize = World.World.TILE_SIZE;
            int headHeight = PLAYER_HEIGHT / 3;

            Vector2[] checkPoints = new Vector2[]
            {
                new Vector2(Position.X + 5, Position.Y + 5),
                new Vector2(Position.X + PLAYER_WIDTH - 5, Position.Y + 5),
                new Vector2(Position.X + PLAYER_WIDTH / 2, Position.Y + headHeight / 2)
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
            int tileSize = World.World.TILE_SIZE;

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
                if (tile != null && tile.Type == TileType.Water && tile.LiquidVolume > 0.05f)
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

                if (currentFrame >= 6)
                {
                    currentFrame = 0;
                }
            }

            currentAnimationRow = IDLE_DOWN_ROW;
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
                    int hitTileY = (int)((Position.Y + Velocity.Y + PLAYER_HEIGHT) / World.World.TILE_SIZE);
                    Position = new Vector2(Position.X, hitTileY * World.World.TILE_SIZE - PLAYER_HEIGHT);

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
            int tileSize = World.World.TILE_SIZE;

            Vector2[] checkPoints = new Vector2[]
            {
                new Vector2(position.X + 1, position.Y + 0),
                new Vector2(position.X + PLAYER_WIDTH - 1, position.Y + 0),
                new Vector2(position.X + 1, position.Y + PLAYER_HEIGHT - 1),
                new Vector2(position.X + PLAYER_WIDTH - 1, position.Y + PLAYER_HEIGHT - 1),
                new Vector2(position.X + PLAYER_WIDTH / 2, position.Y + 0),
                new Vector2(position.X + PLAYER_WIDTH / 2, position.Y + PLAYER_HEIGHT - 1),
                new Vector2(position.X + 1, position.Y + PLAYER_HEIGHT / 2),
                new Vector2(position.X + PLAYER_WIDTH - 1, position.Y + PLAYER_HEIGHT / 2)
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

        private bool hasLoggedDraw = false;

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, ItemType heldItemType, Texture2D itemSpriteSheet, int animationFrame)
        {
            Rectangle playerRect = new Rectangle(
                (int)Position.X,
                (int)Position.Y,
                PLAYER_WIDTH,
                PLAYER_HEIGHT
            );
            spriteBatch.Draw(pixelTexture, playerRect, Color.Yellow);

            if (heldItemType != ItemType.None && itemSpriteSheet != null)
            {
                int totalFrames = 1;
                int itemFrameWidth = itemSpriteSheet.Width;
                int itemFrameHeight = itemSpriteSheet.Height;
                int currentFrame = 0;
                float rotation = 0f;
                float scale = 0.5f;
                SpriteEffects flip = facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                // Handle Runic Pickaxe animation
                if (heldItemType == ItemType.RunicPickaxe)
                {
                    totalFrames = 3;
                    itemFrameWidth = itemSpriteSheet.Width / 2;
                    itemFrameHeight = itemSpriteSheet.Height / 2;
                    currentFrame = animationFrame;
                    scale = 0.5f;

                    if (animationFrame == 1)
                    {
                        rotation = facingRight ? MathHelper.PiOver4 * 0.5f : MathHelper.PiOver4 * 1.5f;
                    }
                    else if (animationFrame == 2)
                    {
                        rotation = facingRight ? MathHelper.PiOver4 * 1.5f : -MathHelper.PiOver4 * 0.5f;
                    }
                    else
                    {
                        rotation = facingRight ? -MathHelper.PiOver4 * 0.25f : MathHelper.PiOver4 * 0.25f;
                    }
                }
                // NEW: Handle Wood Sword animation (same style as Runic Pickaxe)
                else if (heldItemType == ItemType.WoodSword)
                {
                    totalFrames = 3;
                    // Wood sword uses single sprite, not a sheet
                    itemFrameWidth = itemSpriteSheet.Width;
                    itemFrameHeight = itemSpriteSheet.Height;
                    currentFrame = animationFrame;
                    scale = 0.5f;

                    if (animationFrame == 1)
                    {
                        rotation = facingRight ? MathHelper.PiOver4 * 0.5f : MathHelper.PiOver4 * 1.5f;
                    }
                    else if (animationFrame == 2)
                    {
                        rotation = facingRight ? MathHelper.PiOver4 * 1.5f : -MathHelper.PiOver4 * 0.5f;
                    }
                    else
                    {
                        rotation = facingRight ? -MathHelper.PiOver4 * 0.25f : MathHelper.PiOver4 * 0.25f;
                    }
                }
                else if (heldItemType == ItemType.Torch)
                {
                    scale = 0.6f;
                    rotation = 0f;
                    flip = SpriteEffects.None;
                }
                else
                {
                    scale = 0.5f;
                    rotation = 0f;
                }

                int sourceX = 0;
                int sourceY = 0;

                if (heldItemType == ItemType.RunicPickaxe)
                {
                    if (currentFrame == 1) sourceX = itemFrameWidth;
                    if (currentFrame == 2) sourceY = itemFrameHeight;
                }
                // Wood sword uses single sprite, no frame offset needed

                Rectangle sourceRect = new Rectangle(
                    sourceX,
                    sourceY,
                    itemFrameWidth,
                    itemFrameHeight
                );

                Vector2 origin = new Vector2(itemFrameWidth / 2f, itemFrameHeight / 2f);

                Vector2 drawPosition = new Vector2(
                    Position.X + (PLAYER_WIDTH * 0.75f),
                    Position.Y + (PLAYER_HEIGHT * 0.35f)
                );

                if (heldItemType == ItemType.Torch)
                {
                    drawPosition.Y += 5;
                }

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
        
        public bool GetFacingRight()
        {
            return facingRight;
        }
    }
}
