# Patch validation

## v1.1 combined manual ZIP

- The three official archives match Nitroplus’s v1.01 updater byte for byte. The two English patch files match the tested versions, including the September 2026 Windows menu crash fix.
- After the five game files were extracted into an isolated folder containing the Japanese v1.00 game files, their SHA-256 checksums matched the tested v1.01 installation.
- The published ZIP contains the same five game files and a `README.txt`. Its SHA-256 matches the [published checksum](Gekkou_no_Carnevale_English_Patch_v1.1_Manual_Combined.zip.sha256).
- Microsoft Defender reported no detections in a custom scan of the five extracted game files.

## Gameplay checks

Windows 11 tests with the English game files covered startup, translated settings, saving and loading, the backlog, and exit. Both settings for **Stop voice on click** were tested, including when a voiced line was followed by narration. The main text box and backlog were checked for ruby text, word wrapping, and long passages. An older save was loaded at the reported wrapping issue, and the line displayed correctly in both views.

Gameplay checks covered selected scenes and saves. Full playthroughs and other Windows or hardware configurations were outside that test coverage.
