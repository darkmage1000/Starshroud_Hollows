using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarshroudHollows.Interfaces; // Using the interface
using System.Collections.Generic;
using System.Linq;

namespace StarshroudHollows.Systems.Summons
{
    public class SummonSystem
    {
        private List<Summon> activeSummons;
        private World.World world;
        private Texture2D echoWispTexture;
        private const int MAX_ECHO_WISPS = 3;

        public SummonSystem(World.World world)
        {
            this.world = world;
            activeSummons = new List<Summon>();
        }

        public void LoadTextures(Texture2D echoWisp)
        {
            echoWispTexture = echoWisp;
        }

        public bool SummonEchoWisp(Vector2 position)
        {
            int currentWispCount = activeSummons.OfType<EchoWisp>().Count();
            if (currentWispCount >= MAX_ECHO_WISPS)
            {
                var oldestWisp = activeSummons.OfType<EchoWisp>().FirstOrDefault();
                if (oldestWisp != null) activeSummons.Remove(oldestWisp);
            }

            EchoWisp wisp = new EchoWisp(position);
            if (echoWispTexture != null) wisp.SetTexture(echoWispTexture);
            activeSummons.Add(wisp);
            return true;
        }

        public void Update(float deltaTime, Vector2 playerPosition, List<IDamageable> targets)
        {
            foreach (var summon in activeSummons.ToList())
            {
                summon.Update(deltaTime, playerPosition, targets, world);
            }
            activeSummons.RemoveAll(s => !s.IsActive);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            foreach (var summon in activeSummons)
            {
                summon.Draw(spriteBatch, pixelTexture);
            }
        }

        public List<Summon> GetActiveSummons() => activeSummons;

        // Add these two methods back into your SummonSystem.cs file

        /// <summary>
        /// Get the number of active Echo Wisps
        /// </summary>
        public int GetActiveEchoWispCount()
        {
            return activeSummons.OfType<EchoWisp>().Count();
        }

        /// <summary>
        /// Get the maximum number of Echo Wisps allowed
        /// </summary>
        public int GetMaxEchoWisps()
        {
            return MAX_ECHO_WISPS;
        }
    }
}