using StarshroudHollows.Entities;
using StarshroudHollows.Enums;
using StarshroudHollows.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;

namespace StarshroudHollows.Systems
{
    public class MiningSystem
    {
        private StarshroudHollows.World.World world;
        private Inventory inventory;
        private const int MINING_RANGE = 4;
        private const int PLACEMENT_RANGE = 4;

        public Action<Point, TileType> OnChestMined;
        public Action<Point, ItemType> OnChestPlaced;

        private Point? targetedTile;
        private float miningProgress;
        private Point? currentlyMiningTile;
        private int selectedHotbarSlot;

        public int CurrentAnimationFrame { get; private set; }

        private MouseState previousMouseState;

        private List<DroppedItem> droppedItems;
        private const float MAGNET_RANGE = 64f;

        private float placementCooldown;
        private const float PLACEMENT_DELAY = 0.15f;

        private Random random;

        private SoundEffect mineDirtSound;
        private SoundEffect mineStoneSound;
        private SoundEffect mineTorchSound;
        private SoundEffect placeTorchSound;

        private float gameSoundVolume = 1.0f;

        public MiningSystem(StarshroudHollows.World.World world, Inventory inventory, SoundEffect mineDirt, SoundEffect mineStone, SoundEffect mineTorch, SoundEffect placeTorch, float initialSoundVolume)
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

            mineDirtSound = mineDirt;
            mineStoneSound = mineStone;
            mineTorchSound = mineTorch;
            placeTorchSound = placeTorch;

            this.gameSoundVolume = initialSoundVolume;
        }

        public void SetItemTextureMap(Dictionary<ItemType, Texture2D> itemTextureMap)
        {
            // This static property is assumed to exist on your DroppedItem class
            // DroppedItem.ItemTextures = itemTextureMap;
        }

        public void SetSoundVolume(float newVolume)
        {
            gameSoundVolume = newVolume;
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

        public void DropItem(Vector2 position, ItemType itemType, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 itemPosition = position + new Vector2(
                    (float)(random.NextDouble() - 0.5) * 20,
                    (float)(random.NextDouble() - 0.5) * 20
                );
                droppedItems.Add(new DroppedItem(itemPosition, itemType));
            }
        }

        public void Update(GameTime gameTime, Vector2 playerCenter, Camera camera, Vector2 playerPosition, int playerWidth, int playerHeight, bool autoMiningActive, Vector2 lastPlayerDirection)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (placementCooldown > 0)
            {
                placementCooldown -= deltaTime;
            }

            MouseState mouseState = Mouse.GetState();
            KeyboardState keyboardState = Keyboard.GetState();

            // Check if hammer is equipped
            InventorySlot selectedSlot = inventory.GetSlot(selectedHotbarSlot);
            bool isHammerEquipped = selectedSlot != null && selectedSlot.ItemType == ItemType.Hammer;

            // ONLY hammer can mine walls (no shift)
            bool shouldMineWalls = isHammerEquipped;

            bool isMining = false;
            int targetX, targetY;
            Vector2 miningWorldPos;

            if (autoMiningActive && lastPlayerDirection.LengthSquared() > 0)
            {
                isMining = true;
                miningWorldPos = playerCenter;

                if (lastPlayerDirection.X != 0)
                {
                    miningWorldPos.X += lastPlayerDirection.X * StarshroudHollows.World.World.TILE_SIZE * 1.0f;
                }
                else
                {
                    miningWorldPos += lastPlayerDirection * StarshroudHollows.World.World.TILE_SIZE * 1.5f;
                }
            }
            else if (mouseState.LeftButton == ButtonState.Pressed)
            {
                isMining = true;
                miningWorldPos = GetMouseWorldPosition(mouseState, camera);
            }
            else
            {
                miningWorldPos = GetMouseWorldPosition(mouseState, camera);
            }

            targetX = (int)(miningWorldPos.X / StarshroudHollows.World.World.TILE_SIZE);
            targetY = (int)(miningWorldPos.Y / StarshroudHollows.World.World.TILE_SIZE);

            float maxDistance = MINING_RANGE * StarshroudHollows.World.World.TILE_SIZE;

            float distanceToTile = Vector2.Distance(
                playerCenter,
                new Vector2(targetX * StarshroudHollows.World.World.TILE_SIZE + StarshroudHollows.World.World.TILE_SIZE / 2,
                           targetY * StarshroudHollows.World.World.TILE_SIZE + StarshroudHollows.World.World.TILE_SIZE / 2)
            );

            Tile targetTile = world.GetTile(targetX, targetY);
            // FIXED: Prioritize wall targeting when hammer is equipped
            if (targetTile != null && distanceToTile <= maxDistance)
            {
                // When hammer is equipped, prioritize walls over furniture
                if (isHammerEquipped && targetTile.HasWall)
                {
                    targetedTile = new Point(targetX, targetY);
                }
                else if (targetTile.IsActive)
                {
                    targetedTile = new Point(targetX, targetY);
                }
                else
                {
                    // Allow targeting empty tiles for placement
                    targetedTile = new Point(targetX, targetY);
                }
            }
            else
            {
                targetedTile = null;
            }

            if (isMining && distanceToTile <= maxDistance)
            {
                int horizontalDir = (int)lastPlayerDirection.X;
                int verticalDir = (int)lastPlayerDirection.Y;

                if (autoMiningActive && horizontalDir != 0)
                {
                    // Horizontal auto-mining (left/right)
                    int targetX_Multi = (int)((playerPosition.X + (playerWidth / 2) + (horizontalDir * (StarshroudHollows.World.World.TILE_SIZE / 2 + 1))) / StarshroudHollows.World.World.TILE_SIZE);
                    int playerTopTileY = (int)(playerPosition.Y / StarshroudHollows.World.World.TILE_SIZE);
                    int playerBottomTileY = (int)((playerPosition.Y + playerHeight - 1) / StarshroudHollows.World.World.TILE_SIZE);
                    int targetHeadY = playerTopTileY;
                    int targetFootY = playerBottomTileY;
                    ProcessHorizontalAutoMining(targetX_Multi, targetHeadY, targetFootY, deltaTime, lastPlayerDirection);
                }
                else if (autoMiningActive && verticalDir != 0)
                {
                    // Vertical auto-mining (up/down)
                    int playerLeftTileX = (int)(playerPosition.X / StarshroudHollows.World.World.TILE_SIZE);
                    int playerRightTileX = (int)((playerPosition.X + playerWidth - 1) / StarshroudHollows.World.World.TILE_SIZE);
                    int targetY_Multi = (int)((playerPosition.Y + (playerHeight / 2) + (verticalDir * (StarshroudHollows.World.World.TILE_SIZE / 2 + 1))) / StarshroudHollows.World.World.TILE_SIZE);
                    ProcessVerticalAutoMining(playerLeftTileX, playerRightTileX, targetY_Multi, deltaTime, lastPlayerDirection);
                }
                else if (targetedTile.HasValue && targetTile != null && (targetTile.IsActive || (shouldMineWalls && targetTile.HasWall)))
                {
                    ProcessMining(targetedTile.Value.X, targetedTile.Value.Y, deltaTime, shouldMineWalls);
                }
                else
                {
                    currentlyMiningTile = null;
                    miningProgress = 0f;
                    CurrentAnimationFrame = 0;
                }
            }
            else if (!isMining)
            {
                currentlyMiningTile = null;
                miningProgress = 0f;
                CurrentAnimationFrame = 0;
            }

            if (mouseState.RightButton == ButtonState.Pressed && placementCooldown <= 0)
            {
                if (distanceToTile <= PLACEMENT_RANGE * StarshroudHollows.World.World.TILE_SIZE && targetedTile.HasValue)
                {
                    int placeX = targetedTile.Value.X;
                    int placeY = targetedTile.Value.Y;

                    // --- FIX FOR BACKGROUND WALLS ---
                    // Special handling: Hammer places BACKGROUND walls.
                    if (isHammerEquipped)
                    {
                        // Scan hotbar for the first wall item
                        for (int i = 0; i < 10; i++)
                        {
                            InventorySlot wallSlot = inventory.GetSlot(i);
                            if (wallSlot != null && !wallSlot.IsEmpty() && IsWall(ItemTypeExtensions.ToTileType(wallSlot.ItemType)))
                            {
                                TileType wallType = ItemTypeExtensions.ToTileType(wallSlot.ItemType);
                                Tile tile = world.GetTile(placeX, placeY);

                                // NEW LOGIC: Allow placing wall behind furniture
                                bool isBlockedBySolid = world.IsSolidAtPosition(placeX, placeY) && !IsFurniture(tile.Type);

                                if (tile != null && !isBlockedBySolid && !tile.HasWall)
                                {
                                    // FIX: Modify the tile and call SetTile to ensure it's tracked for saving
                                    tile.WallType = wallType;
                                    world.SetTile(placeX, placeY, tile); // ← This ensures the change is tracked!
                                    wallSlot.Count--;
                                    if (wallSlot.Count <= 0)
                                    {
                                        wallSlot.ItemType = ItemType.None;
                                        wallSlot.Count = 0;
                                    }
                                    placementCooldown = PLACEMENT_DELAY;
                                    break; // Exit loop once a wall is placed
                                }
                            }
                        }
                        return; // Hammer action is complete, do not proceed to normal placement.
                    }

                    InventorySlot currentSlot = inventory.GetSlot(selectedHotbarSlot);
                    if (currentSlot == null || currentSlot.IsEmpty()) return;

                    // --- NORMAL PLACEMENT LOGIC ---
                    if (currentSlot.ItemType == ItemType.Acorn)
                    {
                        if (CanPlantAcorn(placeX, placeY))
                        {
                            world.SetTile(placeX, placeY, new Tile(TileType.Sapling));
                            world.AddSapling(placeX, placeY);
                            currentSlot.Count--;
                        }
                        placementCooldown = PLACEMENT_DELAY;
                    }
                    else if (currentSlot.ItemType == ItemType.Door)
                    {
                        Tile placeTile = world.GetTile(placeX, placeY);
                        Tile above = world.GetTile(placeX, placeY - 1);
                        Tile below = world.GetTile(placeX, placeY + 1);

                        if (below != null && below.IsActive &&
                            (placeTile == null || !placeTile.IsActive) &&
                            (above == null || !above.IsActive))
                        {
                            Rectangle blockRect = new Rectangle(placeX * StarshroudHollows.World.World.TILE_SIZE, (placeY - 1) * StarshroudHollows.World.World.TILE_SIZE, StarshroudHollows.World.World.TILE_SIZE, StarshroudHollows.World.World.TILE_SIZE * 2);
                            Rectangle playerRect = new Rectangle((int)playerPosition.X, (int)playerPosition.Y, playerWidth, playerHeight);
                            if (!blockRect.Intersects(playerRect))
                            {
                                // NEW: Doors are OPEN by default when placed
                                Tile doorTop = new Tile(TileType.Door);
                                doorTop.IsDoorOpen = true;  // OPEN by default
                                
                                Tile doorBottom = new Tile(TileType.Door);
                                doorBottom.IsDoorOpen = true;  // OPEN by default
                                
                                world.SetTile(placeX, placeY - 1, doorTop);
                                world.SetTile(placeX, placeY, doorBottom);
                                currentSlot.Count--;
                            }
                        }
                        placementCooldown = PLACEMENT_DELAY;
                    }
                    else if (currentSlot.ItemType == ItemType.Bed)
                    {
                        Tile placeTile = world.GetTile(placeX, placeY);
                        Tile below = world.GetTile(placeX, placeY + 1);
                        if (below != null && below.IsActive && (placeTile == null || !placeTile.IsActive))
                        {
                            Rectangle blockRect = new Rectangle(placeX * StarshroudHollows.World.World.TILE_SIZE, placeY * StarshroudHollows.World.World.TILE_SIZE, StarshroudHollows.World.World.TILE_SIZE, StarshroudHollows.World.World.TILE_SIZE);
                            Rectangle playerRect = new Rectangle((int)playerPosition.X, (int)playerPosition.Y, playerWidth, playerHeight);
                            if (!blockRect.Intersects(playerRect))
                            {
                                world.SetTile(placeX, placeY, new Tile(TileType.Bed));
                                currentSlot.Count--;
                            }
                        }
                        placementCooldown = PLACEMENT_DELAY;
                    }
                    else // --- FIX FOR NORMAL WALLS AND BLOCKS ---
                    {
                        Tile placeTile = world.GetTile(placeX, placeY);
                        if (placeTile == null || placeTile.IsActive) return;

                        Rectangle blockRect = new Rectangle(placeX * StarshroudHollows.World.World.TILE_SIZE, placeY * StarshroudHollows.World.World.TILE_SIZE, StarshroudHollows.World.World.TILE_SIZE, StarshroudHollows.World.World.TILE_SIZE);
                        Rectangle playerRect = new Rectangle((int)playerPosition.X, (int)playerPosition.Y, playerWidth, playerHeight);
                        if (blockRect.Intersects(playerRect)) return;

                        ItemType itemType = currentSlot.ItemType;
                        TileType blockType = ItemTypeExtensions.ToTileType(itemType);

                        if (IsPlaceable(blockType) || IsWall(blockType)) // Allow both placeable blocks and wall blocks
                        {
                            if (blockType == TileType.Torch)
                            {
                                if (HasAdjacentSolidBlock(placeX, placeY) || world.GetTile(placeX, placeY).HasWall)
                                {
                                    world.SetTile(placeX, placeY, new Tile(blockType));
                                    placeTorchSound?.Play(volume: gameSoundVolume, pitch: 0.0f, pan: 0.0f);
                                    currentSlot.Count--;
                                }
                            }
                            else
                            {
                                world.SetTile(placeX, placeY, new Tile(blockType));
                                if (itemType == ItemType.WoodChest || itemType == ItemType.SilverChest || itemType == ItemType.MagicChest)
                                {
                                    OnChestPlaced?.Invoke(new Point(placeX, placeY), itemType);
                                }
                                currentSlot.Count--;
                            }
                            placementCooldown = PLACEMENT_DELAY;
                        }
                    }

                    if (currentSlot.Count <= 0)
                    {
                        currentSlot.ItemType = ItemType.None;
                        currentSlot.Count = 0;
                    }
                }
            }

            foreach (DroppedItem item in droppedItems)
            {
                item.Update(deltaTime, world);
                item.ApplyMagnetism(playerCenter, MAGNET_RANGE);
            }

            List<DroppedItem> itemsToRemove = new List<DroppedItem>();
            foreach (DroppedItem item in droppedItems)
            {
                if (item.CanCollect(playerPosition, playerWidth, playerHeight))
                {
                    if (inventory.AddItem(item.ItemType, 1))
                    {
                        itemsToRemove.Add(item);
                    }
                }
            }
            itemsToRemove.ForEach(item => droppedItems.Remove(item));

            previousMouseState = mouseState;
        }

        private void ProcessHorizontalAutoMining(int targetX, int targetHeadY, int targetFootY, float deltaTime, Vector2 lastPlayerDirection)
        {
            // 1. Identify all tiles to be mined in the vertical column
            List<Point> tilesToMine = new List<Point>();
            for (int y = targetHeadY; y <= targetFootY; y++)
            {
                Tile tile = world.GetTile(targetX, y);
                if (tile != null && tile.IsActive)
                {
                    tilesToMine.Add(new Point(targetX, y));
                }
            }

            // If there's nothing to mine, reset and exit
            if (tilesToMine.Count == 0)
            {
                currentlyMiningTile = null;
                miningProgress = 0f;
                CurrentAnimationFrame = 0;
                return;
            }

            // Use the top-most block as the reference for tracking the mining target
            Point primaryTarget = tilesToMine[0];
            if (currentlyMiningTile == null || currentlyMiningTile.Value != primaryTarget)
            {
                currentlyMiningTile = primaryTarget;
                miningProgress = 0f;
            }

            // 2. Determine the hardest block to set the mining time
            InventorySlot selectedSlot = inventory.GetSlot(selectedHotbarSlot);
            ItemType currentTool = selectedSlot?.ItemType ?? ItemType.None;
            TileType hardestType = TileType.Air;
            float maxMiningTime = 0f;

            foreach (Point tilePos in tilesToMine)
            {
                Tile tile = world.GetTile(tilePos.X, tilePos.Y);
                if (tile != null)
                {
                    // Find the tile that takes the longest to mine
                    float time = GetMiningTime(tile.Type);
                    if (time > maxMiningTime)
                    {
                        maxMiningTime = time;
                        hardestType = tile.Type;
                    }
                }
            }

            // 3. Check if the current tool can mine the hardest block
            if (!ToolProperties.CanMine(currentTool, hardestType))
            {
                miningProgress = 0f;
                CurrentAnimationFrame = 0;
                return;
            }

            // 4. Calculate mining progress based on the hardest block
            float miningSpeed = ToolProperties.GetMiningSpeed(currentTool);
            float adjustedMiningTime = maxMiningTime / miningSpeed;
            if (adjustedMiningTime <= 0) adjustedMiningTime = 0.1f; // Prevent division by zero
            miningProgress += deltaTime / adjustedMiningTime;
            CurrentAnimationFrame = (int)(miningProgress * 3) % 3;

            // 5. Break all targeted blocks when mining is complete
            if (miningProgress >= 1f)
            {
                foreach (Point tilePos in tilesToMine)
                {
                    // Ensure the block still exists before breaking
                    if (world.GetTile(tilePos.X, tilePos.Y)?.IsActive == true)
                    {
                        BreakBlock(tilePos.X, tilePos.Y);
                    }
                }
                miningProgress = 0f;
                currentlyMiningTile = null;
                CurrentAnimationFrame = 0;
            }
        }

        private void ProcessVerticalAutoMining(int targetLeftX, int targetRightX, int targetY, float deltaTime, Vector2 lastPlayerDirection)
        {
            // 1. Identify all tiles to be mined in the horizontal row
            List<Point> tilesToMine = new List<Point>();
            for (int x = targetLeftX; x <= targetRightX; x++)
            {
                Tile tile = world.GetTile(x, targetY);
                if (tile != null && tile.IsActive)
                {
                    tilesToMine.Add(new Point(x, targetY));
                }
            }

            // If there's nothing to mine, reset and exit
            if (tilesToMine.Count == 0)
            {
                currentlyMiningTile = null;
                miningProgress = 0f;
                CurrentAnimationFrame = 0;
                return;
            }

            // Use the left-most block as the reference for tracking the mining target
            Point primaryTarget = tilesToMine[0];
            if (currentlyMiningTile == null || currentlyMiningTile.Value != primaryTarget)
            {
                currentlyMiningTile = primaryTarget;
                miningProgress = 0f;
            }

            // 2. Determine the hardest block to set the mining time
            InventorySlot selectedSlot = inventory.GetSlot(selectedHotbarSlot);
            ItemType currentTool = selectedSlot?.ItemType ?? ItemType.None;
            TileType hardestType = TileType.Air;
            float maxMiningTime = 0f;

            foreach (Point tilePos in tilesToMine)
            {
                Tile tile = world.GetTile(tilePos.X, tilePos.Y);
                if (tile != null)
                {
                    // Find the tile that takes the longest to mine
                    float time = GetMiningTime(tile.Type);
                    if (time > maxMiningTime)
                    {
                        maxMiningTime = time;
                        hardestType = tile.Type;
                    }
                }
            }

            // 3. Check if the current tool can mine the hardest block
            if (!ToolProperties.CanMine(currentTool, hardestType))
            {
                miningProgress = 0f;
                CurrentAnimationFrame = 0;
                return;
            }

            // 4. Calculate mining progress based on the hardest block
            float miningSpeed = ToolProperties.GetMiningSpeed(currentTool);
            float adjustedMiningTime = maxMiningTime / miningSpeed;
            if (adjustedMiningTime <= 0) adjustedMiningTime = 0.1f; // Prevent division by zero
            miningProgress += deltaTime / adjustedMiningTime;
            CurrentAnimationFrame = (int)(miningProgress * 3) % 3;

            // 5. Break all targeted blocks when mining is complete
            if (miningProgress >= 1f)
            {
                foreach (Point tilePos in tilesToMine)
                {
                    // Ensure the block still exists before breaking
                    if (world.GetTile(tilePos.X, tilePos.Y)?.IsActive == true)
                    {
                        BreakBlock(tilePos.X, tilePos.Y);
                    }
                }
                miningProgress = 0f;
                currentlyMiningTile = null;
                CurrentAnimationFrame = 0;
            }
        }

        private void ProcessMining(int targetX, int targetY, float deltaTime, bool miningWall = false)
        {
            Point targetPoint = new Point(targetX, targetY);

            if (currentlyMiningTile == null || currentlyMiningTile.Value != targetPoint)
            {
                currentlyMiningTile = targetPoint;
                miningProgress = 0f;
            }

            Tile tile = world.GetTile(targetPoint.X, targetPoint.Y);

            if (tile != null && (tile.Type == TileType.Water || tile.Type == TileType.Lava))
            {
                miningProgress = 0f;
                CurrentAnimationFrame = 0;
                return;
            }

            if (tile != null)
            {
                // Mining walls with hammer - can mine walls even in air tiles
                if (miningWall)
                {
                    if (tile.HasWall)
                    {
                        float wallMiningTime = 0.5f; // Walls mine faster
                        miningProgress += deltaTime / wallMiningTime;
                        CurrentAnimationFrame = (int)(miningProgress * 3) % 3;

                        if (miningProgress >= 1f)
                        {
                            // Drop the wall item
                            ItemType wallItem = ItemTypeExtensions.FromTileType(tile.WallType);
                            DropItem(new Vector2(targetX * StarshroudHollows.World.World.TILE_SIZE + 8,
                                               targetY * StarshroudHollows.World.World.TILE_SIZE + 8), wallItem, 1);

                            // FIX: Remove wall and call SetTile to ensure it's tracked for saving
                            tile.WallType = TileType.Air;
                            world.SetTile(targetX, targetY, tile); // ← This ensures the change is tracked!

                            miningProgress = 0f;
                            currentlyMiningTile = null;
                            CurrentAnimationFrame = 0;
                            mineDirtSound?.Play(volume: gameSoundVolume, pitch: 0.0f, pan: 0.0f);
                        }
                        return;
                    }
                    else
                    {
                        // No wall to mine
                        miningProgress = 0f;
                        CurrentAnimationFrame = 0;
                        return;
                    }
                }

                // Mining foreground blocks (normal mining)
                InventorySlot selectedSlot = inventory.GetSlot(selectedHotbarSlot);
                ItemType currentTool = selectedSlot != null && !selectedSlot.IsEmpty() ? selectedSlot.ItemType : ItemType.None;

                if (!ToolProperties.CanMine(currentTool, tile.Type))
                {
                    miningProgress = 0f;
                    CurrentAnimationFrame = 0;
                }
                else
                {
                    float baseMiningTime = GetMiningTime(tile.Type);
                    float miningSpeed = ToolProperties.GetMiningSpeed(currentTool);
                    float adjustedMiningTime = baseMiningTime / miningSpeed;
                    miningProgress += deltaTime / adjustedMiningTime;
                    CurrentAnimationFrame = (int)(miningProgress * 3) % 3;

                    if (miningProgress >= 1f)
                    {
                        BreakBlock(targetPoint.X, targetPoint.Y);
                        miningProgress = 0f;
                        currentlyMiningTile = null;
                        CurrentAnimationFrame = 0;
                    }
                }
            }
        }

        private void BreakBlock(int x, int y)
        {
            Tile tile = world.GetTile(x, y);
            if (tile == null) return;

            TileType tileType = tile.Type;
            ItemType droppedItemType;
            int dropCount = 1;

            SoundEffect breakSound = null;
            if (tileType == TileType.Dirt || tileType == TileType.Grass) { breakSound = mineDirtSound; }
            else if (tileType == TileType.Stone || tileType == TileType.Copper || tileType == TileType.Silver || tileType == TileType.Platinum || tileType == TileType.Coal) { breakSound = mineStoneSound; }
            else if (tileType == TileType.Torch) { breakSound = mineTorchSound; }

            if ((tile.Type == TileType.Wood || tile.Type == TileType.Leaves) && tile.IsPartOfTree)
            {
                int woodBlockCount = CountTreeWoodBlocks(x, y);
                world.RemoveTree(x, y);
                droppedItemType = ItemType.Wood;
                // Give more wood: multiply by 2
                if (woodBlockCount <= 10) dropCount = woodBlockCount * 2;
                else if (woodBlockCount <= 13) dropCount = woodBlockCount * 2;
                else dropCount = woodBlockCount * 2;
                breakSound = mineTorchSound;
                Vector2 acornPosition = new Vector2(x * StarshroudHollows.World.World.TILE_SIZE + 8, y * StarshroudHollows.World.World.TILE_SIZE + 8);
                int acornCount = random.Next(1, 3);
                for (int i = 0; i < acornCount; i++) droppedItems.Add(new DroppedItem(acornPosition + new Vector2((float)(random.NextDouble() - 0.5) * 20, (float)(random.NextDouble() - 0.5) * 20), ItemType.Acorn));
            }
            else
            {
                if (tileType == TileType.WoodChest || tileType == TileType.SilverChest || tileType == TileType.MagicChest)
                {
                    mineStoneSound?.Play(volume: gameSoundVolume, pitch: 0.0f, pan: 0.0f);
                    OnChestMined?.Invoke(new Point(x, y), tileType);
                    // CRITICAL FIX: Remove the chest tile from the world!
                    world.SetTile(x, y, new Tile(TileType.Air));
                    return;
                }
                world.SetTile(x, y, new Tile(TileType.Air));
                droppedItemType = (tileType == TileType.Grass) ? ItemType.Dirt : ItemTypeExtensions.FromTileType(tileType);
            }
            breakSound?.Play(volume: gameSoundVolume, pitch: 0.0f, pan: 0.0f);
            Vector2 basePosition = new Vector2(x * StarshroudHollows.World.World.TILE_SIZE + 8, y * StarshroudHollows.World.World.TILE_SIZE + 8);
            for (int i = 0; i < dropCount; i++) droppedItems.Add(new DroppedItem(basePosition + new Vector2((float)(random.NextDouble() - 0.5) * 16, (float)(random.NextDouble() - 0.5) * 16), droppedItemType));
        }

        private bool HasAdjacentSolidBlock(int x, int y)
        {
            return world.IsSolidAtPosition(x - 1, y) || world.IsSolidAtPosition(x + 1, y) || world.IsSolidAtPosition(x, y - 1) || world.IsSolidAtPosition(x, y + 1);
        }

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
                case TileType.SummonAltar:
                case TileType.Door:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsWall(TileType type)
        {
            return type == TileType.DirtWall || type == TileType.StoneWall || type == TileType.WoodWall ||
                   type == TileType.CopperWall || type == TileType.IronWall || type == TileType.SilverWall ||
                   type == TileType.GoldWall || type == TileType.PlatinumWall || type == TileType.SnowWall;
        }

        // NEW helper method to identify furniture
        private bool IsFurniture(TileType type)
        {
            return type == TileType.Bed ||
                   type == TileType.WoodCraftingBench ||
                   type == TileType.CopperCraftingBench ||
                   type == TileType.WoodChest ||
                   type == TileType.SilverChest ||
                   type == TileType.MagicChest ||
                   type == TileType.SummonAltar;
        }

        public void DrawItems(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            foreach (DroppedItem item in droppedItems) item.Draw(spriteBatch, pixelTexture);
        }

        private Vector2 GetMouseWorldPosition(MouseState mouseState, Camera camera)
        {
            return Vector2.Transform(new Vector2(mouseState.X, mouseState.Y), Matrix.Invert(camera.GetTransformMatrix()));
        }

        private float GetMiningTime(TileType type)
        {
            switch (type) { case TileType.Dirt: case TileType.Grass: return 0.5f; case TileType.Stone: return 1.0f; case TileType.Copper: return 1.5f; case TileType.Silver: return 2.0f; case TileType.Platinum: return 3.0f; case TileType.Wood: case TileType.Leaves: return 0.8f; case TileType.Coal: return 0.7f; case TileType.Torch: return 0.3f; case TileType.WoodCraftingBench: case TileType.CopperCraftingBench: case TileType.Sapling: return 0.3f; case TileType.WoodChest: case TileType.SilverChest: case TileType.MagicChest: return 0.5f; case TileType.SummonAltar: return 1.5f; default: return 1.0f; }
        }

        private int CountTreeWoodBlocks(int x, int y)
        {
            int woodCount = 0;
            for (int dx = -1; dx <= 1; dx++) for (int dy = -10; dy <= 10; dy++) { Tile t = world.GetTile(x + dx, y + dy); if (t?.Type == TileType.Wood && t.IsPartOfTree) woodCount++; }
            return woodCount;
        }

        private bool CanPlantAcorn(int x, int y)
        {
            Tile targetTile = world.GetTile(x, y); if (targetTile?.IsActive == true) return false;
            Tile groundTile = world.GetTile(x, y + 1); if (groundTile == null || !groundTile.IsActive || (groundTile.Type != TileType.Grass && groundTile.Type != TileType.Dirt)) return false;
            for (int cY = y - 1; cY >= y - 2; cY--) { if (world.GetTile(x, cY)?.IsActive == true) return false; }
            return true;
        }
    }
}

