# To-do list

[Italiano](DA_FARE.md) · **English**

## To be tested on hardware (first of all)

- Writing the firmware to the Pro Micro (Adapter tab → Install firmware). Recommended first test: reinstall xum1541 v08, with the IEC cable unplugged.
- Downloading the firmware and Zadig from GitHub (Adapter tab → Required tools).
- Full installer (`bin\XUMER-TOOL-Setup.exe`): Start menu and Desktop shortcuts, "Installed apps" entry, uninstall. So far only tested in a test folder.
- Games Floppy Runner (new graphics) and PETSCII Assault, screen saver: only tested with static screenshots.
- Preferences: theme drop-down list (mouse and arrow keys), changing theme and font, then Save.
- Narrow window dragged by hand: the toolbars compact themselves, tooltips appear on the icons.
- New icon in the Windows taskbar and Start menu, after reinstalling.

## Big jobs, in order

1. **Fast copy (S2 protocol)**
   - The code is already there (`src/FastCopy.cs`, `src/Asm6502.cs`, `drivecode/*.a65`) but it has never been tested on a real drive.
   - Test the assembler and the fast read with a disk inserted, then writing to a disk you can sacrifice, finally add the option to the interface.
   - Expected result: a whole disk in less than a minute, instead of about 10.
2. **The drive as a Windows drive letter (e.g. Z:)**
   - The disk in the 1571 shows up in File Explorer as a normal drive.
   - Needs the free WinFsp driver; XUMER TOOL acts as a bridge to the adapter.
   - Keep directories and files already read in memory; read from the drive only when a file is actually opened.
   - Also: mount a D64/D71 image as a drive.
3. **RFIEC: the PC as a drive for the C64** (optional)
   - Only needed with RetroFixer's RFIEC firmware: the C64 loads and saves files on the PC.
   - RFIEC's sources are GPL-3: rewrite from the protocol, without copying code, to stay MIT-licensed.

## Proposed ideas (to be chosen)

My picks: D64 viewer, animated disk map, drive animation with the LED.

### Features

- **D64/D71/D81 image viewer**: open an image from the PC and see its directory on the blue screen; extract files, run them in VICE, write it to a disk. Works without a drive.
- **File preview**: decoded BASIC listing, SEQ text, hex view.
- **Disk → disk copy** in a single pass.
- **Disk map (BAM)**: grid of tracks and sectors in colors (free, used, with errors), filling up while reading.
- **Floppy archive**: list of the images created, with search ("which floppy had…?").
- **Verify after writing** an image.
