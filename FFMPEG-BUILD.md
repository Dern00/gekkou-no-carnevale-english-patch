# FFmpeg renderer for the withdrawn installer

The withdrawn installer used FFmpeg 9.1 under the GNU Lesser General Public License, version 2.1 or later. It ran the portable executable during installation to add subtitles to the original opening. It copied the original MPEG audio. The current manual patch leaves the opening movie as installed.

The renderer was built from the unmodified FFmpeg 9.1 source archive. Upstream source and documentation are available from [FFmpeg](https://ffmpeg.org/download.html).

Build from the extracted upstream source with a shell, GNU Make, and an i686 MinGW-w64 toolchain on PATH.

```sh
sh configure --enable-cross-compile --cross-prefix=i686-w64-mingw32- \
    --target-os=mingw32 --arch=x86 \
    --disable-everything --disable-autodetect --disable-doc --disable-debug \
    --disable-network --disable-ffprobe --disable-ffplay --disable-x86asm \
    --enable-small --enable-encoder=mpeg1video --enable-decoder=mpeg1video,pam,mp2 \
    --enable-demuxer=mpegps,concat,image2 --enable-muxer=mpeg1system \
    --enable-parser=mpegvideo,mpegaudio --enable-protocol=file \
    --enable-filter=overlay,scale,format
make -j8 V=1 ffmpeg.exe
```

Pass the resulting `ffmpeg.exe` to `build_installer.py --ffmpeg`. The patch build also reads a verified copy of the original game movie to calculate the checksum of the render. The installer payload contains the renderer and timed, transparent captions.
