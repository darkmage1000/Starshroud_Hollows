using Microsoft.Xna.Framework;

namespace StarshroudHollows.Interfaces
{
    public interface IDamageable
    {
        Vector2 Position { get; }
        bool IsAlive { get; }
        Rectangle GetHitbox();
        void TakeDamage(float damage);
        bool CanBeDamaged();
        void ResetHitCooldown();
    }
}