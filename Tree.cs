using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StarshroudHollows.World
{
    public class Tree
    {
        public int BaseX { get; set; }  // X position of tree base
        public int BaseY { get; set; }  // Y position of tree base (surface)
        public int Height { get; set; } // Tree trunk height
        public List<Point> TilePositions { get; set; }  // All tiles that belong to this tree

        public Tree(int baseX, int baseY, int height)
        {
            BaseX = baseX;
            BaseY = baseY;
            Height = height;
            TilePositions = new List<Point>();
        }

        public void AddTile(int x, int y)
        {
            TilePositions.Add(new Point(x, y));
        }

        public bool ContainsTile(int x, int y)
        {
            return TilePositions.Contains(new Point(x, y));
        }
    }
}