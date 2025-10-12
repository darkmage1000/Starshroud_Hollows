using Microsoft.Xna.Framework;
using Claude4_5Terraria.Enums;
using Claude4_5Terraria.Systems;
using System;
using Claude4_5Terraria.Player;
using Microsoft.Xna.Framework.Input;

namespace Claude4_5Terraria.Systems
{
    public class MagicSystem
    {
        private Claude4_5Terraria.Player.Player player;
        private World.World world;
        private Camera camera;

        // Mana Stats
        public float MaxMana { get; private set; }
        public float CurrentMana { get; private set; }

        // Mana Regen
        private float manaRegenTimer;
        private const float MANA_REGEN_INTERVAL = 5.0f; // CHANGED: Regenerate every 5 seconds
        private const float MANA_REGEN_AMOUNT = 1.0f;   // Regenerate 1 mana point

        // Spell Stats
        private const float MAGIC_BOLT_COST = 2.0f;
        private const float MAGIC_BOLT_DAMAGE = 1.0f;

        // Casting Speed/Cooldown
        private float castCooldownTimer = 0f;
        private const float CAST_COOLDOWN = 0.15f;

        // Projectile System Reference
        private ProjectileSystem projectileSystem;

        public MagicSystem(Claude4_5Terraria.Player.Player player, ProjectileSystem projectileSystem, World.World world, Camera camera)
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
            HandleManaRegen(deltaTime);

            if (castCooldownTimer > 0)
            {
                castCooldownTimer -= deltaTime;
            }

            if (mouseState.LeftButton == ButtonState.Pressed && IsWand(heldItem))
            {
                if (castCooldownTimer <= 0)
                {
                    TryCastSpell(heldItem, mouseState);
                }
            }
        }

        private bool IsWand(ItemType item)
        {
            return item == ItemType.WoodWand || item == ItemType.FireWand ||
                   item == ItemType.LightningWand || item == ItemType.NatureWand ||
                   item == ItemType.WaterWand || item == ItemType.HalfMoonWand ||
                   item == ItemType.RunicLaserWand || item == ItemType.WoodSummonStaff;
        }

        private void HandleManaRegen(float deltaTime)
        {
            if (CurrentMana < MaxMana)
            {
                manaRegenTimer += deltaTime;
                if (manaRegenTimer >= MANA_REGEN_INTERVAL)
                {
                    CurrentMana = Math.Min(MaxMana, CurrentMana + MANA_REGEN_AMOUNT);
                    manaRegenTimer = 0f;
                }
            }
            else
            {
                manaRegenTimer = 0f;
            }
        }

        private void TryCastSpell(ItemType wand, MouseState mouseState)
        {
            ProjectileType spellType;
            float manaCost;
            float damage;

            switch (wand)
            {
                case ItemType.WoodWand:
                    spellType = ProjectileType.MagicBolt;
                    manaCost = 0.5f;
                    damage = 1f;
                    break;
                case ItemType.FireWand:
                    spellType = ProjectileType.FireBolt;
                    manaCost = 0.7f;
                    damage = 3f;
                    break;
                case ItemType.LightningWand:
                    spellType = ProjectileType.LightningBlast;
                    manaCost = 1f;
                    damage = 4f;
                    break;
                case ItemType.NatureWand:
                    spellType = ProjectileType.NatureVine;
                    manaCost = 4f;
                    damage = 2f;
                    break;
                case ItemType.WaterWand:
                    spellType = ProjectileType.WaterBubble;
                    manaCost = 0.9f;
                    damage = 2f;
                    break;
                case ItemType.HalfMoonWand:
                    spellType = ProjectileType.HalfMoonSlash;
                    manaCost = 5f;
                    damage = 5f;
                    break;
                case ItemType.RunicLaserWand:
                    spellType = ProjectileType.RunicLaser;
                    manaCost = 0.01f;
                    damage = 8f;
                    break;
                default:
                    return;
            }

            if (CurrentMana >= manaCost)
            {
                CastSpell(spellType, damage, manaCost, mouseState);
            }
        }

        private void CastSpell(ProjectileType spellType, float damage, float manaCost, MouseState mouseState)
        {
            Vector2 mouseWorldPosition = camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
            Vector2 playerCenter = player.GetCenterPosition();

            Vector2 direction = mouseWorldPosition - playerCenter;
            if (direction.LengthSquared() > 0)
            {
                direction.Normalize();
            }

            Vector2 spawnPosition = playerCenter + direction * (Player.Player.PLAYER_WIDTH / 2 + 5);

            CurrentMana -= manaCost;
            castCooldownTimer = CAST_COOLDOWN;

            if (spellType == ProjectileType.NatureVine)
            {
                projectileSystem.LaunchAtPosition(spellType, spawnPosition, mouseWorldPosition, damage);
            }
            else
            {
                projectileSystem.Launch(spellType, spawnPosition, direction, damage);
            }

            Logger.Log($"[MAGIC] Cast {spellType}. Mana cost: {manaCost}. Mana remaining: {CurrentMana}/{MaxMana}");
        }

        public bool TryCastMagicBolt(MouseState mouseState)
        {
            TryCastSpell(ItemType.WoodWand, mouseState);
            return true;
        }

        public float GetCurrentMana() => CurrentMana;
        public float GetMaxMana() => MaxMana;
        
        public void RestoreMana()
        {
            CurrentMana = MaxMana;
            Logger.Log($"[MAGIC] Mana fully restored to {MaxMana}");
        }
    }
}