# OfflineInteriors test plan

## Static checks performed for this source revision

- `data/interiors.json` parses as JSON and uses schema version 1 / `StoryModeOnly`.
- All four teleport records have unique IDs, finite coordinate objects, IPL lists, and ambient pedestrian spawning disabled.
- The built-in registry contains 30 location entries and does not reference known Online apartment IPL identifiers.
- C# source files were checked for balanced braces and parentheses.

These are static checks only; they do not replace a Windows compile or in-game test.

## Required Windows build check

Run from the project root:

```powershell
.\\build.ps1 -GtaVPath "E:\\Games\\Grand Theft Auto V Legacy" -ScriptHookVDotNetPath "E:\\Games\\Grand Theft Auto V Legacy"
```

Confirm the build finishes without errors and that `artifacts\\Release\\OfflineInteriors.dll` is newly generated.

## In-game Story Mode pass

1. Launch Story Mode, load a save, and wait until free roam is active.
2. Visit Franklin's aunt's house, Franklin's mansion, Michael's house, Lester's house, Lifeinvader, Vangelico, the FIB lobby, Recycling Plant, Humane Labs, Tequi-la-la, Paleto sheriff station, and Sandy Shores sheriff station.
3. Confirm the relevant doors are unlocked and the interior geometry appears without missing collision or falling through the map.
4. Visit each teleport access point in `data/interiors.json`, press E, wait for the destination to finish loading, then press E at the return point.
5. Start a mission and confirm the mod stops changing IPLs/doors while the mission is active.
6. Inspect `OfflineInteriors\\OfflineInteriors.log` for initialization, activation, and teleport errors.
7. Repeat after loading a save and after restarting the game. Do not test in GTA Online.

## Known limitations

- A successful compile does not prove every interior works in-game.
- Some interiors are mission-state dependent, have no usable collision, or need more specific elevator/floor access points.
- Ambient pedestrian spawning is deliberately not enabled in the shipped data.
