# AGENTS.md

Compact instructions for OpenCode sessions working in this repo.

## What this repo is

A Xamarin / MvvmCross demo for the **CS203XL RFID reader** using the CSL **Network API (TCP)**. Two solution files, one shared UI/PCL, one underlying CSL library.

- Solution `CS203XL-C-Sharp-APP-for-iOS-ANDROID.sln` -> Android + iOS targets.
- Solution `CS203XL-C-Sharp-APP-for-UWP.sln` -> UWP target.
- Shared UI/ViewModels project: `MobileMvxApp/BLE.Client/BLE.Client.csproj` (`netstandard2.0`, MvvmCross 7.1.2 + Xamarin.Forms).
- Underlying driver: `Library/CSLibrary/CSLibrary2024-NETStandard-TCP.csproj` (`netstandard2.0`, assembly `CSLibrary2024TCP.NETSTD2`). This is the TCP variant for CS203XL — do **not** swap in `CSLibrary2024-NETStandard.csproj` or `CSLibrary2024-NET4.csproj`, those target different readers/OS.

## Repo layout (high-signal only)

- `Library/CSLibrary/` — CSL RFID reader driver. Several sibling csprojs for different TFMs (NET4, NETStandard, NETStandard20, NETStandard-TCP). Only `CSLibrary2024-NETStandard-TCP.csproj` is referenced by the app.
- `Library/CSLibrary-work/` and `Library/CSLibrary.Tools-work/` — **working copies**. Don't touch unless explicitly asked; the canonical source is `Library/CSLibrary/`.
- `Library/CSLibrary.Tools/` — additional helper (HPQueue). Not referenced by this app.
- `MobileMvxApp/BLE.Client/` — shared PCL: `PagesViewModelsSet/`, `Pages/`, `ViewModels/`, `Helpers/`, `Converters/`, `Extensions/`. `BleMvxFormsApp.xaml(.cs)` is the Xamarin.Forms entry; `BleMvxApplication.cs` is the MvxApplication and holds the global `HighLevelInterface _reader`.
- `MobileMvxApp/BLE.Client.Droid/` — Android host. `MainActivity.cs`, `MainApplication.cs`, `Setup.cs`. `TargetFrameworkVersion=v13.0`, ABIs `armeabi-v7a;arm64-v8a`.
- `MobileMvxApp/BLE.Client.iOS/` — iOS host.
- `MobileMvxApp/BLE.Client.UWP/` — UWP host. `Package.appxmanifest`, `App.xaml(.cs)`, `MainPage.xaml(.cs)`, `Setup.cs`.
- `APK Release/` — pre-built `CS203XLCSDemo0.0.1-001.apk`.

## Conventions specific to this codebase

- **Adding a new screen:** put XAML + ViewModel in `MobileMvxApp/BLE.Client/PagesViewModelsSet/<Feature>/PageXxx.xaml(.cs)` + `XxxViewModel.cs`. The folder layout under `PagesViewModelsSet/` (`Inventory`, `ReadWrite`, `RegisterTag`, `RFIDFilter`, `GeigerSearch`, `Settings`, `SecurityKill`, `SpecialFuction`) is the canonical extension point. Do **not** use spaces in folder names.
- `BLE.Client.csproj` has explicit `<Compile Remove="PagesViewModelsSet\NewFolder1\**" />` plus per-page `<DependentUpon>` entries. If you add pages outside the included subfolders, the project will silently drop them — keep new files inside the listed `PagesViewModelsSet/*` subfolders.
- `BleMvxApplication.Initialize()` auto-registers every type whose name ends in `Service` via `CreatableTypes().EndingWith("Service").AsInterfaces().RegisterAsLazySingleton()`. Register a new service with that naming; no manual `Register` call needed. App start VM is `ViewModelMainMenu` — change there to alter launch.
- Per-reader config is JSON, persisted via `PCLStorage` under folder `CSLReader` as `<readerID>.cfg`. See `BleMvxApplication.LoadConfig` / `SaveConfig`. `CONFIG` itself lives in `BleMvxApplication.cs`.
- RFID operations are issued through the static `BleMvxApplication._reader` of type `CSLibrary.HighLevelInterface` — this is the only entry into the CSL driver.

## What is **not** in this repo

- No CI (.github is absent), no lint config, no formatter config, no test project, no pre-commit hooks. Do not search for `make`/`test`/`lint` workflows; they don't exist.
- No `AGENTS.md`, `CLAUDE.md`, `.cursorrules`, `.opencode/` config, or other agent instruction files existed prior to this one.

## Build / runtime gotchas

- `BLE.Client` is referenced as `netstandard2.0` from both solutions. The README claims `netstandard2.1` for the iOS+Android sln — it is **stale**. Trust the csproj.
- The repo's `README.md` is a copy from the CS710S/BLE variant and still describes CS710S + Bluetooth. Do **not** follow it for reader model, transport, or API surface; the active project is **CS203XL over TCP**. Use `Library/CSLibrary/Readme.md.md` for the supported-reader matrix, but only the TCP row applies here.
- Windows installer caveat (also documented in `README.md`): an installer (UWP `.appx`/`.appxupload`, Android `.apk`) **built on Windows 11 may not run on Windows 10**. If Windows 10 deployment is required, build the installer on a Windows 10 machine.
- iOS builds require a Mac with Visual Studio for Mac / Xcode; iOS configurations in the sln use iPhone/iPhoneSimulator/x86/x64/ARM platforms — pick the right one before building.

## Things an agent is likely to miss

- The CSLibrary csproj named `CSLibrary2024-NETStandard-TCP.csproj` still pulls `Plugin.BLE` + `MvvmCross.Plugin.BLE`. That is intentional HAL support; do not "clean up" those refs as unused — they back the polymorphic reader interface even though the CS203XL transport is TCP.
- `BLE.Client.csproj` has explicit `<Compile Remove>` for several `ViewModels` files (`ServiceListViewModel`, `CharacteristicDetailViewModel`, `CharacteristicListViewModel`, `DescriptorDetailViewModel`, `DescriptorListViewModel`). These are intentionally excluded — do not un-exclude.
- `CSLibrary2024-NETStandard-TCP.csproj` sets `<Version>3.0.4</Version>`. Don't bump casually; the version is referenced by consumers.
- The Android csproj uses the old MSBuild XML format (`<Project DefaultTargets="Build" ToolsVersion="4.0" xmlns="...">`), not the SDK style. Do not "modernize" the format.
- `BLE.Client.iOS` lacks `<Compile>` removal entries, so adding files there is straightforward — but the iOS host project must still be built on macOS.