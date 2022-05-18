using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;

namespace TierSelector
{
    internal static class Config
    {
        public enum ItemType
        {
            White,
            Green,
            Red,
            Boss,
            Lunar,
            Void
        }

        internal static ConfigEntry<ItemType> selectedTier;

        public static void Initialise()
        {
        }
    }
}
