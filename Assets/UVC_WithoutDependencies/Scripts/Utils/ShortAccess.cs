using UnityEngine;
using PG.GameBalance;

namespace PG
{
    /// <summary> 
    /// Fast access to settings.
    /// B - Balance.
    /// </summary>
    public static class B
    {
        static Settings _Settings;

        static Settings Settings
        {
            get
            {
                if (_Settings == null)
                {
                    _Settings = Resources.Load<Settings> ("Settings");
                }
                return _Settings;
            }
        }

        public static GameSettings GameSettings { get { return Settings.GameSettings; } }
        public static ResourcesSettings ResourcesSettings { get { return Settings.ResourcesSettings; } }
    }

    /// <summary> 
    /// Constants used in game
    /// C - Constants
    /// </summary>
    /// 
    public static class C
    {
        //CarParams constants
        public const float MPHMult = 2.23693629f;
        public const float KPHMult = 3.6f;
    }
}
