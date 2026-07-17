# Asset provenance

- `InstallerApp/Assets/SkypeSetup.ico` is the multi-resolution icon reconstructed from the supplied `SkypeSetup 7.41.0.101.exe` icon resources. It is used as the compiled executable icon and runtime window icon.
- `Research/RawResources/1` and `Research/RawResources/3` contain the original icon-image resources extracted from the supplied setup executable.
- `InstallerApp/Assets/SkypeBrand.png` is the Skype installer wordmark used to reproduce the original header. The UPX-packed bootstrapper does not expose that wordmark as an ordinary standalone PNG/BMP resource; its custom form data is stored in encoded `TSSMAINFORM` resource data. It has therefore not been falsely labelled as a direct bitmap extraction.

The project deliberately keeps the raw original resources alongside the WinForms recreation so further decoding can be done without losing provenance.


## Skype 7.0 installer resources

The `Skype70*.png` files were copied byte-for-byte from PNG-form RCDATA resources in the supplied unpacked `SkypeSetup 7.0.0.102.exe`. They are not recreated artwork. Their resource IDs are documented in `README-SkypeStyleInstaller.md`.
