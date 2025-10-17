using Microsoft.Xna.Framework;
using StarshroudHollows.Enums;
using System;

namespace StarshroudHollows.Systems
{
    /// <summary>
    /// Represents a summoning altar that can spawn boss portals
    /// </summary>
    public class SummonAltar
    {
        public Point Position { get; private set; }
        public bool IsActive { get; private set; }
        public BossType AssociatedBoss { get; private set; }
        
        private float cooldownTimer = 0f;
        private const float ACTIVATION_COOLDOWN = 2f; // 2 second cooldown between activations

        public SummonAltar(Point position, BossType bossType)
        {
            Position = position;
            AssociatedBoss = bossType;
            IsActive = false;
        }

        public void Update(float deltaTime)
        {
            if (cooldownTimer > 0)
            {
                cooldownTimer -= deltaTime;
                if (cooldownTimer < 0)
                    cooldownTimer = 0;
            }
        }

        /// <summary>
        /// Attempts to activate the altar with the given summon item
        /// Returns true if portal should spawn
        /// </summary>
        public bool TryActivate(ItemType summonItem)
        {
            if (cooldownTimer > 0)
            {
                Logger.Log("[ALTAR] Altar is on cooldown!");
                return false;
            }

            // Check if the summon item matches the boss type
            bool validItem = false;
            switch (AssociatedBoss)
            {
                case BossType.CaveTroll:
                    validItem = summonItem == ItemType.TrollBait;
                    break;
                // Future bosses can be added here
            }

            if (validItem)
            {
                IsActive = true;
                cooldownTimer = ACTIVATION_COOLDOWN;
                Logger.Log($"[ALTAR] Activated! Spawning portal for {AssociatedBoss}");
                return true;
            }

            Logger.Log("[ALTAR] Invalid summon item for this altar!");
            return false;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public bool IsOnCooldown()
        {
            return cooldownTimer > 0;
        }

        public Rectangle GetHitbox()
        {
            return new Rectangle(
                Position.X * World.World.TILE_SIZE,
                Position.Y * World.World.TILE_SIZE,
                World.World.TILE_SIZE,
                World.World.TILE_SIZE
            );
        }
    }

    /// <summary>
    /// Types of bosses that can be summoned
    /// </summary>
    public enum BossType
    {
        CaveTroll,
        // Future bosses here
        // IceGolem,
        // FireDemon,
        // etc.
    }
}