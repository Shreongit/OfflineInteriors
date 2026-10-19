
using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using OfflineInteriors.Core;
using OfflineInteriors.Logging;

namespace OfflineInteriors.Doors
{
    public sealed class DoorManager
    {
        private readonly Dictionary<uint, Vector3> _registeredDoors =
            new Dictionary<uint, Vector3>();

        /// <summary>
        /// Registers a known Story Mode door for future management.
        /// </summary>
        public bool RegisterDoor(uint doorHash, Vector3 position)
        {
            if (!StoryModeGuard.CanModifyInteriors())
                return false;

            if (doorHash == 0 || _registeredDoors.ContainsKey(doorHash))
                return false;

            _registeredDoors.Add(doorHash, position);
            return true;
        }

        /// <summary>
        /// Requests that a registered door be unlocked.
        /// </summary>
        public bool UnlockDoor(uint doorHash)
        {
            if (!StoryModeGuard.CanModifyInteriors())
                return false;

            if (!_registeredDoors.TryGetValue(
                    doorHash,
                    out Vector3 position))
            {
                return false;
            }

            try
            {
                Function.Call(
                    Hash.DOOR_SYSTEM_SET_DOOR_STATE,
                    doorHash,
                    0,
                    false,
                    false);

                ModLogger.Info(
                    string.Format(
                        "Requested unlock for door 0x{0:X8} at {1}.",
                        doorHash,
                        position));

                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error(
                    "Failed to unlock registered door.",
                    ex);

                return false;
            }
        }

        /// <summary>
        /// Removes a door from the registry without changing game state.
        /// </summary>
        public bool UnregisterDoor(uint doorHash)
        {
            return _registeredDoors.Remove(doorHash);
        }

        public void Clear()
        {
            _registeredDoors.Clear();
        }
    }
}
