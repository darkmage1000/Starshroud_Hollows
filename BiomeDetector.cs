using Microsoft.Xna.Framework;
using StarshroudHollows.World;

namespace StarshroudHollows.Systems
{
    public class BiomeDetector
    {
        private World.World world;
        private string currentBiome;
        private string previousBiome;
        
        public string CurrentBiomeName => currentBiome;
        public bool BiomeChanged { get; private set; }
        
        private float notificationTimer = 0f;
        private const float NOTIFICATION_DURATION = 3f;
        
        private int snowBiomeStartX;
        private int snowBiomeEndX;
        private int swampBiomeStartX;
        private int swampBiomeEndX;
        private int jungleBiomeStartX;
        private int jungleBiomeEndX;
        private int volcanicBiomeStartX;
        private int volcanicBiomeEndX;
        
        public BiomeDetector(World.World world, WorldGenerator worldGen)
        {
            this.world = world;
            currentBiome = "Forest";
            previousBiome = "Forest";
            BiomeChanged = false;
            
            // Get biome bounds from WorldGenerator
            snowBiomeStartX = worldGen.GetSnowBiomeStartX();
            snowBiomeEndX = worldGen.GetSnowBiomeEndX();
            swampBiomeStartX = worldGen.GetSwampBiomeStartX();
            swampBiomeEndX = worldGen.GetSwampBiomeEndX();
            jungleBiomeStartX = worldGen.GetJungleBiomeStartX();
            jungleBiomeEndX = worldGen.GetJungleBiomeEndX();
            volcanicBiomeStartX = worldGen.GetVolcanicBiomeStartX();
            volcanicBiomeEndX = worldGen.GetVolcanicBiomeEndX();
        }
        
        public void SetBiomeBounds(int swampStart, int swampEnd, int jungleStart, int jungleEnd, int volcanicStart, int volcanicEnd)
        {
            swampBiomeStartX = swampStart;
            swampBiomeEndX = swampEnd;
            jungleBiomeStartX = jungleStart;
            jungleBiomeEndX = jungleEnd;
            volcanicBiomeStartX = volcanicStart;
            volcanicBiomeEndX = volcanicEnd;
        }
        
        public void Update(float deltaTime, Vector2 playerPosition)
        {
            // Convert player pixel position to tile position
            int playerTileX = (int)(playerPosition.X / World.World.TILE_SIZE);
            
            // Determine which biome the player is in
            string newBiome = DetermineBiome(playerTileX);
            
            // Check if biome changed
            if (newBiome != currentBiome)
            {
                previousBiome = currentBiome;
                currentBiome = newBiome;
                BiomeChanged = true;
                notificationTimer = NOTIFICATION_DURATION;
                Logger.Log($"[BIOME] Entered {currentBiome} biome!");
            }
            else
            {
                BiomeChanged = false;
            }
            
            // Update notification timer
            if (notificationTimer > 0)
            {
                notificationTimer -= deltaTime;
            }
        }
        
        public bool ShouldShowNotification()
        {
            return notificationTimer > 0;
        }
        
        public float GetNotificationAlpha()
        {
            // Fade in first 0.5s, stay visible 2s, fade out last 0.5s
            if (notificationTimer > 2.5f)
            {
                // Fade in (3.0 -> 2.5)
                return (NOTIFICATION_DURATION - notificationTimer) / 0.5f;
            }
            else if (notificationTimer > 0.5f)
            {
                // Fully visible (2.5 -> 0.5)
                return 1f;
            }
            else
            {
                // Fade out (0.5 -> 0)
                return notificationTimer / 0.5f;
            }
        }
        
        private string DetermineBiome(int playerTileX)
        {
            // Check biomes in order (specific biomes first, forest as fallback)
            
            // Snow biome
            if (playerTileX >= snowBiomeStartX && playerTileX <= snowBiomeEndX)
            {
                return "Snow";
            }
            
            // Swamp biome
            if (playerTileX >= swampBiomeStartX && playerTileX <= swampBiomeEndX)
            {
                return "Swamp";
            }
            
            // Jungle biome
            if (playerTileX >= jungleBiomeStartX && playerTileX <= jungleBiomeEndX)
            {
                return "Jungle";
            }
            
            // Volcanic biome
            if (playerTileX >= volcanicBiomeStartX && playerTileX <= volcanicBiomeEndX)
            {
                return "Volcanic";
            }
            
            // Default: Forest biome
            return "Forest";
        }
    }
}
