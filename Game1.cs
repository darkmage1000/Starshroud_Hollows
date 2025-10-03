using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Claude4_5Terraria.World;
using Claude4_5Terraria.Player;
using Claude4_5Terraria.Systems;
using Claude4_5Terraria.UI;

namespace Claude4_5Terraria
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // Core systems
        private World.World world;
        private Player.Player player;
        private Camera camera;
        private TimeSystem timeSystem;
        private LightingSystem lightingSystem;

        // Inventory and crafting
        private Inventory inventory;
        private InventoryUI inventoryUI;
        private MiningSystem miningSystem;

        // Rendering
        private Texture2D pixelTexture;
        private SpriteFont defaultFont;

        // UI
        private HUD hud;
        private PauseMenu pauseMenu;
        private MiningOverlay miningOverlay;

        // Input tracking
        private KeyboardState previousKeyboardState;

        // Screen settings
        private const int SCREEN_WIDTH = 1280;
        private const int SCREEN_HEIGHT = 720;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = SCREEN_WIDTH;
            _graphics.PreferredBackBufferHeight = SCREEN_HEIGHT;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Initialize logger FIRST
            Logger.Initialize();
            Logger.Log("=== GAME STARTING ===");

            // Create pixel texture
            pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            // Load font
            defaultFont = Content.Load<SpriteFont>("DefaultFont");

            // Initialize world
            Logger.LogWorld("Creating world with full terrain generation...");
            world = new World.World();
            WorldGenerator generator = new WorldGenerator(world);
            generator.Generate();

            // Initialize time and lighting
            timeSystem = new TimeSystem();
            lightingSystem = new LightingSystem(world, timeSystem);

            // Spawn player
            int spawnX = World.World.WORLD_WIDTH / 2;
            int groundTileY = world.GetSurfaceHeight(spawnX);
            Vector2 spawnPosition = new Vector2(
                spawnX * World.World.TILE_SIZE,
                groundTileY * World.World.TILE_SIZE - 48
            );
            player = new Player.Player(world, spawnPosition);
            Logger.Log($"Player spawned at: {spawnPosition}");

            // Initialize camera
            camera = new Camera(GraphicsDevice.Viewport);
            camera.Position = player.GetCenterPosition();

            // Initialize inventory and mining
            inventory = new Inventory();
            miningSystem = new MiningSystem(world, inventory);

            // Initialize UI
            hud = new HUD();
            pauseMenu = new PauseMenu();
            miningOverlay = new MiningOverlay(world, miningSystem);
            inventoryUI = new InventoryUI(inventory, miningSystem);
            inventoryUI.Initialize(pixelTexture, defaultFont);

            // Give player starting items for testing
            inventory.AddItem(Enums.ItemType.Wood, 50);
            inventory.AddItem(Enums.ItemType.Coal, 20);
            Logger.LogInventory("STARTING ITEMS", "Wood", 50);
            Logger.LogInventory("STARTING ITEMS", "Coal", 20);

            Logger.Log("=== ALL SYSTEMS LOADED ===");

            previousKeyboardState = Keyboard.GetState();
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState currentKeyboard = Keyboard.GetState();

            // Pause menu handles its own ESC key toggle
            pauseMenu.Update();

            if (pauseMenu.IsPaused)
            {
                previousKeyboardState = currentKeyboard;
                base.Update(gameTime);
                return;
            }

            // Toggle block outlines
            if (currentKeyboard.IsKeyDown(Keys.T) && !previousKeyboardState.IsKeyDown(Keys.T))
            {
                hud.ToggleBlockOutlines();
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update systems
            timeSystem.Update(deltaTime);
            player.Update(gameTime);
            camera.Follow(player.GetCenterPosition(), 0.1f);

            // Update inventory UI (pass player position and world for crafting bench detection)
            inventoryUI.Update(gameTime, player.Position, world);

            // Update mining only if inventory is closed
            if (!inventoryUI.IsInventoryOpen)
            {
                miningSystem.Update(gameTime, player.GetCenterPosition(), camera, player.Position, 24, 48);
            }

            world.UpdateLoadedChunks(camera);

            previousKeyboardState = currentKeyboard;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            Color backgroundColor = GetBackgroundColor(camera.Position.Y);
            GraphicsDevice.Clear(backgroundColor);

            // Draw world with camera transform
            _spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                null,
                null,
                null,
                camera.GetTransformMatrix()
            );

            world.Draw(_spriteBatch, camera, pixelTexture, lightingSystem);
            player.Draw(_spriteBatch, pixelTexture);
            miningSystem.DrawItems(_spriteBatch, pixelTexture);

            miningOverlay.DrawBlockOutlines(_spriteBatch, pixelTexture, camera, player.GetCenterPosition(), hud.ShowBlockOutlines);

            Point? targeted = miningSystem.GetTargetedTile();
            if (targeted.HasValue)
            {
                Rectangle cursorRect = new Rectangle(
                    targeted.Value.X * World.World.TILE_SIZE,
                    targeted.Value.Y * World.World.TILE_SIZE,
                    World.World.TILE_SIZE,
                    World.World.TILE_SIZE
                );
                DrawBorder(cursorRect, 2, Color.White);
            }

            miningOverlay.DrawMiningProgress(_spriteBatch, pixelTexture);

            _spriteBatch.End();

            // Draw UI without camera transform
            _spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp
            );

            hud.Draw(_spriteBatch, pixelTexture, defaultFont, SCREEN_WIDTH, SCREEN_HEIGHT);
            inventoryUI.Draw(_spriteBatch, pixelTexture, defaultFont, SCREEN_WIDTH, SCREEN_HEIGHT);
            pauseMenu.Draw(_spriteBatch, pixelTexture, defaultFont, SCREEN_WIDTH, SCREEN_HEIGHT);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            Logger.Log("=== GAME CLOSING ===");
            Logger.Close();
            base.UnloadContent();
        }

        private void DrawBorder(Rectangle rect, int thickness, Color color)
        {
            _spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            _spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            _spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            _spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private Color GetBackgroundColor(float cameraY)
        {
            float tileDepth = cameraY / World.World.TILE_SIZE;

            Color skyColor = timeSystem.GetSkyColor();
            Color lightDirtBrown = new Color(150, 110, 75);
            Color mediumGray = new Color(80, 80, 85);
            Color darkGray = new Color(40, 40, 45);
            Color veryDarkGray = new Color(20, 20, 22);

            if (tileDepth < 110)
            {
                return skyColor;
            }
            else if (tileDepth < 120)
            {
                float t = (tileDepth - 110) / 10f;
                return Color.Lerp(skyColor, lightDirtBrown, t);
            }
            else if (tileDepth < 130)
            {
                return lightDirtBrown;
            }
            else if (tileDepth < 200)
            {
                float t = (tileDepth - 130) / 70f;
                return Color.Lerp(lightDirtBrown, mediumGray, t);
            }
            else if (tileDepth < 450)
            {
                float t = (tileDepth - 200) / 250f;
                return Color.Lerp(mediumGray, darkGray, t);
            }
            else if (tileDepth < 750)
            {
                float t = (tileDepth - 450) / 300f;
                return Color.Lerp(darkGray, veryDarkGray, t);
            }
            else
            {
                return veryDarkGray;
            }
        }
    }
}