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

        // Continuous placement
        private float placementCooldown;
        private const float PLACEMENT_DELAY = 0.15f;

        private Random random; //added line recently

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
            random = new Random(); // Add this line
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

            // Update placement cooldown
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

            // Mining (left-click)
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
                    float miningTime = GetMiningTime(tile.Type);
                    miningProgress += deltaTime / miningTime;

                    if (miningProgress >= 1f)
                    {
                        BreakBlock(targetedTile.Value.X, targetedTile.Value.Y);
                        miningProgress = 0f;
                        currentlyMiningTile = null;
                    }
                }
            }
            else
            {
                if (mouseState.LeftButton == ButtonState.Released)
                {
                    currentlyMiningTile = null;
                    miningProgress = 0f;
                }
            }

            // Continuous block placement (right-click held)
            if (mouseState.RightButton == ButtonState.Pressed && placementCooldown <= 0f)
            {
                if (TryPlaceBlock(targetX, targetY, distanceToTile, playerPosition, playerWidth, playerHeight))
                {
                    placementCooldown = PLACEMENT_DELAY;
                }
            }

            // Update dropped items
            for (int i = droppedItems.Count - 1; i >= 0; i--)
            {
                DroppedItem item = droppedItems[i];
                item.Update(deltaTime, world);
                item.ApplyMagnetism(playerCenter, MAGNET_RANGE);

                if (item.CanCollect(playerPosition, playerWidth, playerHeight))
                {
                    inventory.AddItem(item.ItemType, 1);
                    droppedItems.RemoveAt(i);
                    continue;
                }

                if (item.ShouldDespawn())
                {
                    droppedItems.RemoveAt(i);
                }
            }

            previousMouseState = mouseState;
        }

        private bool TryPlaceBlock(int targetX, int targetY, float distance, Vector2 playerPosition, int playerWidth, int playerHeight)
        {
            if (distance > PLACEMENT_RANGE * World.World.TILE_SIZE)
                return false;

            Tile targetTile = world.GetTile(targetX, targetY);
            if (targetTile == null || targetTile.IsActive)
                return false;

            InventorySlot selectedSlot = inventory.GetSlot(selectedHotbarSlot);
            if (selectedSlot == null || selectedSlot.IsEmpty())
                return false;

            ItemType blockItemType = selectedSlot.ItemType;
            if (!blockItemType.IsPlaceable())
                return false;

            // Torches need a solid block adjacent to attach to
            if (blockItemType == ItemType.Torch)
            {
                if (!HasAdjacentSolidBlock(targetX, targetY))
                {
                    Logger.Log("[MINING] Cannot place torch - no adjacent solid block");
                    return false;
                }
            }
            else
            {
                // Regular blocks can't be placed inside player
                Rectangle blockRect = new Rectangle(
                    targetX * World.World.TILE_SIZE,
                    targetY * World.World.TILE_SIZE,
                    World.World.TILE_SIZE,
                    World.World.TILE_SIZE
                );

                Rectangle playerRect = new Rectangle(
                    (int)playerPosition.X,
                    (int)playerPosition.Y,
                    playerWidth,
                    playerHeight
                );

                if (blockRect.Intersects(playerRect))
                {
                    return false;
                }
            }

            world.SetTile(targetX, targetY, new Tile(blockItemType.ToTileType()));

            selectedSlot.Count--;
            if (selectedSlot.Count <= 0)
            {
                selectedSlot.ItemType = ItemType.Dirt;
                selectedSlot.Count = 0;
            }

            Logger.Log($"[MINING] Placed {blockItemType} at ({targetX}, {targetY})");

            return true;
        }

        private bool HasAdjacentSolidBlock(int x, int y)
        {
            // Check all 4 adjacent tiles
            if (world.IsSolidAtPosition(x - 1, y)) return true;
            if (world.IsSolidAtPosition(x + 1, y)) return true;
            if (world.IsSolidAtPosition(x, y - 1)) return true;
            if (world.IsSolidAtPosition(x, y + 1)) return true;
            return false;
        }

        private bool IsPlaceable(TileType type)
        {
            // Only allow placement of solid blocks (not ores, not air, not leaves)
            switch (type)
            {
                case TileType.Dirt:
                case TileType.Grass:
                case TileType.Stone:
                case TileType.Wood:
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
            }
            else
            {
                world.SetTile(x, y, new Tile(TileType.Air));
                droppedItemType = ItemTypeExtensions.FromTileType(tileType);  // Convert TileType to ItemType
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
            // Find which tree this belongs to and count its wood blocks
            Point tilePos = new Point(x, y);

            // Simple approximation: check tiles in a 3x20 area around hit point
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
    }
}