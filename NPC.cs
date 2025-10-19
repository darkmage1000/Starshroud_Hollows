using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarshroudHollows.Enums;
using System;
using System.Collections.Generic;

namespace StarshroudHollows.Systems.Housing
{
    /// <summary>
    /// Base NPC class - all NPCs inherit from this
    /// </summary>
    public abstract class NPC
    {
        public string Name { get; protected set; }
        public string Type { get; protected set; }
        public Vector2 Position { get; set; }
        public House AssignedHouse { get; set; }
        public bool IsAlive { get; protected set; }
        
        protected const int NPC_WIDTH = 24;
        protected const int NPC_HEIGHT = 48;
        
        // Movement
        protected Vector2 velocity;
        protected const float MOVE_SPEED = 1.0f;
        protected const float GRAVITY = 0.3f;
        protected const float MAX_FALL_SPEED = 8f;
        protected bool isOnGround = false;
        
        // AI behavior
        protected float idleTimer = 0f;
        protected float wanderTimer = 0f;
        protected int wanderDirection = 0; // -1 left, 0 idle, 1 right
        
        // Dialogue
        protected List<string> dialogueLines;
        protected int currentDialogueIndex = 0;
        
        // NEW: Combat System
        protected float attackCooldown = 0f;
        protected const float ATTACK_COOLDOWN_TIME = 2.0f;
        protected const float ATTACK_RANGE = 300f;
        public bool CanCombat { get; protected set; }
        
        public NPC(string name, string type, Vector2 spawnPosition)
        {
            Name = name;
            Type = type;
            Position = spawnPosition;
            IsAlive = true;
            velocity = Vector2.Zero;
            dialogueLines = new List<string>();
            CanCombat = false; // Most NPCs don't fight by default
        }
        
        public virtual void Update(float deltaTime, StarshroudHollows.World.World world)
        {
            if (!IsAlive) return;
            
            // Update timers
            idleTimer += deltaTime;
            wanderTimer += deltaTime;
            
            // NEW: Update combat cooldown
            if (attackCooldown > 0)
            {
                attackCooldown -= deltaTime;
                if (attackCooldown < 0) attackCooldown = 0;
            }
            
            // Simple wander AI
            if (wanderTimer >= 3f)
            {
                wanderTimer = 0f;
                wanderDirection = new Random().Next(-1, 2); // -1, 0, or 1
            }
            
            // Apply horizontal movement
            if (AssignedHouse != null)
            {
                velocity.X = wanderDirection * MOVE_SPEED;
                
                // Stay within house bounds
                Rectangle houseBounds = AssignedHouse.Bounds;
                int houseLeft = houseBounds.Left * StarshroudHollows.World.World.TILE_SIZE;
                int houseRight = houseBounds.Right * StarshroudHollows.World.World.TILE_SIZE;
                
                if (Position.X < houseLeft + NPC_WIDTH / 2)
                {
                    Position = new Vector2(houseLeft + NPC_WIDTH / 2, Position.Y);
                    wanderDirection = 1; // Turn around
                }
                else if (Position.X > houseRight - NPC_WIDTH / 2)
                {
                    Position = new Vector2(houseRight - NPC_WIDTH / 2, Position.Y);
                    wanderDirection = -1; // Turn around
                }
            }
            
            // Apply gravity
            velocity.Y += GRAVITY;
            if (velocity.Y > MAX_FALL_SPEED)
                velocity.Y = MAX_FALL_SPEED;
            
            // Apply physics
            ApplyPhysics(world);
        }
        
        protected void ApplyPhysics(StarshroudHollows.World.World world)
        {
            // Horizontal movement
            Vector2 newPosition = new Vector2(Position.X + velocity.X, Position.Y);
            if (!CheckCollision(newPosition, world))
            {
                Position = newPosition;
            }
            else
            {
                velocity.X = 0;
            }
            
            // Vertical movement
            newPosition = new Vector2(Position.X, Position.Y + velocity.Y);
            if (!CheckCollision(newPosition, world))
            {
                Position = newPosition;
                isOnGround = false;
            }
            else
            {
                if (velocity.Y > 0)
                {
                    // Landing on ground
                    int hitTileY = (int)((Position.Y + velocity.Y + NPC_HEIGHT) / StarshroudHollows.World.World.TILE_SIZE);
                    Position = new Vector2(Position.X, hitTileY * StarshroudHollows.World.World.TILE_SIZE - NPC_HEIGHT);
                    isOnGround = true;
                }
                velocity.Y = 0;
            }
        }
        
        protected bool CheckCollision(Vector2 position, StarshroudHollows.World.World world)
        {
            int tileSize = StarshroudHollows.World.World.TILE_SIZE;
            
            // Check corners and center bottom
            List<Vector2> checkPoints = new List<Vector2>
            {
                new Vector2(position.X - NPC_WIDTH / 2 + 2, position.Y + 2),
                new Vector2(position.X + NPC_WIDTH / 2 - 2, position.Y + 2),
                new Vector2(position.X - NPC_WIDTH / 2 + 2, position.Y + NPC_HEIGHT - 2),
                new Vector2(position.X + NPC_WIDTH / 2 - 2, position.Y + NPC_HEIGHT - 2),
                new Vector2(position.X, position.Y + NPC_HEIGHT - 2)
            };
            
            foreach (Vector2 point in checkPoints)
            {
                int tileX = (int)(point.X / tileSize);
                int tileY = (int)(point.Y / tileSize);
                if (world.IsSolidAtPosition(tileX, tileY))
                {
                    return true;
                }
            }
            return false;
        }
        
        public Rectangle GetHitbox()
        {
            return new Rectangle(
                (int)(Position.X - NPC_WIDTH / 2), 
                (int)Position.Y, 
                NPC_WIDTH, 
                NPC_HEIGHT
            );
        }
        
        public bool IsPlayerNearby(Vector2 playerPosition, float range = 64f)
        {
            Vector2 npcCenter = new Vector2(Position.X, Position.Y + NPC_HEIGHT / 2);
            return Vector2.Distance(npcCenter, playerPosition) < range;
        }
        
        public string GetCurrentDialogue()
        {
            if (dialogueLines.Count == 0)
                return "...";
            
            return dialogueLines[currentDialogueIndex];
        }
        
        public void CycleDialogue()
        {
            if (dialogueLines.Count == 0) return;
            
            currentDialogueIndex = (currentDialogueIndex + 1) % dialogueLines.Count;
        }
        
        public int GetDialogueIndex()
        {
            return currentDialogueIndex;
        }
        
        public void SetDialogueIndex(int index)
        {
            if (dialogueLines.Count > 0)
            {
                currentDialogueIndex = index % dialogueLines.Count;
            }
        }
        
        public abstract void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture);
        
        // NEW: Combat methods for NPCs that fight
        public virtual bool TryAttack(List<StarshroudHollows.Interfaces.IDamageable> nearbyEnemies, Systems.ProjectileSystem projectileSystem)
        {
            if (!CanCombat || attackCooldown > 0 || nearbyEnemies == null || nearbyEnemies.Count == 0)
                return false;
            
            // Find closest enemy in range
            Vector2 npcCenter = new Vector2(Position.X, Position.Y + NPC_HEIGHT / 2);
            StarshroudHollows.Interfaces.IDamageable closestEnemy = null;
            float closestDistance = ATTACK_RANGE;
            
            foreach (var enemy in nearbyEnemies)
            {
                if (!enemy.IsAlive) continue;
                
                Vector2 enemyPos = enemy.Position;
                float distance = Vector2.Distance(npcCenter, enemyPos);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
            
            if (closestEnemy != null)
            {
                // NPC attacks! (Override in subclass for specific attack behavior)
                OnAttack(closestEnemy, projectileSystem);
                attackCooldown = ATTACK_COOLDOWN_TIME;
                return true;
            }
            
            return false;
        }
        
        protected virtual void OnAttack(StarshroudHollows.Interfaces.IDamageable target, Systems.ProjectileSystem projectileSystem)
        {
            // Override in subclasses - default does nothing
        }
    }
    
    /// <summary>
    /// The Starling Guide - First NPC, appears after surviving first night
    /// </summary>
    public class StarlingGuide : NPC
    {
        private float cloakShimmerTimer = 0f;
        private Random random;
        
        public StarlingGuide(Vector2 spawnPosition) 
            : base("Starling Guide", "Guide", spawnPosition)
        {
            random = new Random();
            InitializeDialogue();
            CanCombat = true; // NEW: Starling Guide can defend himself with magic!
        }
        
        private void InitializeDialogue()
        {
            dialogueLines = new List<string>
            {
                "Greetings, traveler. I am the Starling Guide, oracle of the constellations.",
                "You survived your first night in Starshroud Hollows. Many do not.",
                "The darkness here is not like elsewhere. Creatures emerge when light fades.",
                "Craft better tools to delve deeper. Stone, copper, silver - each layer holds power.",
                "Build houses with walls and doors. Others like me may arrive if you create shelter.",
                "The Cave Troll lurks in the depths. Summon it with Troll Bait - 25 Pieces of Flesh.",
                "Magic flows through wands. Lightning, fire, nature - find them in silver chests.",
                "Biomes hold secrets. Explore the snow lands, seek the jungle depths.",
                "Place a Summon Altar to open portals. Face bosses in their own arenas.",
                "Armor will protect you. Craft it at benches when you gather enough ore.",
                "The stars whisper of greater threats to come. Prepare yourself, hero."
            };
        }
        
        public override void Update(float deltaTime, StarshroudHollows.World.World world)
        {
            base.Update(deltaTime, world);
            cloakShimmerTimer += deltaTime;
        }
        
        // NEW: Starling Guide shoots magic bolts at enemies!
        protected override void OnAttack(StarshroudHollows.Interfaces.IDamageable target, Systems.ProjectileSystem projectileSystem)
        {
            if (projectileSystem == null) return;
            
            Vector2 npcCenter = new Vector2(Position.X, Position.Y + NPC_HEIGHT / 2);
            Vector2 targetPos = target.Position;
            Vector2 direction = Vector2.Normalize(targetPos - npcCenter);
            
            // Shoot a magic bolt (basic wand projectile)
            projectileSystem.Launch(
                Enums.ProjectileType.MagicBolt,
                npcCenter,
                direction,
                5f   // Damage
            );
            
            Logger.Log($"[NPC] {Name} shoots a magic bolt at enemy!");
        }
        
        public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (!IsAlive) return;
            
            Rectangle npcRect = GetHitbox();
            
            // Draw constellation cloak (shimmering dark blue/purple)
            float shimmer = (float)Math.Sin(cloakShimmerTimer * 2f) * 0.2f + 0.8f;
            Color cloakColor = new Color(30, 20, 60) * shimmer;
            Rectangle cloakRect = new Rectangle(
                npcRect.X - 4, 
                npcRect.Y + 10, 
                npcRect.Width + 8, 
                npcRect.Height - 10
            );
            spriteBatch.Draw(pixelTexture, cloakRect, cloakColor);
            
            // Draw constellation stars on cloak (small yellow dots)
            for (int i = 0; i < 8; i++)
            {
                int starX = cloakRect.X + (i % 3) * 8 + 4;
                int starY = cloakRect.Y + (i / 3) * 12 + 8;
                Rectangle starRect = new Rectangle(starX, starY, 2, 2);
                spriteBatch.Draw(pixelTexture, starRect, Color.Yellow * shimmer);
            }
            
            // Draw bird-like body (small, light gray)
            Color bodyColor = new Color(200, 200, 220);
            Rectangle bodyRect = new Rectangle(
                npcRect.X + 4, 
                npcRect.Y + 8, 
                npcRect.Width - 8, 
                20
            );
            spriteBatch.Draw(pixelTexture, bodyRect, bodyColor);
            
            // Draw bird head (smaller, rounder)
            Rectangle headRect = new Rectangle(
                npcRect.X + 6, 
                npcRect.Y, 
                12, 
                12
            );
            spriteBatch.Draw(pixelTexture, headRect, bodyColor);
            
            // Draw beak (tiny orange triangle effect)
            Rectangle beakRect = new Rectangle(
                npcRect.X + 16, 
                npcRect.Y + 4, 
                4, 
                4
            );
            spriteBatch.Draw(pixelTexture, beakRect, Color.Orange);
            
            // Draw eyes (small black dots)
            Rectangle eye1 = new Rectangle(npcRect.X + 8, npcRect.Y + 3, 2, 2);
            Rectangle eye2 = new Rectangle(npcRect.X + 12, npcRect.Y + 3, 2, 2);
            spriteBatch.Draw(pixelTexture, eye1, Color.Black);
            spriteBatch.Draw(pixelTexture, eye2, Color.Black);
        }
    }
}
