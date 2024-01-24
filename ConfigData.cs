using System;

namespace TheLongShockProper
{
    [Serializable]
    public class ConfigData
    {
        public string username;
        public string apiKey;
        public string shockerCode;
        public int baseShock;
        public string highValueParts;
        public int highValuePartBonusShock;
        public int bloodBonusShock;
        public int maxShockLimiter;
        public int deathShockOverride;
        public int shockLockoutTimeSeconds;
        public bool testMode;
    }
}