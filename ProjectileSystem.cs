using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Claude4_5Terraria.Entities;
using Claude4_5Terraria.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Claude4_5Terraria.Systems
{
    // NEW: Enum definition for projectile types 
    public enum ProjectileType
    {
        MagicBolt,
        FireBolt,
        LightningBlast,
        NatureVine,
        WaterBubble,
        HalfMoonSlash,
        RunicLaser
    }

    public abstract class Projectile
    {
        public Vector2 Position { get; set; }
        public bool IsAlive { get; set; }
        public float Damage { get; protected set; }
        protected Texture2D texture; // Sprite texture

        // Abstract methods must be implemented by derived classes
        public abstract void Update(float deltaTime);
        public abstract void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture);
        public abstract Rectangle GetHitbox();
        
        // Set texture (called by ProjectileSystem)
        public void SetTexture(Texture2D tex)
        {
            texture = tex;
        }
    }

    // NEW: Magic Bolt Projectile
    public class MagicBolt : Projectile
    {
        private const float SPEED = 600f; // Very fast projectile, e.g., 600 pixels/sec
        private Vector2 velocity;
        private const int WIDTH = 8;
        private const int HEIGHT = 8;
        private float lifeTimer = 0f;
        private const float MAX_LIFE = 2f; // Despawns after 2 seconds

        // MODIFIED: Constructor now takes Vector2 direction
        public MagicBolt(Vector2 position, Vector2 direction, float damage)
        {
            Position = position;
            IsAlive = true;
            Damage = damage;
            // Velocity is calculated from direction * speed
            velocity = direction * SPEED;
        }

        public override void Update(float deltaTime)
        {
            if (!IsAlive) return;

            // Move the projectile
            Position += velocity * deltaTime;

            // Check max life duration
            lifeTimer += deltaTime;
            if (lifeTimer >= MAX_LIFE)
            {
                IsAlive = false;
            }
        }

        public override Rectangle GetHitbox()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, WIDTH, HEIGHT);
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (IsAlive)
            {
                Rectangle destRect = GetHitbox();
                if (texture != null)
                {
                    spriteBatch.Draw(texture, destRect, Color.White);
                }
                else
                {
                    // Fallback to purple square
                    spriteBatch.Draw(pixelTexture, destRect, Color.MediumPurple);
                }
            }
        }
    }

    // FireBolt: Fast, moderate damage, explodes on hit
    public class FireBolt : Projectile
    {
        private const float SPEED = 700f;
        private Vector2 velocity;
        private const int WIDTH = 12;
        private const int HEIGHT = 12;
        private float lifeTimer = 0f;
        private const float MAX_LIFE = 3f;

        public FireBolt(Vector2 position, Vector2 direction, float damage)
        {
            Position = position;
            IsAlive = true;
            Damage = damage;
            velocity = direction * SPEED;
        }

        public override void Update(float deltaTime)
        {
            if (!IsAlive) return;
            
            Position += velocity * deltaTime;
            
            lifeTimer += deltaTime;
            if (lifeTimer >= MAX_LIFE) IsAlive = false;
        }

        public override Rectangle GetHitbox()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, WIDTH, HEIGHT);
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (IsAlive)
            {
                Rectangle destRect = GetHitbox();
                if (texture != null)
                {
                    spriteBatch.Draw(texture, destRect, Color.White);
                }
                else
                {
                    spriteBatch.Draw(pixelTexture, GetHitbox(), Color.OrangeRed);
                }
            }
        }
    }

    // LightningBlast: Very fast, piercing, chaining
    public class LightningBlast : Projectile
    {
        private const float SPEED = 900f;
        private Vector2 velocity;
        private const int WIDTH = 8;
        private const int HEIGHT = 24;
        private float lifeTimer = 0f;
        private const float MAX_LIFE = 1.5f;
        public bool IsPiercing { get; private set; } // Can hit multiple enemies
        private List<Enemy> hitEnemies; // Track which enemies were already hit

        public LightningBlast(Vector2 position, Vector2 direction, float damage)
        {
            Position = position;
            IsAlive = true;
            Damage = damage;
            velocity = direction * SPEED;
            IsPiercing = true;
            hitEnemies = new List<Enemy>();
        }

        public bool HasHitEnemy(Enemy enemy)
        {
            return hitEnemies.Contains(enemy);
        }

        public void MarkEnemyHit(Enemy enemy)
        {
            hitEnemies.Add(enemy);
        }

        public override void Update(float deltaTime)
        {
            if (!IsAlive) return;
            Position += velocity * deltaTime;
            lifeTimer += deltaTime;
            if (lifeTimer >= MAX_LIFE) IsAlive = false;
        }

        public override Rectangle GetHitbox()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, WIDTH, HEIGHT);
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (IsAlive)
            {
                Rectangle destRect = GetHitbox();
                if (texture != null)
                {
                    spriteBatch.Draw(texture, destRect, Color.White);
                }
                else
                {
                    spriteBatch.Draw(pixelTexture, GetHitbox(), Color.LightCyan);
                }
            }
        }
    }

    // NatureVine: Rises from ground at target position
    public class NatureVine : Projectile
    {
        private const float RISE_SPEED = 400f; // Rises upward
        private Vector2 velocity;
        private const int WIDTH = 10;
        private const int HEIGHT = 10;
        private float lifeTimer = 0f;
        private const float MAX_LIFE = 2f; // Shorter life since it rises from ground
        private Vector2 targetGroundPosition; // Where the mouse clicked
        private bool hasReachedGround = false;
        private float riseDistance = 0f;
        private const float MAX_RISE_DISTANCE = 96f; // Rise 3 tiles (32px * 3)

        public NatureVine(Vector2 startPosition, Vector2 mouseWorldPosition, float damage)
        {
            // Start position is where spell is cast from
            // But we want it to appear at ground level below the mouse cursor
            
            // Find ground position below mouse cursor
            int mouseX = (int)(mouseWorldPosition.X / 32); // Tile X
            int startY = (int)(mouseWorldPosition.Y / 32); // Start searching from mouse Y
            
            // Search downward for solid ground (max 10 tiles)
            int groundY = startY;
            // We'll set actual ground finding in Update since we need world reference
            
            targetGroundPosition = new Vector2(mouseWorldPosition.X, mouseWorldPosition.Y);
            
            // Position starts at ground level
            Position = new Vector2(mouseWorldPosition.X - WIDTH / 2, mouseWorldPosition.Y);
            
            IsAlive = true;
            Damage = damage;
            
            // Velocity is upward
            velocity = new Vector2(0, -RISE_SPEED);
        }

        public override void Update(float deltaTime)
        {
            if (!IsAlive) return;
            
            // Rise upward
            Position += velocity * deltaTime;
            riseDistance += System.Math.Abs(velocity.Y * deltaTime);
            
            // Check if reached max rise distance
            if (riseDistance >= MAX_RISE_DISTANCE)
            {
                IsAlive = false;
            }
            
            // Also check max lifetime
            lifeTimer += deltaTime;
            if (lifeTimer >= MAX_LIFE) IsAlive = false;
        }

        public override Rectangle GetHitbox()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, WIDTH, HEIGHT);
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (IsAlive)
            {
                Rectangle destRect = GetHitbox();
                if (texture != null)
                {
                    spriteBatch.Draw(texture, destRect, Color.White);
                }
                else
                {
                    spriteBatch.Draw(pixelTexture, GetHitbox(), Color.LimeGreen);
                }
            }
        }
    }

    // WaterBubble: Medium speed, bounces, splash damage
    public class WaterBubble : Projectile
    {
        private const float SPEED = 400f;
        private Vector2 velocity;
        private const int WIDTH = 14;
        private const int HEIGHT = 14;
        private float lifeTimer = 0f;
        private const float MAX_LIFE = 3f;

        public WaterBubble(Vector2 position, Vector2 direction, float damage)
        {
            Position = position;
            IsAlive = true;
            Damage = damage;
            velocity = direction * SPEED;
        }

        public override void Update(float deltaTime)
        {
            if (!IsAlive) return;
            Position += velocity * deltaTime;
            lifeTimer += deltaTime;
            if (lifeTimer >= MAX_LIFE) IsAlive = false;
        }

        public override Rectangle GetHitbox()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, WIDTH, HEIGHT);
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (IsAlive)
            {
                Rectangle destRect = GetHitbox();
                if (texture != null)
                {
                    spriteBatch.Draw(texture, destRect, Color.White);
                }
                else
                {
                    spriteBatch.Draw(pixelTexture, GetHitbox(), Color.DeepSkyBlue);
                }
            }
        }
    }

    // HalfMoonSlash: Arc projectile, wide hitbox, piercing
    public class HalfMoonSlash : Projectile
    {
        private const float SPEED = 500f;
        private Vector2 velocity;
        private const int WIDTH = 20;
        private const int HEIGHT = 20;
        private float lifeTimer = 0f;
        private const float MAX_LIFE = 2f;
        public bool IsPiercing { get; private set; }
        private List<Enemy> hitEnemies;

        public HalfMoonSlash(Vector2 position, Vector2 direction, float damage)
        {
            Position = position;
            IsAlive = true;
            Damage = damage;
            velocity = direction * SPEED;
            IsPiercing = true;
            hitEnemies = new List<Enemy>();
        }

        public bool HasHitEnemy(Enemy enemy)
        {
            return hitEnemies.Contains(enemy);
        }

        public void MarkEnemyHit(Enemy enemy)
        {
            hitEnemies.Add(enemy);
        }

        public override void Update(float deltaTime)
        {
            if (!IsAlive) return;
            Position += velocity * deltaTime;
            lifeTimer += deltaTime;
            if (lifeTimer >= MAX_LIFE) IsAlive = false;
        }

        public override Rectangle GetHitbox()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, WIDTH, HEIGHT);
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (IsAlive)
            {
                Rectangle destRect = GetHitbox();
                if (texture != null)
                {
                    spriteBatch.Draw(texture, destRect, Color.White);
                }
                else
                {
                    spriteBatch.Draw(pixelTexture, GetHitbox(), Color.Silver);
                }
            }
        }
    }

    // RunicLaser: Extremely fast beam, high damage, penetrating (continuous damage)
    public class RunicLaser : Projectile
    {
        private const float SPEED = 1200f;
        private Vector2 velocity;
        private const int WIDTH = 6;
        private const int HEIGHT = 32;
        private float lifeTimer = 0f;
        private const float MAX_LIFE = 1f;
        public bool IsPiercing { get; private set; }
        private List<Enemy> hitEnemies;

        public RunicLaser(Vector2 position, Vector2 direction, float damage)
        {
            Position = position;
            IsAlive = true;
            Damage = damage;
            velocity = direction * SPEED;
            IsPiercing = true;
            hitEnemies = new List<Enemy>();
        }

        public bool HasHitEnemy(Enemy enemy)
        {
            return hitEnemies.Contains(enemy);
        }

        public void MarkEnemyHit(Enemy enemy)
        {
            hitEnemies.Add(enemy);
        }

        public override void Update(float deltaTime)
        {
            if (!IsAlive) return;
            Position += velocity * deltaTime;
            lifeTimer += deltaTime;
            if (lifeTimer >= MAX_LIFE) IsAlive = false;
        }

        public override Rectangle GetHitbox()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, WIDTH, HEIGHT);
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (IsAlive)
            {
                Rectangle destRect = GetHitbox();
                if (texture != null)
                {
                    spriteBatch.Draw(texture, destRect, Color.White);
                }
                else
                {
                    spriteBatch.Draw(pixelTexture, GetHitbox(), Color.MediumPurple);
                }
            }
        }
    }

    public class ProjectileSystem
    {
        private List<Projectile> activeProjectiles;
        private World.World world;
        private Dictionary<ProjectileType, Texture2D> projectileTextures;

        public ProjectileSystem(World.World world)
        {
            this.world = world;
            activeProjectiles = new List<Projectile>();
            projectileTextures = new Dictionary<ProjectileType, Texture2D>();
        }

        // Load projectile textures
        public void LoadTexture(ProjectileType type, Texture2D texture)
        {
            projectileTextures[type] = texture;
            Logger.Log($"[PROJECTILE] Loaded texture for {type}");
        }

        // MODIFIED: Launch now takes Vector2 direction instead of bool facingRight
        public void Launch(ProjectileType type, Vector2 position, Vector2 direction, float damage)
        {
            Projectile newProjectile = null;
            
            switch (type)
            {
                case ProjectileType.MagicBolt:
                    newProjectile = new MagicBolt(position, direction, damage);
                    break;
                case ProjectileType.FireBolt:
                    newProjectile = new FireBolt(position, direction, damage);
                    break;
                case ProjectileType.LightningBlast:
                    newProjectile = new LightningBlast(position, direction, damage);
                    break;
                case ProjectileType.NatureVine:
                    // This shouldn't be called for NatureVine - use LaunchAtPosition instead
                    newProjectile = new NatureVine(position, position, damage);
                    break;
                case ProjectileType.WaterBubble:
                    newProjectile = new WaterBubble(position, direction, damage);
                    break;
                case ProjectileType.HalfMoonSlash:
                    newProjectile = new HalfMoonSlash(position, direction, damage);
                    break;
                case ProjectileType.RunicLaser:
                    newProjectile = new RunicLaser(position, direction, damage);
                    break;
            }
            
            if (newProjectile != null)
            {
                // Set texture if available
                if (projectileTextures.ContainsKey(type))
                {
                    newProjectile.SetTexture(projectileTextures[type]);
                }
                
                activeProjectiles.Add(newProjectile);
                Systems.Logger.Log($"[PROJECTILE] Launched {type} at {position} with damage {damage}");
            }
        }

        // NEW: Special launch method for position-based spells (like Nature Vine)
        public void LaunchAtPosition(ProjectileType type, Vector2 playerPosition, Vector2 targetWorldPosition, float damage)
        {
            Projectile newProjectile = null;
            
            if (type == ProjectileType.NatureVine)
            {
                newProjectile = new NatureVine(playerPosition, targetWorldPosition, damage);
            }
            
            if (newProjectile != null)
            {
                // Set texture if available
                if (projectileTextures.ContainsKey(type))
                {
                    newProjectile.SetTexture(projectileTextures[type]);
                }
                
                activeProjectiles.Add(newProjectile);
                Systems.Logger.Log($"[PROJECTILE] Launched {type} at target position {targetWorldPosition} with damage {damage}");
            }
        }

        public void Update(float deltaTime, List<Enemy> activeEnemies)
        {
            // Update positions and check enemy collisions
            foreach (var projectile in activeProjectiles.Where(p => p.IsAlive).ToList())
            {
                projectile.Update(deltaTime);

                // Check collision against enemies
                if (activeEnemies != null)
                {
                    foreach (var enemy in activeEnemies.Where(e => e.IsAlive))
                    {
                        if (projectile.GetHitbox().Intersects(enemy.GetHitbox()))
                        {
                            // Check if this is a piercing projectile
                            bool isPiercing = false;
                            bool alreadyHit = false;
                            
                            if (projectile is LightningBlast lightning)
                            {
                                isPiercing = true;
                                alreadyHit = lightning.HasHitEnemy(enemy);
                            }
                            else if (projectile is HalfMoonSlash halfMoon)
                            {
                                isPiercing = true;
                                alreadyHit = halfMoon.HasHitEnemy(enemy);
                            }
                            else if (projectile is RunicLaser laser)
                            {
                                isPiercing = true;
                                alreadyHit = laser.HasHitEnemy(enemy);
                            }
                            
                            // Skip if already hit this enemy
                            if (alreadyHit) continue;
                            
                            // Check if enemy can be damaged
                            if (!enemy.CanBeDamaged())
                            {
                                continue;
                            }

                            // Apply damage
                            enemy.TakeDamage(projectile.Damage);
                            enemy.ResetHitCooldown();
                            
                            // Mark enemy as hit for piercing projectiles
                            if (isPiercing)
                            {
                                if (projectile is LightningBlast lightning2)
                                    lightning2.MarkEnemyHit(enemy);
                                else if (projectile is HalfMoonSlash halfMoon2)
                                    halfMoon2.MarkEnemyHit(enemy);
                                else if (projectile is RunicLaser laser2)
                                    laser2.MarkEnemyHit(enemy);
                            }
                            else
                            {
                                // Non-piercing projectile dies on hit
                                projectile.IsAlive = false;
                            }
                            
                            break;
                        }
                    }
                }
            }

            // Remove inactive projectiles
            activeProjectiles.RemoveAll(p => !p.IsAlive);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            foreach (var projectile in activeProjectiles.Where(p => p.IsAlive))
            {
                projectile.Draw(spriteBatch, pixelTexture);
            }
        }
    }
}