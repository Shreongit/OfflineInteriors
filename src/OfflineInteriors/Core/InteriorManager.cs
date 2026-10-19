
using System;
using System.Collections.Generic;
using System.IO;
using GTA;
using GTA.UI;
using GTA.Native;
using GTA.Math;
using OfflineInteriors.Interiors;
using OfflineInteriors.Logging;
using OfflineInteriors.Safety;

namespace OfflineInteriors.Core
{
    public sealed class InteriorManager
    {
        private readonly InteriorCatalog _catalog;
        private readonly InteriorStreamer _streamer;
        private readonly MissionSafetyManager _safetyManager;
        private readonly StoryModeInteriorActivator _storyModeActivator;

        private readonly HashSet<string> _processedInteriors =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private bool _isShutdown;
        private bool _catalogLoaded;
        private int _teleportCooldownTicks;
        private int _pendingTeleportFrames;
        private Vector3? _pendingTeleportTarget;
        private float _pendingTeleportHeading;
        private string _pendingTeleportName;
        private string[] _pendingTeleportIpls;

        public InteriorManager()
        {
            _catalog = new InteriorCatalog();
            _streamer = new InteriorStreamer();
            _safetyManager = new MissionSafetyManager();
            _storyModeActivator = new StoryModeInteriorActivator();

            ModLogger.Info("InteriorManager created with " + _storyModeActivator.Count + " built-in Story Mode locations.");
        }

        public InteriorCatalog Catalog => _catalog;

        public bool LoadCatalogue(string filePath)
        {
            if (_isShutdown || _catalogLoaded)
                return false;

            if (!StoryModeGuard.CanModifyInteriors())
                return false;

            bool loaded = _catalog.LoadFromFile(filePath);

            if (loaded)
            {
                _catalogLoaded = true;

                ModLogger.Info(
                    "Interior catalogue initialized with " +
                    _catalog.Count + " definitions.");
            }

            return loaded;
        }

        public void Update()
        {
            if (_isShutdown)
                return;

            if (!_safetyManager.CanModifyWorld())
                return;

            // Activate curated base-game Story Mode interiors as the player
            // approaches them. This registry deliberately excludes Online
            // apartment IPLs and avoids mission-sensitive variants mid-mission.
            _storyModeActivator.Update();
            HandleTeleportLinks();
        }


        private void HandleTeleportLinks()
        {
            if (_teleportCooldownTicks > 0)
                _teleportCooldownTicks--;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead)
                return;

            // IPL requests are asynchronous. Keep the player at the access point
            // until the destination interior reports ready, rather than moving
            // immediately into unloaded geometry.
            if (_pendingTeleportTarget.HasValue)
            {
                ProcessPendingTeleport(player);
                return;
            }

            if (_teleportCooldownTicks > 0 ||
                Function.Call<bool>(Hash.IS_PED_IN_ANY_VEHICLE, player.Handle, false))
                return;

            Vector3 playerPosition = player.Position;
            foreach (InteriorDefinition definition in _catalog.GetAll())
            {
                bool atEntrance = DistanceSquared(playerPosition, definition.EntrancePosition) <= 2.4f * 2.4f;
                bool atExit = DistanceSquared(playerPosition, definition.ExitPosition) <= 2.4f * 2.4f;
                if (!atEntrance && !atExit)
                    continue;

                Screen.ShowSubtitle("Press ~INPUT_CONTEXT~ to use the access point for " + definition.Name + ".", 1000);
                if (!Game.IsControlJustPressed(Control.Context))
                    return;
                    

                if (!TryLoadInterior(definition.Id))
                    return;

                _pendingTeleportTarget = atEntrance ? definition.ExitPosition : definition.EntrancePosition;
                _pendingTeleportHeading = atEntrance
                    ? (definition.EntranceHeading + 180.0f) % 360.0f
                    : definition.EntranceHeading;
                _pendingTeleportName = definition.Name;
                _pendingTeleportIpls = new string[definition.RequiredIpls.Count];
                for (int i = 0; i < definition.RequiredIpls.Count; i++)
                    _pendingTeleportIpls[i] = definition.RequiredIpls[i];
                _pendingTeleportFrames = 600;
                return;
            }
        }

        private void ProcessPendingTeleport(Ped player)
        {
            if (_pendingTeleportFrames-- <= 0)
            {
                ModLogger.Warning("Teleport cancelled because the destination interior did not become ready: " + _pendingTeleportName);
                ClearPendingTeleport();
                return;
            }

            foreach (string ipl in _pendingTeleportIpls)
            {
                if (!Function.Call<bool>(Hash.IS_IPL_ACTIVE, ipl))
                {
                    Function.Call(Hash.REQUEST_IPL, ipl);
                    Screen.ShowSubtitle("Loading " + _pendingTeleportName + "...", 1000);
                    return;
                }
            }

            Vector3 target = _pendingTeleportTarget.Value;
            int interiorId = Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, target.X, target.Y, target.Z);
            if (interiorId == 0 || !Function.Call<bool>(Hash.IS_INTERIOR_READY, interiorId))
            {
                Screen.ShowSubtitle("Loading " + _pendingTeleportName + "...", 1000);
                return;
            }

            Function.Call(Hash.REQUEST_COLLISION_AT_COORD, target.X, target.Y, target.Z);
            player.Velocity = new Vector3(0f, 0f, 0f);
            player.Position = target;
            player.Heading = _pendingTeleportHeading;
            ModLogger.Info("Teleported to access point for " + _pendingTeleportName + ".");
            ClearPendingTeleport();
            _teleportCooldownTicks = 60;
        }

        private void ClearPendingTeleport()
        {
            _pendingTeleportTarget = null;
            _pendingTeleportIpls = null;
            _pendingTeleportName = null;
            _pendingTeleportFrames = 0;
        }

        private static float DistanceSquared(Vector3 a, Vector3 b)
        {
            float x = a.X - b.X;
            float y = a.Y - b.Y;
            float z = a.Z - b.Z;
            return x * x + y * y + z * z;
        }

        public bool RegisterInterior(InteriorDefinition definition)
        {
            if (_isShutdown || definition == null)
                return false;

            return _catalog.Register(definition);
        }

        public bool TryLoadInterior(string id)
        {
            if (_isShutdown ||
                !_safetyManager.CanRequestInteriorAssets())
            {
                return false;
            }

            if (!_catalog.TryGet(id, out InteriorDefinition definition))
                return false;

            if (_processedInteriors.Contains(definition.Id))
                return true;

            var loadedThisAttempt = new List<string>();

            try
            {
                foreach (string ipl in definition.RequiredIpls)
                {
                    if (!_streamer.LoadIpl(ipl))
                    {
                        ModLogger.Warning(
                            "Could not request an IPL for interior: " +
                            definition.Name);

                        foreach (string loadedIpl in loadedThisAttempt)
                            _streamer.ForgetIpl(loadedIpl);

                        return false;
                    }

                    loadedThisAttempt.Add(ipl);
                }

                _processedInteriors.Add(definition.Id);

                ModLogger.Info(
                    "Interior asset requests submitted: " +
                    definition.Name);

                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error(
                    "Failed to process interior: " + definition.Name,
                    ex);

                return false;
            }
        }

        public void Shutdown()
        {
            if (_isShutdown)
                return;

            _isShutdown = true;
            _processedInteriors.Clear();

            ModLogger.Info("InteriorManager shut down.");
        }
    }
}
