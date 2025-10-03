using System;

namespace Claude4_5Terraria.Systems
{
    public class TimeSystem
    {
        private float timeOfDay;  // 0.0 to 1.0 (0 = midnight, 0.5 = noon, 1.0 = midnight)
        private const float DAY_LENGTH_SECONDS = 600f;  // 10 minutes total (5 day + 5 night)

        public TimeSystem()
        {
            timeOfDay = 0.25f;  // Start at dawn (6am)
        }

        public void Update(float deltaTime)
        {
            // Progress time
            timeOfDay += deltaTime / DAY_LENGTH_SECONDS;

            // Wrap around after full day
            if (timeOfDay >= 1.0f)
            {
                timeOfDay -= 1.0f;
            }
        }

        // Get current time as 0.0 to 1.0
        public float GetTimeOfDay()
        {
            return timeOfDay;
        }

        // Check if it's daytime
        public bool IsDaytime()
        {
            // Day is from 0.25 (6am) to 0.75 (6pm)
            return timeOfDay >= 0.25f && timeOfDay < 0.75f;
        }

        // Get ambient light level (0.0 = pitch black, 1.0 = full daylight)
        public float GetAmbientLight()
        {
            // Daytime (6am to 6pm) - 0.25 to 0.75
            if (timeOfDay >= 0.25f && timeOfDay < 0.75f)
            {
                // Full daylight
                return 1.0f;
            }
            // Dawn transition (5am to 6am) - 0.208 to 0.25
            else if (timeOfDay >= 0.208f && timeOfDay < 0.25f)
            {
                // Fade from night to day
                float t = (timeOfDay - 0.208f) / 0.042f;
                return 0.3f + (t * 0.7f);  // 0.3 to 1.0
            }
            // Dusk transition (6pm to 7pm) - 0.75 to 0.792
            else if (timeOfDay >= 0.75f && timeOfDay < 0.792f)
            {
                // Fade from day to night
                float t = (timeOfDay - 0.75f) / 0.042f;
                return 1.0f - (t * 0.7f);  // 1.0 to 0.3
            }
            // Nighttime (7pm to 5am)
            else
            {
                // Dark but visible on surface
                return 0.3f;  // 30% light (can see but darker)
            }
        }

        // Get sky color based on time
        public Microsoft.Xna.Framework.Color GetSkyColor()
        {
            float light = GetAmbientLight();

            // Base sky colors
            var daySky = new Microsoft.Xna.Framework.Color(135, 206, 235);      // Bright blue
            var duskSky = new Microsoft.Xna.Framework.Color(255, 140, 60);      // Orange
            var nightSky = new Microsoft.Xna.Framework.Color(15, 15, 40);       // Dark blue

            // Dawn (5am-6am)
            if (timeOfDay >= 0.208f && timeOfDay < 0.25f)
            {
                float t = (timeOfDay - 0.208f) / 0.042f;
                return Microsoft.Xna.Framework.Color.Lerp(nightSky, daySky, t);
            }
            // Day (6am-5pm)
            else if (timeOfDay >= 0.25f && timeOfDay < 0.708f)
            {
                return daySky;
            }
            // Sunset (5pm-7pm)
            else if (timeOfDay >= 0.708f && timeOfDay < 0.792f)
            {
                float sunsetProgress = (timeOfDay - 0.708f) / 0.084f;

                if (sunsetProgress < 0.5f)
                {
                    // Day to dusk
                    return Microsoft.Xna.Framework.Color.Lerp(daySky, duskSky, sunsetProgress * 2f);
                }
                else
                {
                    // Dusk to night
                    return Microsoft.Xna.Framework.Color.Lerp(duskSky, nightSky, (sunsetProgress - 0.5f) * 2f);
                }
            }
            // Night (7pm-5am)
            else
            {
                return nightSky;
            }
        }

        // Get readable time string for debug
        public string GetTimeString()
        {
            int hours = (int)(timeOfDay * 24);
            int minutes = (int)((timeOfDay * 24 - hours) * 60);
            string period = hours >= 12 ? "PM" : "AM";
            int displayHours = hours > 12 ? hours - 12 : (hours == 0 ? 12 : hours);

            return $"{displayHours}:{minutes:D2} {period}";
        }
    }
}