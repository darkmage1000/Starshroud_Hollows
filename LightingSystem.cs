using Microsoft.Xna.Framework;
using Claude4_5Terraria.Enums;
using System;
using System.Collections.Generic;

namespace Claude4_5Terraria.Systems
{
    public class LightingSystem
    {
        private World.World world;
        private TimeSystem timeSystem;

        private const float TORCH_RADIUS = 12f;  // Increased radius
        private const float TORCH_BRIGHTNESS = 3.5f;  // Increased brightness
        
        private const float LAVA_RADIUS = 8f;  // Lava glows less far than torches
        private const float LAVA_BRIGHTNESS = 2.0f;  // Dimmer orange glow

        private Dictionary<Point, float> lightMap;
        private List<Point> torchPositions;
        private List<Point> lavaPositions;

        // NEW: Dynamic light source for player's held torch
        private Vector2 playerLightWorldPosition;
        private bool isPlayerLightActive;


        public LightingSystem(World.World world, TimeSystem timeSystem)
        {
            this.world = world;
            this.timeSystem = timeSystem;
            lightMap = new Dictionary<Point, float>();
            torchPositions = new List<Point>();
            lavaPositions = new List<Point>();

            playerLightWorldPosition = Vector2.Zero;
            isPlayerLightActive = false;
        }

        // NEW: Public method for Game1 to control the player's light source
        public void SetPlayerLight(Vector2 worldPosition, bool active)
        {
            if (isPlayerLightActive != active)
            {
                // Clear the cache if the light status changes
                lightMap.Clear();
            }
            isPlayerLightActive = active;
            playerLightWorldPosition = worldPosition;
        }

        public void UpdateTorchCache(Vector2 cameraPosition)
        {
            torchPositions.Clear();
            lavaPositions.Clear();

            int cameraTileX = (int)(cameraPosition.X / World.World.TILE_SIZE);
            int cameraTileY = (int)(cameraPosition.Y / World.World.TILE_SIZE);

            int checkRadius = 30;

            for (int dx = -checkRadius; dx <= checkRadius; dx++)
            {
                for (int dy = -checkRadius; dy <= checkRadius; dy++)
                {
                    int checkX = cameraTileX + dx;
                    int checkY = cameraTileY + dy;

                    World.Tile tile = world.GetTile(checkX, checkY);
                    if (tile != null && tile.IsActive)
                    {
                        if (tile.Type == TileType.Torch)
                        {
                            torchPositions.Add(new Point(checkX, checkY));
                        }
                        else if (tile.Type == TileType.Lava)
                        {
                            lavaPositions.Add(new Point(checkX, checkY));
                        }
                    }
                }
            }

            lightMap.Clear();
        }

        public float GetLightLevel(int tileX, int tileY)
        {
            Point tilePos = new Point(tileX, tileY);

            if (lightMap.ContainsKey(tilePos))
            {
                return lightMap[tilePos];
            }

            float finalLight = CalculateLighting(tileX, tileY);
            lightMap[tilePos] = finalLight;

            return finalLight;
        }

        private float CalculateLighting(int tileX, int tileY)
        {
            float ambientLight = timeSystem.GetAmbientLight();
            float sunlight = CalculateSunlight(tileX, tileY, ambientLight);
            float torchLight = CalculateTorchLight(tileX, tileY);
            float lavaLight = CalculateLavaLight(tileX, tileY);
            float exploredLight = world.IsTileExplored(tileX, tileY) ? 0.15f : 0f; // Slight glow for explored areas

            float finalLight = Math.Max(Math.Max(Math.Max(sunlight, torchLight), lavaLight), exploredLight);

            return Math.Min(1.0f, finalLight);
        }

        private float CalculateSunlight(int tileX, int tileY, float ambientLight)
        {
            bool blockedBySolid = false;
            bool underLeaves = false;
            int airGapAbove = 0;

            for (int y = tileY - 1; y >= 0; y--)
            {
                World.Tile tile = world.GetTile(tileX, y);

                if (tile == null || !tile.IsActive)
                {
                    airGapAbove++;
                    continue;
                }

                if (tile.Type == TileType.Leaves)
                {
                    underLeaves = true;
                    continue;
                }

                if (tile.Type == TileType.Wood || tile.Type == TileType.Torch || tile.Type == TileType.Sapling)
                {
                    continue;
                }

                blockedBySolid = true;
                break;
            }

            if (blockedBySolid)
            {
                // Underground ambient light - very dim
                return 0.05f;
            }

            if (underLeaves)
            {
                return ambientLight * 0.7f;
            }

            return ambientLight;
        }

        private float CalculateTorchLight(int tileX, int tileY)
        {
            float maxTorchLight = 0f;

            // --- 1. Check for Placed Torches ---
            foreach (Point torchPos in torchPositions)
            {
                float distance = (float)Math.Sqrt(
                    Math.Pow(tileX - torchPos.X, 2) +
                    Math.Pow(tileY - torchPos.Y, 2)
                );

                if (distance > TORCH_RADIUS)
                    continue;

                if (IsLightPathBlocked(torchPos.X, torchPos.Y, tileX, tileY))
                    continue;

                // Linear falloff for more consistent lighting
                float falloff = 1.0f - (distance / TORCH_RADIUS);
                float lightStrength = TORCH_BRIGHTNESS * falloff;

                // Less aggressive curve so distant blocks are still visible
                lightStrength = (float)Math.Pow(lightStrength, 0.5);

                maxTorchLight = Math.Max(maxTorchLight, lightStrength);
            }

            // --- 2. Check for Player's Held Torch ---
            if (isPlayerLightActive)
            {
                // Convert world position to tile position for light path calculation
                int playerTileX = (int)(playerLightWorldPosition.X / World.World.TILE_SIZE);
                int playerTileY = (int)(playerLightWorldPosition.Y / World.World.TILE_SIZE);

                float distance = (float)Math.Sqrt(
                    Math.Pow(tileX - playerTileX, 2) +
                    Math.Pow(tileY - playerTileY, 2)
                );

                if (distance <= TORCH_RADIUS)
                {
                    // For the held torch, we don't block the light path right at the player's immediate area 
                    // to ensure it lights up the player directly.
                    // However, blocking solid walls further out is still important.

                    // We check if the target tile is blocked *from the player's light tile*.
                    if (!IsLightPathBlocked(playerTileX, playerTileY, tileX, tileY))
                    {
                        float falloff = 1.0f - (distance / TORCH_RADIUS);
                        float lightStrength = TORCH_BRIGHTNESS * falloff;
                        lightStrength = (float)Math.Pow(lightStrength, 0.5);

                        maxTorchLight = Math.Max(maxTorchLight, lightStrength);
                    }
                }
            }

            return maxTorchLight;
        }
        
        private float CalculateLavaLight(int tileX, int tileY)
        {
            float maxLavaLight = 0f;

            foreach (Point lavaPos in lavaPositions)
            {
                float distance = (float)Math.Sqrt(
                    Math.Pow(tileX - lavaPos.X, 2) +
                    Math.Pow(tileY - lavaPos.Y, 2)
                );

                if (distance > LAVA_RADIUS)
                    continue;

                if (IsLightPathBlocked(lavaPos.X, lavaPos.Y, tileX, tileY))
                    continue;

                // Linear falloff
                float falloff = 1.0f - (distance / LAVA_RADIUS);
                float lightStrength = LAVA_BRIGHTNESS * falloff;

                // Smoother curve for lava glow
                lightStrength = (float)Math.Pow(lightStrength, 0.6);

                maxLavaLight = Math.Max(maxLavaLight, lightStrength);
            }

            return maxLavaLight;
        }

        private bool IsLightPathBlocked(int fromX, int fromY, int toX, int toY)
        {
            if (fromX == toX && fromY == toY)
                return false;

            int dx = Math.Abs(toX - fromX);
            int dy = Math.Abs(toY - fromY);
            int x = fromX;
            int y = fromY;
            int n = 1 + dx + dy;
            int x_inc = (toX > fromX) ? 1 : -1;
            int y_inc = (toY > fromY) ? 1 : -1;
            int error = dx - dy;
            dx *= 2;
            dy *= 2;

            for (; n > 0; --n)
            {
                // Don't check the starting torch position or the target position
                if ((x != fromX || y != fromY) && (x != toX || y != toY))
                {
                    World.Tile tile = world.GetTile(x, y);

                    if (tile != null && tile.IsActive)
                    {
                        if (tile.Type != TileType.Torch &&
                            tile.Type != TileType.Leaves &&
                            tile.Type != TileType.Wood &&
                            tile.Type != TileType.Sapling)
                        {
                            // Found a blocking tile before reaching target
                            return true;
                        }
                    }
                }

                if (error > 0)
                {
                    x += x_inc;
                    error -= dy;
                }
                else
                {
                    y += y_inc;
                    error += dx;
                }
            }

            return false;
        }

        public void ClearCache()
        {
            lightMap.Clear();
        }
    }
}