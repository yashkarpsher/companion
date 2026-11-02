param([string]$OutputDirectory = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
function Save-WizardBitmap([string]$path,[int]$width,[int]$height,[bool]$large) {
 $bitmap=[Drawing.Bitmap]::new($width,$height,[Drawing.Imaging.PixelFormat]::Format24bppRgb)
 $graphics=[Drawing.Graphics]::FromImage($bitmap)
 $graphics.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::AntiAlias
 $gradient=[Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Rectangle]::new(0,0,$width,$height),[Drawing.Color]::FromArgb(12,14,24),[Drawing.Color]::FromArgb(52,25,72),[single]65)
 $graphics.FillRectangle($gradient,0,0,$width,$height)
 $pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(199,134,245),3)
 if($large){$x=26;$y=39;$size=47}else{$x=12;$y=12;$size=33}
 $graphics.DrawEllipse($pen,$x,$y,$size,$size)
 $graphics.DrawLine($pen,$x-3,$y+$size+3,$x+$size+3,$y-3)
 if($large){
  $white=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(249,245,255))
  $muted=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(198,181,215))
  $brand=[Drawing.Font]::new('Segoe UI',12,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
  $title=[Drawing.Font]::new('Segoe UI',21,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
  $body=[Drawing.Font]::new('Segoe UI',12,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
  $graphics.DrawString('COMPANION',$brand,$white,20,104)
  $graphics.DrawString("РЯДОМ,`nПОКА ТЫ`nВ ИГРЕ.",$title,$white,[Drawing.RectangleF]::new(20,152,140,110))
  $graphics.DrawString('Твоё пространство.',$body,$muted,20,279)
  $brand.Dispose();$title.Dispose();$body.Dispose();$white.Dispose();$muted.Dispose()
 }
 $bitmap.Save($path,[Drawing.Imaging.ImageFormat]::Bmp)
 $pen.Dispose();$gradient.Dispose();$graphics.Dispose();$bitmap.Dispose()
}
Save-WizardBitmap (Join-Path $OutputDirectory 'wizard.bmp') 164 314 $true
Save-WizardBitmap (Join-Path $OutputDirectory 'wizard-small.bmp') 58 58 $false
