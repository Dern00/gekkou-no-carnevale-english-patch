# Optional in-game opening subtitles

This helper builds the subtitled opening on your PC. The main English patch can be used on its own.

You’ll need the **Subtitled Opening.m4v** from **Project 5’s December 2020 translation extras**, your game’s original `mpeg\OP.mpg`, and a Windows build of [FFmpeg](https://ffmpeg.org/download.html) with the MPEG-1 video encoder.

Close the game. Open PowerShell in the folder where you extracted the patch. Replace the paths with yours and run this command.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Add-Opening-Subtitles.ps1 -GameFolder "C:\Games\Gekkou no Carnevale" -SubtitledVideo "C:\Extras\Subtitled Opening.m4v" -FFmpeg "C:\ffmpeg\bin\ffmpeg.exe"
```

If FFmpeg is already on your PATH, omit the `-FFmpeg` parameter. The script checks the supplied clip, tests the converted movie for decoding errors, and saves the stock opening as `mpeg\OP.before-subtitles.mpg`.

To return to the original opening, close the game and run the restore command.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Add-Opening-Subtitles.ps1 -GameFolder "C:\Games\Gekkou no Carnevale" -Restore
```

The game plays the same opening command in either case. The clip and original soundtrack come from the files you supply.
