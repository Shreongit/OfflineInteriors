# Build status for this package

This package contains the updated C# source and build script. It was **not compiled in the packaging environment**, because that environment does not have the Windows .NET Framework 4.8 build toolchain or the user's `ScriptHookVDotNet3.dll` reference available.

The previous DLL was intentionally excluded so it cannot be mistaken for the new implementation. Build on the Windows machine where ScriptHookVDotNet3 is installed:

```powershell
.\\build.ps1 -GtaVPath "E:\\Games\\Grand Theft Auto V Legacy" -ScriptHookVDotNetPath "E:\\Games\\Grand Theft Auto V Legacy"
```

Resolve any compile errors before installing. Even after a successful compile, the interior registry still needs the Story Mode runtime pass documented in `tests/README.md`.
