# Changelog

[Italiano](CHANGELOG.md)

## 1.3b (beta) – 2026-11-18

- **Disk images from the PC**: the "open image" tile (or dragging onto the blue screen) shows the directory of a D64, D71 or D81 even with no drive; files can be saved to the PC (also by double-click) or run in VICE, and "write image" suggests the open image.
- **Double-click in File Explorer**: Preferences can associate .d64/.d71/.d81 files with XUMER TOOL (your user only, no administrator; undo with "Remove"). If the program is already open, the image opens in the same window.
- **Directory as .txt**, C64 style, next to every image created (on by default, can be turned off in Preferences); a button creates it for all the images in a folder.
- **Right-click menu** on files: save to PC, run in VICE, rename, delete, save the directory as .txt.
- **Tidier Preferences**: coloured sections like the ribbon, one per row, no scrolling.
- Image reading checked against VICE's c1541 (identical directory, free blocks and files); a damaged image gives a clear error. `xumtest image` shows an image's directory and extracts its files.

## 1.2.1b (beta) – 2026-11-18

Fixes after a code review.

- **Internet Archive, sign-in with email and password**: fixed. The site rejected the request and "Sign-in failed" appeared even with the right details.
- **Internet Archive, S3 keys**: pasted keys are checked with the site's dedicated service and wrong keys are really rejected.
- **Sturdier slow copy**: a sector that arrives incomplete is read again up to 3 times instead of stopping the whole copy; if the drive reports a real error, the sector goes into the image's error table.
- **Fast copy not available** for the drive or format (e.g. D71 without a 1571): it switches to the normal copy straight away, with no waiting.
- The "create image" band is dimmed together with its tiles when the drive is disconnected or busy.
- Less wasted work: the disk map no longer recreates its fonts on every update and stops when the copy ends; the directory measures the icon column only once; the Internet Archive keys are saved in a single write.
- `xumtest` (command-line tests) explains how to use it when the drive number is missing.

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
- Safer image reading: a sector that arrives incomplete gives an error instead of ending up damaged in the image.
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
