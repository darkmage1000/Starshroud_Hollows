using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using StarshroudHollows.Entities;
using StarshroudHollows.Enums;
using StarshroudHollows.Player;
using StarshroudHollows.Systems;
using StarshroudHollows.UI;
using StarshroudHollows.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StarshroudHollows
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        #region Fields
        private World.World world;
        private Player.Player player; // Field declaration: Correctly Player.Player
        private Camera camera;
        private Texture2D pixelTexture;
        private Texture2D oozeEnemyTexture;
        private Texture2D zombieEnemyTexture;
        private Texture2D echoWispTexture;
        private Texture2D caveTrollBossTexture;
        private Texture2D playerSpriteSheet;
        private SpriteFont font;
        private Texture2D menuBackgroundTexture;
        private Inventory inventory;
        private BossSystem bossSystem;
        private PortalSystem portalSystem;
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
        private Systems.Housing.HousingSystem housingSystem;
        private ArmorSystem armorSystem;
        private InventoryUI inventoryUI;
        private PauseMenu pauseMenu;
        private MiningOverlay miningOverlay;
        private StartMenu startMenu;
        private SaveMenu saveMenu;
        private HUD hud;
        private ChestUI chestUI;
        private DialogueUI dialogueUI;
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
        private float healthPotionCooldown = 0f;
        private const float HEALTH_POTION_COOLDOWN_TIME = 30f;
        private const float HEALTH_POTION_HEAL_AMOUNT = 30f;
        private float bedSleepTimer = 0f;
        private const float BED_SLEEP_DURATION = 2f; // Hold E for 2 seconds
        private float doorToggleCooldown = 0f;
        private const float DOOR_TOGGLE_COOLDOWN = 0.3f; // Prevent spam-toggling
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

            try
            {
                playerSpriteSheet = Content.Load<Texture2D>("playersprite");  // Your 1028x1028 PNG
                Logger.Log("[SUCCESS] Loaded player sprite sheet.");
                               
                
               
            }
            catch { Logger.Log("[ERROR] Failed to load player sprite!"); }

            // Rest of your code unchanged
            try { oozeEnemyTexture = Content.Load<Texture2D>("OozeEnemy"); } catch { }
            try { zombieEnemyTexture = Content.Load<Texture2D>("ForestZombieSprite"); } catch { }
            try { echoWispTexture = Content.Load<Texture2D>("EchoWispSummon"); } catch { }
            try { caveTrollBossTexture = Content.Load<Texture2D>("cavetrollbossv2"); } catch { }
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

            // Update health potion cooldown
            if (healthPotionCooldown > 0)
            {
                healthPotionCooldown -= deltaTime;
                if (healthPotionCooldown < 0) healthPotionCooldown = 0;
            }
            
            // Update door toggle cooldown
            if (doorToggleCooldown > 0)
            {
                doorToggleCooldown -= deltaTime;
                if (doorToggleCooldown < 0) doorToggleCooldown = 0;
            }

            if (chestUI?.IsOpen == true) { chestUI.Update(); base.Update(gameTime); return; }
            if (saveMenu?.IsOpen == true) { saveMenu.Update(deltaTime, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height); base.Update(gameTime); return; }

            if (pauseMenu != null && keyboardState.IsKeyDown(Keys.Escape) && !previousKeyboardState.IsKeyDown(Keys.Escape))
            {
                pauseMenu.TogglePause();
            }
            
            // Check if we just quit to menu - stop update immediately
            if (startMenu.GetState() != MenuState.Playing)
            {
                previousKeyboardState = keyboardState;
                previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            // DEBUG: F7 key to check housing status
            if (keyboardState.IsKeyDown(Keys.F7) && !previousKeyboardState.IsKeyDown(Keys.F7) && housingSystem != null)
            {
                Logger.Log("=== HOUSING DEBUG ===");
                Logger.Log($"Valid Houses: {housingSystem.GetValidHouses().Count}");
                Logger.Log($"Pending Houses: {housingSystem.GetPendingHouses().Count}");
                Logger.Log($"Active NPCs: {housingSystem.GetActiveNPCs().Count}");
                Logger.Log($"First Night Survived: {timeSystem.HasCompletedFirstNight}");
                Logger.Log($"Player Set Flag: {housingSystem.PlayerSurvivedFirstNight}");
            }
            
            // DEBUG: F8 key to force validate nearby house
            if (keyboardState.IsKeyDown(Keys.F8) && !previousKeyboardState.IsKeyDown(Keys.F8) && housingSystem != null)
            {
                housingSystem.ForceValidateNearbyHouses(player.Position);
            }

            if (pauseMenu != null) pauseMenu.Update();
            
            // ANOTHER check after pauseMenu.Update() in case QuitToMenu was called
            if (startMenu.GetState() != MenuState.Playing)
            {
                previousKeyboardState = keyboardState;
                previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            if (pauseMenu?.IsPaused == true)
            {
                previousKeyboardState = keyboardState;
                previousMouseState = mouseState;
                base.Update(gameTime);
                return;
            }

            totalPlayTime += deltaTime;
            if (timeSystem != null) timeSystem.Update(deltaTime);
            UpdateRainParticles(deltaTime, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            UpdateSnowParticles(deltaTime, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            liquidSystem?.UpdateFlow();

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

            // Health Potion Hotkey (H key) - Use from anywhere in inventory
            if (keyboardState.IsKeyDown(Keys.H) && !previousKeyboardState.IsKeyDown(Keys.H))
            {
                TryUseHealthPotion();
            }
            
            // BED INTERACTION: Hold E to sleep
            if (keyboardState.IsKeyDown(Keys.E) && inventoryUI?.IsInventoryOpen == false)
            {
                // First check for door to toggle (instant)
                Point? doorTile = FindNearbyDoor();
                if (doorTile.HasValue && doorToggleCooldown <= 0)
                {
                    // E key was just pressed (not held)
                    if (!previousKeyboardState.IsKeyDown(Keys.E))
                    {
                        ToggleDoor(doorTile.Value);
                        doorToggleCooldown = DOOR_TOGGLE_COOLDOWN;
                    }
                }
                // Then check for bed to sleep (hold E)
                else
                {
                    TryUseBed(keyboardState, deltaTime);
                }
            }
            else
            {
                // Reset sleep timer if not holding E
                bedSleepTimer = 0f;
            }

            if (mouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released)
            {
                // FIRST: Check for chest to open
                Point? nearbyChest = FindNearbyChest();
                if (nearbyChest.HasValue)
                {
                    var chest = chestSystem?.GetChest(nearbyChest.Value);
                    if (chest != null)
                    {
                        chestUI.OpenChest(chest, inventory);
                        Logger.Log($"[PLAYER] Opened chest at ({nearbyChest.Value.X}, {nearbyChest.Value.Y})");
                    }
                }
                // SECOND: Check for door to toggle
                else
                {
                    Point? doorTile = FindNearbyDoor();
                    if (doorTile.HasValue && doorToggleCooldown <= 0)
                    {
                        ToggleDoor(doorTile.Value);
                        doorToggleCooldown = DOOR_TOGGLE_COOLDOWN;
                    }
                    // THIRD: Check for bed to set spawn
                    else
                    {
                        Point? bedTile = FindNearbyBed();
                        if (bedTile.HasValue)
                        {
                            Vector2 bedSpawnPos = new Vector2(bedTile.Value.X * World.World.TILE_SIZE, bedTile.Value.Y * World.World.TILE_SIZE);
                            player.SetBedSpawn(bedSpawnPos);
                            Logger.Log($"[PLAYER] Bed spawn point set!");
                        }
                        // FOURTH: Check for NPC interaction
                        else
                        {
                            var nearbyNPC = housingSystem?.GetNearestNPC(playerCenter);
                            if (nearbyNPC != null && nearbyNPC.IsPlayerNearby(playerCenter))
                            {
                                // Talk to NPC
                                string dialogue = nearbyNPC.GetCurrentDialogue();
                                Logger.Log($"[NPC] {nearbyNPC.Name}: {dialogue}");
                                dialogueUI.ShowDialogue(nearbyNPC.Name, dialogue); // Show on screen
                                nearbyNPC.CycleDialogue();
                            }
                            else
                            {
                                // Normal item usage
                                var selectedSlot = inventory.GetSlot(miningSystem.GetSelectedHotbarSlot());
                                if (selectedSlot != null && !selectedSlot.IsEmpty())
                                {
                                    if (selectedSlot.ItemType == ItemType.HealthPotion)
                                    {
                                        TryUseHealthPotion();
                                    }
                                    else if (selectedSlot.ItemType == ItemType.RecallPotion)
                                    {
                                        player.TeleportToSpawn();
                                        inventory.RemoveItem(ItemType.RecallPotion, 1);
                                    }
                                    else if (selectedSlot.ItemType == ItemType.TrollBait)
                                    {
                                        // Check if clicking on an altar
                                        int tileX = (int)(player.GetCenterPosition().X / World.World.TILE_SIZE);
                                        int tileY = (int)(player.GetCenterPosition().Y / World.World.TILE_SIZE);
                                        
                                        // Check nearby tiles for altar
                                        bool activatedAltar = false;
                                        for (int dx = -2; dx <= 2; dx++)
                                        {
                                            for (int dy = -2; dy <= 2; dy++)
                                            {
                                                Point altarPos = new Point(tileX + dx, tileY + dy);
                                                if (portalSystem.TryActivateAltar(altarPos, ItemType.TrollBait, inventory))
                                                {
                                                    activatedAltar = true;
                                                    Logger.Log("[PORTAL] Altar activated! Portal spawned!");
                                                    break;
                                                }
                                            }
                                            if (activatedAltar) break;
                                        }
                                        
                                        // If no altar nearby, do direct summon (old way)
                                        if (!activatedAltar)
                                        {
                                            string errorMessage = bossSystem.TrySummonCaveTroll(player.Position);
                                            if (errorMessage == null)
                                            {
                                                inventory.RemoveItem(ItemType.TrollBait, 1);
                                                Logger.Log("[BOSS] Cave Troll summoned! Troll Bait consumed.");
                                            }
                                            else Logger.Log($"[BOSS] Cannot summon: {errorMessage}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // L key to toggle automine
            if (keyboardState.IsKeyDown(Keys.L) && !previousKeyboardState.IsKeyDown(Keys.L))
            {
                ToggleAutoMining(null);
                Logger.Log($"[AUTOMINE] Automine {(isAutoMiningActive ? "ENABLED" : "DISABLED")}");
            }

            if (keyboardState.IsKeyDown(Keys.Q) && !previousKeyboardState.IsKeyDown(Keys.Q) && inventoryUI?.IsInventoryOpen == false)
                DropSelectedItem();

            var heldItem = inventory.GetSlot(miningSystem.GetSelectedHotbarSlot())?.ItemType ?? ItemType.None;
            bool isSword = (heldItem >= ItemType.WoodSword && heldItem <= ItemType.RunicSword) || heldItem == ItemType.TrollClub;
            bool isWand = (heldItem >= ItemType.WoodWand && heldItem <= ItemType.RunicLaserWand);
            bool isHammer = heldItem == ItemType.Hammer;

            if (inventoryUI?.IsInventoryOpen == false)
            {
                bool allowManualMining = !isSword && !isWand;
                if (isAutoMiningActive || allowManualMining)
                {
                    // Corrected constant access
                    miningSystem?.Update(gameTime, playerCenter, camera, player.Position, Player.Player.PLAYER_WIDTH, Player.Player.PLAYER_HEIGHT, isAutoMiningActive, lastPlayerDirection);
                }
            }

            if (isSword) combatSystem?.Update(deltaTime, player.Position, player.GetFacingRight(), mouseState, previousMouseState, heldItem);

            magicSystem?.Update(deltaTime, mouseState, previousMouseState, heldItem);
            projectileSystem?.Update(deltaTime, enemySpawner?.GetActiveEnemies() ?? new List<Interfaces.IDamageable>());

            var summonTargets = new List<Interfaces.IDamageable>();
            if (enemySpawner != null) summonTargets.AddRange(enemySpawner.GetActiveEnemies());
            if (bossSystem != null && bossSystem.HasActiveBoss) summonTargets.Add(bossSystem.ActiveTroll);
            summonSystem?.Update(deltaTime, playerCenter, summonTargets);
            
            // Update housing system
            housingSystem?.Update(deltaTime);
            
            // NEW: Let NPCs fight nearby enemies!
            if (housingSystem != null && projectileSystem != null)
            {
                var activeEnemies = new List<Interfaces.IDamageable>();
                if (enemySpawner != null) activeEnemies.AddRange(enemySpawner.GetActiveEnemies());
                if (bossSystem != null && bossSystem.HasActiveBoss) activeEnemies.Add(bossSystem.ActiveTroll);
                
                foreach (var npc in housingSystem.GetActiveNPCs())
                {
                    if (npc != null && npc.CanCombat)
                    {
                        npc.TryAttack(activeEnemies, projectileSystem);
                    }
                }
            }
            
            // Update housing system with first night status
            if (housingSystem != null && timeSystem != null)
            {
                // CRITICAL: Always sync the flag from TimeSystem to HousingSystem
                if (timeSystem.HasCompletedFirstNight && !housingSystem.PlayerSurvivedFirstNight)
                {
                    Logger.Log("[GAME] Syncing first night completion flag to housing system");
                    housingSystem.PlayerSurvivedFirstNight = true;
                }
            }

            // Corrected player type argument: Player.Player
            bossSystem?.Update(deltaTime, playerCenter, player, inventory, combatSystem, projectileSystem, heldItem);

            // Portal system update
            portalSystem?.Update(deltaTime, player.Position, Player.Player.PLAYER_WIDTH, Player.Player.PLAYER_HEIGHT);

            // Check if player entered arena
            if (portalSystem != null && portalSystem.IsInArena)
            {
                // Teleport player to arena spawn
                Vector2 arenaSpawn = portalSystem.GetArenaPlayerSpawn();
                if (player.Position != arenaSpawn)
                {
                    player.Position = arenaSpawn;
                    camera.Position = player.Position;
                }

                // Spawn boss if not already active
                if (!bossSystem.HasActiveBoss)
                {
                    Vector2 bossSpawn = portalSystem.GetArenaBossSpawn();
                    string error = bossSystem.TrySummonCaveTroll(bossSpawn);
                    if (error != null)
                    {
                        Logger.Log($"[PORTAL] Failed to spawn boss in arena: {error}");
                    }
                }

                // Check if boss was defeated - spawn exit portal
                if (bossSystem != null && !bossSystem.HasActiveBoss && bossSystem.ActiveTroll != null && !bossSystem.ActiveTroll.IsAlive)
                {
                    portalSystem.OnBossDefeated();
                }
            }
            else if (portalSystem != null && !portalSystem.IsInArena)
            {
                // Player left arena - teleport to return position
                Vector2 returnPos = portalSystem.GetReturnPosition();
                if (returnPos != Vector2.Zero && Vector2.Distance(player.Position, returnPos) > 100)
                {
                    player.Position = returnPos;
                    camera.Position = player.Position;
                }
            }

            if (enemySpawner != null)
            {
                Rectangle cameraView = camera.GetVisibleArea(World.World.TILE_SIZE);
                enemySpawner.Update(deltaTime, playerCenter, timeSystem, cameraView);
                // Corrected constant access
                enemySpawner.CheckCombatCollisions(combatSystem, player.Position, Player.Player.PLAYER_WIDTH, Player.Player.PLAYER_HEIGHT, heldItem, inventory);

                float healthBefore = player.Health;
                // Corrected argument type (Player.Player) and constant access
                enemySpawner.CheckPlayerCollisions(player.Position, Player.Player.PLAYER_WIDTH, Player.Player.PLAYER_HEIGHT, player);

                if (healthBefore > 0 && player.Health == player.GetMaxHealth() && healthBefore < player.GetMaxHealth())
                {
                    magicSystem?.RestoreMana();
                }
            }

            hud?.UpdateMinimapData(world);
            inventoryUI.Update(gameTime, player.Position, world, GraphicsDevice.Viewport.Height);
            dialogueUI?.Update(deltaTime);
            #endregion

            previousKeyboardState = keyboardState;
            previousMouseState = mouseState;

            base.Update(gameTime);
        }

        private void InitializeGameSystems(Vector2 playerPosition, bool isNewGame)
        {
            player = new Player.Player(world, playerPosition); // Corrected player instantiation
            player.LoadContent(playerSpriteSheet); // Load player sprite!
            world.SetPlayer(player);
            if (startMenu.IsDebugModeEnabled) player.SetDebugMode(true);
            world.SetLiquidSystem(liquidSystem);
            camera = new Camera(GraphicsDevice.Viewport) { Position = player.Position };
            inventory = new Inventory();
            bossSystem = new BossSystem(world);
            portalSystem = new PortalSystem(world);
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
            housingSystem = new Systems.Housing.HousingSystem(world);
            armorSystem = new ArmorSystem(inventory);
            magicSystem = new MagicSystem(player, projectileSystem, summonSystem, world, camera);
            inventoryUI = new InventoryUI(inventory, miningSystem, armorSystem);
            inventoryUI.Initialize(pixelTexture, font);
            LoadItemSprites(inventoryUI);
            LoadCraftingItemSprites(inventoryUI);
            chestUI = new ChestUI(world);
            dialogueUI = new DialogueUI();
            pauseMenu = new PauseMenu(() => saveMenu?.Open(), QuitToMenu, (v) => { musicVolume = v; MediaPlayer.Volume = v; }, musicVolume, ToggleFullscreen, hud.ToggleFullscreenMap, hud, (n) => ToggleAutoMining(n), isAutoMiningActive, SetGameSoundsVolume, gameSoundsVolume);
            saveMenu = new SaveMenu(SaveGame);
            miningOverlay = new MiningOverlay(world, miningSystem, chestSystem);
            if (liquidSystem != null) { for (int i = 0; i < 5000; i++) liquidSystem.UpdateFlow(); }
            worldGenerated = true;
        }

        protected override void Draw(GameTime gameTime)
        {
            // FIXED: Use time-based sky color for day/night cycle
            Color skyColor = timeSystem != null ? timeSystem.GetSkyColor() : new Color(135, 206, 235);
            GraphicsDevice.Clear(skyColor);

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
                
                // Enable player light when holding torch or lava bucket
                var heldItem = inventory.GetSlot(miningSystem.GetSelectedHotbarSlot())?.ItemType ?? ItemType.None;
                bool isHoldingLightSource = heldItem == ItemType.Torch || heldItem == ItemType.LavaBucket;
                lightingSystem.SetPlayerLight(player.GetCenterPosition(), isHoldingLightSource);
            }

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, camera.GetTransformMatrix());

            world.Draw(spriteBatch, camera, pixelTexture, lightingSystem, miningSystem, player.IsDebugModeActive());
            portalSystem?.Draw(spriteBatch, pixelTexture);
            housingSystem?.Draw(spriteBatch, pixelTexture); // Draw NPCs
            enemySpawner?.DrawEnemies(spriteBatch, oozeEnemyTexture, zombieEnemyTexture, pixelTexture);
            bossSystem?.Draw(spriteBatch, caveTrollBossTexture, pixelTexture);
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

            // NEW: Draw rain/snow with camera transform for proper positioning
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, camera.GetTransformMatrix());
            
            // Draw rain particles if raining
            if (timeSystem != null && timeSystem.IsRaining)
            {
                foreach (Vector2 raindrop in rainParticles)
                {
                    Rectangle rainRect = new Rectangle(
                        (int)raindrop.X, 
                        (int)raindrop.Y, 
                        2, 
                        12
                    );
                    spriteBatch.Draw(pixelTexture, rainRect, Color.LightBlue * 0.6f);
                }
            }
            
            // Draw snow particles if in snow biome
            if (worldGenerator != null && player != null)
            {
                int playerTileX = (int)(player.Position.X / World.World.TILE_SIZE);
                int snowStart = worldGenerator.GetSnowBiomeStartX();
                int snowEnd = worldGenerator.GetSnowBiomeEndX();
                bool inSnowBiome = playerTileX >= snowStart && playerTileX <= snowEnd;
                
                if (inSnowBiome)
                {
                    foreach (Vector2 snowflake in snowParticles)
                    {
                        Rectangle snowRect = new Rectangle(
                            (int)snowflake.X, 
                            (int)snowflake.Y, 
                            4, 
                            4
                        );
                        spriteBatch.Draw(pixelTexture, snowRect, Color.White * 0.8f);
                    }
                }
            }
            
            spriteBatch.End();

            spriteBatch.Begin();
            
            hud?.Draw(spriteBatch, pixelTexture, font, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height,
                player.Position, world, isAutoMiningActive, timeSystem.IsRaining,
                player.Health, player.MaxHealth, player.AirBubbles, player.MaxAirBubbles,
                null, false, 0, 0, 0, 0, timeSystem, magicSystem, healthPotionCooldown);
            inventoryUI?.Draw(spriteBatch, pixelTexture, font, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            chestUI?.Draw(spriteBatch, pixelTexture, font);
            dialogueUI?.Draw(spriteBatch, pixelTexture, font, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            pauseMenu?.Draw(spriteBatch, pixelTexture, font, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            if (saveMenu != null && saveMenu.IsOpen)
            {
                saveMenu.Draw(spriteBatch, pixelTexture, font, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }

        #region Helper Methods
        private void TryUseBed(KeyboardState keyboardState, float deltaTime)
        {
            // Find nearby bed
            Point? bedTile = FindNearbyBed();
            
            if (bedTile.HasValue)
            {
                // Holding E - charge sleep timer
                if (keyboardState.IsKeyDown(Keys.E))
                {
                    bedSleepTimer += deltaTime;
                    
                    if (bedSleepTimer >= BED_SLEEP_DURATION)
                    {
                        // Sleep!
                        timeSystem.AdvanceToMorning();
                        player.Heal(player.GetMaxHealth()); // Full heal when sleeping
                        Logger.Log("[PLAYER] Slept in bed. Time advanced to morning, fully healed!");
                        bedSleepTimer = 0f;
                    }
                }
                else
                {
                    bedSleepTimer = 0f;
                }
            }
            else
            {
                bedSleepTimer = 0f;
            }
        }
        
        private Point? FindNearbyBed()
        {
            int playerTileX = (int)(player.Position.X / World.World.TILE_SIZE);
            int playerTileY = (int)(player.Position.Y / World.World.TILE_SIZE);
            
            // Check 3x3 area around player
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int checkX = playerTileX + dx;
                    int checkY = playerTileY + dy;
                    
                    var tile = world?.GetTile(checkX, checkY);
                    if (tile != null && tile.Type == TileType.Bed)
                    {
                        return new Point(checkX, checkY);
                    }
                }
            }
            
            return null;
        }
        
        private Point? FindNearbyChest()
        {
            int playerTileX = (int)(player.Position.X / World.World.TILE_SIZE);
            int playerTileY = (int)(player.Position.Y / World.World.TILE_SIZE);
            
            // Check 3x3 area around player (better range)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int checkX = playerTileX + dx;
                    int checkY = playerTileY + dy;
                    
                    var tile = world?.GetTile(checkX, checkY);
                    if (tile != null && (tile.Type == TileType.WoodChest || 
                                        tile.Type == TileType.SilverChest || 
                                        tile.Type == TileType.MagicChest))
                    {
                        return new Point(checkX, checkY);
                    }
                }
            }
            
            // Check one more tile away in each direction for better range
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    // Skip tiles we already checked
                    if (dx >= -1 && dx <= 1 && dy >= -1 && dy <= 1) continue;
                    
                    int checkX = playerTileX + dx;
                    int checkY = playerTileY + dy;
                    
                    var tile = world?.GetTile(checkX, checkY);
                    if (tile != null && (tile.Type == TileType.WoodChest || 
                                        tile.Type == TileType.SilverChest || 
                                        tile.Type == TileType.MagicChest))
                    {
                        return new Point(checkX, checkY);
                    }
                }
            }
            
            return null;
        }
        
        private Point? FindNearbyDoor()
        {
            int playerTileX = (int)(player.Position.X / World.World.TILE_SIZE);
            int playerTileY = (int)(player.Position.Y / World.World.TILE_SIZE);
            
            // Check 3x3 area around player
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int checkX = playerTileX + dx;
                    int checkY = playerTileY + dy;
                    
                    var tile = world?.GetTile(checkX, checkY);
                    if (tile != null && tile.Type == TileType.Door)
                    {
                        return new Point(checkX, checkY);
                    }
                }
            }
            
            return null;
        }
        
        private void ToggleDoor(Point doorPos)
        {
            var door = world?.GetTile(doorPos.X, doorPos.Y);
            if (door != null && door.Type == TileType.Door)
            {
                // Create a NEW tile with the OPPOSITE door state
                bool newState = !door.IsDoorOpen;
                
                Tile updatedDoor = new Tile(TileType.Door);
                updatedDoor.IsDoorOpen = newState;
                updatedDoor.Health = door.Health;
                updatedDoor.WallType = door.WallType;
                
                // Set the tile BEFORE logging so we can verify it worked
                world.SetTile(doorPos.X, doorPos.Y, updatedDoor);
                
                // Verify the change stuck by reading it back
                var verifyDoor = world.GetTile(doorPos.X, doorPos.Y);
                string state = newState ? "OPENED" : "CLOSED";
                Logger.Log($"[DOOR] Door {state} at ({doorPos.X}, {doorPos.Y}). Open={verifyDoor?.IsDoorOpen}, Walkable={newState}");
            }
        }
        
        private void TryUseHealthPotion()
        {
            // Check cooldown
            if (healthPotionCooldown > 0)
            {
                Logger.Log($"[PLAYER] Health potion on cooldown! {healthPotionCooldown:F1}s remaining");
                return;
            }

            // Check if player has health potion
            if (!inventory.HasItem(ItemType.HealthPotion, 1))
            {
                Logger.Log("[PLAYER] No health potions in inventory!");
                return;
            }

            // Check if health is already full
            if (player.Health >= player.GetMaxHealth())
            {
                Logger.Log("[PLAYER] Health already full!");
                return;
            }

            // Use the health potion
            player.Heal(HEALTH_POTION_HEAL_AMOUNT);
            inventory.RemoveItem(ItemType.HealthPotion, 1);
            healthPotionCooldown = HEALTH_POTION_COOLDOWN_TIME;
            Logger.Log($"[PLAYER] Used Health Potion! Healed {HEALTH_POTION_HEAL_AMOUNT} HP. Health: {player.Health}/{player.GetMaxHealth()}");
        }
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
            TileType chestTileType = itemType.ToTileType();
            
            // Check if placing a summon altar
            if (chestTileType == TileType.SummonAltar)
            {
                portalSystem?.PlaceAltar(position, BossType.CaveTroll);
                return;
            }
            
            ChestTier tier = ChestTier.Wood;

            // FIX: Explicitly cast the TileType to ChestTier
            if (chestTileType == TileType.SilverChest)
            {
                tier = (ChestTier)(int)TileType.SilverChest;
            }
            else if (chestTileType == TileType.MagicChest)
            {
                tier = (ChestTier)(int)TileType.MagicChest;
            }
            // Fallback for WoodChest is ChestTier.Wood, already set above

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
            
            // Clear all systems
            world = null; player = null; camera = null; inventory = null; miningSystem = null; lightingSystem = null;
            timeSystem = null; worldGenerator = null; chestSystem = null; inventoryUI = null; pauseMenu = null;
            saveMenu = null; miningOverlay = null; summonSystem = null; hud = null; chestUI = null; liquidSystem = null; 
            dialogueUI = null; housingSystem = null; armorSystem = null; bossSystem = null; portalSystem = null; combatSystem = null; 
            enemySpawner = null; magicSystem = null; projectileSystem = null;
            
            // Clear particle lists
            rainParticles?.Clear();
            snowParticles?.Clear();
            
            // Go back to menu
            startMenu.SetState(MenuState.MainMenu);
        }
        private void SaveGame(int slotIndex)
        {
            Logger.Log("[SAVE] ========== STARTING SAVE ==========");
            Logger.Log($"[SAVE] Save slot: {slotIndex + 1}");
            Logger.Log($"[SAVE] Player position: {player.Position}");
            Logger.Log($"[SAVE] Game time: {timeSystem.GetCurrentTime()}");
            Logger.Log($"[SAVE] Play time: {(int)totalPlayTime} seconds");
            
            // CRITICAL FIX: Get ONLY the modified tiles from the dictionary, not all loaded chunks!
            Logger.Log($"[SAVE] Collecting modified tiles from change tracker...");
            var modifiedTilesList = world.GetModifiedTiles();
            Logger.Log($"[SAVE] Found {modifiedTilesList.Count} modified tiles");
            
            // Convert List<TileChangeData> to Dictionary<string, TileData>
            var worldTiles = new Dictionary<string, Systems.TileData>();
            foreach (var change in modifiedTilesList)
            {
                string key = $"{change.X},{change.Y}";
                worldTiles[key] = new Systems.TileData
                {
                    TileType = change.TileType,
                    WallType = change.WallType,
                    LiquidVolume = change.LiquidVolume,
                    IsDoorOpen = false, // TileChangeData doesn't have this, set default
                    IsPartOfTree = false // TileChangeData doesn't have this, set default
                };
            }
            
            SaveData data = new SaveData
            {
                SaveName = $"Starshroud Save {slotIndex + 1}",
                SaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                WorldSeed = currentWorldSeed,
                PlayerPosition = player.Position,
                GameTime = timeSystem.GetCurrentTime(),
                WorldWidth = World.World.WORLD_WIDTH,
                WorldHeight = World.World.WORLD_HEIGHT,
                PlayTimeSeconds = (int)totalPlayTime,
                HasCompletedFirstNight = timeSystem.HasCompletedFirstNight,
                WorldTiles = worldTiles,
                Chests = chestSystem.GetSaveData(),
                NPCs = housingSystem.GetNPCSaveData(),
                Houses = housingSystem.GetHouseSaveData(),
                SnowBiomeStartX = worldGenerator.GetSnowBiomeStartX(),
                SnowBiomeEndX = worldGenerator.GetSnowBiomeEndX()
            };
            
            Logger.Log($"[SAVE] Collecting inventory data ({inventory.GetSlotCount()} slots)...");
            for (int i = 0; i < inventory.GetSlotCount(); i++)
            {
                var slot = inventory.GetSlot(i);
                data.InventorySlots.Add(slot != null && !slot.IsEmpty() ? new InventorySlotData { ItemType = (int)slot.ItemType, Count = slot.Count } : new InventorySlotData());
            }
            
            Logger.Log($"[SAVE] Data collected. Calling SaveSystem.SaveGame...");
            SaveSystem.SaveGame(data, slotIndex);
            
            Logger.Log($"[SAVE] Complete!");
            Logger.Log("[SAVE] ====================================");
        }

        private void GenerateWorld()
        {
            // FIXED: Check if we're loading a save or generating new world
            bool isLoadingSave = startMenu.IsLoadingSavedGame();
            int loadSlotIndex = startMenu.GetLoadingSlotIndex();

            hud = new HUD();
            hud.Initialize(GraphicsDevice, font);
            
            if (isLoadingSave && loadSlotIndex >= 0)
            {
                // LOAD SAVED GAME - NO WORLD GENERATION!
                Logger.Log($"[GAME] ========== LOADING SAVE ==========" );
                Logger.Log($"[GAME] Loading save from slot {loadSlotIndex + 1}");
                SaveData saveData = SaveSystem.LoadGame(loadSlotIndex);
                
                if (saveData != null)
                {
                    Logger.Log($"[GAME] Save data loaded! Regenerating world from seed {saveData.WorldSeed}");
                    
                    // Use saved world seed and time
                    currentWorldSeed = saveData.WorldSeed;
                    totalPlayTime = saveData.PlayTimeSeconds;
                    
                    // REGENERATE the world from the seed - same as new game!
                    world = new World.World(hud);
                    timeSystem = new TimeSystem();
                    timeSystem.SetCurrentTime(saveData.GameTime);
                    
                    if (saveData.HasCompletedFirstNight)
                    {
                        timeSystem.SetFirstNightCompleted();
                        Logger.Log("[GAME] Restored first night completion status");
                    }
                    
                    lightingSystem = new LightingSystem(world, timeSystem);
                    chestSystem = new ChestSystem();
                    liquidSystem = new LiquidSystem(world);
                    world.SetLiquidSystem(liquidSystem);
                    LoadTileSprites(world);
                    
                    // Regenerate world with same seed!
                    worldGenerator = new WorldGenerator(world, currentWorldSeed, chestSystem);
                    worldGenerator.OnProgressUpdate = (p, m) => startMenu.SetLoadingProgress(p * 0.8f, m);
                    worldGenerator.Generate();
                    
                    // CRITICAL FIX: Load saved world tiles!
                    Logger.Log($"[GAME] Loading {saveData.WorldTiles.Count} saved tiles...");
                    world.LoadAllTiles(saveData.WorldTiles);
                    Logger.Log("[GAME] World tiles loaded successfully!");
                    
                    world.EnableChunkUnloading();
                    world.DisableWorldUpdates();
                    // CRITICAL FIX: Enable tile change tracking so modifications are saved!
                    world.EnableTileChangeTracking();
                    Logger.Log("[GAME] Tile change tracking enabled - future changes will be saved");
                    
                    // Initialize game systems with saved player position
                    startMenu.SetLoadingProgress(0.9f, "Initializing game systems...");
                    InitializeGameSystems(saveData.PlayerPosition, false);
                    
                    // Load houses BEFORE NPCs (NPCs need houses to exist)
                    housingSystem.LoadHouses(saveData.Houses);
                    
                    // CRITICAL FIX: Load chests from save data!
                    Logger.Log($"[GAME] Loading {saveData.Chests.Count} chests...");
                    chestSystem.LoadFromData(saveData.Chests);
                    Logger.Log("[GAME] Chests loaded successfully!");
                    
                    // Load NPCs
                    housingSystem.LoadNPCs(saveData.NPCs);
                    
                    // Restore inventory
                    for (int i = 0; i < saveData.InventorySlots.Count && i < inventory.GetSlotCount(); i++)
                    {
                        var slotData = saveData.InventorySlots[i];
                        if (slotData.ItemType != (int)ItemType.None)
                        {
                            var slot = inventory.GetSlot(i);
                            if (slot != null)
                            {
                                slot.ItemType = (ItemType)slotData.ItemType;
                                slot.Count = slotData.Count;
                            }
                        }
                    }
                    
                    startMenu.SetLoadingProgress(1.0f, "Complete!");
                    Logger.Log($"[GAME] Save loaded successfully! Total playtime: {totalPlayTime}s");
                    Logger.Log("[GAME] ====================================");
                }
                else
                {
                    Logger.Log($"[GAME] Failed to load save or save is empty! Creating new world instead...");
                    isLoadingSave = false;
                }
            }
            
            if (!isLoadingSave)
            {
                // GENERATE NEW WORLD
                Logger.Log("[GAME] Generating new world...");
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
                // Use Player.Player.PLAYER_HEIGHT
                InitializeGameSystems(worldGenerator.GetSpawnPosition(Player.Player.PLAYER_HEIGHT), true);

                inventory.AddItem(ItemType.WoodPickaxe, 1);
                inventory.AddItem(ItemType.Hammer, 1);
                inventory.AddItem(ItemType.WoodSword, 1);
                inventory.AddItem(ItemType.WoodWand, 1);
                inventory.AddItem(ItemType.WoodSummonStaff, 1);
                inventory.AddItem(ItemType.Torch, 50);
                inventory.AddItem(ItemType.RecallPotion, 1);
                inventory.AddItem(ItemType.Door, 2);
                
                // Starting walls for testing
                inventory.AddItem(ItemType.DirtWall, 100);
                inventory.AddItem(ItemType.StoneWall, 100);
                inventory.AddItem(ItemType.WoodWall, 100);
                
                Logger.Log("[GAME] New world generated successfully!");
            }
        }

        private void LoadTileSprites(World.World world)
        {
            var spriteMap = new Dictionary<string, TileType> { 
                // Base Blocks
                { "dirt", TileType.Dirt }, 
                { "grass", TileType.Grass }, 
                { "stone", TileType.Stone }, 
                // Ore Blocks
                { "coalblock", TileType.Coal }, 
                { "copperblock", TileType.Copper }, 
                { "ironblock", TileType.Iron }, 
                { "silverblock", TileType.Silver }, 
                { "goldblock", TileType.Gold }, 
                { "platinumblock", TileType.Platinum }, 
                // Snow/Ice Biome
                { "snowblock", TileType.Snow }, 
                { "snowgrassblock", TileType.SnowGrass },
                { "ice block", TileType.Ice },
                { "icicle", TileType.Icicle },
                // Placeable/Functional
                { "torch", TileType.Torch }, 
                { "saplingplanteddirt", TileType.Sapling }, 
                { "woodcraftingtable", TileType.WoodCraftingBench }, 
                { "coppercraftingtable", TileType.CopperCraftingBench }, 
                { "woodchest", TileType.WoodChest }, 
                { "silverchest", TileType.SilverChest }, 
                { "magicchest", TileType.MagicChest },
                { "bed", TileType.Bed },
                // Walls - Note: Wall sprites use "wall" suffix
                { "stonewall", TileType.StoneWall },
                { "forestwoodwall", TileType.WoodWall },
                { "snowtreewoodwall", TileType.WoodWall },
                { "jungletreewoodwall", TileType.WoodWall },
                { "swamptreewoodwall", TileType.WoodWall },
                { "volcanictreewoodwall", TileType.WoodWall }
            };
            foreach (var sprite in spriteMap) { try { world.LoadTileSprite(sprite.Value, Content.Load<Texture2D>(sprite.Key)); } catch (Exception ex) { Logger.Log($"[ERROR] Failed to load tile sprite '{sprite.Key}': {ex.Message}"); } }
        }

        private void LoadItemSprites(InventoryUI iUI)
        {
            var spriteMap = new Dictionary<string, ItemType> { 
                // Pickaxes
                { "woodpickaxe", ItemType.WoodPickaxe }, 
                { "stonepickaxe", ItemType.StonePickaxe }, 
                { "copperpickaxe", ItemType.CopperPickaxe }, 
                { "ironpickaxe", ItemType.IronPickaxe }, 
                { "silverpickaxe", ItemType.SilverPickaxe }, 
                { "goldpickaxe", ItemType.GoldPickaxe }, 
                { "platinumpickaxe", ItemType.PlatinumPickaxe }, 
                { "runicpickaxe", ItemType.RunicPickaxe }, 
                // Swords
                { "woodsword", ItemType.WoodSword }, 
                { "CopperSword", ItemType.CopperSword }, 
                { "IronSword", ItemType.IronSword }, 
                { "SilverSword", ItemType.SilverSword }, 
                { "GoldSword", ItemType.GoldSword }, 
                { "PlatinumSword", ItemType.PlatinumSword }, 
                // Wands
                { "wand", ItemType.WoodWand }, 
                { "FireWand", ItemType.FireWand }, 
                { "LightningWand", ItemType.LightningWand }, 
                { "NatureWand", ItemType.NatureWand }, 
                { "WaterWand", ItemType.WaterWand }, 
                { "HalfMoonWand", ItemType.HalfMoonWand }, 
                // Summon Staffs
                { "WoodSummonStaff", ItemType.WoodSummonStaff }, 
                // Resources
                { "stick", ItemType.Stick }, 
                { "copperbar", ItemType.CopperBar }, 
                { "ironbar", ItemType.IronBar }, 
                { "silverbar", ItemType.SilverBar }, 
                { "goldbar", ItemType.GoldBar }, 
                { "platinumbar", ItemType.PlatinumBar }, 
                { "torch", ItemType.Torch }, 
                { "acorn", ItemType.Acorn }, 
                // FIXED: Added missing block/ore sprites for inventory
                { "dirt", ItemType.Dirt },
                { "grass", ItemType.Grass },
                { "stone", ItemType.Stone },
                { "copperore", ItemType.Copper },
                { "ironore", ItemType.Iron },
                { "silverore", ItemType.Silver },
                { "goldore", ItemType.Gold },
                { "platinumore", ItemType.Platinum },
                { "coal", ItemType.Coal },
                // FIXED: Added crafting benches
                { "woodcraftingtable", ItemType.WoodCraftingBench },
                { "coppercraftingtable", ItemType.CopperCraftingBench },
                // FIXED: Added chests
                { "woodchest", ItemType.WoodChest },
                { "silverchest", ItemType.SilverChest },
                { "magicchest", ItemType.MagicChest },
                // Buckets
                { "bucket", ItemType.EmptyBucket }, 
                { "waterfilledbucket", ItemType.WaterBucket }, 
                { "lavafilledbucket", ItemType.LavaBucket }, 
                // Consumables
                { "HealthPotion", ItemType.HealthPotion }, 
                { "RecallPotion3UsesFrames", ItemType.RecallPotion }, 
                // Placeable
                { "bed", ItemType.Bed }, 
                // Biome Wood Types
                { "foresttreewood", ItemType.Wood }, 
                { "snowtreewood", ItemType.Wood }, 
                { "jungletreewood", ItemType.Wood }, 
                { "swampwood", ItemType.Wood }, 
                { "volcanicwood", ItemType.Wood } 
            };
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
            var spriteMap = new Dictionary<string, ItemType> { 
                // Pickaxes
                { "woodpickaxe", ItemType.WoodPickaxe }, 
                { "stonepickaxe", ItemType.StonePickaxe }, 
                { "copperpickaxe", ItemType.CopperPickaxe }, 
                { "ironpickaxe", ItemType.IronPickaxe }, 
                { "silverpickaxe", ItemType.SilverPickaxe }, 
                { "goldpickaxe", ItemType.GoldPickaxe }, 
                { "platinumpickaxe", ItemType.PlatinumPickaxe }, 
                // Swords
                { "woodsword", ItemType.WoodSword }, 
                { "CopperSword", ItemType.CopperSword }, 
                { "IronSword", ItemType.IronSword }, 
                { "SilverSword", ItemType.SilverSword }, 
                { "GoldSword", ItemType.GoldSword }, 
                { "PlatinumSword", ItemType.PlatinumSword }, 
                // Resources
                { "torch", ItemType.Torch }, 
                { "stick", ItemType.Stick }, 
                { "copperbar", ItemType.CopperBar }, 
                { "ironbar", ItemType.IronBar }, 
                { "silverbar", ItemType.SilverBar }, 
                { "goldbar", ItemType.GoldBar }, 
                { "platinumbar", ItemType.PlatinumBar }, 
                // Buckets
                { "bucket", ItemType.EmptyBucket }, 
                // Placeable
                { "bed", ItemType.Bed } 
            };
            foreach (var sprite in spriteMap) { try { iUI.GetCraftingUI()?.LoadItemSprite(sprite.Value, Content.Load<Texture2D>(sprite.Key)); } catch { } }
        }
        #endregion
    }
}