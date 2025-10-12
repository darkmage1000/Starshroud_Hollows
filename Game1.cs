using Claude4_5Terraria.Entities;
using Claude4_5Terraria.Player;
using Claude4_5Terraria.Systems;
using Claude4_5Terraria.UI;
using Claude4_5Terraria.World;
using Claude4_5Terraria.Enums;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace Claude4_5Terraria
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private World.World world;
        private Claude4_5Terraria.Player.Player player;
        private Camera camera;
        private Texture2D pixelTexture;
        private Texture2D oozeEnemyTexture; // NEW: Ooze enemy sprite
        private SpriteFont font;
        private Texture2D menuBackgroundTexture;

        private Inventory inventory;
        private MiningSystem miningSystem;
        private LightingSystem lightingSystem;
        private TimeSystem timeSystem;
        private WorldGenerator worldGenerator;
        private ChestSystem chestSystem;
        private LiquidSystem liquidSystem; // NEW: Liquid System Field
        private CombatSystem combatSystem; // NEW: Combat System Field
        private EnemySpawner enemySpawner; // NEW: Enemy Spawner Field

        // NEW: Magic System Fields
        private MagicSystem magicSystem;
        private ProjectileSystem projectileSystem;
        private Texture2D wandTexture;

        private InventoryUI inventoryUI;
        private PauseMenu pauseMenu;
        private MiningOverlay miningOverlay;
        private StartMenu startMenu;
        private SaveMenu saveMenu;
        private HUD hud;
        private ChestUI chestUI;

        private KeyboardState previousKeyboardState;
        private MouseState previousMouseState;

        private Song backgroundMusic;
        private float musicVolume = 0.1f;

        private float gameSoundsVolume = 0.5f;

        private bool isMusicMuted = false;
        private bool showMiningOutlines = false;

        private bool worldGenerated = false;
        private int currentWorldSeed;
        private float totalPlayTime;

        private bool isAutoMiningActive = false;
        private Vector2 lastPlayerDirection = Vector2.Zero;

        private Dictionary<ItemType, Texture2D> itemTextureMap;

        // --- NEW RAIN SYSTEM FIELDS (Unchanged) ---
        private List<Vector2> rainParticles;
        private Random random;
        private const int MAX_RAIN_DROPS = 1000;
        private const float RAIN_SPEED = 800f;
        private const float RAIN_OFFSET_X = 500f;
        private const float RAIN_OFFSET_Y = 100f;
        // ------------------------------------------

        // --- NEW SNOW SYSTEM FIELDS ---
        private List<Vector2> snowParticles;
        private const int MAX_SNOW_FLAKES = 1500;
        private const float SNOW_SPEED = 200f;
        private const float SNOW_DRIFT = 50f; // Horizontal drift
        // -------------------------------

        // --- NEW SOUND EFFECT FIELDS ---
        private SoundEffect mineDirtSound;
        private SoundEffect mineStoneSound;
        private SoundEffect mineTorchSound;
        private SoundEffect placeTorchSound;
        // -------------------------------

        // --- NEW: Bed and Sleep fields ---
        private bool isSleeping = false;
        private float sleepProgress = 0f;
        private const float SLEEP_DURATION = 5.0f; // 5 seconds to sleep once started
        private Point? currentBedPosition = null;
        private float bedHoldTime = 0f; // Track how long right-click has been held on bed
        private const float BED_HOLD_REQUIRED = 3.0f; // Must hold for 3 seconds to start sleeping
        // ---------------------------------


        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();

            random = new Random();
            rainParticles = new List<Vector2>();
            snowParticles = new List<Vector2>();
        }

        protected override void Initialize()
        {
            // CRITICAL AUDIO FIX: Ensure Audio is ready immediately
            try
            {
                // This line can sometimes force the audio device to activate
                SoundEffect.MasterVolume = 1.0f;
            }
            catch { }

            previousKeyboardState = Keyboard.GetState();
            previousMouseState = Mouse.GetState();
            totalPlayTime = 0f;
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            DroppedItem.SetStaticContent(Content);

            pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            // NEW: Load enemy sprite
            try
            {
                oozeEnemyTexture = Content.Load<Texture2D>("OozeEnemy");
                Logger.Log("[GAME] Loaded OozeEnemy sprite successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"[GAME] Could not load OozeEnemy sprite: {ex.Message}");
                // DEBUG FIX: Fallback to a large, guaranteed visible color texture (not pixelTexture)
                oozeEnemyTexture = pixelTexture;
            }

            // NEW: Load Wand sprite
            try
            {
                wandTexture = Content.Load<Texture2D>("wand");
                Logger.Log("[GAME] Loaded Wand sprite successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"[GAME] Could not load Wand sprite: {ex.Message}");
                wandTexture = pixelTexture;
            }


            font = Content.Load<SpriteFont>("Font");
            menuBackgroundTexture = Content.Load<Texture2D>("MenuBackground");

            // Initialize item texture map
            itemTextureMap = new Dictionary<ItemType, Texture2D>();

            // NEW: Load Sound Effects (Mapping to Content file names)
            try
            {
                mineDirtSound = Content.Load<SoundEffect>("a_pickaxe_hitting_dirt");
                mineStoneSound = Content.Load<SoundEffect>("a_pickaxe_hitting_stone");
                SoundEffect woodSound = Content.Load<SoundEffect>("hitting a tree");
                mineTorchSound = woodSound;
                placeTorchSound = Content.Load<SoundEffect>("placing and mining a torch");

                // CRITICAL AUDIO FIX: Create a SoundEffectInstance immediately after loading
                mineStoneSound?.CreateInstance().Dispose();

            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] Could not load sound effects: {ex.Message}. Check .mgcb names.");
            }
            // --- END NEW SOUNDS ---

            backgroundMusic = Content.Load<Song>("CozyBackground");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = musicVolume;
            MediaPlayer.Play(backgroundMusic);

            // UPDATED: Pass SetGameSoundsVolume callback and initial volume, AND PlayTestSound
            startMenu = new StartMenu(musicVolume,
                                      (newVolume) => { musicVolume = newVolume; if (!isMusicMuted) MediaPlayer.Volume = musicVolume; },
                                      gameSoundsVolume,
                                      SetGameSoundsVolume,
                                      ToggleFullscreen,
                                      menuBackgroundTexture,
                                      PlayTestSound);

            Logger.Log("[GAME] Content loaded successfully");
        }

        protected override void UnloadContent()
        {
            MediaPlayer.Stop();
            base.UnloadContent();
        }

        public void SetGameSoundsVolume(float newVolume)
        {
            gameSoundsVolume = newVolume;
            miningSystem?.SetSoundVolume(newVolume);
        }

        private void PlayTestSound()
        {
            if (mineDirtSound != null)
            {
                mineDirtSound.Play(volume: gameSoundsVolume, pitch: 0.0f, pan: 0.0f);
            }
        }

        public void ToggleAutoMining(bool? newState = null)
        {
            if (newState.HasValue)
            {
                isAutoMiningActive = newState.Value;
            }
            else
            {
                isAutoMiningActive = !isAutoMiningActive;
            }
            Logger.Log($"[INPUT] Auto-Mining is now {(isAutoMiningActive ? "ON" : "OFF")}");
        }

        private void UpdateRainParticles(float deltaTime, int screenWidth, int screenHeight)
        {
            if (timeSystem != null && timeSystem.IsRaining)
            {
                // Add new particles up to MAX_RAIN_DROPS
                while (rainParticles.Count < MAX_RAIN_DROPS)
                {
                    // Spawn particle randomly within a visible area plus a small offset
                    float spawnX = (float)random.NextDouble() * (screenWidth + (int)RAIN_OFFSET_X * 2) - RAIN_OFFSET_X;
                    float spawnY = (float)random.NextDouble() * (screenHeight + (int)RAIN_OFFSET_Y * 2) - RAIN_OFFSET_Y;
                    rainParticles.Add(new Vector2(spawnX, spawnY));
                }

                // Update particle positions
                for (int i = rainParticles.Count - 1; i >= 0; i--)
                {
                    Vector2 p = rainParticles[i];
                    Vector2 nextP = p;
                    nextP.Y += RAIN_SPEED * deltaTime;
                    nextP.X -= RAIN_SPEED * deltaTime * 0.1f; // Slight diagonal offset

                    // 1. Convert new screen position to world tile coordinates
                    Matrix transformMatrix = camera.GetTransformMatrix();
                    Matrix inverseTransform = Matrix.Invert(transformMatrix);
                    Vector2 worldPos = Vector2.Transform(nextP, inverseTransform);

                    int tileX = (int)(worldPos.X / World.World.TILE_SIZE);
                    int tileY = (int)(worldPos.Y / World.World.TILE_SIZE);

                    // 2. Check for solid block collision (CRITICAL NEW LOGIC)
                    // FIX: IsSolidAtPosition is now available in World.cs
                    if (world.IsSolidAtPosition(tileX, tileY))
                    {
                        // Collision detected: stop the rain particle and respawn it at the top
                        p.Y = -RAIN_OFFSET_Y;
                        p.X = (float)random.NextDouble() * (screenWidth + (int)RAIN_OFFSET_X * 2) - RAIN_OFFSET_X;
                    }
                    // 3. Check bounds for respawn
                    else if (nextP.Y > screenHeight + RAIN_OFFSET_Y || nextP.X < -RAIN_OFFSET_X || nextP.X > screenWidth + RAIN_OFFSET_X)
                    {
                        // Fell off screen: respawn near the top
                        p.Y = -RAIN_OFFSET_Y;
                        p.X = (float)random.NextDouble() * (screenWidth + (int)RAIN_OFFSET_X * 2) - RAIN_OFFSET_X;
                    }
                    else
                    {
                        // No collision and still on screen: update position
                        p = nextP;
                    }

                    rainParticles[i] = p;
                }
            }
            else
            {
                // Clear particles quickly when rain stops
                if (rainParticles.Count > 0)
                {
                    rainParticles.Clear();
                }
            }
        }

        private void UpdateSnowParticles(float deltaTime, int screenWidth, int screenHeight)
        {
            // Check if player is in snow biome
            if (player == null || worldGenerator == null) return;

            Vector2 playerCenter = player.GetCenterPosition();
            int playerTileX = (int)(playerCenter.X / World.World.TILE_SIZE);

            // Get snow biome bounds from world generator
            int snowStartX = worldGenerator.GetSnowBiomeStartX();
            int snowEndX = worldGenerator.GetSnowBiomeEndX();

            bool inSnowBiome = playerTileX >= snowStartX && playerTileX <= snowEndX;

            if (inSnowBiome)
            {
                // Add new snow particles up to MAX_SNOW_FLAKES
                while (snowParticles.Count < MAX_SNOW_FLAKES)
                {
                    float spawnX = (float)random.NextDouble() * (screenWidth + (int)RAIN_OFFSET_X * 2) - RAIN_OFFSET_X;
                    float spawnY = (float)random.NextDouble() * (screenHeight + (int)RAIN_OFFSET_Y * 2) - RAIN_OFFSET_Y;
                    snowParticles.Add(new Vector2(spawnX, spawnY));
                }

                // Update particle positions (slower fall, gentle drift) - FIXED: Use screen coordinates
                for (int i = snowParticles.Count - 1; i >= 0; i--)
                {
                    Vector2 p = snowParticles[i];
                    p.Y += SNOW_SPEED * deltaTime;
                    p.X += (float)Math.Sin(p.Y * 0.01f) * SNOW_DRIFT * deltaTime; // Gentle side-to-side drift

                    // FIXED: Check bounds for respawn (no collision check - snow falls through everything in screen space)
                    if (p.Y > screenHeight + RAIN_OFFSET_Y || p.X < -RAIN_OFFSET_X || p.X > screenWidth + RAIN_OFFSET_X)
                    {
                        // Fell off screen: respawn at top
                        p.Y = -RAIN_OFFSET_Y;
                        p.X = (float)random.NextDouble() * (screenWidth + (int)RAIN_OFFSET_X * 2) - RAIN_OFFSET_X;
                    }

                    snowParticles[i] = p;
                }
            }
            else
            {
                // Not in snow biome - clear particles
                if (snowParticles.Count > 0)
                {
                    snowParticles.Clear();
                }
            }
        }


        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            // --- CRITICAL STATE MANAGEMENT FIX ---
            // This block forces the game state to 'Playing' immediately after world generation finishes.
            if (startMenu.GetState() == MenuState.Loading)
            {
                if (worldGenerated)
                {
                    // FORCE the game state to Playing immediately after world generation is flagged complete
                    startMenu.SetState(MenuState.Playing);
                    Logger.Log("[GAME] DEBUG: Forced state transition to Playing.");
                }

                // If still loading (or just finished loading), skip to base.Update
                if (startMenu.GetState() != MenuState.Playing)
                {
                    previousKeyboardState = keyboardState;
                    previousMouseState = mouseState;
                    base.Update(gameTime);
                    return;
                }
            }
            // --- END CRITICAL FIX ---


            if (startMenu.GetState() == MenuState.MainMenu ||
                startMenu.GetState() == MenuState.Options ||
                startMenu.GetState() == MenuState.LoadMenu)
            {
                startMenu.Update(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

                if (startMenu.GetState() == MenuState.Loading && !worldGenerated)
                {
                    Logger.Log($"[GAME] Menu transitioned to Loading state");
                    Logger.Log($"[GAME] IsLoadingSavedGame: {startMenu.IsLoadingSavedGame()}");
                    Logger.Log($"[GAME] LoadingSlotIndex: {startMenu.GetLoadingSlotIndex()}");
                    Task.Run(() => GenerateWorld());
                }

                previousKeyboardState = keyboardState;
                previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // CRITICAL CHECK FIX: Ensure all required core systems are initialized before proceeding.
            if (timeSystem == null || player == null || world == null || miningSystem == null || inventory == null)
            {
                previousKeyboardState = keyboardState;
                previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            // SINGLE DECLARATION OF playerCenter for the entire remaining scope
            Vector2 playerCenter = player.GetCenterPosition();

            // Update chest UI if open (HIGHEST PRIORITY)
            if (chestUI != null && chestUI.IsOpen)
            {
                chestUI.Update();
                previousKeyboardState = keyboardState;
                previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            // Update save menu if open
            if (saveMenu != null && saveMenu.IsOpen)
            {
                saveMenu.Update(deltaTime, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                previousKeyboardState = keyboardState;
                previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            // Only update pause menu if it exists (after world is generated)
            if (pauseMenu != null)
            {
                // Check for Escape key press to toggle pause state
                if (keyboardState.IsKeyDown(Keys.Escape) && !previousKeyboardState.IsKeyDown(Keys.Escape))
                {
                    pauseMenu.TogglePause();
                }

                pauseMenu.Update();
            }

            if (pauseMenu != null && pauseMenu.IsPaused)
            {

                if (keyboardState.IsKeyDown(Keys.OemPlus) || keyboardState.IsKeyDown(Keys.Add))
                {
                    musicVolume = MathHelper.Clamp(musicVolume + 0.01f, 0f, 1f);
                    if (!isMusicMuted)
                        MediaPlayer.Volume = musicVolume;
                    startMenu.SetMusicVolume(musicVolume);
                }
                if (keyboardState.IsKeyDown(Keys.OemMinus) || keyboardState.IsKeyDown(Keys.Subtract))
                {
                    musicVolume = MathHelper.Clamp(musicVolume - 0.01f, 0f, 1f);
                    if (!isMusicMuted)
                        MediaPlayer.Volume = musicVolume;
                    startMenu.SetMusicVolume(musicVolume);
                }

                if (keyboardState.IsKeyDown(Keys.M) && !previousKeyboardState.IsKeyDown(Keys.M))
                {
                    isMusicMuted = !isMusicMuted;
                    MediaPlayer.Volume = isMusicMuted ? 0f : musicVolume;
                    Logger.Log($"[AUDIO] Music {(isMusicMuted ? "muted" : "unmuted")}");
                }

                previousKeyboardState = keyboardState;
                previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            // --- NEW: Handle Sleeping Logic (Before TimeSystem update) ---
            if (isSleeping)
            {
                // A. Check if player has moved or is no longer on the bed tile
                int playerTileX = (int)(playerCenter.X / World.World.TILE_SIZE);
                int playerTileY = (int)(playerCenter.Y / World.World.TILE_SIZE);

                // Check if player has moved far from the bed's tile
                if (currentBedPosition.HasValue && (Math.Abs(playerTileX - currentBedPosition.Value.X) > 2 || Math.Abs(playerTileY - currentBedPosition.Value.Y) > 2))
                {
                    isSleeping = false;
                    sleepProgress = 0f;
                    Logger.Log("[GAME] Sleeping interrupted: player moved.");
                }
                else
                {
                    // B. Advance sleep progress
                    sleepProgress += deltaTime;

                    if (sleepProgress >= SLEEP_DURATION)
                    {
                        // C. Sleep is complete
                        if (timeSystem != null)
                        {
                            timeSystem.AdvanceToMorning();
                            Logger.Log("[GAME] Slept in bed - time advanced to morning!");
                        }
                        isSleeping = false;
                        sleepProgress = 0f;
                        currentBedPosition = null;
                    }
                }

                // If sleeping, skip all other player/input updates
                previousKeyboardState = keyboardState;
                previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }
            // -----------------------------------------------------------------


            totalPlayTime += deltaTime;

            timeSystem.Update(deltaTime);

            UpdateRainParticles(deltaTime, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            // Update snow particles (always snowing in snow biome)
            UpdateSnowParticles(deltaTime, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            // NEW: Liquid System Update
            liquidSystem.UpdateFlow();


            for (int i = 0; i < 10; i++)
            {
                Keys key = Keys.D1 + i;
                if (keyboardState.IsKeyDown(key) && !previousKeyboardState.IsKeyDown(key))
                {
                    miningSystem.SetSelectedHotbarSlot(i);
                    Logger.Log($"[INPUT] Selected hotbar slot {i}");
                }
            }

            if (keyboardState.IsKeyDown(Keys.T) && !previousKeyboardState.IsKeyDown(Keys.T))
            {
                showMiningOutlines = !showMiningOutlines;
                Logger.Log($"[INPUT] Mining outlines {(showMiningOutlines ? "enabled" : "disabled")}");
            }

            if (keyboardState.IsKeyDown(Keys.L) && !previousKeyboardState.IsKeyDown(Keys.L))
            {
                ToggleAutoMining();
            }

            if (keyboardState.IsKeyDown(Keys.Q) && !previousKeyboardState.IsKeyDown(Keys.Q))
            {
                if (inventoryUI != null && !inventoryUI.IsInventoryOpen)
                {
                    DropSelectedItem();
                }
            }

            // Execute player update before position calculations
            player.Update(gameTime);
            camera.Position = player.Position;

            // Update player center position after player.Update()
            playerCenter = player.GetCenterPosition();

            // NEW: Capture last player direction for auto-mine
            Vector2 currentMovement = Vector2.Zero;
            if (keyboardState.IsKeyDown(Keys.A)) currentMovement.X -= 1;
            if (keyboardState.IsKeyDown(Keys.D)) currentMovement.X += 1;
            if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Space)) currentMovement.Y -= 1;
            if (keyboardState.IsKeyDown(Keys.S)) currentMovement.Y += 1;

            if (currentMovement.LengthSquared() > 0)
            {
                // Prioritize/flatten to cardinal directions as requested.
                if (currentMovement.X != 0)
                {
                    lastPlayerDirection = new Vector2(currentMovement.X, 0);
                }
                else
                {
                    lastPlayerDirection = new Vector2(0, currentMovement.Y);
                }
                // Ensure the direction is a unit vector for consistent targeting
                if (lastPlayerDirection.LengthSquared() > 0)
                {
                    lastPlayerDirection.Normalize();
                }
            }

            // CRITICAL FIX: Block manual left-click if auto-mine is active and we are moving
            if (isAutoMiningActive && lastPlayerDirection.LengthSquared() > 0)
            {
                mouseState = new MouseState(mouseState.X, mouseState.Y, mouseState.ScrollWheelValue, mouseState.LeftButton, ButtonState.Released, mouseState.MiddleButton, mouseState.XButton1, mouseState.XButton2);
            }


            // Get world position from mouse for bed/chest interactions
            Vector2 worldPos = camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
            int tileX = (int)(worldPos.X / World.World.TILE_SIZE);
            int tileY = (int)(worldPos.Y / World.World.TILE_SIZE);

            // FIX for NullReferenceException: Ensure world is not null before accessing its methods
            Tile clickedTile = null;
            if (world != null)
            {
                clickedTile = world.GetTile(tileX, tileY);
            }

            // NEW: Handle right-click to open chests, use Recall Potion, or set bed spawn
            if (mouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released)
            {
                // Check if player is using Recall Potion from hotbar
                var selectedSlot = miningSystem.GetSelectedHotbarSlot();
                var slot = inventory.GetSlot(selectedSlot);

                if (slot != null && slot.ItemType == ItemType.RecallPotion && slot.Count > 0)
                {
                    // Use Recall Potion - teleport to spawn
                    player.TeleportToSpawn();
                    slot.Count--;
                    if (slot.Count <= 0)
                    {
                        slot.ItemType = ItemType.None;
                        slot.Count = 0;
                    }
                    Logger.Log("[GAME] Used Recall Potion - teleported to spawn!");
                }
                // Check if player is using a bucket
                else if (slot != null && (slot.ItemType == ItemType.EmptyBucket || slot.ItemType == ItemType.WaterBucket || slot.ItemType == ItemType.LavaBucket))
                {
                    HandleBucketUse(tileX, tileY, clickedTile, slot);
                }
                // Check if clicking on a bed - SET SPAWN ONLY (don't start sleep yet)
                else if (clickedTile != null && clickedTile.Type == TileType.Bed)
                {
                    Point bedTilePos = new Point(tileX, tileY);
                    Vector2 bedSpawnPos = new Vector2(
                        bedTilePos.X * World.World.TILE_SIZE,
                        (bedTilePos.Y - 2) * World.World.TILE_SIZE
                    );
                    player.SetBedSpawn(bedSpawnPos);
                    // Don't start sleeping yet - need to hold the button
                }
                // Check if clicking on a chest
                else if (clickedTile != null && (clickedTile.Type == TileType.WoodChest ||
                    clickedTile.Type == TileType.SilverChest ||
                    clickedTile.Type == TileType.MagicChest))
                {
                    HandleChestInteraction(mouseState);
                }
            }

            // NEW: Handle HOLDING right-click on bed to sleep (must hold for 1.5+ seconds)
            if (mouseState.RightButton == ButtonState.Pressed && clickedTile != null && clickedTile.Type == TileType.Bed)
            {
                Point bedTilePos = new Point(tileX, tileY);
                Point playerTilePos = new Point((int)(playerCenter.X / World.World.TILE_SIZE), (int)(playerCenter.Y / World.World.TILE_SIZE));

                // Check if player is near/on the bed's tile
                if (playerTilePos.X >= bedTilePos.X - 1 && playerTilePos.X <= bedTilePos.X + 1 &&
                    playerTilePos.Y >= bedTilePos.Y - 2 && playerTilePos.Y <= bedTilePos.Y)
                {
                    // Accumulate hold time
                    bedHoldTime += deltaTime;

                    // Start sleeping only after holding for BED_HOLD_REQUIRED seconds
                    if (bedHoldTime >= BED_HOLD_REQUIRED && !isSleeping)
                    {
                        isSleeping = true;
                        currentBedPosition = bedTilePos;
                        sleepProgress = 0f;
                        Logger.Log($"[GAME] Starting to sleep in bed (held right-click for {bedHoldTime:F1}s).");
                    }
                }
                else
                {
                    // Player moved away - reset hold time
                    bedHoldTime = 0f;
                }
            }
            else
            {
                // Released right-click or not on bed - reset hold time
                bedHoldTime = 0f;
            }

            world.UpdateLoadedChunks(camera);
            world.Update(deltaTime, worldGenerator);
            world.MarkAreaAsExplored(playerCenter);

            miningSystem.Update(
                gameTime,
                playerCenter,
                camera,
                player.Position,
                Claude4_5Terraria.Player.Player.PLAYER_WIDTH,
                Claude4_5Terraria.Player.Player.PLAYER_HEIGHT,
                isAutoMiningActive,
                lastPlayerDirection
            );

            // NEW: Update combat system
            if (combatSystem != null)
            {
                // Get current held item
                var selectedSlot = miningSystem.GetSelectedHotbarSlot();
                var slot = inventory.GetSlot(selectedSlot);
                ItemType heldItem = slot != null ? slot.ItemType : ItemType.None;

                // Check if holding any sword
                bool isSword = heldItem == ItemType.WoodSword || heldItem == ItemType.CopperSword ||
                               heldItem == ItemType.IronSword || heldItem == ItemType.SilverSword ||
                               heldItem == ItemType.GoldSword || heldItem == ItemType.PlatinumSword ||
                               heldItem == ItemType.RunicSword;

                // Only allow sword attacks when holding a sword
                if (isSword)
                {
                    combatSystem.Update(deltaTime, player.Position, player.GetFacingRight(), mouseState, previousMouseState, heldItem);
                }
            }

            // NEW: Update Magic System and Projectiles
            if (magicSystem != null && projectileSystem != null)
            {
                var selectedSlot = miningSystem.GetSelectedHotbarSlot();
                var slot = inventory.GetSlot(selectedSlot);
                ItemType heldItem = slot != null ? slot.ItemType : ItemType.None;

                // Update Magic System (handles mana regen and cast input if wand is held)
                magicSystem.Update(deltaTime, mouseState, previousMouseState, heldItem);

                // Update Projectile System (moves, checks collisions)
                projectileSystem.Update(deltaTime, enemySpawner.GetActiveEnemies());
            }

            // NEW: Update enemy spawner and handle combat/collisions
            if (enemySpawner != null)
            {
                Logger.Log($"[UPDATE] EnemySpawner is updating.");
                Rectangle cameraView = camera.GetVisibleArea(World.World.TILE_SIZE);
                enemySpawner.Update(deltaTime, playerCenter, timeSystem, cameraView);

                // CRITICAL NULL CHECK: Ensure combat system exists before calling its methods
                if (combatSystem != null)
                {
                    // Check combat collisions (sword hitting enemies)
                    var selectedSlot = miningSystem.GetSelectedHotbarSlot();
                    var slot = inventory.GetSlot(selectedSlot);
                    ItemType heldWeapon = slot != null ? slot.ItemType : ItemType.None;
                    enemySpawner.CheckCombatCollisions(combatSystem, player.Position,
                        Claude4_5Terraria.Player.Player.PLAYER_WIDTH,
                        Claude4_5Terraria.Player.Player.PLAYER_HEIGHT,
                        heldWeapon, inventory);
                }

                // Check player collisions (enemies touching player)
                enemySpawner.CheckPlayerCollisions(player.Position,
                    Claude4_5Terraria.Player.Player.PLAYER_WIDTH,
                    Claude4_5Terraria.Player.Player.PLAYER_HEIGHT,
                    player);
            }

            if (hud != null)
            {
                hud.UpdateMinimapData(world);
            }

            inventoryUI.Update(gameTime, player.Position, world, GraphicsDevice.Viewport.Height);

            previousKeyboardState = keyboardState;
            previousMouseState = mouseState;

            base.Update(gameTime);
        }

        // FIX: The SetTile call now needs to reference the LiquidSystem to trigger flow
        private void HandleBucketUse(int tileX, int tileY, Tile clickedTile, InventorySlot slot)
        {
            if (slot == null) return;

            // Empty bucket: try to pick up liquid
            if (slot.ItemType == ItemType.EmptyBucket)
            {
                if (clickedTile != null && clickedTile.IsActive)
                {
                    if (clickedTile.Type == TileType.Water)
                    {
                        // Pick up water
                        world.SetTile(tileX, tileY, new Tile(TileType.Air));
                        slot.ItemType = ItemType.WaterBucket;
                        Logger.Log("[GAME] Filled bucket with water!");
                        liquidSystem.TriggerLocalFlow(tileX, tileY); // Trigger flow after removal
                    }
                    else if (clickedTile.Type == TileType.Lava)
                    {
                        // Pick up lava
                        world.SetTile(tileX, tileY, new Tile(TileType.Air));
                        slot.ItemType = ItemType.LavaBucket;
                        Logger.Log("[GAME] Filled bucket with lava!");
                        liquidSystem.TriggerLocalFlow(tileX, tileY); // Trigger flow after removal
                    }
                }
            }
            // Water bucket: place water
            else if (slot.ItemType == ItemType.WaterBucket)
            {
                if (clickedTile == null || !clickedTile.IsActive)
                {
                    // Place water
                    world.SetTile(tileX, tileY, new Tile(TileType.Water));
                    slot.ItemType = ItemType.EmptyBucket;
                    Logger.Log("[GAME] Placed water!");
                    liquidSystem.TriggerLocalFlow(tileX, tileY); // Trigger flow after placement
                }
            }
            // Lava bucket: place lava
            else if (slot.ItemType == ItemType.LavaBucket)
            {
                if (clickedTile == null || !clickedTile.IsActive)
                {
                    // Place lava
                    world.SetTile(tileX, tileY, new Tile(TileType.Lava));
                    slot.ItemType = ItemType.EmptyBucket;
                    Logger.Log("[GAME] Placed lava!");
                    liquidSystem.TriggerLocalFlow(tileX, tileY); // Trigger flow after placement
                }
            }
        }

        private void HandleChestMined(Point position, TileType tileType)
        {
            if (chestSystem != null)
            {
                // Remove chest and give items to player
                chestSystem.RemoveChest(position, inventory);
                world.SetTile(position.X, position.Y, new Tile(TileType.Air));
                Logger.Log($"[GAME] Chest mined at ({position.X}, {position.Y})");
                liquidSystem.TriggerLocalFlow(position.X, position.Y); // Trigger flow after mining
            }
        }

        // UPDATED: Handle chest placement callback to include custom name
        private void HandleChestPlaced(Point position, ItemType itemType)
        {
            if (chestSystem != null)
            {
                ChestTier tier = ChestTier.Wood;
                if (itemType == ItemType.SilverChest) tier = ChestTier.Silver;
                else if (itemType == ItemType.MagicChest) tier = ChestTier.Magic;

                // NEW: Custom name for player-placed chests
                string customName = "Player's " + tier.ToString() + " Chest";

                // UPDATED: Pass the custom name to PlaceChest
                chestSystem.PlaceChest(position, tier, false, customName);
                Logger.Log($"[GAME] Placed {tier} chest named '{customName}' at ({position.X}, {position.Y})");
            }
        }

        private void HandleChestInteraction(MouseState mouseState)
        {
            // Get world position from mouse position
            Vector2 worldPos = camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
            int tileX = (int)(worldPos.X / World.World.TILE_SIZE);
            int tileY = (int)(worldPos.Y / World.World.TILE_SIZE);

            // Check if clicked tile is a chest
            var tile = world.GetTile(tileX, tileY);
            if (tile != null && (tile.Type == TileType.WoodChest ||
                tile.Type == TileType.SilverChest ||
                tile.Type == TileType.MagicChest))
            {
                Point tilePos = new Point(tileX, tileY);
                Chest chest = chestSystem.GetChest(tilePos);
                if (chest != null && chestUI != null)
                {
                    chestUI.OpenChest(chest, inventory);
                    Logger.Log($"[GAME] Opened {chest.Tier} chest named '{chest.Name}' at ({tileX}, {tileY})");
                }
            }
        }

        private void OpenSaveMenu()
        {
            if (saveMenu != null)
            {
                saveMenu.Open();
            }
        }

        private void LoadTileSprites(World.World world)
        {
            var spriteMap = new System.Collections.Generic.Dictionary<string, Enums.TileType>
            {
                { "dirt", Enums.TileType.Dirt },
                { "grass", Enums.TileType.Grass },
                { "stone", Enums.TileType.Stone },
                { "coalblock", Enums.TileType.Coal },
                { "copperblock", Enums.TileType.Copper },
                { "ironblock", Enums.TileType.Iron },
                { "silverblock", Enums.TileType.Silver },
                { "goldblock", Enums.TileType.Gold },
                { "platinumblock", Enums.TileType.Platinum },
                { "torch", Enums.TileType.Torch },
                { "saplingplanteddirt", Enums.TileType.Sapling },
                { "woodcraftingtable", Enums.TileType.WoodCraftingBench },
                { "coppercraftingtable", Enums.TileType.CopperCraftingBench },
                { "woodchest", Enums.TileType.WoodChest },
                { "silverchest", Enums.TileType.SilverChest },
                { "magicchest", Enums.TileType.MagicChest }
            };

            foreach (var sprite in spriteMap)
            {
                try
                {
                    Texture2D texture = Content.Load<Texture2D>(sprite.Key);
                    world.LoadTileSprite(sprite.Value, texture);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[GAME] Could not load {sprite.Key} sprite: {ex.Message}");
                }
            }
        }

        private void LoadItemSprites(InventoryUI inventoryUI)
        {
            var spriteMap = new System.Collections.Generic.Dictionary<string, Enums.ItemType>
            {
                // Tools - Pickaxes
                { "woodpickaxe", Enums.ItemType.WoodPickaxe },
                { "stonepickaxe", Enums.ItemType.StonePickaxe },
                { "copperpickaxe", Enums.ItemType.CopperPickaxe },
                { "ironpickaxe", Enums.ItemType.IronPickaxe },
                { "silverpickaxe", Enums.ItemType.SilverPickaxe },
                { "goldpickaxe", Enums.ItemType.GoldPickaxe },
                { "platinumpickaxe", Enums.ItemType.PlatinumPickaxe },
                { "runicpickaxe", Enums.ItemType.RunicPickaxe },
                
                // Placeable
                { "woodcraftingtable", Enums.ItemType.WoodCraftingBench },
                { "coppercraftingtable", Enums.ItemType.CopperCraftingBench },
                { "woodchest", Enums.ItemType.WoodChest },
                { "silverchest", Enums.ItemType.SilverChest },
                { "magicchest", Enums.ItemType.MagicChest },
                
                // Ores
                { "copperore", Enums.ItemType.Copper },
                { "ironore", Enums.ItemType.Iron },
                { "silverore", Enums.ItemType.Silver },
                { "goldore", Enums.ItemType.Gold },
                { "platinumore", Enums.ItemType.Platinum },
                { "coal", Enums.ItemType.Coal },
                
                // Bars
                { "copperbar", Enums.ItemType.CopperBar },
                { "ironbar", Enums.ItemType.IronBar },
                { "silverbar", Enums.ItemType.SilverBar },
                { "goldbar", Enums.ItemType.GoldBar },
                { "platinumbar", Enums.ItemType.PlatinumBar },
                
                // Other items
                { "torch", Enums.ItemType.Torch },
                { "acorn", Enums.ItemType.Acorn },
                { "stick", Enums.ItemType.Stick },
                { "dirt", Enums.ItemType.Dirt },
                { "stone", Enums.ItemType.Stone },
                { "grass", Enums.ItemType.Grass },
                
                // Weapons - Swords
                { "woodsword", Enums.ItemType.WoodSword },
                { "CopperSword", Enums.ItemType.CopperSword },
                { "IronSword", Enums.ItemType.IronSword },
                { "SilverSword", Enums.ItemType.SilverSword },
                { "GoldSword", Enums.ItemType.GoldSword },
                { "PlatinumSword", Enums.ItemType.PlatinumSword },
                // RunicSword will be handled separately due to spritesheet
                
                // Weapons - Wands
                { "wand", Enums.ItemType.WoodWand },
                { "FireWand", Enums.ItemType.FireWand },
                { "LightningWand", Enums.ItemType.LightningWand },
                { "NatureWand", Enums.ItemType.NatureWand },
                { "WaterWand", Enums.ItemType.WaterWand },
                { "HalfMoonWand", Enums.ItemType.HalfMoonWand },
                // RunicLaserWand will be handled separately due to spritesheet
                
                // Weapons - Staff
                { "WoodSummonStaff", Enums.ItemType.WoodSummonStaff }
            };

            foreach (var sprite in spriteMap)
            {
                try
                {
                    Texture2D texture = Content.Load<Texture2D>(sprite.Key);
                    inventoryUI.LoadItemSprite(sprite.Value, texture);
                    itemTextureMap[sprite.Value] = texture;
                }
                catch (Exception ex)
                {
                    Logger.Log($"[GAME] Could not load {sprite.Key} item sprite: {ex.Message}");
                }
            }
            
            // NEW: Load RunicSword spritesheet (frame 0 for inventory display)
            try
            {
                Texture2D runicSwordSpriteSheet = Content.Load<Texture2D>("RunicSword Spritesheet");
                inventoryUI.LoadItemSprite(Enums.ItemType.RunicSword, runicSwordSpriteSheet);
                itemTextureMap[Enums.ItemType.RunicSword] = runicSwordSpriteSheet;
                Logger.Log("[GAME] Loaded RunicSword spritesheet successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"[GAME] Could not load RunicSword spritesheet: {ex.Message}");
            }
            
            // NEW: Load RunicLaserWand spritesheet (frame 0 for inventory display)
            try
            {
                Texture2D runicLaserWandSpriteSheet = Content.Load<Texture2D>("RunicLaserWandSpriteSheet");
                inventoryUI.LoadItemSprite(Enums.ItemType.RunicLaserWand, runicLaserWandSpriteSheet);
                itemTextureMap[Enums.ItemType.RunicLaserWand] = runicLaserWandSpriteSheet;
                Logger.Log("[GAME] Loaded RunicLaserWand spritesheet successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"[GAME] Could not load RunicLaserWand spritesheet: {ex.Message}");
            }
        }

        private void LoadSpellTextures(ProjectileSystem projectileSystem)
        {
            var spriteMap = new System.Collections.Generic.Dictionary<string, ProjectileType>
            {
                { "MagicBolt", ProjectileType.MagicBolt },
                { "FireBolt", ProjectileType.FireBolt },
                { "LightningSpell", ProjectileType.LightningBlast },
                { "NatureVineSpell", ProjectileType.NatureVine },
                { "WaterBubbleSpell", ProjectileType.WaterBubble },
                { "HalfMoonSpell", ProjectileType.HalfMoonSlash },
                { "RunicLaserBeamSpell", ProjectileType.RunicLaser }
            };

            foreach (var sprite in spriteMap)
            {
                try
                {
                    Texture2D texture = Content.Load<Texture2D>(sprite.Key);
                    projectileSystem.LoadTexture(sprite.Value, texture);
                    Logger.Log($"[GAME] Loaded {sprite.Key} spell texture successfully");
                }
                catch (Exception ex)
                {
                    Logger.Log($"[GAME] Could not load {sprite.Key} spell sprite: {ex.Message}");
                }
            }
        }

        private void LoadCraftingItemSprites(InventoryUI inventoryUI)
        {
            var spriteMap = new System.Collections.Generic.Dictionary<string, Enums.ItemType>
            {
                { "woodpickaxe", Enums.ItemType.WoodPickaxe },
                { "stonepickaxe", Enums.ItemType.StonePickaxe },
                { "copperpickaxe", Enums.ItemType.CopperPickaxe },
                { "ironpickaxe", Enums.ItemType.IronPickaxe },
                { "silverpickaxe", Enums.ItemType.SilverPickaxe },
                { "goldpickaxe", Enums.ItemType.GoldPickaxe },
                { "platinumpickaxe", Enums.ItemType.PlatinumPickaxe },
                { "runicpickaxe", Enums.ItemType.RunicPickaxe },
                { "woodcraftingtable", Enums.ItemType.WoodCraftingBench },
                { "coppercraftingtable", Enums.ItemType.CopperCraftingBench },
                { "woodchest", Enums.ItemType.WoodChest },
                { "silverchest", Enums.ItemType.SilverChest },
                { "magicchest", Enums.ItemType.MagicChest },
                { "copperore", Enums.ItemType.Copper },
                { "ironore", Enums.ItemType.Iron },
                { "silverore", Enums.ItemType.Silver },
                { "goldore", Enums.ItemType.Gold },
                { "platinumore", Enums.ItemType.Platinum },
                { "coal", Enums.ItemType.Coal },
                { "copperbar", Enums.ItemType.CopperBar },
                { "ironbar", Enums.ItemType.IronBar },
                { "silverbar", Enums.ItemType.SilverBar },
                { "goldbar", Enums.ItemType.GoldBar },
                { "platinumbar", Enums.ItemType.PlatinumBar },
                { "torch", Enums.ItemType.Torch },
                { "acorn", Enums.ItemType.Acorn },
                { "stick", Enums.ItemType.Stick },
                { "dirt", Enums.ItemType.Dirt },
                { "stone", Enums.ItemType.Stone },
                { "grass", Enums.ItemType.Grass }
            };

            foreach (var sprite in spriteMap)
            {
                try
                {
                    Texture2D texture = Content.Load<Texture2D>(sprite.Key);
                    inventoryUI.GetCraftingUI()?.LoadItemSprite(sprite.Value, texture);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[GAME] Could not load {sprite.Key} crafting sprite: {ex.Message}");
                }
            }
        }

        private void ToggleFullscreen()
        {
            graphics.IsFullScreen = !graphics.IsFullScreen;

            if (graphics.IsFullScreen)
            {
                graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }
            else
            {
                graphics.PreferredBackBufferWidth = 1920;
                graphics.PreferredBackBufferHeight = 1080;
            }

            graphics.ApplyChanges();
            Logger.Log($"[GRAPHICS] Fullscreen {(graphics.IsFullScreen ? "enabled" : "disabled")}");
        }

        private void DropSelectedItem()
        {
            if (inventory == null || miningSystem == null || player == null)
            {
                Logger.Log("[DROP] Cannot drop - systems not initialized");
                return;
            }

            int selectedSlot = miningSystem.GetSelectedHotbarSlot();
            var slot = inventory.GetSlot(selectedSlot);

            if (slot == null)
            {
                Logger.Log($"[DROP] Slot {selectedSlot} is null");
                return;
            }

            if (slot.IsEmpty())
            {
                Logger.Log($"[DROP] Slot {selectedSlot} is empty");
                return;
            }

            // Drop item in front of player with some velocity
            Vector2 dropPosition = new Vector2(
                player.Position.X + Claude4_5Terraria.Player.Player.PLAYER_WIDTH / 2,
                player.Position.Y + Claude4_5Terraria.Player.Player.PLAYER_HEIGHT / 2
            );

            // Add offset in direction player is facing (throw it forward)
            dropPosition.X += 64;
            dropPosition.Y -= 32;

            Logger.Log($"[DROP] Dropping {slot.ItemType} from slot {selectedSlot} at {dropPosition}");
            miningSystem.DropItem(dropPosition, slot.ItemType, 1);
            slot.Count--;
            if (slot.Count <= 0)
            {
                slot.ItemType = ItemType.None;
                slot.Count = 0;
            }
            Logger.Log($"[DROP] Successfully dropped {slot.ItemType}. Remaining: {slot.Count}");
        }

        private void QuitToMenu()
        {
            Logger.Log("[GAME] Quitting to main menu");
            worldGenerated = false;
            world = null;
            player = null;
            camera = null;
            inventory = null;
            miningSystem = null;
            lightingSystem = null;
            timeSystem = null;
            worldGenerator = null;
            chestSystem = null;
            inventoryUI = null;
            pauseMenu = null;
            saveMenu = null;
            miningOverlay = null;
            hud = null;
            chestUI = null;
            liquidSystem = null; // NEW
            isSleeping = false; // Reset sleeping state
            startMenu.SetState(MenuState.MainMenu);
            Logger.Log("[GAME] Returned to main menu");
        }

        private void SaveGame(int slotIndex)
        {
            Logger.Log($"[GAME] Saving game - Player position before save: {player.Position}");

            SaveData data = new SaveData
            {
                SaveName = $"Starshroud Save {slotIndex + 1}",
                WorldSeed = currentWorldSeed,
                PlayerPosition = player.Position,
                GameTime = timeSystem.GetCurrentTime(),
                WorldWidth = World.World.WORLD_WIDTH,
                WorldHeight = World.World.WORLD_HEIGHT,
                PlayTimeSeconds = (int)totalPlayTime,
                TileChanges = world.GetModifiedTiles(),
                Chests = chestSystem.GetSaveData()
            };

            Logger.Log($"[GAME] SaveData.PlayerPosition = {data.PlayerPosition}");
            Logger.Log($"[GAME] Saving {data.Chests.Count} chests");

            for (int i = 0; i < inventory.GetSlotCount(); i++)
            {
                var slot = inventory.GetSlot(i);
                if (slot != null && !slot.IsEmpty())
                {
                    data.InventorySlots.Add(new InventorySlotData
                    {
                        ItemType = (int)slot.ItemType,
                        Count = slot.Count
                    });
                }
                else
                {
                    data.InventorySlots.Add(new InventorySlotData
                    {
                        ItemType = 0,
                        Count = 0
                    });
                }
            }

            SaveSystem.SaveGame(data, slotIndex);
            Logger.Log($"[GAME] Game saved to slot {slotIndex + 1}");
        }

        private void LoadGameFromSave(SaveData data)
        {
            currentWorldSeed = data.WorldSeed;
            totalPlayTime = data.PlayTimeSeconds;
            hud = new HUD();
            // FIX: HUD.Initialize now requires SpriteFont
            hud.Initialize(GraphicsDevice, font);
            world = new World.World(hud);
            timeSystem = new TimeSystem();
            timeSystem.SetCurrentTime(data.GameTime);
            lightingSystem = new LightingSystem(world, timeSystem);
            chestSystem = new ChestSystem();
            liquidSystem = new LiquidSystem(world); // NEW: Initialize liquid system
            LoadTileSprites(world);
            worldGenerator = new WorldGenerator(world, data.WorldSeed, chestSystem);
            worldGenerator.OnProgressUpdate = (progress, message) =>
            {
                startMenu.SetLoadingProgress(progress, message);
            };
            worldGenerator.Generate();
            world.EnableChunkUnloading();
            world.EnableTileChangeTracking();
            player = new Claude4_5Terraria.Player.Player(world, data.PlayerPosition);
            world.SetPlayer(player); // Set player reference for World/HUD/Bed logic

            // Enable debug mode if toggled in start menu
            if (startMenu.IsDebugModeEnabled)
            {
                player.SetDebugMode(true);
            }
            // FIX: World now needs LiquidSystem reference for SetTile/TriggerLiquidSpreadCheck
            world.SetLiquidSystem(liquidSystem);

            // CRITICAL FIX: Clear all generated liquids before applying save changes
            // This prevents duplication of water from world generation
            Logger.Log("[GAME] Clearing generated liquids before applying save...");
            for (int x = 0; x < World.World.WORLD_WIDTH; x++)
            {
                for (int y = 0; y < World.World.WORLD_HEIGHT; y++)
                {
                    var tile = world.GetTile(x, y);
                    if (tile != null && (tile.Type == TileType.Water || tile.Type == TileType.Lava))
                    {
                        tile.LiquidVolume = 0f;
                        tile.Type = TileType.Air;
                    }
                }
            }

            world.ApplyTileChanges(data.TileChanges);
            chestSystem.LoadFromData(data.Chests);
            Logger.Log($"[GAME] Loaded {chestSystem.GetChestCount()} chests from save");

            // NEW: Activate all loaded liquid tiles for flow simulation
            liquidSystem.ActivateAllLiquids();

            world.DisableWorldUpdates();
            camera = new Camera(GraphicsDevice.Viewport);
            camera.Position = player.Position;
            inventory = new Inventory();
            for (int i = 0; i < data.InventorySlots.Count && i < inventory.GetSlotCount(); i++)
            {
                var slotData = data.InventorySlots[i];
                var slot = inventory.GetSlot(i);
                if (slot != null && slotData.Count > 0)
                {
                    slot.ItemType = (Enums.ItemType)slotData.ItemType;
                    slot.Count = slotData.Count;
                }
            }
            miningSystem = new MiningSystem(world, inventory, mineDirtSound, mineStoneSound, mineTorchSound, placeTorchSound, gameSoundsVolume);

            miningSystem.OnChestMined += HandleChestMined;
            miningSystem.OnChestPlaced += HandleChestPlaced;

            combatSystem = new CombatSystem(); // NEW: Initialize combat system
            enemySpawner = new EnemySpawner(world); // NEW: Initialize enemy spawner

            // NEW: Initialize Projectile and Magic Systems
            projectileSystem = new ProjectileSystem(world);
            LoadSpellTextures(projectileSystem); // Load spell sprites
            // FIX: Pass world and camera to MagicSystem constructor
            magicSystem = new MagicSystem(player, projectileSystem, world, camera);

            inventoryUI = new InventoryUI(inventory, miningSystem);
            inventoryUI.Initialize(pixelTexture, font);
            LoadItemSprites(inventoryUI);
            LoadCraftingItemSprites(inventoryUI);
            chestUI = new ChestUI(world); // UPDATED: Pass world reference
            pauseMenu = new PauseMenu(OpenSaveMenu, QuitToMenu,
                                      (newVolume) => { musicVolume = newVolume; if (!isMusicMuted) MediaPlayer.Volume = musicVolume; }, musicVolume,
                                      ToggleFullscreen, hud.ToggleFullscreenMap, hud,
                                      ToggleAutoMining, isAutoMiningActive,
                                      SetGameSoundsVolume, gameSoundsVolume);
            saveMenu = new SaveMenu(SaveGame);
            miningOverlay = new MiningOverlay(world, miningSystem, chestSystem);
            worldGenerated = true;
            Logger.Log("[GAME] Game loaded successfully");
        }

        private void GenerateWorld()
        {
            if (startMenu.IsLoadingSavedGame())
            {
                Logger.Log("[GAME] ===== LOADING SAVED GAME =====");
                int slotIndex = startMenu.GetLoadingSlotIndex();
                Logger.Log($"[GAME] Attempting to load from slot: {slotIndex}");
                SaveData saveData = SaveSystem.LoadGame(slotIndex);
                if (saveData != null)
                {
                    LoadGameFromSave(saveData);
                    return;
                }
                else
                {
                    Logger.Log("[GAME] ERROR: Failed to load save data");
                    Logger.Log("[GAME] Starting new game instead");
                }
            }
            else
            {
                Logger.Log("[GAME] ===== STARTING NEW GAME =====");
            }

            hud = new HUD();
            // FIX: HUD.Initialize now requires SpriteFont
            hud.Initialize(GraphicsDevice, font);
            currentWorldSeed = System.Environment.TickCount;
            totalPlayTime = 0f;
            world = new World.World(hud);
            timeSystem = new TimeSystem();
            lightingSystem = new LightingSystem(world, timeSystem);
            chestSystem = new ChestSystem();
            liquidSystem = new LiquidSystem(world); // NEW: Initialize liquid system
            LoadTileSprites(world);
            worldGenerator = new WorldGenerator(world, currentWorldSeed, chestSystem);
            worldGenerator.OnProgressUpdate = (progress, message) =>
            {
                startMenu.SetLoadingProgress(progress, message);
            };
            worldGenerator.Generate();
            world.EnableChunkUnloading();
            world.EnableTileChangeTracking();
            Vector2 spawnPosition = worldGenerator.GetSpawnPosition(64);
            player = new Claude4_5Terraria.Player.Player(world, spawnPosition);
            world.SetPlayer(player); // Set player reference for World/HUD/Bed logic

            // Enable debug mode if toggled in start menu
            if (startMenu.IsDebugModeEnabled)
            {
                player.SetDebugMode(true);
            }
            // FIX: World now needs LiquidSystem reference for SetTile/TriggerLocalFlow
            world.SetLiquidSystem(liquidSystem);
            camera = new Camera(GraphicsDevice.Viewport);
            camera.Position = player.Position;
            inventory = new Inventory();

            // NEW: Give player starting items for testing
            inventory.AddItem(ItemType.WoodSword, 1);
            inventory.AddItem(ItemType.CopperSword, 1);
            inventory.AddItem(ItemType.IronSword, 1);
            inventory.AddItem(ItemType.SilverSword, 1);
            inventory.AddItem(ItemType.GoldSword, 1);
            inventory.AddItem(ItemType.PlatinumSword, 1);
            inventory.AddItem(ItemType.RunicSword, 1); // Added!
            inventory.AddItem(ItemType.WoodWand, 1);
            inventory.AddItem(ItemType.FireWand, 1);
            inventory.AddItem(ItemType.LightningWand, 1);
            inventory.AddItem(ItemType.NatureWand, 1);
            inventory.AddItem(ItemType.WaterWand, 1);
            inventory.AddItem(ItemType.HalfMoonWand, 1);
            inventory.AddItem(ItemType.RunicLaserWand, 1);
            Logger.Log("[GAME] Added all weapons to starting inventory for testing");

            miningSystem = new MiningSystem(world, inventory, mineDirtSound, mineStoneSound, mineTorchSound, placeTorchSound, gameSoundsVolume);

            miningSystem.OnChestMined += HandleChestMined;
            miningSystem.OnChestPlaced += HandleChestPlaced;

            combatSystem = new CombatSystem(); // NEW: Initialize combat system
            enemySpawner = new EnemySpawner(world); // NEW: Initialize enemy spawner

            // NEW: Initialize Projectile and Magic Systems
            projectileSystem = new ProjectileSystem(world);
            LoadSpellTextures(projectileSystem); // Load spell sprites
            // FIX: Pass world and camera to MagicSystem constructor
            magicSystem = new MagicSystem(player, projectileSystem, world, camera);

            inventoryUI = new InventoryUI(inventory, miningSystem);
            inventoryUI.Initialize(pixelTexture, font);
            LoadItemSprites(inventoryUI);
            LoadCraftingItemSprites(inventoryUI);
            chestUI = new ChestUI(world); // UPDATED: Pass world reference
            pauseMenu = new PauseMenu(OpenSaveMenu, QuitToMenu,
                                      (newVolume) => { musicVolume = newVolume; if (!isMusicMuted) MediaPlayer.Volume = musicVolume; }, musicVolume,
                                      ToggleFullscreen, hud.ToggleFullscreenMap, hud,
                                      ToggleAutoMining, isAutoMiningActive,
                                      SetGameSoundsVolume, gameSoundsVolume);
            saveMenu = new SaveMenu(SaveGame);
            miningOverlay = new MiningOverlay(world, miningSystem, chestSystem);
            worldGenerated = true;
            Logger.Log($"[GAME] World generated with {chestSystem.GetChestCount()} chests");
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(
                    sortMode: SpriteSortMode.Deferred,
                    blendState: BlendState.AlphaBlend,
                    samplerState: SamplerState.PointClamp
                );

            if (startMenu.GetState() != MenuState.Playing)
            {
                startMenu.Draw(spriteBatch, pixelTexture, font,
                    GraphicsDevice.Viewport.Width,
                    GraphicsDevice.Viewport.Height);
                spriteBatch.End();
                base.Draw(gameTime);
                return;
            }

            spriteBatch.End();

            Color skyColor = timeSystem != null ? timeSystem.GetSkyColor() : Color.Black;
            GraphicsDevice.Clear(skyColor);

            Matrix transformMatrix = camera.GetTransformMatrix();

            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp,
                transformMatrix: transformMatrix
            );

            // FIX: World.Draw signature is now correct - pass debug mode status
            bool playerDebugMode = player != null && player.IsDebugModeActive();
            world.Draw(spriteBatch, camera, pixelTexture, lightingSystem, miningSystem, playerDebugMode);

            miningSystem.DrawItems(spriteBatch, pixelTexture);

            // NEW: Draw projectiles (before enemies/player)
            if (projectileSystem != null)
            {
                projectileSystem.Draw(spriteBatch, pixelTexture);
            }

            // NEW: Draw enemies
            if (enemySpawner != null)
            {
                // DEBUG: Force a very visible draw if the texture failed
                if (oozeEnemyTexture == pixelTexture)
                {
                    Logger.Log("[DRAW] WARNING: Ooze texture failed to load! Drawing as large red block.");
                }

                enemySpawner.DrawEnemies(spriteBatch, oozeEnemyTexture, pixelTexture);
            }

            // 1. Determine currently held item
            ItemType heldItemType = inventory.GetSlot(miningSystem.GetSelectedHotbarSlot())?.ItemType ?? ItemType.None;
            Texture2D heldItemTexture = null;
            if (heldItemType != ItemType.None)
            {
                itemTextureMap.TryGetValue(heldItemType, out heldItemTexture);
            }

            // 2. Determine animation frame (mining or combat)
            int animationFrame = miningSystem.CurrentAnimationFrame;

            // Check if holding any sword and attacking - use combat animation
            bool isSword = heldItemType == ItemType.WoodSword || heldItemType == ItemType.CopperSword ||
                           heldItemType == ItemType.IronSword || heldItemType == ItemType.SilverSword ||
                           heldItemType == ItemType.GoldSword || heldItemType == ItemType.PlatinumSword ||
                           heldItemType == ItemType.RunicSword;
            
            if (isSword && combatSystem != null && combatSystem.IsAttacking())
            {
                animationFrame = combatSystem.CurrentAnimationFrame;
            }

            // 3. Pass data to Player Draw method
            player.Draw(
                spriteBatch,
                pixelTexture,
                heldItemType,
                heldItemTexture,
                animationFrame
            );

            // 3. Handle Torch Lighting and Lava Bucket Lighting
            if (heldItemType == ItemType.Torch || heldItemType == ItemType.LavaBucket)
            {
                // Activate and position the dynamic light source at the player's center
                lightingSystem.SetPlayerLight(player.GetCenterPosition(), true);
            }
            else
            {
                // Deactivate the dynamic light source
                lightingSystem.SetPlayerLight(Vector2.Zero, false);
            }

            Vector2 playerCenter = player.GetCenterPosition();
            miningOverlay.DrawBlockOutlines(spriteBatch, pixelTexture, camera, playerCenter, showMiningOutlines);
            miningOverlay.DrawMiningProgress(spriteBatch, pixelTexture);

            spriteBatch.End();

            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp
            );

            // Draw chest tooltip (in screen space, after world rendering)
            MouseState currentMouseState = Mouse.GetState();
            miningOverlay.DrawChestTooltip(spriteBatch, pixelTexture, font, camera, new Vector2(currentMouseState.X, currentMouseState.Y));

            // --- NEW: Draw Rain Particles ---
            if (timeSystem.IsRaining)
            {
                Color rainColor = new Color(30, 60, 120, 180); // Dark blue, semi-transparent
                foreach (Vector2 p in rainParticles)
                {
                    // Draw a longer thin line (1x8 pixel rain drop for faster movement)
                    spriteBatch.Draw(pixelTexture, p, null, rainColor, 0f, Vector2.Zero, new Vector2(1f, 8f), SpriteEffects.None, 0f);
                }
            }
            // --- END NEW ---

            // --- NEW: Draw Snow Particles ---
            if (snowParticles.Count > 0)
            {
                Color snowColor = new Color(255, 255, 255, 200); // White, semi-transparent
                foreach (Vector2 p in snowParticles)
                {
                    // Draw snowflakes as small 3x3 pixel squares
                    spriteBatch.Draw(pixelTexture, p, null, snowColor, 0f, Vector2.Zero, new Vector2(3f, 3f), SpriteEffects.None, 0f);
                }
            }
            // --- END NEW ---


            inventoryUI.Draw(
                spriteBatch,
                pixelTexture,
                font,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height
            );

            if (hud != null)
            {
                // UPDATED: Passed MagicSystem, TimeSystem, and all sleep/bed data to HUD.Draw 
                hud.Draw(spriteBatch, pixelTexture, font,
                    GraphicsDevice.Viewport.Width,
                    GraphicsDevice.Viewport.Height,
                    player.Position,
                    world,
                    isAutoMiningActive,
                    timeSystem.IsRaining,
                    player.Health,
                    player.MaxHealth,
                    player.AirBubbles,
                    player.MaxAirBubbles,
                    currentBedPosition,
                    isSleeping,
                    sleepProgress,
                    SLEEP_DURATION,
                    bedHoldTime,
                    BED_HOLD_REQUIRED,
                    timeSystem,
                    magicSystem // CRITICAL: Pass the Magic System object
                );
            }

            pauseMenu.Draw(spriteBatch, pixelTexture, font,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height);

            if (saveMenu != null)
            {
                saveMenu.Draw(spriteBatch, pixelTexture, font,
                    GraphicsDevice.Viewport.Width,
                    GraphicsDevice.Viewport.Height);
            }

            if (chestUI != null && chestUI.IsOpen)
            {
                // FIX: ChestUI.Draw signature is now correct
                chestUI.Draw(spriteBatch, pixelTexture, font);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}