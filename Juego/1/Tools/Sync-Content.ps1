param(
    [string]$ContentDirectory = (Join-Path $PSScriptRoot '../Content')
)

$ErrorActionPreference = 'Stop'
$contentRoot = (Resolve-Path -LiteralPath $ContentDirectory).Path
& (Join-Path $PSScriptRoot 'Pack-SelectionBackgrounds.ps1') -ContentDirectory $contentRoot
$manifestPath = Join-Path $contentRoot 'Content.mgcb'
$original = [System.IO.File]::ReadAllText($manifestPath)
$sections = [regex]::Split($original, '(?m)^#begin ')
$header = $sections[0].TrimEnd()
$entries = @{}

foreach ($section in $sections | Select-Object -Skip 1) {
    $path = (($section -split '\r?\n', 2)[0]).Replace('\', '/')
    if ($path -match '(^|/)(bin|obj)/' -or $path -match '\.(mgcb|xnb|mgcontent|mgstats)$' -or
        $path -match '^Backgrounds/Selection/(ElegirV2|DudaV2|ContinuarV2)-Sheet\.png$') {
        continue
    }
    if (Test-Path -LiteralPath (Join-Path $contentRoot $path) -PathType Leaf) {
        $block = "#begin $section".TrimEnd()
        if ($path -match '^SFX/' -and $path -match '\.(wav|mp3|ogg)$') {
            $block = $block.Replace('/processor:SongProcessor', '/processor:SoundEffectProcessor')
        }
        $entries[$path] = $block
    }
}

$importers = @{
    '.png' = 'TextureImporter'; '.jpg' = 'TextureImporter'
    '.jpeg' = 'TextureImporter'; '.bmp' = 'TextureImporter'
    '.gif' = 'TextureImporter'; '.tga' = 'TextureImporter'
    '.dds' = 'TextureImporter'; '.wav' = 'WavImporter'
    '.mp3' = 'Mp3Importer'; '.ogg' = 'OggImporter'
    '.spritefont' = 'FontDescriptionImporter'; '.fx' = 'EffectImporter'
}

foreach ($file in Get-ChildItem -LiteralPath $contentRoot -Recurse -File) {
    $path = $file.FullName.Substring($contentRoot.Length + 1).Replace('\', '/')
    if ($path -match '(^|/)(bin|obj)/' -or $entries.ContainsKey($path) -or
        $path -match '^Backgrounds/Selection/(ElegirV2|DudaV2|ContinuarV2)-Sheet\.png$') {
        continue
    }
    $extension = $file.Extension.ToLowerInvariant()
    if (!$importers.ContainsKey($extension)) {
        continue
    }

    $parameters = @()
    switch ($importers[$extension]) {
        'TextureImporter' {
            $processor = 'TextureProcessor'
            $parameters = @(
                '/processorParam:ColorKeyEnabled=False'
                '/processorParam:GenerateMipmaps=False'
                '/processorParam:PremultiplyAlpha=True'
                '/processorParam:TextureFormat=Color'
            )
        }
        'FontDescriptionImporter' { $processor = 'FontDescriptionProcessor' }
        'EffectImporter' { $processor = 'EffectProcessor' }
        default {
            $processor = 'SoundEffectProcessor'
            if ($path -notmatch '^SFX/') { $processor = 'SongProcessor' }
            $parameters = @('/processorParam:Quality=Best')
        }
    }

    $entries[$path] = (@(
        "#begin $path"
        "/importer:$($importers[$extension])"
        "/processor:$processor"
    ) + $parameters + "/build:$path") -join "`r`n"
}

$blocks = foreach ($path in $entries.Keys | Sort-Object) { $entries[$path] }
$updated = $header + "`r`n`r`n" + ($blocks -join "`r`n`r`n") + "`r`n"
if ($updated -cne $original) {
    [System.IO.File]::WriteAllText($manifestPath, $updated, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Content.mgcb sincronizado: $($entries.Count) recursos."
}
