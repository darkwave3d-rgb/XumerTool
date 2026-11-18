XUMER TOOL 1.3b (beta) - by darkwave3D
========================================

Windows program to use Commodore 1541, 1571 and 1581 drives
with the xum1541 USB adapter (Arduino Pro Micro).

FIRST START
0. Extract the "XUMER TOOL" folder wherever you like (e.g. Documents) and run
   XumerTool.exe. Nothing needs to be installed.
   If Windows shows the SmartScreen warning: More info -> Run anyway.
1. Install the WinUSB driver for the adapter with Zadig
   (in the program: Adapter tab -> Required tools -> Download Zadig).
   In Zadig: Options -> List All Devices, choose "xum1541", WinUSB, Install Driver.
2. Connect the adapter to the drive and to the PC, turn on the drive.
3. Start XUMER TOOL: it connects by itself. Press "Directory".

WHAT IT DOES
- Directory, reading and writing PRG/SEQ/P00 files
- D64, D71 (1571 double-sided) and D81 disk images
- DOS commands, formatting, validation
- Running programs in the VICE emulator
- Writing the xum1541 firmware to the Pro Micro
- Themes, retro fonts, games and PETSCII screen saver
- Italian and English interface (Preferences -> Language)

WHERE THE FILES ARE
- Program: this folder
- Settings: %APPDATA%\XUMERTool
- Files for VICE: Documents\XUMER TOOL\VICE (can be changed)

TO REMOVE IT
Just delete this folder. To also delete the settings,
delete %APPDATA%\XUMERTool.

LICENSE
XUMER TOOL is released under the MIT license (see LICENSE.txt).
Included third-party components keep their own licenses:
- 6502 routines for fast copy (OpenCBM/d64copy): BSD 3-Clause,
  see Licenze\BSD-OpenCBM-d64copy.txt
- fonts: SIL Open Font License 1.1, see Licenze\OFL-*.txt
Full credits in the program's "Info" window.
