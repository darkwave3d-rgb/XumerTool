# Changelog

[Italiano](CHANGELOG.md)

## 1.1b (beta) – 2026-11-18

- **English interface**: all texts of the program and the installer are translated. The language is chosen in Preferences (applied on restart); on first start it follows the Windows language.
- Translations live in a text file (`lang\en.txt`): adding other languages needs no changes to the interface code.
- The executable is now called `XumerTool.exe`; the installer removes the old `XumTool.exe`.
- The installation also includes an English `README.txt`.
- Updated icon: the label now reads "xumerTool".

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
