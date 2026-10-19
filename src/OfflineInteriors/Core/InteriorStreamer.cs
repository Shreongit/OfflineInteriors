
using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace OfflineInteriors.Core
{
    public sealed class InteriorStreamer
    {
        private readonly HashSet<string> _loadedIpls =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Requests loading an IPL if it has not already been requested.
        /// Only call this with IPLs verified for Story Mode.
        /// </summary>
        public bool LoadIpl(string iplName)
        {
            if (string.IsNullOrWhiteSpace(iplName))
                return false;

            if (!StoryModeGuard.CanModifyInteriors())
                return false;

            if (!IsValidIplName(iplName))
                return false;

            if (_loadedIpls.Contains(iplName))
                return true;

            try
            {
                if (Function.Call<bool>(Hash.IS_IPL_ACTIVE, iplName))
                {
                    _loadedIpls.Add(iplName);
                    return true;
                }

                Function.Call(Hash.REQUEST_IPL, iplName);
                _loadedIpls.Add(iplName);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Stops tracking an IPL without forcibly unloading shared assets.
        /// </summary>
        public void ForgetIpl(string iplName)
        {
            if (!string.IsNullOrWhiteSpace(iplName))
                _loadedIpls.Remove(iplName);
        }

        public IReadOnlyCollection<string> GetTrackedIpls()
        {
            return _loadedIpls;
        }

        private static bool IsValidIplName(string iplName)
        {
            if (iplName.Length > 128)
                return false;

            foreach (char character in iplName)
            {
                if (!(char.IsLetterOrDigit(character) ||
                      character == '_' ||
                      character == '-'))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
