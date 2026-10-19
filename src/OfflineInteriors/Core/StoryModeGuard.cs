using GTA;
using GTA.Native;

namespace OfflineInteriors.Core
{
    public static class StoryModeGuard
    {
        /// <summary>
        /// Blocks modifications while a network session is active.
        /// </summary>
        public static bool IsStoryMode()
        {
            try
            {
                bool sessionStarted =
                    Function.Call<bool>(Hash.NETWORK_IS_SESSION_STARTED);

                bool inSession =
                    Function.Call<bool>(Hash.NETWORK_IS_IN_SESSION);

                return !sessionStarted && !inSession;
            }
            catch
            {
                // Fail closed if the game state cannot be checked.
                return false;
            }
        }

        public static bool CanModifyInteriors()
        {
            return IsStoryMode();
        }
    }
}