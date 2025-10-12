using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Claude4_5Terraria.Enums;
using System;

namespace Claude4_5Terraria.Systems
{
    public class CombatSystem
    {
        private bool isAttacking;
        private float attackTimer;
        private const float ATTACK_DURATION = 0.3f; // 300ms active swing animation time
        private float cooldownTimer;

        // Animation frames (0 = idle, 1 = mid-swing, 2 = full swing)
        public int CurrentAnimationFrame { get; private set; }

        // Attack direction (for determining hit area)
        private bool attackingRight;

        // NEW: Stores the recovery time calculated based on the weapon used.
        private float activeWeaponRecoveryTime = 0.5f; // Defaults to WoodSword speed

        public CombatSystem()
        {
            isAttacking = false;
            attackTimer = 0f;
            cooldownTimer = 0f;
            CurrentAnimationFrame = 0;
            attackingRight = true;
        }

        // NEW METHOD: Defines the weapon's inherent attack speed (recovery time).
        private float GetWeaponRecoveryTime(ItemType weapon)
        {
            switch (weapon)
            {
                case ItemType.WoodSword:
                    // Wood Sword is slow: 0.5 seconds of recovery after the 0.3s swing.
                    return 0.5f;
                // Future: Add faster weapons here (e.g., return 0.1f for a fast dagger)
                default:
                    return 0.2f; // Default/Fist recovery
            }
        }

        public void Update(float deltaTime, Vector2 playerPosition, bool facingRight, MouseState mouseState, MouseState previousMouseState)
        {
            // Update cooldown
            if (cooldownTimer > 0)
            {
                cooldownTimer -= deltaTime;
            }

            // FIX: Check for attack input (left mouse button)
            // CRITICAL: A new attack can ONLY start if the previous one is NOT active (isAttacking) 
            // AND the cooldown is fully reset (cooldownTimer <= 0).
            if (mouseState.LeftButton == ButtonState.Pressed &&
                previousMouseState.LeftButton == ButtonState.Released &&
                cooldownTimer <= 0 &&
                !isAttacking)
            {
                // NOTE: We rely on Game1.cs to check if the held item is a sword before calling this Update block.
                // We'll set the recovery time here based on the assumed weapon.
                activeWeaponRecoveryTime = GetWeaponRecoveryTime(ItemType.WoodSword); // Assuming WoodSword is held
                StartAttack(facingRight);
            }

            // Update attack animation
            if (isAttacking)
            {
                attackTimer += deltaTime;

                // Update animation frame based on progress
                float progress = attackTimer / ATTACK_DURATION;
                if (progress < 0.33f)
                {
                    CurrentAnimationFrame = 0; // Start
                }
                else if (progress < 0.66f)
                {
                    CurrentAnimationFrame = 1; // Mid-swing
                }
                else
                {
                    CurrentAnimationFrame = 2; // Full swing
                }

                // End attack
                if (attackTimer >= ATTACK_DURATION)
                {
                    // FIX: Set the cooldown based on the determined weapon recovery time.
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
            Logger.Log($"[COMBAT] Attack started, facing {(facingRight ? "right" : "left")}");
        }

        public bool IsAttacking()
        {
            return isAttacking;
        }

        public bool CanAttack()
        {
            return !isAttacking && cooldownTimer <= 0;
        }

        public Rectangle GetAttackHitbox(Vector2 playerPosition, int playerWidth, int playerHeight)
        {
            if (!isAttacking)
            {
                return Rectangle.Empty;
            }

            const int ATTACK_RANGE = 64; // 2 tiles (32px * 2)
            const int ATTACK_HEIGHT = 64; // 2 tiles vertical

            int hitboxX;
            int hitboxY = (int)playerPosition.Y + (playerHeight / 2) - (ATTACK_HEIGHT / 2);

            if (attackingRight)
            {
                hitboxX = (int)playerPosition.X + playerWidth;
            }
            else
            {
                hitboxX = (int)playerPosition.X - ATTACK_RANGE;
            }

            return new Rectangle(hitboxX, hitboxY, ATTACK_RANGE, ATTACK_HEIGHT);
        }

        public float GetAttackDamage(ItemType weapon)
        {
            switch (weapon)
            {
                case ItemType.WoodSword:
                    return 2f;
                // Future weapons can be added here
                default:
                    return 1f;
            }
        }
    }
}