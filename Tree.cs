using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StarshroudHollows.Enums;

namespace StarshroudHollows.World
{
    public class Tree
    {
        public int BaseX { get; set; }  // X position of tree base
        public int BaseY { get; set; }  // Y position of tree base (surface)
        public int Height { get; set; } // Tree trunk height
        public List<Point> TilePositions { get; set; }  // All tiles that belong to this tree
        public TileType TreeType { get; set; } // Type of tree (Wood, SnowTree, JungleTree, etc.)
        public float Scale { get; set; } // Random scale for visual variety (0.8 to 1.2)

        public Tree(int baseX, int baseY, int height, TileType treeType = TileType.Wood)
        {
            BaseX = baseX;
            BaseY = baseY;
            Height = height;
            TreeType = treeType;
            TilePositions = new List<Point>();
            
            // Random scale between 0.8 and 1.2 for visual variety
            Random rand = new Random(baseX * 1000 + baseY);
            Scale = 0.8f + (float)(rand.NextDouble() * 0.4);
        }

        public void AddTile(int x, int y)
        {
            TilePositions.Add(new Point(x, y));
        }

        public bool ContainsTile(int x, int y)
        {
            return TilePositions.Contains(new Point(x, y));
        }
        
        // NEW: Check if a tile position is within the tree's visual sprite bounds
        public bool IsWithinSpriteBounds(int x, int y)
        {
            // Calculate sprite dimensions based on scale
            int spriteWidth = (int)(10 * Scale); // Approximate width in tiles
            int spriteHeight = (int)(Height * 1.5f * Scale); // Trees are 1.5x their trunk height
            
            // Calculate the bounds (centered on base)
            int left = BaseX - spriteWidth / 2;
            int right = BaseX + spriteWidth / 2;
            int top = BaseY - spriteHeight;
            int bottom = BaseY;
            
            return x >= left && x <= right && y >= top && y <= bottom;
        }
    }
}