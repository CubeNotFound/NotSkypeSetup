# NotSkypeSetup
[![Bastion GSLP License](https://img.shields.io/badge/Bastion-GSLP%20v2.1-blue?style=for-the-badge)](LICENSE.md)
[![Discord](https://img.shields.io/discord/871577956852584488?label=Discord&style=for-the-badge)](https://discord.gg/EmEG5Eecys)
![Free Palestine!](https://img.shields.io/badge/Free-PALESTINE-darkgreen?style=for-the-badge)
  
*Part of the NotSkype relaunch*  
  
WinForms-based classic Skype 7 setup experience recreation.  
Gives a single-file `Setup.exe` with the installer UI, configuration, artwork, and payload files embedded.  
For the full build notes, installer behavior, payload setup, and UI skin details, please view [README-SkypeStyleInstaller.md](README-SkypeStyleInstaller.md).

## Basic Usage
1. Put the application files you want to install in `InstallerApp\Payload`.
2. Update `InstallerApp\installer.config.xml`.
3. Build `SkypeStyleInstaller.sln`.
4. Use the built `Setup.exe`.

## Notes.
* Some recreated UI pages/dialogs may exist mainly for accuracy and may not have any backend behavior yet.
  * An example of this is the 7.0 style's Offer Page.
* The detailed README has the more specific setup and limitation notes.
