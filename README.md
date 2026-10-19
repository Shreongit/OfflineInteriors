# OfflineInteriors

**A Story Mode interior-access mod for Grand Theft Auto V Legacy, built with C# and ScriptHookVDotNet.**

OfflineInteriors makes selected single-player interiors accessible through configurable interior locations and access points. It includes IPL streaming, door interaction support, and configurable entrance and exit links, with a focus on keeping the experience restricted to GTA V Story Mode.

## Features

- **31 configured interior locations** covering selected safehouses, offices, stores, and mission-related locations.
- **Story Mode focused** — designed for single-player gameplay, excluding GTA Online apartment interiors.
- **IPL streaming** — requests the required interior placement data when needed.
- **Door interaction support** — handles supported door-access operations.
- **Configurable access points** — includes four E-key access links for selected interiors.
- **JSON-based catalogue** — interior definitions can be maintained in a data file without changing the core manager.
- **Story-state safeguards** — includes checks intended to avoid activating certain locations in inappropriate game states.
- **C# implementation** — developed using ScriptHookVDotNet and the .NET Framework 4.8.
- **Build automation** — includes a PowerShell build script for compiling and staging the mod.

> **Note:** The 31 locations are configured catalogue entries, not a claim that every hidden, cut, or mission-exclusive interior in GTA V is supported.

## Requirements

- Grand Theft Auto V Legacy for Windows
- ScriptHookV
- ScriptHookVDotNet 3
- .NET Framework 4.8 targeting/build support
- Windows PowerShell

Use a compatible version of ScriptHookVDotNet for your installed GTA V build.

## Installation

1. Install ScriptHookV and ScriptHookVDotNet.
2. Build OfflineInteriors using the instructions below, or use a release package if one is provided.
3. Locate the generated `OfflineInteriors.dll`.
4. Copy the DLL into the GTA V `scripts` directory.
5. Copy the interior catalogue into the expected data location, preserving its directory structure.

Example installation layout:

```text
Grand Theft Auto V Legacy/
├── GTA5.exe
├── ScriptHookV.dll
├── ScriptHookVDotNet3.dll
└── scripts/
    ├── OfflineInteriors.dll
    └── OfflineInteriors/
        └── data/
            └── interiors.json
```

If the `scripts` directory does not exist, create it. Preserve the catalogue's expected path so the mod can load its interior definitions.

Launch GTA V and enter **Story Mode**. Check the ScriptHookVDotNet log if the mod fails to initialize.

## Building from Source

Clone or download this repository, then open PowerShell in the project root.

Example paths used in the following command:

- GTA V installation: `E:\Games\Grand Theft Auto V Legacy`
- ScriptHookVDotNet references: the GTA V installation directory

Run:

```powershell
.\build.ps1 `
  -GtaVPath "E:\Games\Grand Theft Auto V Legacy" `
  -ScriptHookVDotNetPath "E:\Games\Grand Theft Auto V Legacy"
```

The build script restores dependencies, compiles the project, and stages the output if the build succeeds.

Expected project file:

```text
src/OfflineInteriors/OfflineInteriors.csproj
```

Expected release output:

```text
artifacts/Release/OfflineInteriors.dll
```

The exact output location depends on the build script configuration. A successful compilation confirms that the project builds; it does not independently validate every interior in-game.

## Interior Catalogue

The interior catalogue is maintained in JSON:

```text
scripts/OfflineInteriors/data/interiors.json
```

The catalogue uses schema version `1` and a `StoryModeOnly` game-mode designation. Interior definitions can contain fields such as:

| Field | Purpose |
|---|---|
| `id` | Unique identifier for the location |
| `name` | Human-readable interior name |
| `entrance` | Entrance coordinates |
| `exit` | Exit coordinates |
| `entranceHeading` | Player heading at the entrance |
| `interiorId` | Associated game interior identifier |
| `allowAmbientPeds` | Whether ambient pedestrians are permitted |
| `maxAmbientPeds` | Maximum configured ambient pedestrian count |
| `requiredIpls` | IPLs required by the location |

When adding a location, use verified coordinates and IPL identifiers. Some interiors depend on mission progression, game state, or additional assets, so a catalogue entry alone does not guarantee that an interior is usable.

## Project Structure

```text
OfflineInteriors/
├── src/
│   └── OfflineInteriors/
│       ├── Core/
│       │   ├── InteriorManager.cs
│       │   ├── InteriorStreamer.cs
│       │   ├── StoryModeGuard.cs
│       │   └── StoryModeInteriorActivator.cs
│       ├── Doors/
│       │   └── DoorManager.cs
│       ├── Interiors/
│       │   └── InteriorCatalog.cs
│       ├── Population/
│       │   └── PedestrianManager.cs
│       └── OfflineInteriors.cs
├── data/
│   └── interiors.json
├── tests/
├── artifacts/
├── build.ps1
├── README.md
└── .gitignore
```

Some generated directories, such as `artifacts`, may appear only after a build.

## Troubleshooting

**The mod does not load**
- Confirm ScriptHookV and ScriptHookVDotNet are installed and compatible with your game version.
- Verify that `OfflineInteriors.dll` is inside the `scripts` directory.
- Check the ScriptHookVDotNet log for initialization errors.

**An interior does not load correctly**
- Verify its required IPL identifiers.
- Confirm that the location is available in the current Story Mode state.
- Check the coordinates and interior identifier in the catalogue.

**A door does not respond**
- Confirm the door model, door hash, and coordinates are correct.
- Verify that the door's native operation is supported by the installed ScriptHookVDotNet version.
- Check whether the door depends on mission progression or other game conditions.

**The build fails**
- Verify that the build script points to the correct GTA V installation and ScriptHookVDotNet DLLs.
- Review the compiler output for missing API members or incompatible references.

## Compatibility and Scope

OfflineInteriors targets **GTA V Legacy Story Mode**. It is not intended to unlock GTA Online apartments or provide unrestricted access to every interior in the game.

Back up your game configuration and mod files before installing or updating. Avoid combining the mod with other interior or map modifications that alter the same IPLs, doors, or locations.

## Contributing

Contributions are welcome. When adding or improving an interior:

1. Verify the location in Story Mode.
2. Document its required IPLs and game-state dependencies.
3. Confirm entrance, exit, and door behavior.
4. Keep Online-only content outside the Story Mode catalogue.
5. Include relevant build or regression-test results.

## License

No license has been specified in this README. Add a `LICENSE` file before distributing the project so others know how they may use, modify, and redistribute the code.

---

**OfflineInteriors** — expanding access to selected GTA V Story Mode interiors through a configurable C# mod.