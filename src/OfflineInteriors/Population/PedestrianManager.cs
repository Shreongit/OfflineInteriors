
using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using OfflineInteriors.Core;
using OfflineInteriors.Logging;
using OfflineInteriors.Interiors;

namespace OfflineInteriors.Population
{
    public sealed class PedestrianManager
    {
        private readonly Dictionary<string, List<Ped>> _spawnedPeds =
            new Dictionary<string, List<Ped>>(
                StringComparer.OrdinalIgnoreCase);

        public bool TrySpawnPedestrian(
            InteriorDefinition interior,
            string modelName,
            Vector3 position,
            float heading)
        {
            if (interior == null ||
                !interior.AllowAmbientPeds ||
                interior.MaxAmbientPeds <= 0 ||
                string.IsNullOrWhiteSpace(modelName))
            {
                return false;
            }

            if (!StoryModeGuard.CanModifyInteriors())
                return false;

            Model model = default(Model);

            try
            {
                if (!_spawnedPeds.TryGetValue(
                        interior.Id,
                        out List<Ped> peds))
                {
                    peds = new List<Ped>();
                    _spawnedPeds.Add(interior.Id, peds);
                }

                peds.RemoveAll(
                    ped => ped == null || !ped.Exists());

                if (peds.Count >= interior.MaxAmbientPeds)
                    return false;

                model = new Model(modelName);

                if (!model.IsInCdImage || !model.IsValid)
                    return false;

                model.Request(1000);

                if (!model.IsLoaded)
                    return false;

                Ped createdPed = Ped.Create(model, position, heading);

                if (createdPed == null || !createdPed.Exists())
                    return false;

                peds.Add(createdPed);

                ModLogger.Info(
                    "Spawned ambient pedestrian for " + interior.Name);

                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error(
                    "Failed to spawn an ambient pedestrian.",
                    ex);

                return false;
            }
            finally
            {
                if (model != null)
                    model.MarkAsNoLongerNeeded();
            }
        }

        public void Cleanup()
        {
            foreach (List<Ped> peds in _spawnedPeds.Values)
            {
                foreach (Ped ped in peds)
                {
                    try
                    {
                        if (ped != null && ped.Exists())
                            ped.Delete();
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Warning(
                            "Could not remove a managed pedestrian: " +
                            ex.Message);
                    }
                }
            }

            _spawnedPeds.Clear();
        }
    }
}
