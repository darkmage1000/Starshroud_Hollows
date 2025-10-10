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
        private SpriteFont font;
        private Texture2D menuBackgroundTexture;

        private Inventory inventory;
        private MiningSystem miningSystem;
        private LightingSystem lightingSystem;
        private TimeSystem timeSystem;
        private WorldGenerator worldGenerator;
        private ChestSystem chestSystem; // NEW: Chest system

        private InventoryUI inventoryUI;
        private PauseMenu pauseMenu;
        private MiningOverlay miningOverlay;
        private StartMenu startMenu;
        private SaveMenu saveMenu;
        private LoadMenu loadMenu;
        private HUD hud;
        private ChestUI chestUI; // NEW: Chest UI

        private KeyboardState previousKeyboardState;
        private MouseState previousMouseState; // NEW: Track mouse state

        private Song backgroundMusic;
        private float musicVolume = 0.1f;

        // NEW: Game Sounds Volume Field
        private float gameSoundsVolume = 0.5f;

        private bool isMusicMuted = false;
        private bool showMiningOutlines = false;

        private bool worldGenerated = false;
        private int currentWorldSeed;
        private float totalPlayTime;

        // NEW: Auto-Mining Control
        private bool isAutoMiningActive = false;
        private Vector2 lastPlayerDirection = Vector2.Zero; // Tracks movement direction for auto-mine

        // NEW: Dictionary to hold all loaded item textures for drawing
        private Dictionary<ItemType, Texture2D> itemTextureMap;

        // --- NEW RAIN SYSTEM FIELDS (Unchanged) ---
        private List<Vector2> rainParticles;
        private Random random;
        private const int MAX_RAIN_DROPS = 1000;
        private const float RAIN_SPEED = 800f;  // MUCH faster rain
        private const float RAIN_OFFSET_X = 500f;
        private const float RAIN_OFFSET_Y = 100f;
        // ------------------------------------------

        // --- NEW SOUND EFFECT FIELDS ---
        private SoundEffect mineDirtSound;
        private SoundEffect mineStoneSound;
        private SoundEffect mineTorchSound;
        private SoundEffect placeTorchSound;
        // -------------------------------


        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();

            random = new Random(); // Initialize Random
            rainParticles = new List<Vector2>(); // Initialize particle list
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
            previousMouseState = Mouse.GetState(); // NEW
            totalPlayTime = 0f;
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            DroppedItem.SetStaticContent(Content);

            pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            font = Content.Load<SpriteFont>("Font");
            menuBackgroundTexture = Content.Load<Texture2D>("MenuBackground");

            // Initialize item texture map
            itemTextureMap = new Dictionary<ItemType, Texture2D>();

            // NEW: Load Sound Effects (Mapping to Content file names)
            try
            {
                mineDirtSound = Content.Load<SoundEffect>("a_pickaxe_hitting_dirt");
                mineStoneSound = Content.Load<SoundEffect>("a_pickaxe_hitting_stone");
                SoundEffect woodSound = Content.Load<SoundEffect>("hitting a tree");  // Load wood sound separately
                mineTorchSound = woodSound;  // Use wood sound for torch
                placeTorchSound = Content.Load<SoundEffect>("placing and mining a torch");  // FIXED: spaces

                // CRITICAL AUDIO FIX: Create a SoundEffectInstance immediately after loading
                // This initializes the audio track resources, preventing late-game crashes/silence.
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
            // CRITICAL FIX FOR CS1729: StartMenu constructor now includes PlayTestSound
            startMenu = new StartMenu(musicVolume,
                                      (newVolume) => { musicVolume = newVolume; if (!isMusicMuted) MediaPlayer.Volume = musicVolume; },
                                      gameSoundsVolume,
                                      SetGameSoundsVolume,
                                      ToggleFullscreen,
                                      menuBackgroundTexture,
                                      PlayTestSound); // NEW: Pass the test sound action (7th argument)

            Logger.Log("[GAME] Content loaded successfully");
        }

        protected override void UnloadContent()
        {
            MediaPlayer.Stop();
            base.UnloadContent();
        }

        // NEW: Public method for PauseMenu/StartMenu to set sound volume
        public void SetGameSoundsVolume(float newVolume)
        {
            gameSoundsVolume = newVolume;
            // Update the mining system immediately if it exists
            // This ensures sound volume changes instantly even if the MiningSystem was created earlier.
            miningSystem?.SetSoundVolume(newVolume);
        }

        // NEW: Plays a sound based on the current SFX volume level (for volume testing)
        private void PlayTestSound()
        {
            if (mineDirtSound != null)
            {
                // Play the dirt sound at the current gameSoundsVolume level
                mineDirtSound.Play(volume: gameSoundsVolume, pitch: 0.0f, pan: 0.0f);
            }
        }

        // NEW: Public method for PauseMenu to toggle auto-mining state
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

        // NEW: Rain particle update logic (Unchanged from previous step)
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


        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState(); // NEW

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
                previousMouseState = mouseState; // NEW
                base.Update(gameTime);
                return;
            }

            if (startMenu.GetState() == MenuState.Loading)
            {
                if (worldGenerated)
                {
                    startMenu.SetState(MenuState.Playing);
                }

                previousKeyboardState = keyboardState;
                previousMouseState = mouseState; // NEW
                base.Update(gameTime);
                return;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

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

            // Check if game objects still exist (not quit to menu)
            if (timeSystem == null || player == null || world == null)
            {
                previousKeyboardState = keyboardState;
                previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            totalPlayTime += deltaTime;

            timeSystem.Update(deltaTime);

            // NEW: Update rain particles (must run after timeSystem update)
            UpdateRainParticles(deltaTime, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);


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

            // NEW AUTO-MINE TOGGLE: Press L to toggle auto-mining
            if (keyboardState.IsKeyDown(Keys.L) && !previousKeyboardState.IsKeyDown(Keys.L))
            {
                ToggleAutoMining();
            }

            // NEW: Press Q to drop currently selected item (only when inventory is CLOSED)
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

            // SINGLE DECLARATION OF playerCenter for the entire remaining scope
            Vector2 playerCenter = new Vector2(
                player.Position.X + Claude4_5Terraria.Player.Player.PLAYER_WIDTH / 2,
                player.Position.Y + Claude4_5Terraria.Player.Player.PLAYER_HEIGHT / 2
            );

            // NEW: Capture last player direction for auto-mine
            Vector2 currentMovement = Vector2.Zero;
            if (keyboardState.IsKeyDown(Keys.A)) currentMovement.X -= 1;
            if (keyboardState.IsKeyDown(Keys.D)) currentMovement.X += 1;
            if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Space)) currentMovement.Y -= 1; // Jumping or going up
            if (keyboardState.IsKeyDown(Keys.S)) currentMovement.Y += 1; // Going down (not used for jumping)

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


            // NEW: Handle right-click to open chests, use Recall Potion, or use bed
            if (mouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released)
            {
                // Get world position from mouse
                Vector2 worldPos = camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
                int tileX = (int)(worldPos.X / World.World.TILE_SIZE);
                int tileY = (int)(worldPos.Y / World.World.TILE_SIZE);
                var clickedTile = world.GetTile(tileX, tileY);
                
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
                // Check if clicking on a bed
                else if (clickedTile != null && clickedTile.Type == TileType.Bed)
                {
                    // Set bed as spawn and speed up time
                    Point bedTilePos = new Point(tileX, tileY);
                    Vector2 bedSpawnPos = new Vector2(
                        bedTilePos.X * World.World.TILE_SIZE,
                        (bedTilePos.Y - 2) * World.World.TILE_SIZE  // Spawn 2 tiles above bed
                    );
                    player.SetBedSpawn(bedSpawnPos);
                    
                    // Speed up time (advance to morning)
                    if (timeSystem != null)
                    {
                        timeSystem.AdvanceToMorning();
                        Logger.Log("[GAME] Slept in bed - time advanced to morning!");
                    }
                }
                // Check if clicking on a chest
                else if (clickedTile != null && (clickedTile.Type == TileType.WoodChest ||
                    clickedTile.Type == TileType.SilverChest ||
                    clickedTile.Type == TileType.MagicChest))
                {
                    HandleChestInteraction(mouseState);
                }
            }

            // NOTE: player.Update() and camera.Position update were moved up.

            world.UpdateLoadedChunks(camera);
            world.Update(deltaTime, worldGenerator);
            world.MarkAreaAsExplored(playerCenter);

            // Pass auto-mine state and direction to MiningSystem
            miningSystem.Update(
                gameTime,
                playerCenter,
                camera,
                player.Position,
                Claude4_5Terraria.Player.Player.PLAYER_WIDTH,
                Claude4_5Terraria.Player.Player.PLAYER_HEIGHT,
                isAutoMiningActive, // NEW PARAMETER
                lastPlayerDirection  // NEW PARAMETER
            );

            // Update minimap
            if (hud != null)
            {
                hud.UpdateMinimapData(world);
            }

            inventoryUI.Update(gameTime, player.Position, world, GraphicsDevice.Viewport.Height);

            previousKeyboardState = keyboardState;
            previousMouseState = mouseState;

            base.Update(gameTime);
        }

        // NEW: Handle chest mining callback
        private void HandleChestMined(Point position, TileType tileType)
        {
            if (chestSystem != null)
            {
                // Remove chest and give items to player
                chestSystem.RemoveChest(position, inventory);
                world.SetTile(position.X, position.Y, new Tile(TileType.Air));
                Logger.Log($"[GAME] Chest mined at ({position.X}, {position.Y})");
            }
        }

        // NEW: Handle bucket use
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
                    }
                    else if (clickedTile.Type == TileType.Lava)
                    {
                        // Pick up lava
                        world.SetTile(tileX, tileY, new Tile(TileType.Air));
                        slot.ItemType = ItemType.LavaBucket;
                        Logger.Log("[GAME] Filled bucket with lava!");
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
                }
            }
        }

        // NEW: Handle chest placement callback
        private void HandleChestPlaced(Point position, ItemType itemType)
        {
            if (chestSystem != null)
            {
                ChestTier tier = ChestTier.Wood;
                if (itemType == ItemType.SilverChest) tier = ChestTier.Silver;
                else if (itemType == ItemType.MagicChest) tier = ChestTier.Magic;

                chestSystem.PlaceChest(position, tier, false);
                Logger.Log($"[GAME] Placed {tier} chest at ({position.X}, {position.Y})");
            }
        }

        // NEW: Handle chest interaction
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
                    Logger.Log($"[GAME] Opened {chest.Tier} chest at ({tileX}, {tileY})");
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
                { "silverblock", Enums.TileType.Silver },
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
                { "woodpickaxe", Enums.ItemType.WoodPickaxe },
                { "stonepickaxe", Enums.ItemType.StonePickaxe },
                { "copperpickaxe", Enums.ItemType.CopperPickaxe },
                { "silverpickaxe", Enums.ItemType.SilverPickaxe },
                { "platinumpickaxe", Enums.ItemType.PlatinumPickaxe },
                { "runicpickaxe", Enums.ItemType.RunicPickaxe },
                { "woodcraftingtable", Enums.ItemType.WoodCraftingBench },
                { "coppercraftingtable", Enums.ItemType.CopperCraftingBench },
                { "woodchest", Enums.ItemType.WoodChest },
                { "silverchest", Enums.ItemType.SilverChest },
                { "magicchest", Enums.ItemType.MagicChest },
                { "copperore", Enums.ItemType.Copper },
                { "silverore", Enums.ItemType.Silver },
                { "platinumore", Enums.ItemType.Platinum },
                { "coal", Enums.ItemType.Coal },
                { "copperbar", Enums.ItemType.CopperBar },
                { "silverbar", Enums.ItemType.SilverBar },
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
                    inventoryUI.LoadItemSprite(sprite.Value, texture);
                    // NEW: Store texture for Player Drawing
                    itemTextureMap[sprite.Value] = texture;
                }
                catch (Exception ex)
                {
                    Logger.Log($"[GAME] Could not load {sprite.Key} item sprite: {ex.Message}");
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
                { "silverpickaxe", Enums.ItemType.SilverPickaxe },
                { "platinumpickaxe", Enums.ItemType.PlatinumPickaxe },
                { "runicpickaxe", Enums.ItemType.RunicPickaxe },
                { "woodcraftingtable", Enums.ItemType.WoodCraftingBench },
                { "coppercraftingtable", Enums.ItemType.CopperCraftingBench },
                { "woodchest", Enums.ItemType.WoodChest },
                { "silverchest", Enums.ItemType.SilverChest },
                { "magicchest", Enums.ItemType.MagicChest },
                { "copperore", Enums.ItemType.Copper },
                { "silverore", Enums.ItemType.Silver },
                { "platinumore", Enums.ItemType.Platinum },
                { "coal", Enums.ItemType.Coal },
                { "copperbar", Enums.ItemType.CopperBar },
                { "silverbar", Enums.ItemType.SilverBar },
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

        // NEW: Drop currently selected item
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
            // Throw it further away to prevent instant pickup
            dropPosition.X += 64; // Drop 2 tiles away (was 32)
            dropPosition.Y -= 32; // Drop higher above (was 16)

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
            hud.Initialize(GraphicsDevice);
            world = new World.World(hud);
            timeSystem = new TimeSystem();
            timeSystem.SetCurrentTime(data.GameTime);
            lightingSystem = new LightingSystem(world, timeSystem);
            chestSystem = new ChestSystem();
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
            world.ApplyTileChanges(data.TileChanges);
            chestSystem.LoadFromData(data.Chests);
            Logger.Log($"[GAME] Loaded {chestSystem.GetChestCount()} chests from save");
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
            // CRITICAL FIX: Passing 6 arguments to MiningSystem constructor
            miningSystem = new MiningSystem(world, inventory, mineDirtSound, mineStoneSound, mineTorchSound, placeTorchSound, gameSoundsVolume);

            // NEW: Subscribe to chest events
            miningSystem.OnChestMined += HandleChestMined;
            miningSystem.OnChestPlaced += HandleChestPlaced;

            inventoryUI = new InventoryUI(inventory, miningSystem);
            inventoryUI.Initialize(pixelTexture, font);
            LoadItemSprites(inventoryUI);
            LoadCraftingItemSprites(inventoryUI);
            chestUI = new ChestUI();
            // UPDATED: Pass SetGameSoundsVolume and initial volume to PauseMenu
            pauseMenu = new PauseMenu(OpenSaveMenu, QuitToMenu,
                                      (newVolume) => { musicVolume = newVolume; if (!isMusicMuted) MediaPlayer.Volume = musicVolume; }, musicVolume,
                                      ToggleFullscreen, hud.ToggleFullscreenMap, hud,
                                      ToggleAutoMining, isAutoMiningActive,
                                      SetGameSoundsVolume, gameSoundsVolume);
            saveMenu = new SaveMenu(SaveGame);
            miningOverlay = new MiningOverlay(world, miningSystem);
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
            hud.Initialize(GraphicsDevice);
            currentWorldSeed = System.Environment.TickCount;
            totalPlayTime = 0f;
            world = new World.World(hud);
            timeSystem = new TimeSystem();
            lightingSystem = new LightingSystem(world, timeSystem);
            chestSystem = new ChestSystem();
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
            camera = new Camera(GraphicsDevice.Viewport);
            camera.Position = player.Position;
            inventory = new Inventory();
            // CRITICAL FIX: Passing 6 arguments to MiningSystem constructor
            miningSystem = new MiningSystem(world, inventory, mineDirtSound, mineStoneSound, mineTorchSound, placeTorchSound, gameSoundsVolume);

            // NEW: Subscribe to chest events
            miningSystem.OnChestMined += HandleChestMined;
            miningSystem.OnChestPlaced += HandleChestPlaced;

            inventoryUI = new InventoryUI(inventory, miningSystem);
            inventoryUI.Initialize(pixelTexture, font);
            LoadItemSprites(inventoryUI);
            LoadCraftingItemSprites(inventoryUI);
            chestUI = new ChestUI();
            // UPDATED: Pass SetGameSoundsVolume and initial volume to PauseMenu
            pauseMenu = new PauseMenu(OpenSaveMenu, QuitToMenu,
                                      (newVolume) => { musicVolume = newVolume; if (!isMusicMuted) MediaPlayer.Volume = musicVolume; }, musicVolume,
                                      ToggleFullscreen, hud.ToggleFullscreenMap, hud,
                                      ToggleAutoMining, isAutoMiningActive,
                                      SetGameSoundsVolume, gameSoundsVolume);
            saveMenu = new SaveMenu(SaveGame);
            miningOverlay = new MiningOverlay(world, miningSystem);
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

            Color skyColor = timeSystem.GetSkyColor();
            GraphicsDevice.Clear(skyColor);

            Matrix transformMatrix = camera.GetTransformMatrix();

            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp,
                transformMatrix: transformMatrix
            );

            world.Draw(spriteBatch, camera, pixelTexture, lightingSystem, miningSystem);
            miningSystem.DrawItems(spriteBatch, pixelTexture);

            // 1. Determine currently held item
            ItemType heldItemType = inventory.GetSlot(miningSystem.GetSelectedHotbarSlot())?.ItemType ?? ItemType.None;
            Texture2D heldItemTexture = null;
            if (heldItemType != ItemType.None)
            {
                itemTextureMap.TryGetValue(heldItemType, out heldItemTexture);
            }

            // 2. Pass data to Player Draw method
            player.Draw(
                spriteBatch,
                pixelTexture,
                heldItemType,
                heldItemTexture,
                miningSystem.CurrentAnimationFrame
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

            Vector2 playerCenter = new Vector2(
                player.Position.X + Claude4_5Terraria.Player.Player.PLAYER_WIDTH / 2,
                player.Position.Y + Claude4_5Terraria.Player.Player.PLAYER_HEIGHT / 2
            );
            miningOverlay.DrawBlockOutlines(spriteBatch, pixelTexture, camera, playerCenter, showMiningOutlines);
            miningOverlay.DrawMiningProgress(spriteBatch, pixelTexture);

            spriteBatch.End();

            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp
            );

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


            inventoryUI.Draw(
                spriteBatch,
                pixelTexture,
                font,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height
            );

            if (hud != null)
            {
                // UPDATED: Passed player health parameters to HUD.Draw
                hud.Draw(spriteBatch, pixelTexture, font,
                    GraphicsDevice.Viewport.Width,
                    GraphicsDevice.Viewport.Height,
                    player.Position,
                    world,
                    isAutoMiningActive, // Auto-mine state
                    timeSystem.IsRaining, // Weather state
                    player.Health, // Player current health
                    player.MaxHealth, // Player max health
                    player.AirBubbles, // Player air
                    player.MaxAirBubbles // Player max air
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
                chestUI.Draw(spriteBatch, pixelTexture, font);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}