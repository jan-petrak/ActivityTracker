Add-Type -AssemblyName System.Drawing

function New-IconBitmap {
    param([int]$Size)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    # Dark background with rounded corners
    $cornerRadius = [Math]::Max(2, [int]($Size * 0.16))
    $bgRect = New-Object System.Drawing.Rectangle(0, 0, $Size, $Size)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc($bgRect.X, $bgRect.Y, $cornerRadius * 2, $cornerRadius * 2, 180, 90)
    $path.AddArc($bgRect.Right - $cornerRadius * 2, $bgRect.Y, $cornerRadius * 2, $cornerRadius * 2, 270, 90)
    $path.AddArc($bgRect.Right - $cornerRadius * 2, $bgRect.Bottom - $cornerRadius * 2, $cornerRadius * 2, $cornerRadius * 2, 0, 90)
    $path.AddArc($bgRect.X, $bgRect.Bottom - $cornerRadius * 2, $cornerRadius * 2, $cornerRadius * 2, 90, 90)
    $path.CloseFigure()

    # Gradient background (deep blue to midnight)
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($bgRect, [System.Drawing.Color]::FromArgb(255, 30, 32, 48), [System.Drawing.Color]::FromArgb(255, 15, 17, 23), 45)
    $g.FillPath($bgBrush, $path)

    # Clock face
    $cx = $Size / 2.0
    $cy = $Size / 2.0
    $r = $Size * 0.34

    # Amber ring (clock outer)
    $ringW = [Math]::Max(1.0, $Size * 0.055)
    $amber = [System.Drawing.Color]::FromArgb(255, 245, 158, 11)
    $amberPen = New-Object System.Drawing.Pen($amber, $ringW)
    $ringRect = New-Object System.Drawing.RectangleF([single]($cx - $r), [single]($cy - $r), [single]($r * 2), [single]($r * 2))
    $g.DrawEllipse($amberPen, $ringRect)

    # Hour marks at 12, 3, 6, 9
    $markLen = $Size * 0.06
    $markPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 232, 233, 240), [single]([Math]::Max(1.0, $Size * 0.025)))
    $markPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $markPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    # 12
    $g.DrawLine($markPen, [single]$cx, [single]($cy - $r + $ringW * 0.5), [single]$cx, [single]($cy - $r + $ringW * 0.5 + $markLen))
    # 6
    $g.DrawLine($markPen, [single]$cx, [single]($cy + $r - $ringW * 0.5), [single]$cx, [single]($cy + $r - $ringW * 0.5 - $markLen))
    # 3
    $g.DrawLine($markPen, [single]($cx + $r - $ringW * 0.5), [single]$cy, [single]($cx + $r - $ringW * 0.5 - $markLen), [single]$cy)
    # 9
    $g.DrawLine($markPen, [single]($cx - $r + $ringW * 0.5), [single]$cy, [single]($cx - $r + $ringW * 0.5 + $markLen), [single]$cy)

    # Clock hands (amber, pointing to ~10:10-ish)
    $handPen = New-Object System.Drawing.Pen($amber, [single]([Math]::Max(1.5, $Size * 0.04)))
    $handPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $handPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    # Hour hand (to ~2 o'clock direction, outward)
    $hourLen = $r * 0.55
    $hx = $cx + $hourLen * [Math]::Sin([Math]::PI * 60 / 180)
    $hy = $cy - $hourLen * [Math]::Cos([Math]::PI * 60 / 180)
    $g.DrawLine($handPen, [single]$cx, [single]$cy, [single]$hx, [single]$hy)
    # Minute hand (to 12)
    $minLen = $r * 0.75
    $g.DrawLine($handPen, [single]$cx, [single]$cy, [single]$cx, [single]($cy - $minLen))

    # Center dot
    $dotR = [Math]::Max(1.0, $Size * 0.045)
    $centerBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 232, 233, 240))
    $g.FillEllipse($centerBrush, [single]($cx - $dotR), [single]($cy - $dotR), [single]($dotR * 2), [single]($dotR * 2))

    $g.Dispose()
    return $bmp
}

function Save-IconFile {
    param([string]$Path, [int[]]$Sizes)

    $bitmaps = @()
    foreach ($sz in $Sizes) { $bitmaps += New-IconBitmap -Size $sz }

    $pngStreams = @()
    foreach ($bmp in $bitmaps) {
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngStreams += ,$ms.ToArray()
    }

    $out = [System.IO.File]::Open($Path, [System.IO.FileMode]::Create)
    $bw = New-Object System.IO.BinaryWriter($out)

    # ICONDIR
    $bw.Write([uint16]0)          # reserved
    $bw.Write([uint16]1)          # type 1 = icon
    $bw.Write([uint16]$Sizes.Count)

    $headerLen = 6 + 16 * $Sizes.Count
    $offset = $headerLen

    for ($i = 0; $i -lt $Sizes.Count; $i++) {
        $size = $Sizes[$i]
        $data = $pngStreams[$i]
        $w = if ($size -ge 256) { 0 } else { [byte]$size }
        $h = if ($size -ge 256) { 0 } else { [byte]$size }
        $bw.Write([byte]$w)       # width
        $bw.Write([byte]$h)       # height
        $bw.Write([byte]0)        # palette count
        $bw.Write([byte]0)        # reserved
        $bw.Write([uint16]1)      # planes
        $bw.Write([uint16]32)     # bpp
        $bw.Write([uint32]$data.Length) # size
        $bw.Write([uint32]$offset)
        $offset += $data.Length
    }

    foreach ($data in $pngStreams) { $bw.Write($data) }

    $bw.Flush()
    $out.Close()

    foreach ($bmp in $bitmaps) { $bmp.Dispose() }
}

$outPath = Join-Path $PSScriptRoot 'AppIcon.ico'
Save-IconFile -Path $outPath -Sizes @(16, 24, 32, 48, 64, 128, 256)
Write-Host "Icon written to: $outPath"
