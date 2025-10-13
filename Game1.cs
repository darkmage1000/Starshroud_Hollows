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

        #region Fields
        private World.World world;
        private Player.Player player;
        private Camera camera;
        private Texture2D pixelTexture;
        private Texture2D oozeEnemyTexture;
        private Texture2D zombieEnemyTexture;
        private Texture2D echoWispTexture;
        private SpriteFont font;
        private Texture2D menuBackgroundTexture;
        private Inventory inventory;
        private BossSystem bossSystem;
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
        private Systems.Summons.SummonSystem summonSystem;
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
        private bool worldGenerated = false;
        private int currentWorldSeed;
        private float totalPlayTime;
        private bool isAutoMiningActive = false;
        private Vector2 lastPlayerDirection = Vector2.Zero;
        private Dictionary<ItemType, Texture2D> itemTextureMap;
        private List<Vector2> rainParticles;
        private List<Vector2> snowParticles;
        private Random random;
        private SoundEffect mineDirtSound;
        private SoundEffect mineStoneSound;
        private SoundEffect mineTorchSound;
        private SoundEffect placeTorchSound;
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
            try { SoundEffect.MasterVolume = 1.0f; } catch { }
            previousKeyboardState = Keyboard.GetState();
            previousMouseState = Mouse.GetState();
            totalPlayTime = 0f;
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Entities.DroppedItem.SetStaticContent(Content);
            pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            try { oozeEnemyTexture = Content.Load<Texture2D>("OozeEnemy"); } catch { }
            try { zombieEnemyTexture = Content.Load<Texture2D>("ZombieEnemy"); } catch { }
            try { echoWispTexture = Content.Load<Texture2D>("EchoWispSummon"); } catch { }

            font = Content.Load<SpriteFont>("Font");
            menuBackgroundTexture = Content.Load<Texture2D>("MenuBackground");
            itemTextureMap = new Dictionary<ItemType, Texture2D>();

            try
            {
                mineDirtSound = Content.Load<SoundEffect>("a_pickaxe_hitting_dirt");
                mineStoneSound = Content.Load<SoundEffect>("a_pickaxe_hitting_stone");
                mineTorchSound = Content.Load<SoundEffect>("hitting a tree");
                placeTorchSound = Content.Load<SoundEffect>("placing and mining a torch");
            }
            catch (Exception ex) { Logger.Log($"[ERROR] Could not load sound effects: {ex.Message}"); }

            backgroundMusic = Content.Load<Song>("CozyBackground");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = musicVolume;
            MediaPlayer.Play(backgroundMusic);

            startMenu = new StartMenu(musicVolume, (v) => { musicVolume = v; MediaPlayer.Volume = v; }, gameSoundsVolume, SetGameSoundsVolume, ToggleFullscreen, menuBackgroundTexture, PlayTestSound);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            if (startMenu.GetState() != MenuState.Playing)
            {
                startMenu.Update(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                if (startMenu.GetState() == MenuState.Loading && !worldGenerated && !isGeneratingWorld)
                {
                    isGeneratingWorld = true;
                    worldGenerationTask = Task.Run(() => GenerateWorld());
                }
                if (isGeneratingWorld && worldGenerationTask != null && worldGenerationTask.IsCompleted)
                {
                    isGeneratingWorld = false;
                    worldGenerationTask = null;
                    startMenu.SetState(MenuState.Playing);
                }
                base.Update(gameTime);
                return;
            }

            if (timeSystem == null || player == null || world == null)
            {
                base.Update(gameTime);
                return;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (chestUI?.IsOpen == true) { chestUI.Update(); base.Update(gameTime); return; }
            if (saveMenu?.IsOpen == true) { saveMenu.Update(deltaTime, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height); base.Update(gameTime); return; }

            if (pauseMenu != null && keyboardState.IsKeyDown(Keys.Escape) && !previousKeyboardState.IsKeyDown(Keys.Escape))
            {
                pauseMenu.TogglePause();
            }

            if (pauseMenu != null) pauseMenu.Update();

            if (pauseMenu?.IsPaused == true)
            {
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

            Vector2 oldPlayerPos = player.Position;
            player.Update(gameTime);
            Vector2 playerMovement = player.Position - oldPlayerPos;
            if (playerMovement.LengthSquared() > 0.1f)
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

            if (mouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released)
            {
                var selectedSlot = inventory.GetSlot(miningSystem.GetSelectedHotbarSlot());
                if (selectedSlot != null && !selectedSlot.IsEmpty())
                {
                    if (selectedSlot.ItemType == ItemType.RecallPotion)
                    {
                        player.TeleportToSpawn();
                        inventory.RemoveItem(ItemType.RecallPotion, 1);
                    }
                    else if (selectedSlot.ItemType == ItemType.TrollBait)
                    {
                        string errorMessage = bossSystem.TrySummonCaveTroll(player.Position);
                        if (errorMessage == null) inventory.RemoveItem(ItemType.TrollBait, 1);
                        else Logger.Log($"[BOSS] Cannot summon: {errorMessage}");
                    }
                }
            }

            if (keyboardState.IsKeyDown(Keys.Q) && !previousKeyboardState.IsKeyDown(Keys.Q) && inventoryUI?.IsInventoryOpen == false)
                DropSelectedItem();

            var heldItem = inventory.GetSlot(miningSystem.GetSelectedHotbarSlot())?.ItemType ?? ItemType.None;
            bool isSword = (heldItem >= ItemType.WoodSword && heldItem <= ItemType.RunicSword) || heldItem == ItemType.TrollClub;
            bool isWand = (heldItem >= ItemType.WoodWand && heldItem <= ItemType.RunicLaserWand);

            if (inventoryUI?.IsInventoryOpen == false)
            {
                bool allowManualMining = !isSword && !isWand;
                if (isAutoMiningActive || allowManualMining)
                {
                    miningSystem?.Update(gameTime, playerCenter, camera, player.Position, Player.Player.PLAYER_WIDTH, Player.Player.PLAYER_HEIGHT, isAutoMiningActive, lastPlayerDirection);
                }
            }

            if (isSword) combatSystem?.Update(deltaTime, player.Position, player.GetFacingRight(), mouseState, previousMouseState, heldItem);

            magicSystem?.Update(deltaTime, mouseState, previousMouseState, heldItem);
            projectileSystem?.Update(deltaTime, enemySpawner?.GetActiveEnemies() ?? new List<Enemy>());

            var summonTargets = new List<Interfaces.IDamageable>();
            if (enemySpawner != null) summonTargets.AddRange(enemySpawner.GetActiveEnemies());
            if (bossSystem != null && bossSystem.HasActiveBoss) summonTargets.Add(bossSystem.ActiveTroll);
            summonSystem?.Update(deltaTime, playerCenter, summonTargets);

            bossSystem?.Update(deltaTime, playerCenter, player, inventory, combatSystem, projectileSystem, heldItem);

            if (enemySpawner != null)
            {
                Rectangle cameraView = camera.GetVisibleArea(World.World.TILE_SIZE);
                enemySpawner.Update(deltaTime, playerCenter, timeSystem, cameraView);
                enemySpawner.CheckCombatCollisions(combatSystem, player.Position, Player.Player.PLAYER_WIDTH, Player.Player.PLAYER_HEIGHT, heldItem, inventory);

                float healthBefore = player.Health;
                enemySpawner.CheckPlayerCollisions(player.Position, Player.Player.PLAYER_WIDTH, Player.Player.PLAYER_HEIGHT, player);

                if (healthBefore > 0 && player.Health == player.GetMaxHealth() && healthBefore < player.GetMaxHealth())
                {
                    magicSystem?.RestoreMana();
                }
            }

            hud?.UpdateMinimapData(world);
            inventoryUI.Update(gameTime, player.Position, world, GraphicsDevice.Viewport.Height);
            #endregion

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
            bossSystem = new BossSystem(world);
            miningSystem = new MiningSystem(world, inventory, mineDirtSound, mineStoneSound, mineTorchSound, placeTorchSound, gameSoundsVolume);
            miningSystem.SetItemTextureMap(itemTextureMap);
            miningSystem.OnChestMined += HandleChestMined;
            miningSystem.OnChestPlaced += HandleChestPlaced;
            combatSystem = new CombatSystem();
            enemySpawner = new EnemySpawner(world);
            projectileSystem = new ProjectileSystem(world);
            LoadSpellTextures(projectileSystem);
            summonSystem = new Systems.Summons.SummonSystem(world);
            summonSystem.LoadTextures(echoWispTexture);
            magicSystem = new MagicSystem(player, projectileSystem, summonSystem, world, camera);
            inventoryUI = new InventoryUI(inventory, miningSystem);
            inventoryUI.Initialize(pixelTexture, font);
            LoadItemSprites(inventoryUI);
            LoadCraftingItemSprites(inventoryUI);
            chestUI = new ChestUI(world);
            pauseMenu = new PauseMenu(() => saveMenu?.Open(), QuitToMenu, (v) => { musicVolume = v; MediaPlayer.Volume = v; }, musicVolume, ToggleFullscreen, hud.ToggleFullscreenMap, hud, (n) => ToggleAutoMining(n), isAutoMiningActive, SetGameSoundsVolume, gameSoundsVolume);
            saveMenu = new SaveMenu(SaveGame);
            miningOverlay = new MiningOverlay(world, miningSystem, chestSystem);
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

            if (lightingSystem != null && summonSystem != null)
            {
                lightingSystem.SetSummonLights(summonSystem.GetActiveSummons().Select(s => s.Position).ToList());
            }

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, camera.GetTransformMatrix());

            world.Draw(spriteBatch, camera, pixelTexture, lightingSystem, miningSystem, player.IsDebugModeActive());
            enemySpawner?.DrawEnemies(spriteBatch, oozeEnemyTexture, zombieEnemyTexture, pixelTexture);
            bossSystem?.Draw(spriteBatch, null, pixelTexture);
            projectileSystem?.Draw(spriteBatch, pixelTexture);
            summonSystem?.Draw(spriteBatch, pixelTexture);

            if (player != null)
            {
                var heldItem = inventory.GetSlot(miningSystem.GetSelectedHotbarSlot())?.ItemType ?? ItemType.None;
                Texture2D itemTexture = itemTextureMap.ContainsKey(heldItem) ? itemTextureMap[heldItem] : null;

                int animationFrame = 0;
                bool isSword = heldItem >= ItemType.WoodSword && heldItem <= ItemType.RunicSword;
                bool isPickaxe = heldItem >= ItemType.WoodPickaxe && heldItem <= ItemType.RunicPickaxe;

                if (isSword && combatSystem != null && combatSystem.IsAttacking())
                {
                    animationFrame = combatSystem.GetCurrentAttackFrame();
                }
                else if (isPickaxe && miningSystem != null && miningSystem.GetCurrentlyMiningTile().HasValue)
                {
                    animationFrame = miningSystem.CurrentAnimationFrame;
                }
                player.Draw(spriteBatch, pixelTexture, heldItem, itemTexture, animationFrame);
            }

            spriteBatch.End();

            spriteBatch.Begin();
            hud?.Draw(spriteBatch, pixelTexture, font, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height,
                player.Position, world, isAutoMiningActive, timeSystem.IsRaining,
                player.Health, player.MaxHealth, player.AirBubbles, player.MaxAirBubbles,
                null, false, 0, 0, 0, 0, timeSystem, magicSystem);
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
        public void SetGameSoundsVolume(float v) { gameSoundsVolume = v; miningSystem?.SetSoundVolume(v); }
        private void PlayTestSound() { mineDirtSound?.Play(volume: gameSoundsVolume, pitch: 0.0f, pan: 0.0f); }
        public void ToggleAutoMining(bool? n) { isAutoMiningActive = n ?? !isAutoMiningActive; }

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
            const int MAX_RAIN_DROPS = 1000;
            while (rainParticles.Count < MAX_RAIN_DROPS)
            {
                rainParticles.Add(new Vector2(camera.Position.X + random.Next(-screenWidth, screenWidth * 2), camera.Position.Y + random.Next(-screenHeight, 0)));
            }
            for (int i = rainParticles.Count - 1; i >= 0; i--)
            {
                rainParticles[i] += new Vector2(500f, 800f) * deltaTime;
                if (rainParticles[i].Y > camera.Position.Y + screenHeight + 100f)
                {
                    rainParticles[i] = new Vector2(camera.Position.X + random.Next(-screenWidth, screenWidth * 2), camera.Position.Y - 100f);
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
            const int MAX_SNOW_FLAKES = 1500;
            while (snowParticles.Count < MAX_SNOW_FLAKES)
            {
                snowParticles.Add(new Vector2(camera.Position.X + random.Next(-screenWidth, screenWidth * 2), camera.Position.Y + random.Next(-screenHeight, 0)));
            }
            for (int i = snowParticles.Count - 1; i >= 0; i--)
            {
                Vector2 velocity = new Vector2((float)Math.Sin(snowParticles[i].Y * 0.01f) * 50f, 200f) * deltaTime;
                Vector2 newPosition = snowParticles[i] + velocity;
                if (world.IsSolidAtPosition((int)(newPosition.X / 32), (int)(newPosition.Y / 32)))
                {
                    snowParticles[i] = new Vector2(camera.Position.X + random.Next(-screenWidth, screenWidth * 2), camera.Position.Y - 100);
                }
                else
                {
                    snowParticles[i] = newPosition;
                    if (snowParticles[i].Y > camera.Position.Y + screenHeight + 100)
                    {
                        snowParticles[i] = new Vector2(camera.Position.X + random.Next(-screenWidth, screenWidth * 2), camera.Position.Y - 100);
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
                miningSystem.DropItem(player.GetCenterPosition(), item.ItemType, 1);
                inventory.RemoveItem(item.ItemType, 1);
            }
        }
        private void HandleChestMined(Point position, TileType chestType) => chestSystem.RemoveChest(position, inventory);
        private void HandleChestPlaced(Point position, ItemType itemType)
        {
            TileType chestType = itemType.ToTileType();
            ChestTier tier = ChestTier.Wood;
            if (chestType == TileType.SilverChest) tier = ChestTier.Silver;
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
            world = null; player = null; camera = null; inventory = null; miningSystem = null; lightingSystem = null;
            timeSystem = null; worldGenerator = null; chestSystem = null; inventoryUI = null; pauseMenu = null;
            saveMenu = null; miningOverlay = null; summonSystem = null; hud = null; chestUI = null; liquidSystem = null;
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
                data.InventorySlots.Add(slot != null && !slot.IsEmpty() ? new InventorySlotData { ItemType = (int)slot.ItemType, Count = slot.Count } : new InventorySlotData());
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

            inventory.AddItem(ItemType.RunicPickaxe, 1);
            inventory.AddItem(ItemType.Torch, 50);
            inventory.AddItem(ItemType.WoodSword, 1);
            inventory.AddItem(ItemType.CopperSword, 1);
            inventory.AddItem(ItemType.IronSword, 1);
            inventory.AddItem(ItemType.GoldSword, 1);
            inventory.AddItem(ItemType.WoodWand, 1);
            inventory.AddItem(ItemType.FireWand, 1);
            inventory.AddItem(ItemType.LightningWand, 1);
            inventory.AddItem(ItemType.NatureWand, 1);
            inventory.AddItem(ItemType.WaterWand, 1);
            inventory.AddItem(ItemType.HalfMoonWand, 1);
            inventory.AddItem(ItemType.RunicLaserWand, 1);
            inventory.AddItem(ItemType.WoodSummonStaff, 1);
            inventory.AddItem(ItemType.PieceOfFlesh, 75);
        }

        private void LoadTileSprites(World.World world)
        {
            var spriteMap = new Dictionary<string, TileType> { { "dirt", TileType.Dirt }, { "grass", TileType.Grass }, { "stone", TileType.Stone }, { "coalblock", TileType.Coal }, { "copperblock", TileType.Copper }, { "ironblock", TileType.Iron }, { "silverblock", TileType.Silver }, { "goldblock", TileType.Gold }, { "platinumblock", TileType.Platinum }, { "torch", TileType.Torch }, { "saplingplanteddirt", TileType.Sapling }, { "woodcraftingtable", TileType.WoodCraftingBench }, { "coppercraftingtable", TileType.CopperCraftingBench }, { "woodchest", TileType.WoodChest }, { "silverchest", TileType.SilverChest }, { "magicchest", TileType.MagicChest } };
            foreach (var sprite in spriteMap) { try { world.LoadTileSprite(sprite.Value, Content.Load<Texture2D>(sprite.Key)); } catch { } }
        }

        private void LoadItemSprites(InventoryUI iUI)
        {
            var spriteMap = new Dictionary<string, ItemType> { { "woodpickaxe", ItemType.WoodPickaxe }, { "stonepickaxe", ItemType.StonePickaxe }, { "copperpickaxe", ItemType.CopperPickaxe }, { "silverpickaxe", ItemType.SilverPickaxe }, { "platinumpickaxe", ItemType.PlatinumPickaxe }, { "runicpickaxe", ItemType.RunicPickaxe }, { "woodsword", ItemType.WoodSword }, { "CopperSword", ItemType.CopperSword }, { "IronSword", ItemType.IronSword }, { "SilverSword", ItemType.SilverSword }, { "GoldSword", ItemType.GoldSword }, { "PlatinumSword", ItemType.PlatinumSword }, { "wand", ItemType.WoodWand }, { "FireWand", ItemType.FireWand }, { "LightningWand", ItemType.LightningWand }, { "NatureWand", ItemType.NatureWand }, { "WaterWand", ItemType.WaterWand }, { "HalfMoonWand", ItemType.HalfMoonWand }, { "WoodSummonStaff", ItemType.WoodSummonStaff }, { "stick", ItemType.Stick }, { "copperbar", ItemType.CopperBar }, { "silverbar", ItemType.SilverBar }, { "platinumbar", ItemType.PlatinumBar }, { "torch", ItemType.Torch }, { "acorn", ItemType.Acorn } };
            foreach (var sprite in spriteMap) { try { Texture2D tex = Content.Load<Texture2D>(sprite.Key); iUI.LoadItemSprite(sprite.Value, tex); itemTextureMap[sprite.Value] = tex; } catch { } }
            try { Texture2D tex = Content.Load<Texture2D>("RunicSword Spritesheet"); iUI.LoadItemSprite(ItemType.RunicSword, tex); itemTextureMap[ItemType.RunicSword] = tex; } catch { }
            try { Texture2D tex = Content.Load<Texture2D>("RunicLaserWandSpriteSheet"); iUI.LoadItemSprite(ItemType.RunicLaserWand, tex); itemTextureMap[ItemType.RunicLaserWand] = tex; } catch { }
        }

        private void LoadSpellTextures(ProjectileSystem pS)
        {
            var spriteMap = new Dictionary<string, ProjectileType> { { "MagicBolt", ProjectileType.MagicBolt }, { "FireBolt", ProjectileType.FireBolt }, { "LightningSpell", ProjectileType.LightningBlast }, { "NatureVineSpell", ProjectileType.NatureVine }, { "WaterBubbleSpell", ProjectileType.WaterBubble }, { "HalfMoonSpell", ProjectileType.HalfMoonSlash }, { "RunicLaserBeamSpell", ProjectileType.RunicLaser } };
            foreach (var sprite in spriteMap) { try { pS.LoadTexture(sprite.Value, Content.Load<Texture2D>(sprite.Key)); } catch { } }
        }

        private void LoadCraftingItemSprites(InventoryUI iUI)
        {
            var spriteMap = new Dictionary<string, ItemType> { { "woodpickaxe", ItemType.WoodPickaxe }, { "stonepickaxe", ItemType.StonePickaxe }, { "copperpickaxe", ItemType.CopperPickaxe }, { "silverpickaxe", ItemType.SilverPickaxe }, { "platinumpickaxe", ItemType.PlatinumPickaxe }, { "woodsword", ItemType.WoodSword }, { "CopperSword", ItemType.CopperSword }, { "torch", ItemType.Torch }, { "stick", ItemType.Stick }, { "copperbar", ItemType.CopperBar }, { "silverbar", ItemType.SilverBar } };
            foreach (var sprite in spriteMap) { try { iUI.GetCraftingUI()?.LoadItemSprite(sprite.Value, Content.Load<Texture2D>(sprite.Key)); } catch { } }
        }
        #endregion
    }
}