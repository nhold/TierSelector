using BepInEx.Configuration;

namespace Toasted
{
    public static class Config
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

        public static ConfigEntry<ItemType> selectedTier;

        public static bool IsInitialised = false;

        public static void Initialise(TierSelector selector)
        {
            if (IsInitialised)
                return;

            IsInitialised = true;

            selectedTier = selector.Config.Bind("ItemSelector", "Selected Tier", ItemType.White, "Which tier you want (White = 0, Green = 1, Red = 2, Boss = 3, Lunar = 4, Void = 5.");
        }
    }
}
