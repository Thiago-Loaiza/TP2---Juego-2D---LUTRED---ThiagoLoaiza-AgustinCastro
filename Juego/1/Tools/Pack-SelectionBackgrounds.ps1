param([string]$ContentDirectory = (Join-Path $PSScriptRoot '../Content'))

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$sourceRoot = Join-Path $ContentDirectory 'Backgrounds/Selection'
$atlasRoot = Join-Path $sourceRoot 'Atlas'
$names = @('ElegirV2', 'DudaV2', 'ContinuarV2')

foreach ($name in $names) {
    $sourcePath = Join-Path $sourceRoot "$name-Sheet.png"
    if (!(Test-Path -LiteralPath $sourcePath)) { continue }
    $source = [System.Drawing.Bitmap]::new($sourcePath)
    try {
        if ($source.Height -ne 720 -or $source.Width % 1280 -ne 0) {
            throw "La hoja $name debe contener cuadros de 1280 por 720 en una fila."
        }
        $count = [int]($source.Width / 1280)
        $pages = [int][Math]::Ceiling($count / 12.0)
        New-Item -ItemType Directory -Path $atlasRoot -Force | Out-Null
        for ($page = 0; $page -lt $pages; $page++) {
            $targetPath = Join-Path $atlasRoot "$name-$page.png"
            if ((Test-Path -LiteralPath $targetPath) -and
                (Get-Item -LiteralPath $targetPath).LastWriteTimeUtc -ge
                (Get-Item -LiteralPath $sourcePath).LastWriteTimeUtc -and
                (Get-Item -LiteralPath $targetPath).LastWriteTimeUtc -ge
                (Get-Item -LiteralPath $PSCommandPath).LastWriteTimeUtc) { continue }

            $frames = [Math]::Min(12, $count - $page * 12)
            $rows = [int][Math]::Ceiling($frames / 3.0)
            $atlas = [System.Drawing.Bitmap]::new(3840, $rows * 720,
                [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            $graphics = [System.Drawing.Graphics]::FromImage($atlas)
            try {
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                for ($i = 0; $i -lt $frames; $i++) {
                    $destination = [System.Drawing.Rectangle]::new(
                        ($i % 3) * 1280, [int][Math]::Floor($i / 3.0) * 720, 1280, 720)
                    $graphics.DrawImage($source, $destination,
                        ($page * 12 + $i) * 1280, 0, 1280, 720, [System.Drawing.GraphicsUnit]::Pixel)
                }
                $atlas.Save($targetPath, [System.Drawing.Imaging.ImageFormat]::Png)
            }
            finally {
                $graphics.Dispose()
                $atlas.Dispose()
            }
        }
    }
    finally { $source.Dispose() }
}
