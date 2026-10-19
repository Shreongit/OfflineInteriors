
using System;
using System.IO;
using GTA;
using OfflineInteriors.Core;
using OfflineInteriors.Logging;

namespace OfflineInteriors
{
    public sealed class OfflineInteriors : Script
    {
        private InteriorManager _interiorManager;
        private bool _initialized;
        private bool _shutdown;

        public OfflineInteriors()
        {
            // Input interactions need per-frame polling; the interior registry
            // throttles its heavier world scans internally.
            Interval = 0;

            Tick += OnTick;
            Aborted += OnAborted;

            ModLogger.Info("OfflineInteriors is starting.");
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (_shutdown)
                return;

            try
            {
                if (!_initialized)
                {
                    InitializeMod();
                    return;
                }

                _interiorManager?.Update();
            }
            catch (Exception ex)
            {
                ModLogger.Error(
                    "An error occurred during a game tick.",
                    ex);
            }
        }

        private void InitializeMod()
        {
            if (!StoryModeGuard.CanModifyInteriors())
                return;

            ModLogger.Info("Initializing Story Mode interior systems.");

            string scriptsDirectory =
                AppDomain.CurrentDomain.BaseDirectory;

            string cataloguePath = Path.Combine(
                scriptsDirectory,
                "OfflineInteriors",
                "data",
                "interiors.json");

            _interiorManager = new InteriorManager();

            if (_interiorManager.LoadCatalogue(cataloguePath))
            {
                ModLogger.Info(
                    "Interior catalogue loaded successfully.");
            }
            else
            {
                ModLogger.Warning(
                    "Teleport catalogue was not loaded. " +
                    "Built-in Story Mode IPL and door activation remains enabled.");
            }

            _initialized = true;

            ModLogger.Info("OfflineInteriors initialized.");
        }

        private void OnAborted(object sender, EventArgs e)
        {
            if (_shutdown)
                return;

            _shutdown = true;

            try
            {
                _interiorManager?.Shutdown();
            }
            catch (Exception ex)
            {
                ModLogger.Error(
                    "An error occurred while shutting down.",
                    ex);
            }

            ModLogger.Info("OfflineInteriors has stopped.");
        }
    }
}
