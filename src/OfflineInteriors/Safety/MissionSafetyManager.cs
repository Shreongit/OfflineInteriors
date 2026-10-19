using GTA;
using GTA.Native;
using OfflineInteriors.Core;

namespace OfflineInteriors.Safety
{
    public sealed class MissionSafetyManager
    {
        /// <summary>
        /// Returns whether ordinary interior modifications are permitted.
        /// </summary>
        public bool CanModifyWorld()
        {
            if (!StoryModeGuard.CanModifyInteriors())
                return false;

            try
            {
                // Avoid modifying the world during cutscenes.
                if (Game.IsCutsceneActive)
                    return false;

                // Do not alter IPL variants or door states while a Story Mode
                // mission is running. This prevents mission interiors from
                // being replaced by the free-roam variants in the registry.
                if (Function.Call<bool>((Hash)0xA33CDCCDA663159E))
                    return false;

                return true;
            }
            catch
            {
                // Fail closed if the game state cannot be determined.
                return false;
            }
        }

        /// <summary>
        /// Checks whether optional ambient pedestrians may be spawned.
        /// </summary>
        public bool CanSpawnAmbientPeds()
        {
            return CanModifyWorld();
        }

        /// <summary>
        /// Checks whether the mod may request interior asset loading.
        /// </summary>
        public bool CanRequestInteriorAssets()
        {
            return CanModifyWorld();
        }
    }
}