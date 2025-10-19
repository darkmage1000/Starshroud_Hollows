using Microsoft.Xna.Framework;
using System;

namespace StarshroudHollows.World
{
    public class Sapling
    {
        public Point Position { get; private set; }
        public float GrowthTime { get; private set; }
        public const float TIME_TO_GROW = 120f; // 2 minutes

        public Sapling(int x, int y)
        {
            Position = new Point(x, y);
            GrowthTime = 0f;
        }

        public void Update(float deltaTime)
        {
            GrowthTime += deltaTime;
        }

        public bool IsReadyToGrow()
        {
            return GrowthTime >= TIME_TO_GROW;
        }

        public float GetGrowthProgress()
        {
            return Math.Min(1f, GrowthTime / TIME_TO_GROW);
        }
    }
}