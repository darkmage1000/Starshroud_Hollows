using Microsoft.Xna.Framework;
using Claude4_5Terraria.Enums;
using Claude4_5Terraria.Systems;
using System;
// FIX: Add using directive for Player to correctly access static members
using Claude4_5Terraria.Player;
using Microsoft.Xna.Framework.Input; // NEW: Added MouseState reference

namespace Claude4_5Terraria.Systems
{
    public class MagicSystem
    {
        private Claude4_5Terraria.Player.Player player; // Reference to the player entity
        private World.World world; // NEW: Need World for Camera/Mouse logic
        private Camera camera;     // NEW: Need Camera for mouse-to-world conversion

        // Mana Stats
        public float MaxMana { get; private set; }
        public float CurrentMana { get; private set; }

        // Mana Regen
        private float manaRegenTimer;
        // FIX: Mana recovered every 5 seconds
        private const float MANA_REGEN_INTERVAL = 5.0f;
        private const float MANA_REGEN_AMOUNT = 1.0f;

        // Spell Stats
        private const float MAGIC_BOLT_COST = 2.0f; // FIX: Mana cost is now 2.0f
        private const float MAGIC_BOLT_DAMAGE = 1.0f;

        // NEW: Casting Speed/Cooldown
        private float castCooldownTimer = 0f;
        private const float CAST_COOLDOWN = 0.15f; // Shoots every 0.15 seconds (very fast)

        // Projectile System Reference
        private ProjectileSystem projectileSystem;

        public MagicSystem(Claude4_5Terraria.Player.Player player, ProjectileSystem projectileSystem, World.World world, Camera camera) // NEW: Pass World and Camera
        {
            this.player = player;
            this.projectileSystem = projectileSystem;
            this.world = world;
            this.camera = camera;

            MaxMana = 20.0f;
            CurrentMana = MaxMana;
            manaRegenTimer = 0f;
            Logger.Log("[MAGIC] Magic System initialized with 20 Max Mana.");
        }

        public void Update(float deltaTime, MouseState mouseState, MouseState previousMouseState, ItemType heldItem)
        {
            // 1. Mana Regeneration
            HandleManaRegen(deltaTime);

            // Update cast cooldown
            if (castCooldownTimer > 0)
            {
                castCooldownTimer -= deltaTime;
            }

            // 2. Casting Input (Left Click)
            if (mouseState.LeftButton == ButtonState.Pressed && heldItem == ItemType.WoodWand)
            {
                // FIX: Only attempt to cast if the internal cooldown is ready
                if (castCooldownTimer <= 0)
                {
                    TryCastMagicBolt(mouseState);
                }
            }
        }

        private void HandleManaRegen(float deltaTime)
        {
            // Only regenerate if mana is not full
            if (CurrentMana < MaxMana)
            {
                manaRegenTimer += deltaTime;
                if (manaRegenTimer >= MANA_REGEN_INTERVAL)
                {
                    CurrentMana = Math.Min(MaxMana, CurrentMana + MANA_REGEN_AMOUNT);
                    manaRegenTimer = 0f;
                    Logger.Log($"[MAGIC] Mana regenerated. Current: {CurrentMana}/{MaxMana}");
                }
            }
            else
            {
                manaRegenTimer = 0f;
            }
        }

        public bool TryCastMagicBolt(MouseState mouseState)
        {
            if (CurrentMana >= MAGIC_BOLT_COST)
            {
                // Calculate aiming direction
                Vector2 mouseWorldPosition = camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));

                Vector2 playerCenter = player.GetCenterPosition();

                // Calculate spawn position (just outside the player)
                Vector2 direction = mouseWorldPosition - playerCenter;
                if (direction.LengthSquared() > 0)
                {
                    direction.Normalize();
                }

                // FIX: Access static player size constants via the Player class
                // spawn position is slightly offset from player center
                Vector2 spawnPosition = playerCenter + direction * (Player.Player.PLAYER_WIDTH / 2 + 5);

                CurrentMana -= MAGIC_BOLT_COST;

                // Set the cooldown immediately after a successful cast
                castCooldownTimer = CAST_COOLDOWN;

                // Launch the projectile, passing the normalized direction vector
                projectileSystem.Launch(ProjectileType.MagicBolt, spawnPosition, direction, MAGIC_BOLT_DAMAGE);

                Logger.Log($"[MAGIC] Cast Magic Bolt. Mana remaining: {CurrentMana}/{MaxMana}");
                return true;
            }

            return false;
        }

        // Getter for HUD
        public float GetCurrentMana() => CurrentMana;
        public float GetMaxMana() => MaxMana;
    }
}