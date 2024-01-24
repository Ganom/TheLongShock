using System;
using UnityEngine.Serialization;

namespace TheLongShockProper
{
    [Serializable]
    public class ShockPostData
    {
        public string username;
        public string name;
        public string code;
        public string intensity;
        public string duration;
        public string apiKey;
        public string op;
    }
}