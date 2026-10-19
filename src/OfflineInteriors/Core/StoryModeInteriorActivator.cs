using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using OfflineInteriors.Logging;

namespace OfflineInteriors.Core
{
    /// <summary>
    /// Activates a curated set of base-game Story Mode interiors only when the
    /// player is nearby. Online apartments and multiplayer map IPLs are not
    /// present in this registry. IPL requests are intentionally not undone:
    /// GTA interiors often share assets and unloading them can break another
    /// location or a mission that has already requested the same asset.
    /// </summary>
    public sealed class StoryModeInteriorActivator
    {
        private const float ActivationRadiusSquared = 300.0f * 300.0f;
        private const float DoorControlRadiusSquared = 85.0f * 85.0f;
        private readonly HashSet<string> _activated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<InteriorEntry> _entries = BuildEntries();
        private int _frameCounter;

        public int Count => _entries.Count;

        public void Update()
        {
            // The parent Script polls every frame for E-key teleport input. IPL
            // and door maintenance only needs to run about twice per second.
            if (++_frameCounter < 30) return;
            _frameCounter = 0;

            if (!StoryModeGuard.CanModifyInteriors()) return;
            if (Game.IsCutsceneActive) return;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return;

            Vector3 position = player.Position;
            foreach (InteriorEntry entry in _entries)
            {
                if (DistanceSquared(position, entry.Center) > ActivationRadiusSquared)
                    continue;

                try
                {
                    Activate(entry, position);
                }
                catch (Exception ex)
                {
                    ModLogger.Warning("Interior activation failed for " + entry.Name + ": " + ex.Message);
                }
            }
        }

        private void Activate(InteriorEntry entry, Vector3 playerPosition)
        {
            // A few mission interiors have mutually exclusive IPL variants. Do not
            // replace those variants while the game reports an active mission.
            if (IsMissionActive() && entry.MissionSensitive)
                return;
            if (entry.BlockedByActiveIpl != null &&
                Function.Call<bool>(Hash.IS_IPL_ACTIVE, entry.BlockedByActiveIpl))
                return;

            bool firstActivation = !_activated.Contains(entry.Id);
            if (firstActivation)
            {
                foreach (string removeIpl in entry.RemoveIpls)
                    Function.Call(Hash.REMOVE_IPL, removeIpl);
            }

            foreach (string ipl in entry.Ipls)
            {
                if (!Function.Call<bool>(Hash.IS_IPL_ACTIVE, ipl))
                    Function.Call(Hash.REQUEST_IPL, ipl);
            }

            int interiorId = Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS,
                entry.InteriorProbe.X, entry.InteriorProbe.Y, entry.InteriorProbe.Z);

            if (interiorId != 0 && Function.Call<bool>(Hash.IS_VALID_INTERIOR, interiorId))
            {
                Function.Call(Hash.CAP_INTERIOR, interiorId, false);
                Function.Call(Hash.DISABLE_INTERIOR, interiorId, false);
                Function.Call(Hash.UNPIN_INTERIOR, interiorId);
                Function.Call(Hash.PIN_INTERIOR_IN_MEMORY, interiorId);
            }

            foreach (DoorEntry door in entry.Doors)
            {
                Vector3 doorReference = door.SystemDoor ? entry.Center : door.Position;
                float doorRangeSquared = door.SystemDoor ? DoorControlRadiusSquared : 28.0f * 28.0f;
                if (DistanceSquared(playerPosition, doorReference) > doorRangeSquared)
                    continue;

                if (door.SystemDoor)
                {
                    // Door-system state 0 is unlocked.
                    Function.Call(Hash.DOOR_SYSTEM_SET_DOOR_STATE,
                        door.Hash, 0, false, false);
                }
                else
                {
                    // Use the native hash directly because this SHVDN version
                    // does not expose DOOR_CONTROL in its Hash enum.
                    Function.Call((Hash)0x9B12F9A0,
                        door.Hash,
                        door.Position.X,
                        door.Position.Y,
                        door.Position.Z,
                        false, 0.0f, 0.0f, 0.0f);
                }
            }

            if (_activated.Add(entry.Id))
                ModLogger.Info("Activated Story Mode interior assets: " + entry.Name);
        }

        private static bool IsMissionActive()
        {
            try
            {
                // GET_MISSION_FLAG. Fail closed for mission-sensitive locations.
                return Function.Call<bool>((Hash)0xA33CDCCDA663159E);
            }
            catch
            {
                return true;
            }
        }

        private static float DistanceSquared(Vector3 a, Vector3 b)
        {
            float x = a.X - b.X;
            float y = a.Y - b.Y;
            float z = a.Z - b.Z;
            return x * x + y * y + z * z;
        }

        private static List<InteriorEntry> BuildEntries()
        {
            var list = new List<InteriorEntry>();

            // Player safehouses. These use base-game Story Mode assets only.
            list.Add(E("clinton-residence", "Franklin's Aunt's House", new Vector3(-15f, -1441f, 31f), new Vector3(-15f, -1441f, 31f), false,
                new[] { "v_franklins" }, null,
                S(613961892), S(-1040675994)));
            list.Add(E("franklin-mansion", "Franklin's Mansion", new Vector3(8f, 540f, 176f), new Vector3(7.3f, 531.3f, 175.9f), false,
                new[] { "v_franklinshouse" }, null,
                S(1201219326), D(2052512905, 18.65038f, 546.3401f, 176.3448f)));
            list.Add(E("michael-house", "Michael's House", new Vector3(-820f, 165f, 70f), new Vector3(-809.2f, 180.7f, 73.2f), false,
                new[] { "v_michael", "v_michael_garage" }, null,
                S(-1978427516), S(-2127416656), S(-2097039789), S(-1986583853)));
            list.Add(E("trevor-trailer", "Trevor's Trailer", new Vector3(1973f, 3815f, 34f), new Vector3(1973f, 3815f, 34f), false,
                new string[0], null,
                S(1736361794), S(1113956670)));
            list.Add(E("lester-house", "Lester's House", new Vector3(1274f, -1720f, 55f), new Vector3(1274.9f, -1714.7f, 53.8f), false,
                new[] { "v_lesters" }, null,
                S(-565026078)));
            list.Add(E("epsilon-room", "Epsilon Storage Room", new Vector3(245f, 370f, 105f), new Vector3(245.1564f, 370.211f, 104.738f), false,
                new[] { "v_epsilonism" }, null,
                D(-1230442770, 241.3621f, 361.0471f, 105.8883f)));
            list.Add(E("janitor-apartment", "Janitor's Apartment", new Vector3(-108f, -9f, 70f), new Vector3(-108f, -9f, 70f), false,
                new[] { "v_janitor" }, null,
                D(486670049, -107.5373f, -9.0181f, 70.67085f)));

            // Commercial and mission interiors. Mission-variant sites are skipped
            // while GET_MISSION_FLAG is true to avoid replacing mission state.
            list.Add(E("premium-deluxe", "Premium Deluxe Motorsport", new Vector3(-45f, -1100f, 27f), new Vector3(-44f, -1097.5f, 27f), true,
                new[] { "shr_int", "shr_int_lod" }, new[] { "fakeint", "fakeint_lod" },
                S(-1831288286)));
            list.Add(E("lifeinvader", "Lifeinvader Office", new Vector3(-1050f, -235f, 40f), new Vector3(-1042.5f, -240.7f, 38.1f), true,
                new[] { "facelobby", "facelobby_lod" }, new[] { "facelobbyfake", "facelobbyfake_lod" },
                D(-340230128, -1042.518f, -240.6915f, 38.11796f)));
            list.Add(E("vangelico", "Vangelico Jewelry Store", new Vector3(-630f, -237f, 40f), new Vector3(-630.4f, -236.7f, 40f), true,
                new[] { "post_hiest_unload", "post_hiest_unload_lod" }, new[] { "jewel2fake", "jewel2fake_lod" },
                D(9467943, -630.4265f, -238.4375f, 38.20653f), D(1425919976, -631.9554f, -236.3333f, 38.20653f)));
            list[list.Count - 1].BlockedByActiveIpl = "bh1_16_refurb";
            list.Add(E("fib-lobby", "FIB Lobby", new Vector3(110f, -744f, 45f), new Vector3(110f, -744f, 45f), true,
                new[] { "fiblobby", "fiblobby_lod" }, new[] { "fiblobbyfake", "fiblobbyfake_lod" },
                D(-1517873911, 106.3793f, -742.6982f, 46.51962f), D(-90456267, 105.7607f, -746.646f, 46.18266f)));
            list.Add(E("recycling-plant", "Recycling Plant", new Vector3(-598f, -1608f, 26f), new Vector3(-598.6f, -1608.4f, 26f), true,
                new[] { "v_recycle" }, null,
                D(1099436502, -608.7289f, -1610.315f, 27.15894f), D(-1627599682, -611.32f, -1610.089f, 27.15894f), D(1099436502, -592.9376f, -1631.577f, 27.15931f), D(-1627599682, -592.7109f, -1628.986f, 27.15931f)));
            list.Add(E("humane-labs", "Humane Labs Interior", new Vector3(3523f, 3708f, 20f), new Vector3(3540.65f, 3675.77f, 28.12f), true,
                new[] { "v_lab" }, null,
                D(-1081024910, 3620.843f, 3751.527f, 27.69009f), D(-1081024910, 3627.713f, 3746.716f, 27.69009f)));
            list.Add(E("foundry", "Foundry", new Vector3(1087f, -1988f, 29f), new Vector3(1087.2f, -1988.4f, 28.65f), true,
                new[] { "v_foundry" }, null,
                D(-1428622127, 1083.547f, -1975.435f, 31.62222f), D(-1428622127, 1085.307f, -2018.561f, 41.62894f)));
            list.Add(E("solomon-office", "Solomon's Office", new Vector3(-1005f, -478f, 50f), new Vector3(-1005f, -478f, 50f), true,
                new[] { "v_58_sol_office" }, null));
            list.Add(E("tequi-la-la", "Tequi-la-la", new Vector3(-565f, 276f, 83f), new Vector3(-556.5f, 286.3f, 81.2f), true,
                new[] { "v_rockclub" }, null,
                D(993120320, -565.1712f, 276.6259f, 83.28626f), D(993120320, -561.2866f, 293.5044f, 87.77851f)));
            list.Add(E("paleto-sheriff", "Paleto Bay Sheriff Station", new Vector3(-445f, 6014f, 31f), new Vector3(-444.9f, 6013.6f, 30.7f), true,
                new[] { "v_sheriff" }, null,
                D(-1501157055, -444.4985f, 6017.06f, 31.86633f), D(-1501157055, -442.66f, 6015.222f, 31.86633f)));
            list.Add(E("sandy-sheriff", "Sandy Shores Sheriff Station", new Vector3(1854f, 3687f, 33f), new Vector3(1854.25f, 3686.74f, 33.27f), true,
                new[] { "v_sheriff2" }, null,
                D(-1765048490, 1855.685f, 3683.93f, 34.59282f)));
            list.Add(E("morgue", "Coroner / Morgue", new Vector3(-447f, 280f, 78f), new Vector3(-447.4833f, 280.3197f, 77.5215f), true,
                new[] { "coroner_int_on" }, new[] { "coroner_int_off" }));
            list.Add(E("slaughterhouse", "Abattoir", new Vector3(962f, -2180f, 31f), new Vector3(982.23f, -2160.38f, 28.48f), true,
                new[] { "v_abattoir" }, null,
                D(-1468417022, 962.9084f, -2105.813f, 32.52716f), D(1755793225, 962.0066f, -2183.816f, 31.06194f)));
            list.Add(E("blain-county-bank", "Paleto Bay Bank", new Vector3(-111f, 6464f, 31f), new Vector3(-111f, 6464f, 31f), true,
                new[] { "v_bank4" }, null));
            list.Add(E("oneil-ranch", "O'Neil Ranch Interior", new Vector3(2430f, 4968f, 42f), new Vector3(2430f, 4968f, 42f), true,
                new[] { "farm", "farmint", "farm_lod", "farm_props", "des_farmhouse" },
                new[] { "farm_burnt_lod", "farm_burnt_props", "farmint_cap", "farmint_cap_lod" }));
            list[list.Count - 1].BlockedByActiveIpl = "farm_burnt";
            list.Add(E("trevor-apartment", "Floyd's Apartment", new Vector3(-1150f, -1518f, 10f), new Vector3(-1149.7f, -1521.1f, 10.8f), true,
                new[] { "v_trevors" }, new[] { "vb_30_crimetape" },
                D(-607040053, -1149.709f, -1521.088f, 10.78267f)));
            list.Add(E("chopshop", "Hayes Autos / Chop Shop", new Vector3(479f, -1317f, 28f), new Vector3(479.06f, -1316.83f, 28.2f), true,
                new[] { "v_chopshop" }, null,
                D(-664582244, 482.8112f, -1311.953f, 29.35057f), D(-190780785, 484.5652f, -1315.574f, 30.20331f)));
            list.Add(E("meth-lab", "Meth Lab", new Vector3(1392f, 3605f, 35f), new Vector3(1392f, 3605f, 35f), true,
                new[] { "v_methlab" }, null));
            list.Add(E("torture-room", "Torture Room", new Vector3(147.5f, -2201f, 4f), new Vector3(147.5f, -2201f, 4f), true,
                new[] { "v_torture" }, null));

            // Base-game public interiors and stores with no online apartment IPLs.
            list.Add(E("ammunation-range", "Ammu-Nation Shooting Range", new Vector3(16f, -1110f, 29f), new Vector3(16f, -1110f, 29f), false,
                new[] { "v_gun" }, null));
            list.Add(E("cinema", "Cinema Interior", new Vector3(300f, 200f, 104f), new Vector3(300f, 200f, 104f), false,
                new[] { "v_cinema" }, null));
            list.Add(E("stripclub", "Vanilla Unicorn", new Vector3(128f, -1299f, 29f), new Vector3(128f, -1299f, 29f), false,
                new[] { "v_strip3" }, null));
            list.Add(E("police-station", "Mission Row Police Station", new Vector3(441f, -982f, 30f), new Vector3(441f, -982f, 30f), false,
                new[] { "v_policehub" }, null));
            list.Add(E("pacific-bank", "Pacific Standard Bank Interior", new Vector3(235f, 216f, 106f), new Vector3(235f, 216f, 106f), true,
                new[] { "v_bank" }, null));

            return list;
        }

        private static InteriorEntry E(string id, string name, Vector3 center, Vector3 probe,
            bool missionSensitive, string[] ipls, string[] removeIpls, params DoorEntry[] doors)
        {
            return new InteriorEntry(id, name, center, probe, missionSensitive,
                ipls ?? new string[0], removeIpls ?? new string[0], doors ?? new DoorEntry[0]);
        }

        private static DoorEntry D(int hash, float x, float y, float z)
        {
            return new DoorEntry(hash, new Vector3(x, y, z), false);
        }

        private static DoorEntry S(int hash)
        {
            return new DoorEntry(hash, new Vector3(0f, 0f, 0f), true);
        }

        private sealed class InteriorEntry
        {
            public string Id; public string Name; public Vector3 Center; public Vector3 InteriorProbe;
            public bool MissionSensitive; public string[] Ipls; public string[] RemoveIpls; public DoorEntry[] Doors;
            public string BlockedByActiveIpl;
            public InteriorEntry(string id, string name, Vector3 center, Vector3 probe, bool missionSensitive,
                string[] ipls, string[] removeIpls, DoorEntry[] doors)
            { Id = id; Name = name; Center = center; InteriorProbe = probe; MissionSensitive = missionSensitive; Ipls = ipls; RemoveIpls = removeIpls; Doors = doors; }
        }

        private sealed class DoorEntry
        {
            public int Hash; public Vector3 Position; public bool SystemDoor;
            public DoorEntry(int hash, Vector3 position, bool systemDoor) { Hash = hash; Position = position; SystemDoor = systemDoor; }
        }
    }
}
