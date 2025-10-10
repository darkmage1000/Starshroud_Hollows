using Claude4_5Terraria.Entities;
using Claude4_5Terraria.Enums;
using Claude4_5Terraria.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Claude4_5Terraria.Systems
{
    public class MiningSystem
    {
        private World.World world;
        private Inventory inventory;
        private const int MINING_RANGE = 4;
        private const int PLACEMENT_RANGE = 4;

        // NEW: Callbacks for chest system
        public Action<Point, TileType> OnChestMined;
        public Action<Point, ItemType> OnChestPlaced;

        private Point? targetedTile;
        private float miningProgress;
        private Point? currentlyMiningTile;
        private int selectedHotbarSlot;

        // NEW: Animation frame property
        public int CurrentAnimationFrame { get; private set; } // 0, 1, or 2

        private MouseState previousMouseState;

        private List<DroppedItem> droppedItems;
        private const float MAGNET_RANGE = 64f;

        private float placementCooldown;
        private const float PLACEMENT_DELAY = 0.15f;

        private Random random;

        public MiningSystem(World.World world, Inventory inventory)
        {
            this.world = world;
            this.inventory = inventory;
            targetedTile = null;
            miningProgress = 0f;
            currentlyMiningTile = null;
            CurrentAnimationFrame = 0;
            previousMouseState = Mouse.GetState();
            droppedItems = new List<DroppedItem>();
            selectedHotbarSlot = 0;
            placementCooldown = 0f;
            random = new Random();
        }

        public Point? GetTargetedTile()
        {
            return targetedTile;
        }

        public float GetMiningProgress()
        {
            return miningProgress;
        }

        public Point? GetCurrentlyMiningTile()
        {
            return currentlyMiningTile;
        }

        public int GetSelectedHotbarSlot()
        {
            return selectedHotbarSlot;
        }

        public void SetSelectedHotbarSlot(int slot)
        {
            if (slot >= 0 && slot < 10)
            {
                selectedHotbarSlot = slot;
            }
        }

        // UPDATED: Auto-mine functionality is contained here
        public void Update(GameTime gameTime, Vector2 playerCenter, Camera camera, Vector2 playerPosition, int playerWidth, int playerHeight, bool autoMiningActive, Vector2 lastPlayerDirection)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (placementCooldown > 0)
            {
                placementCooldown -= deltaTime;
            }

            MouseState mouseState = Mouse.GetState();

            // --- DETERMINE TARGETING MODE (Auto or Manual) ---
            bool isMining = false;
            int targetX, targetY;
            Vector2 miningWorldPos;

            // Auto-Mine Targeting: If active and moving
            if (autoMiningActive && lastPlayerDirection.LengthSquared() > 0)
            {
                isMining = true;

                // Determine the base target (player center)
                miningWorldPos = playerCenter;

                // CRITICAL FIX: Horizontal Multi-Block Mining
                if (lastPlayerDirection.X != 0)
                {
                    // Target the block immediately adjacent to the player's body.
                    // This is 1.0 TILE_SIZE from the player's center for a 1-tile wide player.
                    miningWorldPos.X += lastPlayerDirection.X * World.World.TILE_SIZE * 1.0f;

                    // We must also target the vertical center between the top and bottom adjacent tiles.
                    // Since the player's center is at Y+32, and the two blocks are Y+0/Y+32, targeting Y+16/Y+48 (center)
                    // isn't necessary if the targeted block logic handles the 2x1 area.
                    // However, to ensure *both* blocks are hit in the horizontal path, we target the *top* one.
                    // Let's rely on the dedicated multi-block processor below. We just need to ensure the single target is adjacent.
                }
                else // Vertical movement (mines a single block)
                {
                    // For vertical, 1.5 tiles ensures it targets the center of the next block up or down
                    miningWorldPos += lastPlayerDirection * World.World.TILE_SIZE * 1.5f;
                }
            }
            // Manual Targeting: If left mouse button is pressed
            else if (mouseState.LeftButton == ButtonState.Pressed)
            {
                isMining = true;
                miningWorldPos = GetMouseWorldPosition(mouseState, camera);
            }
            // Default target (for visual outlines only)
            else
            {
                miningWorldPos = GetMouseWorldPosition(mouseState, camera);
            }

            targetX = (int)(miningWorldPos.X / World.World.TILE_SIZE);
            targetY = (int)(miningWorldPos.Y / World.World.TILE_SIZE);

            float maxDistance = MINING_RANGE * World.World.TILE_SIZE;

            // Calculate distance to the visually targeted tile (for range check)
            float distanceToTile = Vector2.Distance(
                playerCenter,
                new Vector2(targetX * World.World.TILE_SIZE + World.World.TILE_SIZE / 2,
                           targetY * World.World.TILE_SIZE + World.World.TILE_SIZE / 2)
            );


            // Update targetedTile for mouse-over/visual feedback
            Tile targetTile = world.GetTile(targetX, targetY);
            if (targetTile != null && targetTile.IsActive && distanceToTile <= maxDistance)
            {
                targetedTile = new Point(targetX, targetY);
            }
            else
            {
                targetedTile = null;
            }

            // ===== MINING (Triggered by manual mouse OR auto-mine logic) =====
            if (isMining && distanceToTile <= maxDistance)
            {
                // Determine the vertical position of the two blocks in front of the player (head and foot level)
                int horizontalDir = (int)lastPlayerDirection.X;

                if (autoMiningActive && horizontalDir != 0)
                {
                    // Target the two blocks horizontally adjacent to the player's two-block height
                    // Top block in front of player: (targetX, PlayerTopY)
                    // Bottom block in front of player: (targetX, PlayerBottomY)

                    // We need the tile Y position of the player's top block (head level)
                    int playerTopTileY = (int)(playerPosition.Y / World.World.TILE_SIZE);
                    int playerBottomTileY = (int)((playerPosition.Y + playerHeight - 1) / World.World.TILE_SIZE);

                    int targetHeadY = playerTopTileY; // The block level the player's head is in
                    int targetFootY = playerBottomTileY; // The block level the player's feet are in

                    // Target the block *in front* of the player
                    int targetX_Multi = (int)((playerPosition.X + (playerWidth / 2) + (horizontalDir * World.World.TILE_SIZE)) / World.World.TILE_SIZE);

                    // Call the specialized multi-block horizontal auto-mine processor
                    ProcessHorizontalAutoMining(targetX_Multi, targetHeadY, targetFootY, deltaTime, lastPlayerDirection);
                }
                else if (targetedTile.HasValue && targetTile != null && targetTile.IsActive)
                {
                    // Manual mining or vertical auto-mining (single block)
                    ProcessMining(targetedTile.Value.X, targetedTile.Value.Y, deltaTime);
                }
                else
                {
                    // Ensure state resets if we are pressing but hit nothing
                    currentlyMiningTile = null;
                    miningProgress = 0f;
                    CurrentAnimationFrame = 0;
                }
            }
            else if (!isMining) // Stop mining if neither auto-mine nor manual click is active
            {
                currentlyMiningTile = null;
                miningProgress = 0f;
                CurrentAnimationFrame = 0; // Reset animation frame
            }

            // ===== PLACEMENT (RIGHT CLICK - unchanged) =====
            if (mouseState.RightButton == ButtonState.Pressed && placementCooldown <= 0)
            {
                if (distanceToTile <= PLACEMENT_RANGE * World.World.TILE_SIZE)
                {
                    // Use the mouse position for placement, as auto-mine is only for digging
                    int placeX = (int)(GetMouseWorldPosition(mouseState, camera).X / World.World.TILE_SIZE);
                    int placeY = (int)(GetMouseWorldPosition(mouseState, camera).Y / World.World.TILE_SIZE);

                    InventorySlot selectedSlot = inventory.GetSlot(selectedHotbarSlot);

                    // ACORN PLANTING
                    if (selectedSlot != null && !selectedSlot.IsEmpty() && selectedSlot.ItemType == ItemType.Acorn)
                    {
                        if (CanPlantAcorn(placeX, placeY))
                        {
                            // Place sapling tile at the planting position
                            world.SetTile(placeX, placeY, new Tile(TileType.Sapling));
                            world.AddSapling(placeX, placeY);
                            selectedSlot.Count--;
                            if (selectedSlot.Count <= 0)
                            {
                                selectedSlot.ItemType = ItemType.None;
                                selectedSlot.Count = 0;
                            }
                            placementCooldown = PLACEMENT_DELAY;
                            Logger.Log($"[PLACE] Planted acorn (sapling tile) at ({placeX}, {placeY})");
                        }
                        else
                        {
                            Logger.Log($"[PLACE] Cannot plant acorn - need grass/dirt with 3 tiles of space above");
                            placementCooldown = PLACEMENT_DELAY;
                        }
                    }
                    // REGULAR BLOCK PLACEMENT
                    else
                    {
                        Tile placeTile = world.GetTile(placeX, placeY);

                        if (placeTile != null && !placeTile.IsActive)
                        {
                            Rectangle blockRect = new Rectangle(
                                placeX * World.World.TILE_SIZE,
                                placeY * World.World.TILE_SIZE,
                                World.World.TILE_SIZE,
                                World.World.TILE_SIZE
                            );

                            Rectangle playerRect = new Rectangle(
                                (int)playerPosition.X,
                                (int)playerPosition.Y,
                                playerWidth,
                                playerHeight
                            );

                            if (!blockRect.Intersects(playerRect))
                            {
                                if (selectedSlot != null && !selectedSlot.IsEmpty())
                                {
                                    ItemType itemType = selectedSlot.ItemType;
                                    TileType blockType = ItemTypeExtensions.ToTileType(itemType);

                                    if (IsPlaceable(blockType))
                                    {
                                        // TORCH PLACEMENT
                                        if (blockType == TileType.Torch)
                                        {
                                            // Check if underground
                                            bool isUnderground = false;
                                            for (int checkY = placeY - 1; checkY >= 0; checkY--)
                                            {
                                                Tile checkTile = world.GetTile(placeX, checkY);
                                                if (checkTile != null && checkTile.IsActive &&
                                                    checkTile.Type != TileType.Leaves &&
                                                    checkTile.Type != TileType.Wood &&
                                                    checkTile.Type != TileType.Sapling)
                                                {
                                                    isUnderground = true;
                                                    break;
                                                }
                                            }

                                            // Underground: allow floating torches
                                            // Surface: require adjacent block
                                            if (!isUnderground && !HasAdjacentSolidBlock(placeX, placeY))
                                            {
                                                Logger.Log($"[PLACE] Torch requires adjacent wall or surface");
                                                placementCooldown = PLACEMENT_DELAY;
                                            }
                                            else
                                            {
                                                world.SetTile(placeX, placeY, new Tile(blockType));
                                                Logger.Log($"[PLACE] Placed {blockType} at ({placeX}, {placeY})");

                                                selectedSlot.Count--;
                                                if (selectedSlot.Count <= 0)
                                                {
                                                    selectedSlot.ItemType = ItemType.None;
                                                    selectedSlot.Count = 0;
                                                }

                                                placementCooldown = PLACEMENT_DELAY;
                                            }
                                        }
                                        // ALL OTHER BLOCKS
                                        else
                                        {
                                            world.SetTile(placeX, placeY, new Tile(blockType));
                                            Logger.Log($"[PLACE] Placed {blockType} at ({placeX}, {placeY})");

                                            // NEW: Notify if a chest was placed
                                            if (itemType == ItemType.WoodChest || itemType == ItemType.SilverChest || itemType == ItemType.MagicChest)
                                            {
                                                OnChestPlaced?.Invoke(new Point(placeX, placeY), itemType);
                                            }

                                            selectedSlot.Count--;
                                            if (selectedSlot.Count <= 0)
                                            {
                                                selectedSlot.ItemType = ItemType.None;
                                                selectedSlot.Count = 0;
                                            }

                                            placementCooldown = PLACEMENT_DELAY;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // ===== UPDATE DROPPED ITEMS (unchanged) =====
            foreach (DroppedItem item in droppedItems)
            {
                item.Update(deltaTime, world);
                item.ApplyMagnetism(playerCenter, MAGNET_RANGE);
            }

            // ===== AUTO-PICKUP ITEMS (unchanged) =====
            List<DroppedItem> itemsToRemove = new List<DroppedItem>();
            Rectangle playerRect2 = new Rectangle((int)playerPosition.X, (int)playerPosition.Y, playerWidth, playerHeight);
            foreach (DroppedItem item in droppedItems)
            {
                if (playerRect2.Intersects(item.GetBounds()))
                {
                    inventory.AddItem(item.ItemType, 1);
                    itemsToRemove.Add(item);
                    Logger.Log($"[MINING] Picked up {item.ItemType}");
                }
            }
            foreach (DroppedItem item in itemsToRemove)
            {
                droppedItems.Remove(item);
            }

            previousMouseState = mouseState;
        }

        // NEW: Specialized handler for two-block horizontal auto-mining
        private void ProcessHorizontalAutoMining(int targetX, int targetHeadY, int targetFootY, float deltaTime, Vector2 lastPlayerDirection)
        {
            Point targetBottom = new Point(targetX, targetFootY);
            Point targetTop = new Point(targetX, targetHeadY);

            // We track the bottom block as the "currentlyMiningTile" for UI/consistency
            if (currentlyMiningTile == null || currentlyMiningTile.Value != targetBottom)
            {
                currentlyMiningTile = targetBottom;
                miningProgress = 0f;
            }

            Tile tileTop = world.GetTile(targetTop.X, targetTop.Y);
            Tile tileBottom = world.GetTile(targetBottom.X, targetBottom.Y);

            bool needsTop = tileTop != null && tileTop.IsActive;
            bool needsBottom = tileBottom != null && tileBottom.IsActive;

            // Exit if there is nothing to mine
            if (!needsTop && !needsBottom)
            {
                currentlyMiningTile = null;
                miningProgress = 0f;
                CurrentAnimationFrame = 0;
                return;
            }

            InventorySlot selectedSlot = inventory.GetSlot(selectedHotbarSlot);
            ItemType currentTool = selectedSlot?.ItemType ?? ItemType.None;

            // Check if the tool can mine the hardest material in the two blocks
            TileType hardestType = TileType.Air;
            if (needsTop) hardestType = tileTop.Type;
            if (needsBottom && GetMiningTime(tileBottom.Type) > GetMiningTime(hardestType))
            {
                hardestType = tileBottom.Type;
            }

            if (!ToolProperties.CanMine(currentTool, hardestType))
            {
                if (miningProgress == 0f) Logger.Log($"[AUTO-MINE] Cannot clear path with {currentTool}");
                miningProgress = 0f;
                CurrentAnimationFrame = 0;
                return;
            }

            // --- Apply mining progress ---
            float baseMiningTime = GetMiningTime(hardestType);
            float miningSpeed = ToolProperties.GetMiningSpeed(currentTool);
            float adjustedMiningTime = baseMiningTime / miningSpeed;

            // We apply progress at full speed, targeting the two blocks simultaneously
            // (The time adjustment should be handled implicitly by the mining time values)
            miningProgress += deltaTime / adjustedMiningTime;

            // Update animation frame based on progress
            CurrentAnimationFrame = (int)(miningProgress * 3) % 3;

            // --- Check for break ---
            if (miningProgress >= 1f)
            {
                if (needsTop)
                {
                    BreakBlock(targetTop.X, targetTop.Y);
                    Logger.Log($"[AUTO-MINE] Mined top block ({targetTop.X}, {targetTop.Y})");
                }
                if (needsBottom)
                {
                    BreakBlock(targetBottom.X, targetBottom.Y);
                    Logger.Log($"[AUTO-MINE] Mined bottom block ({targetBottom.X}, {targetBottom.Y})");
                }

                // Reset state after clearing both blocks
                miningProgress = 0f;
                currentlyMiningTile = null;
                CurrentAnimationFrame = 0;
            }
        }

        // Core mining logic, remains for manual and vertical auto-mining
        private void ProcessMining(int targetX, int targetY, float deltaTime)
        {
            Point targetPoint = new Point(targetX, targetY);

            if (currentlyMiningTile == null || currentlyMiningTile.Value != targetPoint)
            {
                currentlyMiningTile = targetPoint;
                miningProgress = 0f;
            }

            Tile tile = world.GetTile(targetPoint.X, targetPoint.Y);

            if (tile != null)
            {
                InventorySlot selectedSlot = inventory.GetSlot(selectedHotbarSlot);
                ItemType currentTool;
                if (selectedSlot != null && !selectedSlot.IsEmpty())
                {
                    currentTool = selectedSlot.ItemType;
                }
                else
                {
                    currentTool = ItemType.None;
                }

                bool canMine = ToolProperties.CanMine(currentTool, tile.Type);

                if (!canMine)
                {
                    if (miningProgress == 0f)
                    {
                        Logger.Log($"[MINING] Cannot mine {tile.Type} with {currentTool} - need better tool");
                    }
                    miningProgress = 0f;
                    CurrentAnimationFrame = 0; // Reset animation frame
                }
                else
                {
                    float baseMiningTime = GetMiningTime(tile.Type);
                    float miningSpeed = ToolProperties.GetMiningSpeed(currentTool);
                    float adjustedMiningTime = baseMiningTime / miningSpeed;

                    miningProgress += deltaTime / adjustedMiningTime;

                    // Update animation frame based on progress (cycles 0, 1, 2)
                    CurrentAnimationFrame = (int)(miningProgress * 3) % 3;

                    if (miningProgress >= 1f)
                    {
                        BreakBlock(targetPoint.X, targetPoint.Y);
                        miningProgress = 0f;
                        currentlyMiningTile = null;
                        CurrentAnimationFrame = 0; // Reset animation frame
                        Logger.Log($"[MINING] Mined {tile.Type} with {currentTool} at ({targetPoint.X}, {targetPoint.Y})");
                    }
                }
            }
        }

        // --- RESTORED METHOD BODY ---
        private bool HasAdjacentSolidBlock(int x, int y)
        {
            bool left = world.IsSolidAtPosition(x - 1, y);
            bool right = world.IsSolidAtPosition(x + 1, y);
            bool up = world.IsSolidAtPosition(x, y - 1);
            bool down = world.IsSolidAtPosition(x, y + 1);

            return left || right || up || down;
        }

        // --- RESTORED METHOD BODY ---
        private bool IsPlaceable(TileType type)
        {
            switch (type)
            {
                case TileType.Dirt:
                case TileType.Grass:
                case TileType.Stone:
                case TileType.Wood:
                case TileType.WoodCraftingBench:
                case TileType.CopperCraftingBench:
                case TileType.WoodChest:
                case TileType.SilverChest:
                case TileType.MagicChest:
                case TileType.Torch:
                    return true;
                default:
                    return false;
            }
        }

        // --- RESTORED METHOD BODY ---
        public void DrawItems(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            foreach (DroppedItem item in droppedItems)
            {
                item.Draw(spriteBatch, pixelTexture);
            }
        }

        private Vector2 GetMouseWorldPosition(MouseState mouseState, Camera camera)
        {
            Vector2 mouseScreen = new Vector2(mouseState.X, mouseState.Y);
            Matrix inverseTransform = Matrix.Invert(camera.GetTransformMatrix());
            Vector2 mouseWorld = Vector2.Transform(mouseScreen, inverseTransform);
            return mouseWorld;
        }

        private float GetMiningTime(TileType type)
        {
            switch (type)
            {
                case TileType.Dirt:
                case TileType.Grass:
                    return 0.5f;
                case TileType.Stone:
                    return 1.0f;
                case TileType.Copper:
                    return 1.5f;
                case TileType.Silver:
                    return 2.0f;
                case TileType.Platinum:
                    return 3.0f;
                case TileType.Wood:
                case TileType.Leaves:
                    return 0.8f;
                case TileType.Coal:
                    return 0.7f;
                case TileType.Torch:
                case TileType.WoodCraftingBench:
                case TileType.CopperCraftingBench:
                case TileType.Sapling:
                    return 0.3f;
                case TileType.WoodChest:
                case TileType.SilverChest:
                case TileType.MagicChest:
                    return 0.5f;
                default:
                    return 1.0f;
            }
        }

        private void BreakBlock(int x, int y)
        {
            Tile tile = world.GetTile(x, y);
            if (tile == null) return;

            TileType tileType = tile.Type;
            ItemType droppedItemType;
            int dropCount = 1;

            if ((tile.Type == TileType.Wood || tile.Type == TileType.Leaves) && tile.IsPartOfTree)
            {
                int woodBlockCount = CountTreeWoodBlocks(x, y);
                world.RemoveTree(x, y);

                droppedItemType = ItemType.Wood;

                if (woodBlockCount <= 10)
                {
                    dropCount = 3;
                }
                else if (woodBlockCount <= 13)
                {
                    dropCount = 4;
                }
                else
                {
                    dropCount = 5;
                }

                Vector2 acornPosition = new Vector2(
                    x * World.World.TILE_SIZE + World.World.TILE_SIZE / 2 - 8,
                    y * World.World.TILE_SIZE + World.World.TILE_SIZE / 2 - 8
                );
                int acornCount = random.Next(1, 3);
                for (int i = 0; i < acornCount; i++)
                {
                    Vector2 pos = acornPosition + new Vector2(
                        (float)(random.NextDouble() - 0.5) * 20,
                        (float)(random.NextDouble() - 0.5) * 20
                    );
                    droppedItems.Add(new DroppedItem(pos, ItemType.Acorn));
                }
            }
            else
            {
                // Check if it's a chest before breaking
                if (tileType == TileType.WoodChest || tileType == TileType.SilverChest || tileType == TileType.MagicChest)
                {
                    // Notify that a chest was mined (Game1 will handle chest system)
                    OnChestMined?.Invoke(new Point(x, y), tileType);
                    // Don't drop anything here - ChestSystem.RemoveChest handles it
                    return;
                }

                world.SetTile(x, y, new Tile(TileType.Air));
                droppedItemType = (tileType == TileType.Grass) ? ItemType.Dirt : ItemTypeExtensions.FromTileType(tileType);
                dropCount = 1;
            }

            Vector2 basePosition = new Vector2(
                x * World.World.TILE_SIZE + World.World.TILE_SIZE / 2 - 8,
                y * World.World.TILE_SIZE + World.World.TILE_SIZE / 2 - 8
            );

            for (int i = 0; i < dropCount; i++)
            {
                Vector2 itemPosition = basePosition + new Vector2(
                    (float)(random.NextDouble() - 0.5) * 16,
                    (float)(random.NextDouble() - 0.5) * 16
                );
                droppedItems.Add(new DroppedItem(itemPosition, droppedItemType));
            }
        }

        private int CountTreeWoodBlocks(int x, int y)
        {
            int woodCount = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -10; dy <= 10; dy++)
                {
                    Tile checkTile = world.GetTile(x + dx, y + dy);
                    if (checkTile != null && checkTile.Type == TileType.Wood && checkTile.IsPartOfTree)
                    {
                        woodCount++;
                    }
                }
            }

            return woodCount;
        }

        private bool CanPlantAcorn(int x, int y)
        {
            // Check if the planting spot is empty
            Tile targetTile = world.GetTile(x, y);
            if (targetTile != null && targetTile.IsActive)
                return false; // Can't plant if there's already a block here (including another sapling)

            // Check if there's grass or dirt BELOW
            Tile groundTile = world.GetTile(x, y + 1);
            if (groundTile == null || !groundTile.IsActive)
                return false;

            if (groundTile.Type != TileType.Grass && groundTile.Type != TileType.Dirt)
                return false;

            // Check if there's space above (2 more tiles of air)
            for (int checkY = y - 1; checkY >= y - 2; checkY--)
            {
                Tile checkTile = world.GetTile(x, checkY);
                if (checkTile != null && checkTile.IsActive)
                    return false;
            }

            return true;
        }
    }
}