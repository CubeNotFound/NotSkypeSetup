# Skype-style single-file WinForms installer

This project builds a self-contained `Setup.exe`. The installer configuration, UI artwork and every file under `InstallerApp\Payload` are embedded into the executable at compile time. The `Research` folder is intentionally omitted because it is not needed to build or run the installer.

## Build

1. Edit `InstallerApp/installer.config.xml`.
2. Replace the placeholder under `InstallerApp\Payload` with the complete distributable contents of your application.
3. Ensure `MainExecutable` exactly matches the path to the primary executable relative to `InstallerApp\Payload`, for example `MyApp.exe` or `bin\\MyApp.exe`.
4. Open `SkypeStyleInstaller.sln`, select `Release | Any CPU`, and rebuild.
5. Distribute only `InstallerApp/bin/Release/Setup.exe`.

Do not distribute the source `Assets`, `InstallerApp\Payload`, or config folders. They are build inputs and are already compiled into `Setup.exe`.

## Installation model

The installer enumerates all embedded payload resources, recreates their relative directory structure under the chosen installation folder, and overwrites files with the same names. This supports multi-file applications: executables, DLLs, JSON files, subfolders, runtimes and other application resources are copied exactly as arranged under `InstallerApp\Payload`.

It then:

- verifies that `MainExecutable` exists;
- copies the setup executable into the installation folder as `Uninstall.exe`;
- creates the selected desktop and Start menu shortcuts;
- writes `.installer-manifest.txt`, listing every file owned by the installer;
- registers the application under the current user's Windows uninstall registry key.

The app appears in Windows Settings under **Apps > Installed apps** and in classic **Programs and Features**. The registry entry contains `DisplayName`, `DisplayVersion`, `Publisher`, `InstallLocation`, `DisplayIcon`, `UninstallString`, `QuietUninstallString`, `EstimatedSize`, `NoModify`, and `NoRepair`.

Because installation is per-user and defaults to `%LocalAppData%`, administrator rights are normally unnecessary. Installing to `Program Files` requires elevation, which this sample does not automatically request.

## Updates

When an existing uninstall entry and matching main executable are found, the UI enters update mode and installs into the existing folder. Files present in the new payload overwrite older versions. Files removed from a newer payload are not automatically deleted during update; this protects user-created files. For strict replacement semantics, add a versioned cleanup list or compare the old and new manifests before copying.

## Uninstaller

`Uninstall.exe` is the same compiled bootstrapper running in `/uninstall` mode. It copies itself to `%TEMP%`, launches the temporary copy, and exits so Windows no longer locks the installed uninstaller file.

The temporary worker then:

- asks for confirmation unless `/quiet` is supplied;
- removes desktop and Start menu shortcuts;
- deletes the application's uninstall registry key;
- deletes only files listed in `.installer-manifest.txt`;
- removes directories only when empty;
- leaves untracked user data/configuration files in place;
- deletes the temporary uninstaller after completion.

This is why Geek/Revo force removal is no longer required.

## Commands

```text
Setup.exe
Uninstall.exe /uninstall
Uninstall.exe /uninstall /quiet
```

## Important limitations

- The payload is embedded without compression beyond normal PE/resource storage, so large applications produce a large `Setup.exe`. A ZIP-based embedded container can be added later for compression.
- Running applications may lock DLLs or executables. Production installers should detect and close the app before updating/uninstalling or schedule locked files for deletion after reboot.
- The sample writes only HKCU registry entries. Machine-wide installation would require HKLM, elevation, and 32/64-bit registry-view handling.
- Code-sign the final `Setup.exe` and installed `Uninstall.exe` for real distribution.

## Broken-install uninstall lookup

`Setup.exe /uninstall` now resolves the installation folder in this order:

1. The current executable directory, but only when it actually contains the install manifest or configured main file.
2. The registered `InstallLocation` under the product's HKCU uninstall key.
3. The expanded `DefaultInstallFolder` from the embedded config.

The uninstaller also treats `MainExecutable` and `Uninstall.exe` as installer-owned recovery files even if `.installer-manifest.txt` is missing or incomplete. File deletion is retried to handle short-lived locks, and the install directory is removed once empty.


## Payload folder location

Use exactly one payload directory:

```text
InstallerApp\Payload\
```

All application files placed there are embedded into `Setup.exe` during compilation. The former root-level `Payload` directory has been removed.

`MainExecutable` is relative to this folder. For example:

```text
InstallerApp\Payload\MyApp.txt
```

requires:

```xml
<MainExecutable>MyApp.txt</MainExecutable>
```

For a nested file such as `InstallerApp\Payload\bin\MyApp.exe`, use `<MainExecutable>bin\MyApp.exe</MainExecutable>`. Rebuild the project after adding or changing payload files; already-built `Setup.exe` files do not update automatically.


## Skype 7.0 UI skin

Set:

```xml
<SetupUiVersion>7.0</SetupUiVersion>
```

The 7.0 skin is based on the unpacked `SkypeSetup 7.0.0.102.exe` Delphi/VCL form resource (`TSSMAINFORM`), rather than the later 7.41 screenshot geometry. The original form declares a fixed 720 × 490 client area, Tahoma at 96 DPI, a 720 × 120 header region, a 680 × 270 page host at `(20,156)`, and a 44-pixel bottom button strip.

The following artwork is extracted directly from the unpacked 7.0 installer and embedded into the compiled setup:

- `Skype70Logo.png` — original 102 × 46 wordmark (`RCDATA 10101`)
- `Skype70Header.png` — original 720 × 112 blue/cloud/rainbow header (`RCDATA 10201`)
- `Skype70ClickToCall.png` — original Click-to-Call illustration (`RCDATA 10701`)
- `Skype70Wlm.png` — original Messenger migration illustration (`RCDATA 10801`)
- `Skype70Bing.png` / `Skype70BingAlt.png` — original Bing/MSN offer artwork (`RCDATA 20101/20102`)

When `ShowOfferPage` is enabled, choose the 7.0-era intermediate page with:

```xml
<Skype70OfferPage>Plugin</Skype70OfferPage>
```

Supported values are `Plugin`, `Bing`, `Yandex`, and `Wlm`.

The 7.41 skin remains selected with:

```xml
<SetupUiVersion>7.41</SetupUiVersion>
```


## 7.0 fine typography pass

The Skype 7.0 main and offer pages use enlarged Tahoma 9 pt body text and 11 pt headings to match the supplied official captures. The startup checkbox is checked by default and remains outside the collapsible More Options section. The 7.0 exit dialog uses Segoe UI 9 pt to match the native dialog rendering in the supplied capture. The 7.41 builders were not modified.


## Skype 7.0 exit-dialog heading refinement

The 7.0 dialog uses Segoe UI Semibold for the in-dialog heading and compensates for WinForms/GDI label left padding so the heading and body copy align with the original Delphi dialog. The 7.41 branch is unchanged.
