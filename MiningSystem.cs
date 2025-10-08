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

        private Point? targetedTile;
        private float miningProgress;
        private Point? currentlyMiningTile;
        private int selectedHotbarSlot;

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

        public void Update(GameTime gameTime, Vector2 playerCenter, Camera camera, Vector2 playerPosition, int playerWidth, int playerHeight)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (placementCooldown > 0)
            {
                placementCooldown -= deltaTime;
            }

            MouseState mouseState = Mouse.GetState();
            Vector2 mouseWorldPos = GetMouseWorldPosition(mouseState, camera);

            int targetX = (int)(mouseWorldPos.X / World.World.TILE_SIZE);
            int targetY = (int)(mouseWorldPos.Y / World.World.TILE_SIZE);

            float distanceToTile = Vector2.Distance(
                playerCenter,
                new Vector2(targetX * World.World.TILE_SIZE + World.World.TILE_SIZE / 2,
                           targetY * World.World.TILE_SIZE + World.World.TILE_SIZE / 2)
            );

            float maxDistance = MINING_RANGE * World.World.TILE_SIZE;

            Tile targetTile = world.GetTile(targetX, targetY);
            if (targetTile != null && targetTile.IsActive && distanceToTile <= maxDistance)
            {
                targetedTile = new Point(targetX, targetY);
            }
            else
            {
                targetedTile = null;
                currentlyMiningTile = null;
                miningProgress = 0f;
            }

            // ===== MINING (LEFT CLICK) =====
            if (mouseState.LeftButton == ButtonState.Pressed && targetedTile.HasValue)
            {
                if (currentlyMiningTile == null || currentlyMiningTile.Value != targetedTile.Value)
                {
                    currentlyMiningTile = targetedTile;
                    miningProgress = 0f;
                }

                Tile tile = world.GetTile(targetedTile.Value.X, targetedTile.Value.Y);

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
                    }
                    else
                    {
                        float baseMiningTime = GetMiningTime(tile.Type);
                        float miningSpeed = ToolProperties.GetMiningSpeed(currentTool);
                        float adjustedMiningTime = baseMiningTime / miningSpeed;
                        miningProgress += deltaTime / adjustedMiningTime;

                        if (miningProgress >= 1f)
                        {
                            BreakBlock(targetedTile.Value.X, targetedTile.Value.Y);
                            miningProgress = 0f;
                            currentlyMiningTile = null;
                            Logger.Log($"[MINING] Mined {tile.Type} with {currentTool} at ({targetedTile.Value.X}, {targetedTile.Value.Y})");
                        }
                    }
                }
            }
            else if (mouseState.LeftButton == ButtonState.Released)
            {
                currentlyMiningTile = null;
                miningProgress = 0f;
            }

            // ===== PLACEMENT (RIGHT CLICK) =====
            if (mouseState.RightButton == ButtonState.Pressed && placementCooldown <= 0)
            {
                if (distanceToTile <= PLACEMENT_RANGE * World.World.TILE_SIZE)
                {
                    int placeX = targetX;
                    int placeY = targetY;

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
                                selectedSlot.ItemType = ItemType.Dirt;
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
                                                    selectedSlot.ItemType = ItemType.Dirt;
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

                                            selectedSlot.Count--;
                                            if (selectedSlot.Count <= 0)
                                            {
                                                selectedSlot.ItemType = ItemType.Dirt;
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

            // ===== UPDATE DROPPED ITEMS =====
            foreach (DroppedItem item in droppedItems)
            {
                item.Update(deltaTime, world);
                item.ApplyMagnetism(playerCenter, MAGNET_RANGE);
            }

            // ===== AUTO-PICKUP ITEMS =====
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

        private bool HasAdjacentSolidBlock(int x, int y)
        {
            bool left = world.IsSolidAtPosition(x - 1, y);
            bool right = world.IsSolidAtPosition(x + 1, y);
            bool up = world.IsSolidAtPosition(x, y - 1);
            bool down = world.IsSolidAtPosition(x, y + 1);

            return left || right || up || down;
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
                case TileType.Torch:
                    return true;
                default:
                    return false;
            }
        }

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
                world.SetTile(x, y, new Tile(TileType.Air));
                droppedItemType = ItemTypeExtensions.FromTileType(tileType);
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