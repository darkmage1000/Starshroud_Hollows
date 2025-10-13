using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Claude4_5Terraria.Enums;
using System;
using System.Collections.Generic; // Add this for List

namespace Claude4_5Terraria.Systems
{
    public class CombatSystem
    {
        private bool isAttacking;
        private float attackTimer;
        private const float ATTACK_DURATION = 0.4f;
        private float cooldownTimer;
        public int CurrentAnimationFrame { get; private set; }
        private bool attackingRight;
        private float activeWeaponRecoveryTime = 0.5f;

        // NEW: Tracks targets hit during a single swing to prevent multi-hits
        private List<object> hitTargets;

        public CombatSystem()
        {
            isAttacking = false;
            attackTimer = 0f;
            cooldownTimer = 0f;
            CurrentAnimationFrame = 0;
            attackingRight = true;
            hitTargets = new List<object>(); // Initialize the list
        }

        private float GetWeaponRecoveryTime(ItemType weapon)
        {
            switch (weapon)
            {
                case ItemType.WoodSword: return 0.5f;
                case ItemType.CopperSword:
                case ItemType.IronSword:
                case ItemType.SilverSword:
                case ItemType.GoldSword:
                case ItemType.PlatinumSword: return 0.3f;
                case ItemType.RunicSword: return 0.3f;
                case ItemType.TrollClub: return 1.2f;
                default: return 0.2f;
            }
        }

        public void Update(float deltaTime, Vector2 playerPosition, bool facingRight, MouseState mouseState, MouseState previousMouseState, ItemType currentWeapon = ItemType.WoodSword)
        {
            if (cooldownTimer > 0)
            {
                cooldownTimer -= deltaTime;
            }

            if (mouseState.LeftButton == ButtonState.Pressed &&
                previousMouseState.LeftButton == ButtonState.Released &&
                cooldownTimer <= 0 &&
                !isAttacking)
            {
                activeWeaponRecoveryTime = GetWeaponRecoveryTime(currentWeapon);
                StartAttack(facingRight);
            }

            if (isAttacking)
            {
                attackTimer += deltaTime;
                float progress = attackTimer / ATTACK_DURATION;
                if (progress < 0.33f) CurrentAnimationFrame = 0;
                else if (progress < 0.66f) CurrentAnimationFrame = 1;
                else CurrentAnimationFrame = 2;

                if (attackTimer >= ATTACK_DURATION)
                {
                    isAttacking = false;
                    attackTimer = 0f;
                    cooldownTimer = activeWeaponRecoveryTime;
                    CurrentAnimationFrame = 0;
                }
            }
        }

        private void StartAttack(bool facingRight)
        {
            isAttacking = true;
            attackTimer = 0f;
            attackingRight = facingRight;
            CurrentAnimationFrame = 0;
            hitTargets.Clear(); // Clear hit targets at the start of a new swing
            Logger.Log($"[COMBAT] Attack started, facing {(facingRight ? "right" : "left")}");
        }

        public bool IsAttacking() => isAttacking;
        public int GetCurrentAttackFrame() => CurrentAnimationFrame;
        public bool CanAttack() => !isAttacking && cooldownTimer <= 0;

        // NEW: Checks if a target has already been hit by the current swing
        public bool HasAlreadyHit(object target) => hitTargets.Contains(target);

        // NEW: Registers a target as hit for the current swing
        public void RegisterHit(object target)
        {
            if (!hitTargets.Contains(target))
            {
                hitTargets.Add(target);
            }
        }

        public Rectangle GetAttackHitbox(Vector2 playerPosition, int playerWidth, int playerHeight)
        {
            if (!isAttacking) return Rectangle.Empty;

            const int ATTACK_RANGE = 64;
            const int ATTACK_HEIGHT = 64;
            int hitboxX;
            int hitboxY = (int)playerPosition.Y + (playerHeight / 2) - (ATTACK_HEIGHT / 2);

            if (attackingRight) hitboxX = (int)playerPosition.X + playerWidth;
            else hitboxX = (int)playerPosition.X - ATTACK_RANGE;

            return new Rectangle(hitboxX, hitboxY, ATTACK_RANGE, ATTACK_HEIGHT);
        }

        public float GetAttackDamage(ItemType weapon)
        {
            switch (weapon)
            {
                case ItemType.WoodSword: return 2f;
                case ItemType.CopperSword: return 3f;
                case ItemType.IronSword: return 4f;
                case ItemType.SilverSword: return 5f;
                case ItemType.GoldSword: return 6f;
                case ItemType.PlatinumSword: return 7f;
                case ItemType.RunicSword: return 10f;
                case ItemType.TrollClub: return 10f;
                default: return 1f;
            }
        }
    }
}