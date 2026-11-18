# Changelog

[Italiano](CHANGELOG.md)

## 1.2b (beta) – 2026-11-18

- **Portable version only**: the installer is set aside for now. Extract the zip wherever you like; `LEGGIMI.txt` and `README.txt` explain how to start and how to remove it (just delete the folder).
- **New ribbon toolbar**: each section has its title centred and every command is a clickable tile with its name and icon; when the window gets narrower the names get shorter. "format" has a warning triangle.
- **Fast copy (experimental)**: in "create image" the "fast" tile (disk icon with a yellow bolt) starts the fast copy, using OpenCBM's S2 protocol: a whole disk in about one minute instead of ten. It only reads; if something goes wrong it redoes the copy with the normal method. Tested with a 1571 and D64 images.
- **Internet Archive tab**: connect your archive.org account (with email and password, which are never saved, or with the S3 keys). The keys stay on the PC, encrypted. Uploading images will come in the next version.
- **Drive message legend**: in the DOS commands tab ("Error messages") every DOS code with its meaning and what to do; in the log the explanation appears by itself under each error.
- **Tested with a 1541**: reading and formatting work.
- **Disk map**: during "Create image", "Write image" and "Disk in VICE" the blue screen shows tracks and sectors filling in as they go (done, in progress, with errors). D71 images show both sides next to each other.
- **Drive in the header**: a small drawn drive with the green power LED and the red one that flickers while working and blinks after an error.
- **File type icons** (PRG, SEQ, USR, REL, DEL) in pixel style in the directory.
- **Block progress bar** in PETSCII style.
- The screen saver no longer starts while an operation is running.
- Fixed: after connecting or reading the directory the buttons sometimes stayed disabled (they came back only by changing theme).
- Faster startup: the search for VICE runs in the background.
- Safer image reading: a sector that arrives incomplete gives an error instead of ending up in the image.
- Closing the program during an operation no longer makes it hang.

## 1.1b (beta) – 2026-11-18

- **English interface**: all texts of the program are translated. The language is chosen in Preferences (applied on restart); on first start it follows the Windows language.
- Translations live in a text file (`lang\en.txt`): adding other languages needs no changes to the interface code.
- **Portable version**: besides the installer, a zip to extract and run with nothing to install.
- The executable is now called `XumerTool.exe`.
- Updated icon: the label now reads "xumerTool"; it loads faster in the Windows taskbar.

## 1.0b (beta) – 2026-11-18

First public version, for testing.

**Drives and disks** (developed and tested with an xum1541 on an Arduino Pro Micro and a Commodore 1571):
- automatic connection to the adapter, bus reset, drive selection 8–11
- directory, reading and writing PRG/SEQ/P00 files, rename, delete, format, validate
- D64, D71 (1571 double-sided) and D81 disk images, reading and writing
- DOS commands with a list of ready-made commands and drive status reading

**Interface**
- 10 themes: Light, Commodore, Rosé Pine, Dark, Tokyo Night, Catppuccin, Everforest, Gruvbox, Nord, Lunar Tear, chosen from a drop-down list with a preview
- toolbars that adapt to the window width (short labels, then icons only with tooltips)
- 10 retro fonts for the directory screen
- running programs and disks in the VICE emulator
- original games (Floppy Runner, PETSCII Assault) and a PETSCII screen saver
- per-user installer, no administrator rights needed

**Still to be tested on hardware**
- fast copy (OpenCBM S2 protocol)
- writing the xum1541 firmware to the Pro Micro
- 1541 and 1581 drives (D81 images): supported, but only tested with a 1571
