# To-do list

[Italiano](DA_FARE.md) · **English**

## To be tested on hardware (first of all)

- Writing the firmware to the Pro Micro (Adapter tab → Install firmware). Recommended first test: reinstall xum1541 v08, with the IEC cable unplugged.
- Downloading the firmware and Zadig from GitHub (Adapter tab → Required tools).
- Fast copy with D71 images and with a 1541.
- Disk map and drive LEDs during a long copy.
- Games Floppy Runner (new graphics) and PETSCII Assault, screen saver: only tested with static screenshots.
- Preferences: theme drop-down list (mouse and arrow keys), changing theme and font, then Save.
- Narrow window dragged by hand: the toolbars compact themselves, tooltips appear on the icons.
- New icon in the Windows taskbar.

## Big jobs, in order

1. **Fast copy (S2 protocol)** – D64 reading tested on a 1571 (about one minute instead of ten), in the "fast" tile of "create image", falling back to the normal copy on errors.
   - Still to test: D71 images and the 1541 drive.
   - Fast writing (`WriteImageFast`): to be tested on a disk you can sacrifice, then added as a "fast" tile in "write image".
2. **The drive as a Windows drive letter (e.g. Z:)**
   - The disk in the 1571 shows up in File Explorer as a normal drive.
   - Needs the free WinFsp driver; XUMER TOOL acts as a bridge to the adapter.
   - Keep directories and files already read in memory; read from the drive only when a file is actually opened.
   - Also: mount a D64/D71 image as a drive.
3. **RFIEC: the PC as a drive for the C64** (optional)
   - Only needed with RetroFixer's RFIEC firmware: the C64 loads and saves files on the PC.
   - RFIEC's sources are GPL-3: rewrite from the protocol, without copying code, to stay MIT-licensed.

## Proposed ideas (to be chosen)

My picks: D64 viewer, file preview.

### Features

- **Internet Archive**: the Internet Archive tab connects the app to an archive.org account; the next step is uploading disk images there directly from the app.
- **D64/D71/D81 image viewer**: open an image from the PC and see its directory on the blue screen; extract files, run them in VICE, write it to a disk. Works without a drive.
- **File preview**: decoded BASIC listing, SEQ text, hex view.
- **Disk → disk copy** in a single pass.
- **BAM map**: free and used blocks of the disk, without having to copy it.
- **Floppy archive**: list of the images created, with search ("which floppy had…?").
- **Verify after writing** an image.
