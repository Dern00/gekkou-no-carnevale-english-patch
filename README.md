<p align="center">
  <img src="banner.svg" alt="Gekkou no Carnevale English Patch" width="1000">
</p>

# Gekkou no Carnevale English Patch

A complete English retranslation of **Gekkou no Carnevale** (月光のカルネヴァーレ), Nitroplus’s original Japanese visual novel for Windows. **v1.1** is available as a five-file manual patch that includes Nitroplus’s official v1.01 update.

<p align="center">
  <a href="https://github.com/Dern00/gekkou-no-carnevale-english-patch/releases/download/v1.1/Gekkou_no_Carnevale_English_Patch_v1.1_Manual_Combined.zip"><img src="download.svg" alt="Download v1.1" width="250" height="52"></a>
</p>

[Installation](#installation) · [Existing saves](#existing-saves) · [Removing the patch](#removing-the-patch) · [Report an issue](https://github.com/Dern00/gekkou-no-carnevale-english-patch/issues)

## Features

- A new translation from the Japanese v1.01 script covering all routes, choices, and endings.
- English menus, settings, interface artwork, and the all-endings card.
- English word wrapping, taller boxes for long passages, and ruby readings in both the story and the backlog.
- A remembered **Stop voice on click** option in **Settings → Text**. Turn it off to let voices finish over narration, or on to cut the current voice when you advance.
- The September 2026 crash fix for Windows menu input.

## Requirements

- The original Japanese Windows release from Nitroplus, either **v1.00** or **v1.01**.
- A Windows environment that can already launch your copy of the Japanese game. The patch was tested on Windows 11. [Nitroplus’s support page](https://www.nitroplus.co.jp/support/sp12/) covers the original game.

## Installation

The ZIP includes Nitroplus’s three v1.01 archives, `0cg.pak`, `script.pak`, and `system.pak`, plus the English patch’s `0script.pak` and `system.dll`. You can extract it directly over a fresh Japanese v1.00 installation in one step. [Nitroplus v1.01 update notes](https://www.nitroplus.co.jp/support/sp12/374.php)

1. Close the game.
2. In the folder containing `GekkounoCarnevale.exe`, back up any existing copies of the five files listed above.
3. Extract the five game files from the combined ZIP directly beside `GekkounoCarnevale.exe`. Accept replacement when Windows prompts. [Download the combined v1.1 ZIP](https://github.com/Dern00/gekkou-no-carnevale-english-patch/releases/download/v1.1/Gekkou_no_Carnevale_English_Patch_v1.1_Manual_Combined.zip)
4. Launch `GekkounoCarnevale.exe` as usual.

The ZIP includes a `README.txt` with installation and removal instructions. The manual patch leaves the installed opening movie as-is. A stock installation keeps its original Japanese opening.

## Existing saves

A new game gives you a clean English backlog. An existing save can retain the text that was on screen when it was made, along with older backlog entries. The engine fixes the textbox dimensions on older saves that use the six reassigned layouts. Loading and advancing from selected older saves was tested. If a save has trouble resuming, keep it and try starting from an earlier point.

## Removing the patch

Close the game and restore the files you backed up. Remove any files from the ZIP that were absent before installation. On a fresh v1.00 copy, those are `0cg.pak` and `0script.pak`.

## Feedback and verification

If you report a bug, include the patch version, the surrounding dialogue or menu, and steps to reproduce it. A screenshot is helpful. Mention whether it happened after loading an existing save. [Open an issue](https://github.com/Dern00/gekkou-no-carnevale-english-patch/issues).

The release includes a SHA-256 checksum for the combined ZIP. [Validation](VALIDATION.md).

## Credits

Original game, story, artwork, music, and branding by **Nitroplus** and their respective creators. This is an unofficial fan translation. [Original game page](https://www.nitroplus.co.jp/game/12-carnevale/).
