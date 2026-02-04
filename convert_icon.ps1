param (
    [Parameter(Mandatory = $true)]
    [string]$InputFile,
    [Parameter(Mandatory = $true)]
    [string]$OutputFile
)

Add-Type -AssemblyName System.Drawing

function Write-Ico {
    param($SourcePath, $DestPath)
    
    $source = [System.Drawing.Image]::FromFile($SourcePath)
    $sizes = @(16, 32, 48, 256)
    $streams = New-Object System.Collections.Generic.List[System.IO.MemoryStream]
    
    try {
        # Generate PNG streams for each size
        foreach ($size in $sizes) {
            $bmp = New-Object System.Drawing.Bitmap($size, $size)
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.DrawImage($source, 0, 0, $size, $size)
            
            $ms = New-Object System.IO.MemoryStream
            $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
            $streams.Add($ms)
            
            $g.Dispose()
            $bmp.Dispose()
        }
        
        $fileStream = [System.IO.File]::OpenWrite($DestPath)
        $binaryWriter = New-Object System.IO.BinaryWriter($fileStream)
        
        # ICO Header
        $binaryWriter.Write([uint16]0)    # Reserved
        $binaryWriter.Write([uint16]1)    # Type (1 = Icon)
        $binaryWriter.Write([uint16]$sizes.Count)
        
        $offset = 6 + (16 * $sizes.Count)
        
        # Directory entries
        for ($i = 0; $i -lt $sizes.Count; $i++) {
            $size = $sizes[$i]
            $pngData = $streams[$i].ToArray()
            
            # Width/Height (0 means 256)
            $storedSize = $size
            if ($size -eq 256) { $storedSize = 0 }
            
            $binaryWriter.Write([byte]$storedSize) 
            $binaryWriter.Write([byte]$storedSize)
            $binaryWriter.Write([byte]0)    # Color palette
            $binaryWriter.Write([byte]0)    # Reserved
            $binaryWriter.Write([uint16]1)  # Color planes
            $binaryWriter.Write([uint16]32) # Bits per pixel
            $binaryWriter.Write([uint32]$pngData.Length)
            $binaryWriter.Write([uint32]$offset)
            
            $offset += $pngData.Length
        }
        
        # Image data
        foreach ($ms in $streams) {
            $binaryWriter.Write($ms.ToArray())
        }
        
        $binaryWriter.Close()
        $fileStream.Close()
    }
    finally {
        $source.Dispose()
        foreach ($ms in $streams) { if ($ms) { $ms.Dispose() } }
    }
}

if (Test-Path $OutputFile) { Remove-Item $OutputFile }
Write-Ico -SourcePath $InputFile -DestPath $OutputFile
Write-Host "Success: Created multi-size icon at $OutputFile"
