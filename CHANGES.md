# Changes in this OfflineInteriors source update

- Added `StoryModeInteriorActivator`, a proximity-based registry for base-game Story Mode interior IPLs and door controls.
- Integrated activation into the existing `InteriorManager` rather than leaving `Update()` empty.
- Added mission/cutscene guards and a permanent-story-state guard for Vangelico and the burnt O'Neil Ranch state.
- Added E-key teleport access links that wait for IPL activation and interior readiness before moving the player.
- Added access links for the morgue, Solomon's Office, Union Depository Vault, and Humane Labs elevator.
- Added a valid Visual Studio solution file and release data staging to `build.ps1`.
- Updated install instructions, manual test plan, and third-party attribution notes.
- Removed stale compiled DLL/PDB files from the package so an older build cannot be mistaken for the updated implementation.
