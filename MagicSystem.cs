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
        // Faster regen for testing
        private const float MANA_REGEN_INTERVAL = 1.0f; // 1 second
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

            // 2. Casting Input (Left Click) - Check if holding any wand
            if (mouseState.LeftButton == ButtonState.Pressed && IsWand(heldItem))
            {
                // Only attempt to cast if the internal cooldown is ready
                if (castCooldownTimer <= 0)
                {
                    TryCastSpell(heldItem, mouseState);
                }
            }
        }

        // Helper method to check if an item is a wand
        private bool IsWand(ItemType item)
        {
            return item == ItemType.WoodWand || item == ItemType.FireWand || 
                   item == ItemType.LightningWand || item == ItemType.NatureWand || 
                   item == ItemType.WaterWand || item == ItemType.HalfMoonWand || 
                   item == ItemType.RunicLaserWand || item == ItemType.WoodSummonStaff;
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

        // New unified casting method that handles all wand types
        private void TryCastSpell(ItemType wand, MouseState mouseState)
        {
            ProjectileType spellType;
            float manaCost;
            float damage;

            // Determine spell properties based on wand type
            switch (wand)
            {
                case ItemType.WoodWand:
                    spellType = ProjectileType.MagicBolt;
                    manaCost = 2f;
                    damage = 1f;
                    break;
                case ItemType.FireWand:
                    spellType = ProjectileType.FireBolt;
                    manaCost = 3f;
                    damage = 3f;
                    break;
                case ItemType.LightningWand:
                    spellType = ProjectileType.LightningBlast;
                    manaCost = 4f;
                    damage = 4f;
                    break;
                case ItemType.NatureWand:
                    spellType = ProjectileType.NatureVine;
                    manaCost = 3f;
                    damage = 2f;
                    break;
                case ItemType.WaterWand:
                    spellType = ProjectileType.WaterBubble;
                    manaCost = 3f;
                    damage = 2f;
                    break;
                case ItemType.HalfMoonWand:
                    spellType = ProjectileType.HalfMoonSlash;
                    manaCost = 5f;
                    damage = 5f;
                    break;
                case ItemType.RunicLaserWand:
                    spellType = ProjectileType.RunicLaser;
                    manaCost = 6f;
                    damage = 8f;
                    break;
                default:
                    return; // Not a valid wand
            }

            // Check if we have enough mana
            if (CurrentMana >= manaCost)
            {
                CastSpell(spellType, damage, manaCost, mouseState);
            }
        }

        // Shared casting logic
        private void CastSpell(ProjectileType spellType, float damage, float manaCost, MouseState mouseState)
        {
            // Calculate aiming direction and mouse world position
            Vector2 mouseWorldPosition = camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
            Vector2 playerCenter = player.GetCenterPosition();

            // Calculate spawn position (just outside the player)
            Vector2 direction = mouseWorldPosition - playerCenter;
            if (direction.LengthSquared() > 0)
            {
                direction.Normalize();
            }

            // Spawn position is slightly offset from player center
            Vector2 spawnPosition = playerCenter + direction * (Player.Player.PLAYER_WIDTH / 2 + 5);

            CurrentMana -= manaCost;

            // Set the cooldown immediately after a successful cast
            castCooldownTimer = CAST_COOLDOWN;

            // Special handling for Nature Vine - it rises from ground at mouse position
            if (spellType == ProjectileType.NatureVine)
            {
                // Pass mouse world position directly for ground-rising spell
                projectileSystem.LaunchAtPosition(spellType, spawnPosition, mouseWorldPosition, damage);
            }
            else
            {
                // Normal directional projectiles
                projectileSystem.Launch(spellType, spawnPosition, direction, damage);
            }

            Logger.Log($"[MAGIC] Cast {spellType}. Mana cost: {manaCost}, Damage: {damage}. Mana remaining: {CurrentMana}/{MaxMana}");
        }

        // Old method kept for compatibility - now redirects to new system
        public bool TryCastMagicBolt(MouseState mouseState)
        {
            TryCastSpell(ItemType.WoodWand, mouseState);
            return true;
        }

        // Getter for HUD
        public float GetCurrentMana() => CurrentMana;
        public float GetMaxMana() => MaxMana;
    }
}