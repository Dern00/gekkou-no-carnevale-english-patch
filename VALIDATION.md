# Patch validation

## v1.1 combined manual ZIP

- The three official archives match Nitroplus’s v1.01 updater byte for byte. The two English files match the previously tested patch, including the September menu crash fix.
- After extraction over the corresponding v1.00 files in an isolated test folder, all five SHA-256 digests matched the tested v1.01 installation.
- Microsoft Defender’s custom scan reported no detections for the ZIP or the five extracted files.

## Earlier v1.1 installer tests

- The new installer was tested on clean v1.00 and v1.01 copies and an upgrade from v1.0. Repeat installs returned the same movie, and Restore returned the exact original movie in all three cases.
- The rendered file has the same 3,163 video frames, 800 × 608 dimensions, and duration as the original. Its MP2 audio is identical. All three installs produced the same output checksum using the portable FFmpeg build.
- An isolated copy of the game displayed both subtitle tracks and resumed the script after the full opening played without input.
- The installed English engine and scenario overlay match the tested v1.0 files, including both sites in the September Windows menu crash fix.

## v1.0 patch contents

- The English overlay reconciles all 65,958 translated story and choice records across 417 scenario scripts.
- The installer’s engine delta reproduces the DLL used in the gameplay tests. Both changes from the September 2026 Windows menu crash fix match the earlier fixed DLL.
- The optional v1.0 opening helper was tested with the earlier subtitled M4V and a verified stock opening. FFmpeg decoded the resulting movie successfully, the original MP2 audio stream matched byte for byte, and Restore brought back the original movie’s SHA-256.

## Installer

- A v1.00 test copy was installed with the official updater downloaded over HTTPS. All three official v1.01 files and both English patch files matched their expected hashes.
- The local updater fallback was also tested. A checksum mismatch stopped installation.
- Installation, repeat installation, and exact-file restoration were tested on v1.00 and v1.01 copies. The v1.01 test completed offline.
- An upgrade from an earlier combined test build preserved its installed optional opening. Restore verified the original backups.

## Gameplay checks

Windows 11 tests covered startup, translated settings, saving and loading, the backlog, and exit. Voice-on-click was tested with both settings and across a transition from voice to narration. The story and backlog were checked for ruby, word wrapping and long passages. An older save was loaded at the reported wrapping issue, and its line was verified in both the corrected story box and the backlog. The subtitled opening was played through in the earlier gameplay build.

The gameplay checks sampled scenes and supported save behavior. Full playthroughs and every hardware or Windows configuration remain unverified.
