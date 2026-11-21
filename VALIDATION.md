# v1.0 validation

## Patch contents

- The English overlay reconciles all 65,958 translated story and choice records across 417 scenario scripts.
- The installer’s engine delta reproduces the DLL used in the gameplay tests. Both changes from the September 2026 Windows menu crash fix match the earlier fixed DLL.
- The optional opening helper was tested with the Project 5 M4V and a verified stock opening. FFmpeg decoded the resulting movie successfully, the original MP2 audio stream matched byte for byte, and Restore brought back the original movie’s SHA-256.

## Installer

- A v1.00 test copy was installed with the official updater downloaded over HTTPS. All three official v1.01 files and both English patch files matched their expected hashes.
- The local updater fallback was also tested. A checksum mismatch stopped installation.
- Installation, repeat installation, and exact-file restoration were tested on v1.00 and v1.01 copies. The v1.01 test completed offline.
- An upgrade from an earlier combined test build preserved its installed optional opening. Restore verified the original backups.

## Gameplay checks

Windows 11 tests covered startup, translated settings, saving and loading, the backlog, and exit. Voice-on-click was tested with both settings and across a transition from voice to narration. The story and backlog were checked for ruby, word wrapping and long passages. An older save was loaded at the reported wrapping issue, and its line was verified in both the corrected story box and the backlog. The subtitled opening was played through in the earlier gameplay build.

The gameplay checks sampled scenes and supported save behavior. Full playthroughs and every hardware or Windows configuration remain unverified.
