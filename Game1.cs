using Claude4_5Terraria.Entities;
using Claude4_5Terraria.Player;
using Claude4_5Terraria.Systems;
using Claude4_5Terraria.UI;
using Claude4_5Terraria.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using System.Threading.Tasks;


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

        private InventoryUI inventoryUI;
        private PauseMenu pauseMenu;
        private MiningOverlay miningOverlay;
        private StartMenu startMenu;
        private SaveMenu saveMenu;
        private LoadMenu loadMenu;
        private HUD hud;

        private KeyboardState previousKeyboardState;

        private Song backgroundMusic;
        private float musicVolume = 0.1f;
        private bool isMusicMuted = false;
        private bool showMiningOutlines = false;

        private bool worldGenerated = false;
        private int currentWorldSeed;
        private float totalPlayTime;
       

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            previousKeyboardState = Keyboard.GetState();
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

            backgroundMusic = Content.Load<Song>("CozyBackground");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = musicVolume;
            MediaPlayer.Play(backgroundMusic);

            startMenu = new StartMenu(musicVolume, (newVolume) =>
            {
                musicVolume = newVolume;
                MediaPlayer.Volume = isMusicMuted ? 0f : musicVolume;
            }, ToggleFullscreen, menuBackgroundTexture);

            Logger.Log("[GAME] Content loaded successfully");
        }

        protected override void UnloadContent()
        {
            MediaPlayer.Stop();
            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();

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
                base.Update(gameTime);
                return;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update save menu if open
            if (saveMenu != null && saveMenu.IsOpen)
            {
                saveMenu.Update(deltaTime, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                previousKeyboardState = keyboardState;
                base.Update(gameTime);
                return;
            }

            // Only update pause menu if it exists (after world is generated)
            if (pauseMenu != null)
            {
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
                base.Update(gameTime);
                return;
            }

            // Check if game objects still exist (not quit to menu)
            if (timeSystem == null || player == null || world == null)
            {
                previousKeyboardState = keyboardState;
                base.Update(gameTime);
                return;
            }

            totalPlayTime += deltaTime;

            timeSystem.Update(deltaTime);

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

            player.Update(gameTime);

            camera.Position = player.Position;

            Vector2 playerCenter = new Vector2(
                player.Position.X + Claude4_5Terraria.Player.Player.PLAYER_WIDTH / 2,
                player.Position.Y + Claude4_5Terraria.Player.Player.PLAYER_HEIGHT / 2
            );

            world.UpdateLoadedChunks(camera);
            world.Update(deltaTime, worldGenerator);
            world.MarkAreaAsExplored(playerCenter);

            miningSystem.Update(
                gameTime,
                playerCenter,
                camera,
                player.Position,
                Claude4_5Terraria.Player.Player.PLAYER_WIDTH,
                Claude4_5Terraria.Player.Player.PLAYER_HEIGHT
            );

            inventoryUI.Update(gameTime, player.Position, world, GraphicsDevice.Viewport.Height);

            previousKeyboardState = keyboardState;

            base.Update(gameTime);
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
            // Dictionary of sprite names to tile types
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
                { "coppercraftingtable", Enums.TileType.CopperCraftingBench }
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
            // Dictionary of sprite names to item types
            var spriteMap = new System.Collections.Generic.Dictionary<string, Enums.ItemType>
            {
                // Tools
                { "woodpickaxe", Enums.ItemType.WoodPickaxe },
                { "stonepickaxe", Enums.ItemType.StonePickaxe },
                { "copperpickaxe", Enums.ItemType.CopperPickaxe },
                { "silverpickaxe", Enums.ItemType.SilverPickaxe },
                { "platinumpickaxe", Enums.ItemType.PlatinumPickaxe },
                
                // Workbenches
                { "woodcraftingtable", Enums.ItemType.WoodCraftingBench },
                { "coppercraftingtable", Enums.ItemType.CopperCraftingBench },
                
                // Ores (inventory items)
                { "copperore", Enums.ItemType.Copper },
                { "silverore", Enums.ItemType.Silver },
                { "platinumore", Enums.ItemType.Platinum },
                { "coal", Enums.ItemType.Coal },
                
                // Bars
                { "copperbar", Enums.ItemType.CopperBar },
                { "silverbar", Enums.ItemType.SilverBar },
                { "platinumbar", Enums.ItemType.PlatinumBar },
                
                // Misc
                { "torch", Enums.ItemType.Torch },
                { "acorn", Enums.ItemType.Acorn },
                { "stick", Enums.ItemType.Stick },
                
                // Use block textures for dirt and stone in inventory
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
                }
                catch (Exception ex)
                {
                    Logger.Log($"[GAME] Could not load {sprite.Key} item sprite: {ex.Message}");
                }
            }
        }
        private void LoadCraftingItemSprites(InventoryUI inventoryUI)
        {
            // Load sprites for the crafting UI
            var spriteMap = new System.Collections.Generic.Dictionary<string, Enums.ItemType>
            {
                // Tools
                { "woodpickaxe", Enums.ItemType.WoodPickaxe },
                { "stonepickaxe", Enums.ItemType.StonePickaxe },
                { "copperpickaxe", Enums.ItemType.CopperPickaxe },
                { "silverpickaxe", Enums.ItemType.SilverPickaxe },
                { "platinumpickaxe", Enums.ItemType.PlatinumPickaxe },
                
                // Workbenches
                { "woodcraftingtable", Enums.ItemType.WoodCraftingBench },
                { "coppercraftingtable", Enums.ItemType.CopperCraftingBench },
                
                // Ores
                { "copperore", Enums.ItemType.Copper },
                { "silverore", Enums.ItemType.Silver },
                { "platinumore", Enums.ItemType.Platinum },
                { "coal", Enums.ItemType.Coal },
                
                // Bars
                { "copperbar", Enums.ItemType.CopperBar },
                { "silverbar", Enums.ItemType.SilverBar },
                { "platinumbar", Enums.ItemType.PlatinumBar },
                
                // Misc
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
                // Going to fullscreen - use native resolution
                graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }
            else
            {
                // Going to windowed - use default size
                graphics.PreferredBackBufferWidth = 1920;
                graphics.PreferredBackBufferHeight = 1080;
            }
            
            graphics.ApplyChanges();
            Logger.Log($"[GRAPHICS] Fullscreen {(graphics.IsFullScreen ? "enabled" : "disabled")}");
        }

        private void QuitToMenu()
        {
            Logger.Log("[GAME] Quitting to main menu");

            // Reset game state flags
            worldGenerated = false;

            // Clear game objects (they'll be recreated when starting/loading a new game)
            world = null;
            player = null;
            camera = null;
            inventory = null;
            miningSystem = null;
            lightingSystem = null;
            timeSystem = null;
            worldGenerator = null;
            inventoryUI = null;
            pauseMenu = null;
            saveMenu = null;
            miningOverlay = null;

            // Return to main menu
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
                TileChanges = world.GetModifiedTiles()
            };

            Logger.Log($"[GAME] SaveData.PlayerPosition = {data.PlayerPosition}");

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

            world = new World.World();
            timeSystem = new TimeSystem();
            timeSystem.SetCurrentTime(data.GameTime);
            lightingSystem = new LightingSystem(world, timeSystem);
            
            // Load all tile sprites
            LoadTileSprites(world);

            worldGenerator = new WorldGenerator(world, data.WorldSeed);

            worldGenerator.OnProgressUpdate = (progress, message) =>
            {
                startMenu.SetLoadingProgress(progress, message);
            };

            worldGenerator.Generate();
            world.EnableChunkUnloading();
            world.EnableTileChangeTracking();  // CRITICAL FIX: Enable tracking BEFORE applying changes

            player = new Claude4_5Terraria.Player.Player(world, data.PlayerPosition);

            // APPLY SAVED TILE CHANGES after world generation
            world.ApplyTileChanges(data.TileChanges);
            
            // Disable world updates to prevent saplings/trees from regenerating
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

            miningSystem = new MiningSystem(world, inventory);
            inventoryUI = new InventoryUI(inventory, miningSystem);
            inventoryUI.Initialize(pixelTexture, font);
            LoadItemSprites(inventoryUI);
            LoadCraftingItemSprites(inventoryUI);
            LoadCraftingItemSprites(inventoryUI);
            
            pauseMenu = new PauseMenu(OpenSaveMenu, QuitToMenu, (newVolume) =>
            {
                musicVolume = newVolume;
                if (!isMusicMuted)
                    MediaPlayer.Volume = musicVolume;
            }, musicVolume, ToggleFullscreen);
            saveMenu = new SaveMenu(SaveGame);
            miningOverlay = new MiningOverlay(world, miningSystem);
            hud = new HUD();

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
                    Logger.Log($"[GAME] Save data loaded successfully!");
                    Logger.Log($"[GAME] Save name: {saveData.SaveName}");
                    Logger.Log($"[GAME] Player position: {saveData.PlayerPosition}");
                    Logger.Log($"[GAME] World seed: {saveData.WorldSeed}");
                    LoadGameFromSave(saveData);
                    return;
                }
                else
                {
                    Logger.Log("[GAME] ERROR: Failed to load save data - saveData is NULL");
                    Logger.Log("[GAME] Starting new game instead");
                }
            }
            else
            {
                Logger.Log("[GAME] ===== STARTING NEW GAME =====");
            }

            currentWorldSeed = System.Environment.TickCount;
            totalPlayTime = 0f;

            world = new World.World();
            timeSystem = new TimeSystem();
            lightingSystem = new LightingSystem(world, timeSystem);
            
            // Load all tile sprites
            LoadTileSprites(world);

            worldGenerator = new WorldGenerator(world, currentWorldSeed);

            worldGenerator.OnProgressUpdate = (progress, message) =>
            {
                startMenu.SetLoadingProgress(progress, message);
            };

            worldGenerator.Generate();
            world.EnableChunkUnloading();
            world.EnableTileChangeTracking();  // CRITICAL FIX: Enable tracking for new games

            Vector2 spawnPosition = worldGenerator.GetSpawnPosition(64);
            player = new Claude4_5Terraria.Player.Player(world, spawnPosition);

            camera = new Camera(GraphicsDevice.Viewport);
            camera.Position = player.Position;

            inventory = new Inventory();
            miningSystem = new MiningSystem(world, inventory);
            inventoryUI = new InventoryUI(inventory, miningSystem);
            inventoryUI.Initialize(pixelTexture, font);
            LoadItemSprites(inventoryUI);
            LoadCraftingItemSprites(inventoryUI);
            
            pauseMenu = new PauseMenu(OpenSaveMenu, QuitToMenu, (newVolume) =>
            {
                musicVolume = newVolume;
                if (!isMusicMuted)
                    MediaPlayer.Volume = musicVolume;
            }, musicVolume, ToggleFullscreen);
            saveMenu = new SaveMenu(SaveGame);
            miningOverlay = new MiningOverlay(world, miningSystem);
            hud = new HUD();

            worldGenerated = true;
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
            player.Draw(spriteBatch, pixelTexture);

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

            inventoryUI.Draw(
                spriteBatch,
                pixelTexture,
                font,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height
            );

            // Draw HUD with coordinates
            if (hud != null)
            {
                hud.Draw(spriteBatch, pixelTexture, font,
                    GraphicsDevice.Viewport.Width,
                    GraphicsDevice.Viewport.Height,
                    player.Position);
            }

            pauseMenu.Draw(spriteBatch, pixelTexture, font,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height);

            // Draw save menu on top of everything
            if (saveMenu != null)
            {
                saveMenu.Draw(spriteBatch, pixelTexture, font,
                    GraphicsDevice.Viewport.Width,
                    GraphicsDevice.Viewport.Height);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}