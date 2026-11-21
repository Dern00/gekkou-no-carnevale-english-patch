"""Build the Windows installer from the verified English patch and opening inputs."""

import argparse
import ctypes
from ctypes import wintypes
import hashlib
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import zipfile
from opening_cues import render_opening


SOURCE = Path(__file__).resolve().parent
OUTPUT = Path('out/Gekkou.no.Carnevale.English.Patch.v1.1.exe')
OFFICIAL_HASHES = (
    '9d75bc49495a3231cd26e65b245f12c89bf5f850a4a170f9956f233e5afc9bf0',
    '1cdb83f61990f45f7496d047ec7316a635331b24f48b81b98da1fbe3cc197c8a',
    '5e9c1180732f3ffec614983bfc735b686c6d5347a0b9f5404338d1128d09500f',
)
STOCK_ENGINE = 'ae6b5b463734f6eeefc1c61f05b44354ce52140578a7b151a962168dd354a158'
ENGLISH_OVERLAY = 'd7a77b77250be50a7063058fc542da6d7c7d36ecbc5632c4b25072ee15fb1db5'
ENGLISH_ENGINE = 'fff97b3fea508da5ab7c407e13234e1a4cfdd7174ccffaa62120b92c6f1538a9'
STOCK_OPENING = '2a5ceb8c09622e2ad43f5e2e3a906831815641e6e9fcef0e928fea0d0e19170c'
PRERELEASE_ENGINES = {
    'aba31f2fa285a0ec7dfbc7dcecbb9e2c4ba0032f83645ce97738ec97f073e951',
    '1648d339770e9675e01a2e781d33d6e0ee615da1f2ad8f4f5432d658537e414c',
}


class DeltaInput(ctypes.Structure):
    _fields_ = [('data', ctypes.c_char_p), ('size', ctypes.c_size_t), ('editable', wintypes.BOOL)]


class DeltaOutput(ctypes.Structure):
    _fields_ = [('data', ctypes.c_void_p), ('size', ctypes.c_size_t)]


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def delta_pair(original, patched):
    """Create a raw Microsoft delta and verify it reproduces the English engine."""
    delta = ctypes.WinDLL('msdelta.dll', use_last_error=True)
    delta.CreateDeltaB.argtypes = [
        ctypes.c_longlong, ctypes.c_longlong, ctypes.c_longlong, DeltaInput, DeltaInput,
        DeltaInput, DeltaInput, DeltaInput, ctypes.c_void_p, wintypes.UINT, ctypes.POINTER(DeltaOutput)
    ]
    delta.CreateDeltaB.restype = wintypes.BOOL
    delta.ApplyDeltaB.argtypes = [ctypes.c_longlong, DeltaInput, DeltaInput, ctypes.POINTER(DeltaOutput)]
    delta.ApplyDeltaB.restype = wintypes.BOOL
    delta.DeltaFree.argtypes = [ctypes.c_void_p]
    delta.DeltaFree.restype = wintypes.BOOL
    source = DeltaInput(original, len(original), False)
    target = DeltaInput(patched, len(patched), False)
    result = DeltaOutput()
    if not delta.CreateDeltaB(1, 0, 0, source, target,
                              DeltaInput(), DeltaInput(), DeltaInput(), None, 0, ctypes.byref(result)):
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        blob = ctypes.string_at(result.data, result.size)
    finally:
        delta.DeltaFree(result.data)
    verify = DeltaOutput()
    if not delta.ApplyDeltaB(0, source, DeltaInput(blob, len(blob), False), ctypes.byref(verify)):
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        if ctypes.string_at(verify.data, verify.size) != patched:
            raise ValueError('Engine delta did not reproduce the verified English DLL.')
    finally:
        delta.DeltaFree(verify.data)
    return blob


OPENING_ARGS = [
    '-hide_banner', '-loglevel', 'error', '-nostdin', '-cpuflags', '0',
    '-filter_complex_threads', '1', '-threads', '1', '-c:v', 'mpeg1video', '-i', 'stock.mpg',
    '-f', 'concat', '-safe', '0', '-i', 'cues.ffconcat',
    '-filter_complex', '[0:v][1:v]overlay=eof_action=pass:format=yuv420[out]',
    '-map', '[out]', '-map', '0:a:0', '-threads', '1',
    '-c:v', 'mpeg1video', '-b:v', '5600k', '-minrate:v', '5600k',
    '-maxrate:v', '5600k', '-bufsize:v', '1120k', '-muxrate', '6000k',
    '-g', '15', '-bf', '2', '-pix_fmt', 'yuv420p', '-c:a', 'copy', '-f', 'mpeg', 'subtitled.mpg',
]


def add_opening(archive, stage, ffmpeg, stock):
    if sha256(stock.read_bytes()) != STOCK_OPENING:
        raise ValueError('The original opening does not match the supported Japanese release.')
    opening = stage / 'opening'
    opening.mkdir()
    render_opening(opening)
    shutil.copyfile(stock, opening / 'stock.mpg')
    subprocess.run([ffmpeg.resolve(), *OPENING_ARGS], cwd=opening, check=True)
    patched_hash = sha256((opening / 'subtitled.mpg').read_bytes())
    archive.write(ffmpeg, 'opening/ffmpeg.exe')
    archive.writestr('opening/arguments.txt', ' '.join(OPENING_ARGS))
    for file in sorted(opening.iterdir()):
        if file.suffix in {'.pam', '.ffconcat'}:
            archive.write(file, 'opening/' + file.name)
    return patched_hash


def build(overlay, engine, stock_engine, prerelease, stock_opening, ffmpeg, output):
    overlay, engine, stock_engine = (path.read_bytes() for path in (overlay, engine, stock_engine))
    if sha256(overlay) != ENGLISH_OVERLAY or sha256(engine) != ENGLISH_ENGINE:
        raise ValueError('The English inputs differ from the verified v1.0 build.')
    if sha256(stock_engine) != STOCK_ENGINE:
        raise ValueError('The stock Windows game engine has an unrecognized SHA-256.')
    bases = {STOCK_ENGINE: stock_engine}
    for path in prerelease:
        data = path.read_bytes()
        digest = sha256(data)
        if digest not in PRERELEASE_ENGINES:
            raise ValueError(f'Unrecognized pre-release engine: {path.name}')
        bases[digest] = data
    compiler = Path(os.environ['WINDIR']) / 'Microsoft.NET/Framework/v4.0.30319/csc.exe'
    output = output.resolve()
    output.parent.mkdir(exist_ok=True, parents=True)
    with tempfile.TemporaryDirectory(dir=output.parent) as directory:
        stage = Path(directory)
        payload = stage / 'payload.zip'
        with zipfile.ZipFile(payload, 'w', zipfile.ZIP_DEFLATED) as archive:
            opening_hash = add_opening(archive, stage, ffmpeg, stock_opening)
            archive.writestr('sha256.txt', '\n'.join((*OFFICIAL_HASHES, ENGLISH_OVERLAY, ENGLISH_ENGINE, opening_hash)) + '\n')
            archive.writestr('0script.pak', overlay)
            for digest, original in sorted(bases.items()):
                archive.writestr(f'engine/{digest}.delta', delta_pair(original, engine))
        result = stage / output.name
        subprocess.run([
            str(compiler), '/nologo', '/target:winexe', '/platform:anycpu', '/optimize+',
            '/r:System.Windows.Forms.dll', '/r:System.Drawing.dll', '/r:System.IO.Compression.dll',
            f'/resource:{payload},payload.zip', f'/out:{result}',
            str(SOURCE / 'CombinedInstaller.cs'), str(SOURCE / 'BinaryDelta.cs'),
        ], check=True)
        os.replace(result, output)
    return output


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--overlay', required=True, type=Path)
    parser.add_argument('--engine', required=True, type=Path)
    parser.add_argument('--stock-engine', required=True, type=Path)
    parser.add_argument('--prerelease-engine', action='append', default=[], type=Path)
    parser.add_argument('--stock-opening', required=True, type=Path)
    parser.add_argument('--ffmpeg', required=True, type=Path)
    parser.add_argument('--output', default=OUTPUT, type=Path)
    args = parser.parse_args()
    print(build(args.overlay, args.engine, args.stock_engine, args.prerelease_engine,
                args.stock_opening, args.ffmpeg, args.output))
