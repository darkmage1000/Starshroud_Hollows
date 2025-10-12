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
using System.Linq;

namespace Claude4_5Terraria
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private SpriteBatch additiveSpriteBatch;

        #region Fields
        private World.World world;
        private Player.Player player;
        private Camera camera;
        private Texture2D pixelTexture;
        private Texture2D oozeEnemyTexture;
        private SpriteFont font;
        private Texture2D menuBackgroundTexture;
        private Inventory inventory;
        private MiningSystem miningSystem;
        private LightingSystem lightingSystem;
        private TimeSystem timeSystem;
        private WorldGenerator worldGenerator;
        private ChestSystem chestSystem;
        private LiquidSystem liquidSystem;
        private CombatSystem combatSystem;
        private EnemySpawner enemySpawner;
        private MagicSystem magicSystem;
        private ProjectileSystem projectileSystem;
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
        private List<Vector2> rainParticles;
        private List<Vector2> snowParticles;
        private Random random;
        private const int MAX_RAIN_DROPS = 1000;
        private const float RAIN_SPEED = 800f;
        private const float RAIN_OFFSET_X = 500f;
        private const float RAIN_OFFSET_Y = 100f;
        private const int MAX_SNOW_FLAKES = 1500;
        private const float SNOW_SPEED = 200f;
        private const float SNOW_DRIFT = 50f;
        private SoundEffect mineDirtSound;
        private SoundEffect mineStoneSound;
        private SoundEffect mineTorchSound;
        private SoundEffect placeTorchSound;
        private bool isSleeping = false;
        private float sleepProgress = 0f;
        private const float SLEEP_DURATION = 5.0f;
        private Point? currentBedPosition = null;
        private float bedHoldTime = 0f;
        private const float BED_HOLD_REQUIRED = 3.0f;
        private bool isGeneratingWorld = false;
        private Task worldGenerationTask = null;
        #endregion

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;
            graphics.ApplyChanges();
            random = new Random();
            rainParticles = new List<Vector2>();
            snowParticles = new List<Vector2>();
        }

        protected override void Initialize()
        {
            this.Exiting += OnExiting;
            try { SoundEffect.MasterVolume = 1.0f; } catch { }
            previousKeyboardState = Keyboard.GetState();
            previousMouseState = Mouse.GetState();
            totalPlayTime = 0f;
            base.Initialize();
        }

        private void OnExiting(object sender, EventArgs args)
        {
            // No longer needed, removed isExiting
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            additiveSpriteBatch = new SpriteBatch(GraphicsDevice);
            Entities.DroppedItem.SetStaticContent(Content);
            pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            try { oozeEnemyTexture = Content.Load<Texture2D>("OozeEnemy"); }
            catch { oozeEnemyTexture = pixelTexture; }

            font = Content.Load<SpriteFont>("Font");
            menuBackgroundTexture = Content.Load<Texture2D>("MenuBackground");
            itemTextureMap = new Dictionary<ItemType, Texture2D>();

            try
            {
                mineDirtSound = Content.Load<SoundEffect>("a_pickaxe_hitting_dirt");
                mineStoneSound = Content.Load<SoundEffect>("a_pickaxe_hitting_stone");
                mineTorchSound = Content.Load<SoundEffect>("hitting a tree");
                placeTorchSound = Content.Load<SoundEffect>("placing and mining a torch");
                mineStoneSound?.CreateInstance().Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] Could not load sound effects: {ex.Message}");
            }

            backgroundMusic = Content.Load<Song>("CozyBackground");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = musicVolume;
            MediaPlayer.Play(backgroundMusic);

            startMenu = new StartMenu(musicVolume, (v) => { musicVolume = v; if (!isMusicMuted) MediaPlayer.Volume = v; }, gameSoundsVolume, SetGameSoundsVolume, ToggleFullscreen, menuBackgroundTexture, PlayTestSound);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            if (startMenu.GetState() != MenuState.Playing)
            {
                startMenu.Update(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                
                // Start world generation on a background task
                if (startMenu.GetState() == MenuState.Loading && !worldGenerated && !isGeneratingWorld)
                {
                    isGeneratingWorld = true;
                    worldGenerationTask = Task.Run(() => 
                    {
                        GenerateWorld();
                    });
                }
                
                // Check if world generation is complete
                if (isGeneratingWorld && worldGenerationTask != null && worldGenerationTask.IsCompleted)
                {
                    isGeneratingWorld = false;
                    worldGenerationTask = null;
                    startMenu.SetState(MenuState.Playing);
                }
                
                base.Update(gameTime);
                return;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (timeSystem == null || player == null || world == null)
            {
                base.Update(gameTime);
                return;
            }

            if (chestUI?.IsOpen == true) { chestUI.Update(); base.Update(gameTime); return; }
            if (saveMenu?.IsOpen == true) { saveMenu.Update(deltaTime, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height); base.Update(gameTime); return; }

            // Check for pause toggle BEFORE updating pauseMenu
            if (pauseMenu != null && keyboardState.IsKeyDown(Keys.Escape) && !previousKeyboardState.IsKeyDown(Keys.Escape))
            {
                pauseMenu.TogglePause();
            }
            
            if (pauseMenu != null)
            {
                pauseMenu.Update();
            }

            if (pauseMenu?.IsPaused == true)
            {
                // Update previous states even when paused so ESC works properly
                previousKeyboardState = keyboardState;
                previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            totalPlayTime += deltaTime;
            timeSystem.Update(deltaTime);
            UpdateRainParticles(deltaTime, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            UpdateSnowParticles(deltaTime, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            liquidSystem.UpdateFlow();
            
            // Track player movement direction for auto-mining
            Vector2 oldPlayerPos = player.Position;
            player.Update(gameTime);
            Vector2 playerMovement = player.Position - oldPlayerPos;
            if (playerMovement.LengthSquared() > 0.1f) // Only update if actually moving
            {
                lastPlayerDirection = playerMovement;
                lastPlayerDirection.Normalize();
            }
            
            camera.Position = player.Position;
            var playerCenter = player.GetCenterPosition();

            #region Main Update Logic
            for (int i = 0; i < 10; i++) 
                if (keyboardState.IsKeyDown(Keys.D1 + i) && !previousKeyboardState.IsKeyDown(Keys.D1 + i)) 
                    miningSystem.SetSelectedHotbarSlot(i);

            // --- ADD THIS CODE BLOCK ---
            // Handle using Recall Potion
            if (mouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released)
            {
                var selectedSlot = inventory.GetSlot(miningSystem.GetSelectedHotbarSlot());
                if (selectedSlot != null && !selectedSlot.IsEmpty() && selectedSlot.ItemType == ItemType.RecallPotion)
                {
                    // Teleport the player and consume one potion.
                    player.TeleportToSpawn();
                    inventory.RemoveItem(ItemType.RecallPotion, 1);
                }
            }
            if (keyboardState.IsKeyDown(Keys.T) && !previousKeyboardState.IsKeyDown(Keys.T)) 
                showMiningOutlines = !showMiningOutlines;
            
            if (keyboardState.IsKeyDown(Keys.L) && !previousKeyboardState.IsKeyDown(Keys.L)) 
                ToggleAutoMining(null);
            
            if (keyboardState.IsKeyDown(Keys.Q) && !previousKeyboardState.IsKeyDown(Keys.Q) && inventoryUI?.IsInventoryOpen == false) 
                DropSelectedItem();
            
            var heldItem = inventory.GetSlot(miningSystem.GetSelectedHotbarSlot())?.ItemType ?? ItemType.None;
            bool isSword = heldItem >= ItemType.WoodSword && heldItem <= ItemType.RunicSword;
            bool isWand = (heldItem >= ItemType.WoodWand && heldItem <= ItemType.RunicLaserWand);
            
            // CRITICAL: Update mining system for mining and placing
            // Auto-mine should work regardless of held item, manual mining only when not sword/wand
            if (inventoryUI?.IsInventoryOpen == false)
            {
                bool allowManualMining = !isSword && !isWand;
                if (isAutoMiningActive || allowManualMining)
                {
                    miningSystem?.Update(gameTime, playerCenter, camera, player.Position, Player.Player.PLAYER_WIDTH, Player.Player.PLAYER_HEIGHT, isAutoMiningActive, lastPlayerDirection);
                }
            }
            
            if (isSword) 
                combatSystem?.Update(deltaTime, player.Position, player.GetFacingRight(), mouseState, previousMouseState, heldItem);
            
            magicSystem?.Update(deltaTime, mouseState, previousMouseState, heldItem);
            projectileSystem?.Update(deltaTime, enemySpawner?.GetActiveEnemies() ?? new List<Enemy>());
            
            if (enemySpawner != null)
            {
                Rectangle cameraView = camera.GetVisibleArea(World.World.TILE_SIZE);  
                enemySpawner.Update(deltaTime, playerCenter, timeSystem, cameraView);
                enemySpawner.CheckCombatCollisions(combatSystem, player.Position, Player.Player.PLAYER_WIDTH, Player.Player.PLAYER_HEIGHT, heldItem, inventory);
                
                // Check player health before collision check
                float healthBefore = player.Health;
                enemySpawner.CheckPlayerCollisions(player.Position, Player.Player.PLAYER_WIDTH, Player.Player.PLAYER_HEIGHT, player);
                
                // If player died (health went to 0 and was restored to max), restore mana
                if (healthBefore > 0 && player.Health == player.GetMaxHealth() && healthBefore < player.GetMaxHealth())
                {
                    magicSystem?.RestoreMana();
                }
            }
            
            hud?.UpdateMinimapData(world);
            inventoryUI.Update(gameTime, player.Position, world, GraphicsDevice.Viewport.Height);
            #endregion

            // Update previous input states at the END of the frame
            previousKeyboardState = keyboardState;
            previousMouseState = mouseState;

            base.Update(gameTime);
        }

        private void InitializeGameSystems(Vector2 playerPosition, bool isNewGame)
        {
            player = new Player.Player(world, playerPosition);
            world.SetPlayer(player);
            if (startMenu.IsDebugModeEnabled) player.SetDebugMode(true);
            world.SetLiquidSystem(liquidSystem);
            camera = new Camera(GraphicsDevice.Viewport) { Position = player.Position };
            inventory = new Inventory();
            miningSystem = new MiningSystem(world, inventory, mineDirtSound, mineStoneSound, mineTorchSound, placeTorchSound, gameSoundsVolume);
            miningSystem.SetItemTextureMap(itemTextureMap);
            miningSystem.OnChestMined += HandleChestMined;
            miningSystem.OnChestPlaced += HandleChestPlaced;
            combatSystem = new CombatSystem();
            enemySpawner = new EnemySpawner(world);
            projectileSystem = new ProjectileSystem(world);
            LoadSpellTextures(projectileSystem);
            magicSystem = new MagicSystem(player, projectileSystem, world, camera);
            inventoryUI = new InventoryUI(inventory, miningSystem);
            inventoryUI.Initialize(pixelTexture, font);
            LoadItemSprites(inventoryUI);
            LoadCraftingItemSprites(inventoryUI);
            chestUI = new ChestUI(world);
            pauseMenu = new PauseMenu(() => saveMenu?.Open(), QuitToMenu, (v) => { musicVolume = v; if (!isMusicMuted) MediaPlayer.Volume = v; }, musicVolume, ToggleFullscreen, hud.ToggleFullscreenMap, hud, (n) => ToggleAutoMining(n), isAutoMiningActive, SetGameSoundsVolume, gameSoundsVolume);
            saveMenu = new SaveMenu(SaveGame);
            miningOverlay = new MiningOverlay(world, miningSystem, chestSystem);
            // Increased from 500 to 2000 to allow liquids to fully settle.
            if (liquidSystem != null) { for (int i = 0; i < 5000; i++) liquidSystem.UpdateFlow(); }
            worldGenerated = true;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(135, 206, 235));
            
            if (startMenu.GetState() != MenuState.Playing)
            {
                spriteBatch.Begin();
                startMenu.Draw(spriteBatch, pixelTexture, font, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                spriteBatch.End();
                base.Draw(gameTime);
                return;
            }

            if (world == null || player == null)
            {
                base.Draw(gameTime);
                return;
            }

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, camera.GetTransformMatrix());
            world.Draw(spriteBatch, camera, pixelTexture, lightingSystem, miningSystem, player.IsDebugModeActive());
            if (showMiningOutlines && miningOverlay != null)
            {
                miningOverlay.DrawBlockOutlines(spriteBatch, pixelTexture, camera, player.GetCenterPosition(), true);
            }
            enemySpawner?.DrawEnemies(spriteBatch, oozeEnemyTexture, pixelTexture);
            projectileSystem?.Draw(spriteBatch, pixelTexture);

            // Draw weather particles (rain or snow)
            if (timeSystem != null && world != null && player != null)
            {
                int playerTileX = (int)(player.Position.X / World.World.TILE_SIZE);
                int surfaceTileY = world.GetSurfaceHeight(playerTileX);
                bool isNearSurface = (player.Position.Y / World.World.TILE_SIZE) < (surfaceTileY + 15);

                // Only draw weather if the player is near the surface
                if (isNearSurface)
                {
                    int snowStart = worldGenerator.GetSnowBiomeStartX();
                    int snowEnd = worldGenerator.GetSnowBiomeEndX();
                    bool inSnowBiome = playerTileX >= snowStart && playerTileX <= snowEnd;

                    if (inSnowBiome)
                    {
                        // In the snow biome, draw snow.
                        foreach (Vector2 snowFlake in snowParticles)
                        {
                            Rectangle snowRect = new Rectangle((int)snowFlake.X, (int)snowFlake.Y, 3, 3);
                            spriteBatch.Draw(pixelTexture, snowRect, Color.White * 0.8f);
                        }
                    }
                    else if (timeSystem.IsRaining)
                    {
                        // Outside the snow biome, draw rain if it's raining.
                        foreach (Vector2 rainDrop in rainParticles)
                        {
                            Rectangle rainRect = new Rectangle((int)rainDrop.X, (int)rainDrop.Y, 2, 12);
                            spriteBatch.Draw(pixelTexture, rainRect, Color.LightBlue * 0.6f);
                        }
                    }
                }
            }

            // Draw the player and their held weapon
            if (player != null)
            {
                var heldItem = inventory.GetSlot(miningSystem.GetSelectedHotbarSlot())?.ItemType ?? ItemType.None;
                Texture2D itemTexture = null;
                if (heldItem != ItemType.None && itemTextureMap.ContainsKey(heldItem))
                {
                    itemTexture = itemTextureMap[heldItem];
                }

                // Determine the correct animation frame for the held item
                int animationFrame = 0;
                bool isSword = heldItem >= ItemType.WoodSword && heldItem <= ItemType.RunicSword;
                bool isPickaxe = heldItem >= ItemType.WoodPickaxe && heldItem <= ItemType.RunicPickaxe;

                if (isSword && combatSystem != null && combatSystem.IsAttacking())
                {
                    // Use attack animation frame for swords
                    animationFrame = combatSystem.GetCurrentAttackFrame();
                }
                else if (isPickaxe && miningSystem != null && miningSystem.GetCurrentlyMiningTile().HasValue)
                {
                    // Use mining animation frame for pickaxes
                    animationFrame = miningSystem.CurrentAnimationFrame;
                }

                player.Draw(spriteBatch, pixelTexture, heldItem, itemTexture, animationFrame); ;
            }
            
            spriteBatch.End();

            spriteBatch.Begin();
            hud?.Draw(spriteBatch, pixelTexture, font, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height,
                player.Position, world, isAutoMiningActive, timeSystem.IsRaining,
                player.Health, player.MaxHealth, player.AirBubbles, player.MaxAirBubbles,
                currentBedPosition, isSleeping, sleepProgress, SLEEP_DURATION,
                bedHoldTime, BED_HOLD_REQUIRED, timeSystem, magicSystem);
            inventoryUI?.Draw(spriteBatch, pixelTexture, font, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            chestUI?.Draw(spriteBatch, pixelTexture, font);
            pauseMenu?.Draw(spriteBatch, pixelTexture, font, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            if (saveMenu != null && saveMenu.IsOpen)
            {
                saveMenu.Draw(spriteBatch, pixelTexture, font, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }

        #region Helper Methods
        public void SetGameSoundsVolume(float v) 
        { 
            gameSoundsVolume = v; 
            miningSystem?.SetSoundVolume(v); 
        }

        private void PlayTestSound() 
        { 
            mineDirtSound?.Play(volume: gameSoundsVolume, pitch: 0.0f, pan: 0.0f); 
        }

        public void ToggleAutoMining(bool? n) 
        { 
            isAutoMiningActive = n ?? !isAutoMiningActive; 
        }

        private void UpdateRainParticles(float deltaTime, int screenWidth, int screenHeight)
        {
            if (player == null || world == null || timeSystem == null) return;

            int playerTileX = (int)(player.Position.X / World.World.TILE_SIZE);
            int surfaceTileY = world.GetSurfaceHeight(playerTileX);
            bool isNearSurface = (player.Position.Y / World.World.TILE_SIZE) < (surfaceTileY + 15);

            if (!timeSystem.IsRaining || !isNearSurface)
            {
                rainParticles.Clear();
                return;
            }

            while (rainParticles.Count < MAX_RAIN_DROPS)
            {
                rainParticles.Add(new Vector2(
                    camera.Position.X + random.Next(-screenWidth, screenWidth * 2),
                    camera.Position.Y + random.Next(-screenHeight, 0)
                ));
            }

            for (int i = rainParticles.Count - 1; i >= 0; i--)
            {
                Vector2 velocity = new Vector2(RAIN_OFFSET_X, RAIN_SPEED) * deltaTime;
                rainParticles[i] += velocity;

                if (rainParticles[i].Y > camera.Position.Y + screenHeight + RAIN_OFFSET_Y)
                {
                    rainParticles[i] = new Vector2(
                        camera.Position.X + random.Next(-screenWidth, screenWidth * 2),
                        camera.Position.Y - RAIN_OFFSET_Y
                    );
                }
            }
        }

        private void UpdateSnowParticles(float deltaTime, int screenWidth, int screenHeight)
        {
            if (player == null || worldGenerator == null || world == null) return;

            int playerTileX = (int)(player.Position.X / World.World.TILE_SIZE);
            int snowStart = worldGenerator.GetSnowBiomeStartX();
            int snowEnd = worldGenerator.GetSnowBiomeEndX();
            bool inSnowBiome = playerTileX >= snowStart && playerTileX <= snowEnd;

            int surfaceTileY = world.GetSurfaceHeight(playerTileX);
            bool isNearSurface = (player.Position.Y / World.World.TILE_SIZE) < (surfaceTileY + 15);

            if (!inSnowBiome || !isNearSurface)
            {
                snowParticles.Clear();
                return;
            }

            while (snowParticles.Count < MAX_SNOW_FLAKES)
            {
                snowParticles.Add(new Vector2(
                    camera.Position.X + random.Next(-screenWidth, screenWidth * 2),
                    camera.Position.Y + random.Next(-screenHeight, 0)
                ));
            }

            for (int i = snowParticles.Count - 1; i >= 0; i--)
            {
                Vector2 velocity = new Vector2(
                    (float)Math.Sin(snowParticles[i].Y * 0.01f) * SNOW_DRIFT,
                    SNOW_SPEED
                ) * deltaTime;

                Vector2 newPosition = snowParticles[i] + velocity;
                int tileX = (int)(newPosition.X / World.World.TILE_SIZE);
                int tileY = (int)(newPosition.Y / World.World.TILE_SIZE);
                bool isSolidBelow = world.IsSolidAtPosition(tileX, tileY);

                if (isSolidBelow)
                {
                    snowParticles[i] = new Vector2(
                        camera.Position.X + random.Next(-screenWidth, screenWidth * 2),
                        camera.Position.Y - 100
                    );
                }
                else
                {
                    snowParticles[i] = newPosition;
                    if (snowParticles[i].Y > camera.Position.Y + screenHeight + 100)
                    {
                        snowParticles[i] = new Vector2(
                            camera.Position.X + random.Next(-screenWidth, screenWidth * 2),
                            camera.Position.Y - 100
                        );
                    }
                }
            }
        }

        private void DropSelectedItem()
        {
            int slot = miningSystem.GetSelectedHotbarSlot();
            var item = inventory.GetSlot(slot);
            if (item != null && !item.IsEmpty())
            {
                Vector2 dropPosition = player.GetCenterPosition();

                // This is the critical line that was missing.
                // It tells the mining system to create the physical item in the world.
                miningSystem.DropItem(dropPosition, item.ItemType, 1);

                // This line removes the item from your inventory after it has been dropped.
                inventory.RemoveItem(item.ItemType, 1);
            }
        }

        private void HandleChestMined(Point position, TileType chestType)
        {
            chestSystem.RemoveChest(position, inventory);
        }

        private void HandleChestPlaced(Point position, ItemType itemType)
        {
            TileType chestType = itemType.ToTileType();
            ChestTier tier = ChestTier.Wood;
            
            if (chestType == TileType.WoodChest) tier = ChestTier.Wood;
            else if (chestType == TileType.SilverChest) tier = ChestTier.Silver;
            else if (chestType == TileType.MagicChest) tier = ChestTier.Magic;
            
            chestSystem.PlaceChest(position, tier, false);
        }

        private void ToggleFullscreen()
        {
            graphics.IsFullScreen = !graphics.IsFullScreen;
            graphics.PreferredBackBufferWidth = graphics.IsFullScreen ? GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width : 1920;
            graphics.PreferredBackBufferHeight = graphics.IsFullScreen ? GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height : 1080;
            graphics.ApplyChanges();
        }

        private void QuitToMenu()
        {
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
            liquidSystem = null;
            isSleeping = false;
            startMenu.SetState(MenuState.MainMenu);
        }

        private void SaveGame(int slotIndex)
        {
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
            
            for (int i = 0; i < inventory.GetSlotCount(); i++)
            {
                var slot = inventory.GetSlot(i);
                data.InventorySlots.Add(slot != null && !slot.IsEmpty() 
                    ? new InventorySlotData { ItemType = (int)slot.ItemType, Count = slot.Count } 
                    : new InventorySlotData());
            }
            
            SaveSystem.SaveGame(data, slotIndex);
        }

        private void GenerateWorld()
        {
            hud = new HUD();
            hud.Initialize(GraphicsDevice, font);
            currentWorldSeed = Environment.TickCount;
            world = new World.World(hud);
            timeSystem = new TimeSystem();
            lightingSystem = new LightingSystem(world, timeSystem);
            chestSystem = new ChestSystem();
            liquidSystem = new LiquidSystem(world);
            world.SetLiquidSystem(liquidSystem);
            LoadTileSprites(world);
            worldGenerator = new WorldGenerator(world, currentWorldSeed, chestSystem);
            worldGenerator.OnProgressUpdate = (p, m) => startMenu.SetLoadingProgress(p, m);
            worldGenerator.Generate();
            world.EnableChunkUnloading();
            world.EnableTileChangeTracking();
            InitializeGameSystems(worldGenerator.GetSpawnPosition(64), true);
            
            // Starting inventory - all weapons, tools, and materials for testing
            inventory.AddItem(ItemType.RunicPickaxe, 1);
            inventory.AddItem(ItemType.Torch, 50);
            
            // All swords
            inventory.AddItem(ItemType.WoodSword, 1);
            inventory.AddItem(ItemType.CopperSword, 1);
            inventory.AddItem(ItemType.IronSword, 1);
            inventory.AddItem(ItemType.SilverSword, 1);
            inventory.AddItem(ItemType.GoldSword, 1);
            inventory.AddItem(ItemType.PlatinumSword, 1);
            inventory.AddItem(ItemType.RunicSword, 1);
            
            // All wands
            inventory.AddItem(ItemType.WoodWand, 1);
            inventory.AddItem(ItemType.FireWand, 1);
            inventory.AddItem(ItemType.LightningWand, 1);
            inventory.AddItem(ItemType.NatureWand, 1);
            inventory.AddItem(ItemType.WaterWand, 1);
            inventory.AddItem(ItemType.HalfMoonWand, 1);
            inventory.AddItem(ItemType.RunicLaserWand, 1);
        }

        private void LoadTileSprites(World.World world)
        {
            var spriteMap = new Dictionary<string, TileType> 
            { 
                { "dirt", TileType.Dirt }, 
                { "grass", TileType.Grass }, 
                { "stone", TileType.Stone }, 
                { "coalblock", TileType.Coal }, 
                { "copperblock", TileType.Copper }, 
                { "ironblock", TileType.Iron }, 
                { "silverblock", TileType.Silver }, 
                { "goldblock", TileType.Gold }, 
                { "platinumblock", TileType.Platinum }, 
                { "torch", TileType.Torch }, 
                { "saplingplanteddirt", TileType.Sapling }, 
                { "woodcraftingtable", TileType.WoodCraftingBench }, 
                { "coppercraftingtable", TileType.CopperCraftingBench }, 
                { "woodchest", TileType.WoodChest }, 
                { "silverchest", TileType.SilverChest }, 
                { "magicchest", TileType.MagicChest } 
            };
            
            foreach (var sprite in spriteMap) 
            {
                try 
                { 
                    world.LoadTileSprite(sprite.Value, Content.Load<Texture2D>(sprite.Key)); 
                } 
                catch { }
            }
        }

        private void LoadItemSprites(InventoryUI iUI)
        {
            var spriteMap = new Dictionary<string, ItemType> 
            {
                {"woodpickaxe", ItemType.WoodPickaxe}, 
                {"stonepickaxe", ItemType.StonePickaxe}, 
                {"copperpickaxe", ItemType.CopperPickaxe},
                {"silverpickaxe", ItemType.SilverPickaxe}, 
                {"platinumpickaxe", ItemType.PlatinumPickaxe},
                {"runicpickaxe", ItemType.RunicPickaxe},
                {"woodsword", ItemType.WoodSword},
                {"CopperSword", ItemType.CopperSword},
                {"IronSword", ItemType.IronSword},
                {"SilverSword", ItemType.SilverSword},
                {"GoldSword", ItemType.GoldSword},
                {"PlatinumSword", ItemType.PlatinumSword},
                {"wand", ItemType.WoodWand},
                {"FireWand", ItemType.FireWand},
                {"LightningWand", ItemType.LightningWand},
                {"NatureWand", ItemType.NatureWand},
                {"WaterWand", ItemType.WaterWand},
                {"HalfMoonWand", ItemType.HalfMoonWand},
                {"stick", ItemType.Stick},
                {"copperbar", ItemType.CopperBar},
                {"silverbar", ItemType.SilverBar},
                {"platinumbar", ItemType.PlatinumBar},
                {"torch", ItemType.Torch},
                {"acorn", ItemType.Acorn}
            };
            
            foreach (var sprite in spriteMap) 
            {
                try 
                { 
                    Texture2D tex = Content.Load<Texture2D>(sprite.Key); 
                    iUI.LoadItemSprite(sprite.Value, tex); 
                    itemTextureMap[sprite.Value] = tex; 
                } 
                catch { }
            }
            
            try 
            { 
                Texture2D tex = Content.Load<Texture2D>("RunicSword Spritesheet"); 
                iUI.LoadItemSprite(ItemType.RunicSword, tex); 
                itemTextureMap[ItemType.RunicSword] = tex; 
            } 
            catch { }
            
            try 
            { 
                Texture2D tex = Content.Load<Texture2D>("RunicLaserWandSpriteSheet"); 
                iUI.LoadItemSprite(ItemType.RunicLaserWand, tex); 
                itemTextureMap[ItemType.RunicLaserWand] = tex; 
            } 
            catch { }
        }

        private void LoadSpellTextures(ProjectileSystem pS)
        {
            var spriteMap = new Dictionary<string, ProjectileType> 
            {
                {"MagicBolt", ProjectileType.MagicBolt}, 
                {"FireBolt", ProjectileType.FireBolt}, 
                {"LightningSpell", ProjectileType.LightningBlast},
                {"NatureVineSpell", ProjectileType.NatureVine}, 
                {"WaterBubbleSpell", ProjectileType.WaterBubble}, 
                {"HalfMoonSpell", ProjectileType.HalfMoonSlash},
                {"RunicLaserBeamSpell", ProjectileType.RunicLaser}
            };
            
            foreach (var sprite in spriteMap) 
            {
                try 
                { 
                    pS.LoadTexture(sprite.Value, Content.Load<Texture2D>(sprite.Key)); 
                } 
                catch { }
            }
        }

        private void LoadCraftingItemSprites(InventoryUI iUI)
        {
            var spriteMap = new Dictionary<string, ItemType> 
            {
                {"woodpickaxe", ItemType.WoodPickaxe}, 
                {"stonepickaxe", ItemType.StonePickaxe},
                {"copperpickaxe", ItemType.CopperPickaxe},
                {"silverpickaxe", ItemType.SilverPickaxe},
                {"platinumpickaxe", ItemType.PlatinumPickaxe},
                {"woodsword", ItemType.WoodSword},
                {"CopperSword", ItemType.CopperSword},
                {"torch", ItemType.Torch},
                {"stick", ItemType.Stick},
                {"copperbar", ItemType.CopperBar},
                {"silverbar", ItemType.SilverBar}
            };
            
            foreach (var sprite in spriteMap) 
            {
                try 
                { 
                    iUI.GetCraftingUI()?.LoadItemSprite(sprite.Value, Content.Load<Texture2D>(sprite.Key)); 
                } 
                catch { }
            }
        }
        #endregion
    }
}
