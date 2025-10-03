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

        private const float SURFACE_DEPTH = 110f;
        private const float DARKNESS_START_DEPTH = 130f;
        private const float FULL_DARKNESS_DEPTH = 150f;

        private const float TORCH_RADIUS = 8f;
        private const float TORCH_BRIGHTNESS = 0.9f;

        private Dictionary<Point, float> torchLightCache;
        private int lastUpdateFrame;

        public LightingSystem(World.World world, TimeSystem timeSystem)
        {
            this.world = world;
            this.timeSystem = timeSystem;
            torchLightCache = new Dictionary<Point, float>();
            lastUpdateFrame = 0;
        }

        public void UpdateTorchCache(Vector2 cameraPosition)
        {
            lastUpdateFrame++;

            // Update torch cache every 10 frames
            if (lastUpdateFrame % 10 != 0)
                return;

            torchLightCache.Clear();

            int cameraTileX = (int)(cameraPosition.X / World.World.TILE_SIZE);
            int cameraTileY = (int)(cameraPosition.Y / World.World.TILE_SIZE);

            int checkRadius = 20;

            // Find all torches near camera
            for (int dx = -checkRadius; dx <= checkRadius; dx++)
            {
                for (int dy = -checkRadius; dy <= checkRadius; dy++)
                {
                    int checkX = cameraTileX + dx;
                    int checkY = cameraTileY + dy;

                    World.Tile tile = world.GetTile(checkX, checkY);
                    if (tile != null && tile.IsActive && tile.Type == TileType.Torch)
                    {
                        Point torchPos = new Point(checkX, checkY);

                        // Calculate light for tiles around this torch
                        for (int tx = -10; tx <= 10; tx++)
                        {
                            for (int ty = -10; ty <= 10; ty++)
                            {
                                int lightX = checkX + tx;
                                int lightY = checkY + ty;
                                Point lightPos = new Point(lightX, lightY);

                                float distance = (float)Math.Sqrt(tx * tx + ty * ty);
                                if (distance <= TORCH_RADIUS)
                                {
                                    float lightValue = TORCH_BRIGHTNESS * (1f - (distance / TORCH_RADIUS));

                                    if (!torchLightCache.ContainsKey(lightPos))
                                    {
                                        torchLightCache[lightPos] = lightValue;
                                    }
                                    else
                                    {
                                        torchLightCache[lightPos] = Math.Max(torchLightCache[lightPos], lightValue);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public float GetLightLevel(int tileX, int tileY)
        {
            float depth = tileY;

            // Surface lighting (based on time of day)
            if (depth < SURFACE_DEPTH)
            {
                return 1f;
            }

            // Transition to darkness
            if (depth < DARKNESS_START_DEPTH)
            {
                float t = (depth - SURFACE_DEPTH) / (DARKNESS_START_DEPTH - SURFACE_DEPTH);
                return 1f - (t * 0.3f);
            }

            // Underground base darkness
            float baseDarkness = 0.15f;
            if (depth < FULL_DARKNESS_DEPTH)
            {
                float t = (depth - DARKNESS_START_DEPTH) / (FULL_DARKNESS_DEPTH - DARKNESS_START_DEPTH);
                baseDarkness = 0.7f - (t * 0.55f);
            }

            // Add torch lighting
            Point tilePos = new Point(tileX, tileY);
            if (torchLightCache.ContainsKey(tilePos))
            {
                float torchLight = torchLightCache[tilePos];
                return Math.Min(1f, baseDarkness + torchLight);
            }

            return baseDarkness;
        }

        public Color ApplyLighting(Color baseColor, int tileX, int tileY)
        {
            float lightLevel = GetLightLevel(tileX, tileY);

            return new Color(
                (int)(baseColor.R * lightLevel),
                (int)(baseColor.G * lightLevel),
                (int)(baseColor.B * lightLevel),
                baseColor.A
            );
        }
    }
}