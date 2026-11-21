<#
.SYNOPSIS
Build or restore the in-game subtitled opening using locally supplied files.
#>
[CmdletBinding(DefaultParameterSetName = 'Install')]
param(
    [Parameter(Mandatory)] [string] $GameFolder,
    [Parameter(Mandatory, ParameterSetName = 'Install')] [string] $SubtitledVideo,
    [Parameter(ParameterSetName = 'Install')] [string] $FFmpeg = 'ffmpeg.exe',
    [Parameter(Mandatory, ParameterSetName = 'Restore')] [switch] $Restore
)

$ErrorActionPreference = 'Stop'
$game = (Resolve-Path -LiteralPath $GameFolder).Path
if (-not (Test-Path -LiteralPath (Join-Path $game 'GekkounoCarnevale.exe') -PathType Leaf)) {
    throw 'Choose the game folder containing GekkounoCarnevale.exe.'
}
$running = Get-Process -Name 'GekkounoCarnevale' -ErrorAction SilentlyContinue
if ($running) { throw 'Close Gekkou no Carnevale before changing the opening.' }

$movie = Join-Path $game 'mpeg\OP.mpg'
$backup = Join-Path $game 'mpeg\OP.before-subtitles.mpg'
$receipt = Join-Path $game 'mpeg\OP.subtitled.sha256'
$originalHash = '2a5ceb8c09622e2ad43f5e2e3a906831815641e6e9fcef0e928fea0d0e19170c'
$oldPatchHash = 'e2fdb2ccbd0c10493ba59ff22381c4e4e9867077631b9594e4f2933b3fbb2a27'
$videoHash = '65af5cd678635f34d379327b0d878d6cfab98dcdb1ed31dbd035fa7db5cf1a46'
$id = [Guid]::NewGuid().ToString('N')
$stage = Join-Path $game "mpeg\OP-$id.mpg"
$pendingReceipt = Join-Path $game "mpeg\OP-$id.sha256"
$previousMovie = Join-Path $game "mpeg\OP-$id.previous.mpg"
$previousReceipt = Join-Path $game "mpeg\OP-$id.previous.sha256"

function FileHash([string] $File) {
    (Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash.ToLowerInvariant()
}

$current = FileHash $movie
if ($current -eq $originalHash -and $Restore) {
    Write-Output 'The original opening is already installed.'
    return
}
if ($current -eq $oldPatchHash -and -not $Restore) {
    Write-Output 'The subtitled opening is already installed.'
    return
}
if ($current -ne $originalHash -and $current -ne $oldPatchHash) {
    if (-not (Test-Path -LiteralPath $receipt) -or ([IO.File]::ReadAllText($receipt).Trim() -ne $current)) {
        throw 'The installed opening differs from the stock and known subtitled versions.'
    }
}

if ($Restore) {
    $candidates = @(
        $backup
        (Join-Path $game 'Before Carnevale combined patch\mpeg\OP.mpg')
        (Join-Path $game 'Before English retranslation\mpeg\OP.mpg')
    )
    $stock = $candidates | Where-Object {
        (Test-Path -LiteralPath $_ -PathType Leaf) -and (FileHash $_) -eq $originalHash
    } | Select-Object -First 1
    if (-not $stock) { throw 'The verified original opening is missing from the backups.' }
    try {
        [IO.File]::Copy($stock, $stage)
        if ((FileHash $stage) -ne $originalHash) { throw 'The staged original failed verification.' }
        if ((FileHash $movie) -ne $current) { throw 'The installed opening changed during restoration.' }
        [IO.File]::Replace($stage, $movie, $previousMovie)
        Write-Output 'Restored the original opening.'
    }
    finally {
        if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage }
        if (Test-Path -LiteralPath $previousMovie) { Remove-Item -LiteralPath $previousMovie }
    }
    return
}

if ($current -ne $originalHash) {
    Write-Output 'The subtitled opening is already installed. Restore the original first to rebuild it.'
    return
}
$video = (Resolve-Path -LiteralPath $SubtitledVideo).Path
if ((FileHash $video) -ne $videoHash) {
    throw 'Choose Subtitled Opening.m4v from the December 2020 Project 5 Extras folder.'
}
$ffmpegPath = (Get-Command $FFmpeg -CommandType Application).Source
try {
    & $ffmpegPath -hide_banner -v error -nostdin -i $video -i $movie -map '0:v:0' -map '1:a:0' `
        -r '30000/1001' -fps_mode cfr -c:v mpeg1video -b:v 5600k -minrate:v 5600k `
        -maxrate:v 5600k -bufsize:v 1120k -muxrate 6000k -g 15 -bf 2 -pix_fmt yuv420p `
        -c:a copy -f mpeg $stage
    if ($LASTEXITCODE -ne 0) { throw 'FFmpeg could not build the subtitled opening.' }
    & $ffmpegPath -hide_banner -v error -nostdin -xerror -i $stage -f null -
    if ($LASTEXITCODE -ne 0) { throw 'The subtitled opening failed its playback check.' }
    if ((FileHash $movie) -ne $originalHash) { throw 'The installed opening changed during conversion.' }
    if (-not (Test-Path -LiteralPath $backup)) { [IO.File]::Copy($movie, $backup) }
    if ((FileHash $backup) -ne $originalHash) { throw 'The backup failed verification.' }
    $built = FileHash $stage
    [IO.File]::WriteAllText($pendingReceipt, $built)
    if (Test-Path -LiteralPath $receipt) {
        [IO.File]::Replace($pendingReceipt, $receipt, $previousReceipt)
    } else {
        [IO.File]::Move($pendingReceipt, $receipt)
    }
    [IO.File]::Replace($stage, $movie, $previousMovie)
    Write-Output 'Installed the subtitled opening and verified its original backup.'
}
finally {
    foreach ($pending in @($stage, $pendingReceipt, $previousMovie, $previousReceipt)) {
        if (Test-Path -LiteralPath $pending) { Remove-Item -LiteralPath $pending }
    }
}
