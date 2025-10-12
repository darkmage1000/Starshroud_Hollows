using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Claude4_5Terraria.Entities;
using Claude4_5Terraria.Enums;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Claude4_5Terraria.Systems
{
    public abstract class Projectile
    {
        public Vector2 Position { get; set; }
        public bool IsAlive { get; set; }
        public float Damage { get; protected set; }
        protected Texture2D texture;
        protected float animationTimer = 0f;
        protected int currentFrame = 0;
        protected const float FRAME_TIME = 0.1f;
        public abstract void Update(float deltaTime);
        public abstract void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture);
        public abstract Rectangle GetHitbox();
        public void SetTexture(Texture2D tex) => texture = tex;
    }

    public class MagicBolt : Projectile
    {
        private const float SPEED = 600f;
        private Vector2 velocity;
        private const int WIDTH = 32, HEIGHT = 14;
        private float lifeTimer = 0f, MAX_LIFE = 2f;

        public MagicBolt(Vector2 position, Vector2 direction, float damage)
        {
            Position = position; IsAlive = true; Damage = damage; velocity = direction * SPEED;
        }
        public override void Update(float deltaTime)
        {
            if (!IsAlive) return; Position += velocity * deltaTime; lifeTimer += deltaTime; if (lifeTimer >= MAX_LIFE) IsAlive = false;
        }
        public override Rectangle GetHitbox() => new Rectangle((int)Position.X, (int)Position.Y, WIDTH, HEIGHT);
        public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (IsAlive) { if (texture != null) spriteBatch.Draw(texture, GetHitbox(), Color.White); else spriteBatch.Draw(pixelTexture, GetHitbox(), Color.MediumPurple); }
        }
    }

    public class FireBolt : Projectile
    {
        private const float SPEED = 700f;
        private Vector2 velocity;
        private const int WIDTH = 32, HEIGHT = 14;
        private float lifeTimer = 0f, MAX_LIFE = 3f;

        public FireBolt(Vector2 pos, Vector2 dir, float dmg) { Position = pos; IsAlive = true; Damage = dmg; velocity = dir * SPEED; }
        public override void Update(float dT) { if (!IsAlive) return; Position += velocity * dT; lifeTimer += dT; if (lifeTimer >= MAX_LIFE) IsAlive = false; }
        public override Rectangle GetHitbox() => new Rectangle((int)Position.X, (int)Position.Y, WIDTH, HEIGHT);
        public override void Draw(SpriteBatch sb, Texture2D pT) { if (IsAlive) { if (texture != null) sb.Draw(texture, GetHitbox(), Color.White); else sb.Draw(pT, GetHitbox(), Color.OrangeRed); } }
    }

    public class LightningBlast : Projectile
    {
        private const float SPEED = 900f;
        private Vector2 velocity;
        private const int WIDTH = 48, HEIGHT = 24;
        private float lifeTimer = 0f, MAX_LIFE = 1.5f;
        private List<Enemy> hitEnemies;

        public LightningBlast(Vector2 pos, Vector2 dir, float dmg) { Position = pos; IsAlive = true; Damage = dmg; velocity = dir * SPEED; hitEnemies = new List<Enemy>(); }
        public bool HasHitEnemy(Enemy e) => hitEnemies.Contains(e);
        public void MarkEnemyHit(Enemy e) => hitEnemies.Add(e);
        public override void Update(float dT) { if (!IsAlive) return; Position += velocity * dT; lifeTimer += dT; if (lifeTimer >= MAX_LIFE) IsAlive = false; }
        public override Rectangle GetHitbox() => new Rectangle((int)Position.X, (int)Position.Y, WIDTH, HEIGHT);
        public override void Draw(SpriteBatch sb, Texture2D pT) { if (IsAlive) { if (texture != null) sb.Draw(texture, GetHitbox(), Color.White); else sb.Draw(pT, GetHitbox(), Color.LightCyan); } }
    }

    public class NatureVine : Projectile
    {
        private const float RISE_SPEED = 150f;
        private const int WIDTH = 24, HEIGHT = 96;
        private float lifeTimer = 0f, MAX_LIFE = 2.5f;
        private float totalRise = 0f;

        public NatureVine(Vector2 pPos, Vector2 tPos, float dmg, World.World w)
        {
            IsAlive = true; Damage = dmg;
            int tX = (int)(tPos.X / World.World.TILE_SIZE), tY = (int)(tPos.Y / World.World.TILE_SIZE), gY = tY;
            for (int i = 0; i < 20; i++) { if (w.IsSolidAtPosition(tX, gY + 1)) break; gY++; }
            Position = new Vector2(tX * World.World.TILE_SIZE, (gY + 1) * World.World.TILE_SIZE);
        }
        public override void Update(float dT) { if (!IsAlive) return; if (totalRise < HEIGHT) { float rA = RISE_SPEED * dT; Position -= new Vector2(0, rA); totalRise += rA; } lifeTimer += dT; if (lifeTimer >= MAX_LIFE) IsAlive = false; }
        public override Rectangle GetHitbox() => new Rectangle((int)Position.X, (int)Position.Y, WIDTH, HEIGHT);
        public override void Draw(SpriteBatch sb, Texture2D pT) { if (IsAlive) { if (texture != null) { int vH = (int)Math.Min(totalRise, HEIGHT); Rectangle sR = new Rectangle(0, texture.Height - vH, texture.Width, vH); Rectangle dR = new Rectangle((int)Position.X, (int)Position.Y + HEIGHT - vH, WIDTH, vH); sb.Draw(texture, dR, sR, Color.White); } else sb.Draw(pT, GetHitbox(), Color.LimeGreen); } }
    }

    public class WaterBubble : Projectile
    {
        private const float SPEED = 150f;
        private Vector2 velocity;
        private const int WIDTH = 32, HEIGHT = 32;
        private float lifeTimer = 0f, MAX_LIFE = 3f;
        private const int FRAME_COUNT = 5;

        public WaterBubble(Vector2 pos, Vector2 dir, float dmg) { Position = pos; IsAlive = true; Damage = dmg; velocity = dir * SPEED; }
        public override void Update(float dT) { if (!IsAlive) return; Position += velocity * dT; lifeTimer += dT; if (lifeTimer >= MAX_LIFE) IsAlive = false; animationTimer += dT; if (animationTimer >= FRAME_TIME) { animationTimer -= FRAME_TIME; currentFrame = (currentFrame + 1) % FRAME_COUNT; } }
        public override Rectangle GetHitbox() => new Rectangle((int)Position.X, (int)Position.Y, WIDTH, HEIGHT);
        public override void Draw(SpriteBatch sb, Texture2D pT) { if (IsAlive) { if (texture != null) { int fW = texture.Width / 3, fH = texture.Height / 2; Rectangle sR = new Rectangle((currentFrame % 3) * fW, (currentFrame / 3) * fH, fW, fH); sb.Draw(texture, GetHitbox(), sR, Color.White); } else sb.Draw(pT, GetHitbox(), Color.DeepSkyBlue); } }
    }

    public class HalfMoonSlash : Projectile
    {
        private const float SPEED = 900f;
        private Vector2 velocity;
        private const int WIDTH = 32, HEIGHT = 32;
        private float lifeTimer = 0f, MAX_LIFE = 2f;
        private List<Enemy> hitEnemies;
        private const int MAX_PIERCES = 5;

        public HalfMoonSlash(Vector2 pos, Vector2 dir, float dmg) { Position = pos; IsAlive = true; Damage = dmg; velocity = dir * SPEED; hitEnemies = new List<Enemy>(); }
        public bool HasHitEnemy(Enemy e) => hitEnemies.Contains(e);
        public void MarkEnemyHit(Enemy e) { hitEnemies.Add(e); if (hitEnemies.Count >= MAX_PIERCES) IsAlive = false; }
        public override void Update(float dT) { if (!IsAlive) return; Position += velocity * dT; lifeTimer += dT; if (lifeTimer >= MAX_LIFE) IsAlive = false; }
        public override Rectangle GetHitbox() => new Rectangle((int)Position.X, (int)Position.Y, WIDTH, HEIGHT);
        public override void Draw(SpriteBatch sb, Texture2D pT) { if (IsAlive) { if (texture != null) sb.Draw(texture, GetHitbox(), Color.White); else sb.Draw(pT, GetHitbox(), Color.Silver); } }
    }

    public class RunicLaser : Projectile
    {
        private float lifeTimer = 0f, MAX_LIFE = 0.25f;
        private const float MAX_LENGTH = 1200f;
        public Vector2 StartPosition { get; private set; }
        public Vector2 EndPosition { get; private set; }
        public Vector2 Direction { get; private set; }
        public float Length { get; private set; }
        public List<Enemy> hitEnemies;

        public RunicLaser(Vector2 startPos, Vector2 dir, float dmg, World.World w)
        {
            IsAlive = true; Damage = dmg; StartPosition = startPos; Position = startPos; Direction = dir; hitEnemies = new List<Enemy>();
            Vector2 endPos = StartPosition;
            for (int i = 8; i < MAX_LENGTH; i += 8) { Vector2 cP = StartPosition + Direction * i; if (w.IsSolidAtPosition((int)cP.X / World.World.TILE_SIZE, (int)cP.Y / World.World.TILE_SIZE)) break; endPos = cP; }
            EndPosition = endPos; Length = Vector2.Distance(StartPosition, EndPosition);
        }
        public override void Update(float dT) { if (!IsAlive) return; lifeTimer += dT; if (lifeTimer >= MAX_LIFE) IsAlive = false; }
        public override Rectangle GetHitbox() => Rectangle.Empty;
        public override void Draw(SpriteBatch sb, Texture2D pT) { if (IsAlive && texture != null && Length > 0) { Rectangle sR = new Rectangle(0, 10, 114, 8); float rot = (float)Math.Atan2(Direction.Y, Direction.X); Vector2 scale = new Vector2(Length / sR.Width, 1f); sb.Draw(texture, StartPosition, sR, Color.White, rot, new Vector2(0, sR.Height / 2f), scale, SpriteEffects.None, 0f); } }
    }

    public class ProjectileSystem
    {
        private List<Projectile> activeProjectiles;
        private World.World world;
        private Dictionary<ProjectileType, Texture2D> projectileTextures;

        public ProjectileSystem(World.World world) { this.world = world; activeProjectiles = new List<Projectile>(); projectileTextures = new Dictionary<ProjectileType, Texture2D>(); }
        public void LoadTexture(ProjectileType type, Texture2D texture) => projectileTextures[type] = texture;
        public void Launch(ProjectileType type, Vector2 pos, Vector2 dir, float dmg)
        {
            Projectile p = null;
            switch (type) { case ProjectileType.MagicBolt: p = new MagicBolt(pos, dir, dmg); break; case ProjectileType.FireBolt: p = new FireBolt(pos, dir, dmg); break; case ProjectileType.LightningBlast: p = new LightningBlast(pos, dir, dmg); break; case ProjectileType.WaterBubble: p = new WaterBubble(pos, dir, dmg); break; case ProjectileType.HalfMoonSlash: p = new HalfMoonSlash(pos, dir, dmg); break; case ProjectileType.RunicLaser: p = new RunicLaser(pos, dir, dmg, world); break; }
            if (p != null) { if (projectileTextures.ContainsKey(type)) p.SetTexture(projectileTextures[type]); activeProjectiles.Add(p); }
        }
        public void LaunchAtPosition(ProjectileType type, Vector2 pPos, Vector2 tPos, float dmg)
        {
            Projectile p = null; if (type == ProjectileType.NatureVine) p = new NatureVine(pPos, tPos, dmg, world);
            if (p != null) { if (projectileTextures.ContainsKey(type)) p.SetTexture(projectileTextures[type]); activeProjectiles.Add(p); }
        }
        public void Update(float dT, List<Enemy> enemies)
        {
            foreach (var p in activeProjectiles.Where(p => p.IsAlive).ToList())
            {
                p.Update(dT); if (!p.IsAlive) continue;
                Rectangle hb = p.GetHitbox(); if (!hb.IsEmpty && world.IsSolidAtPosition(hb.Center.X / 32, hb.Center.Y / 32)) { if (!(p is NatureVine)) { p.IsAlive = false; continue; } }
                if (enemies != null)
                {
                    if (p is RunicLaser laser) { foreach (var e in enemies.Where(e => e.IsAlive && e.CanBeDamaged() && !laser.hitEnemies.Contains(e))) { for (float i = 0; i < laser.Length; i += 16) { if (e.GetHitbox().Contains(laser.StartPosition + laser.Direction * i)) { e.TakeDamage(laser.Damage); e.ResetHitCooldown(); laser.hitEnemies.Add(e); break; } } } }
                    else { foreach (var e in enemies.Where(e => e.IsAlive && e.CanBeDamaged())) { if (p.GetHitbox().Intersects(e.GetHitbox())) { bool aH = false; if (p is LightningBlast l && l.HasHitEnemy(e)) aH = true; else if (p is HalfMoonSlash h && h.HasHitEnemy(e)) aH = true; if (aH) continue; e.TakeDamage(p.Damage); e.ResetHitCooldown(); if (p is LightningBlast l2) l2.MarkEnemyHit(e); else if (p is HalfMoonSlash h2) h2.MarkEnemyHit(e); else p.IsAlive = false; if (!p.IsAlive) break; } } }
                }
            }
            activeProjectiles.RemoveAll(p => !p.IsAlive);
        }
        public void Draw(SpriteBatch sb, Texture2D pT) { foreach (var p in activeProjectiles) p.Draw(sb, pT); }
    }
}