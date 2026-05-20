# Run as Administrator
$ilcDir   = 'Z:\NokiDrome\NokiDrome.UWP\bin\ARM\Release\ilc'
$winkits  = 'C:\Program Files (x86)\Windows Kits\10'
$makeappx = "$winkits\bin\10.0.16299.0\x64\makeappx.exe"
$signtool = "$winkits\bin\10.0.16299.0\x64\signtool.exe"
$pfxPath  = 'Z:\NokiDrome\NokiDrome.UWP\NokiDrome_TemporaryKey.pfx'
$appxOut  = 'Z:\NokiDrome\NokiDrome_ARM.appx'
$layout   = 'Z:\NokiDrome\AppXLayoutARM'

# Clean and create layout
if (Test-Path $layout) { Remove-Item $layout -Recurse -Force }
New-Item $layout               -ItemType Directory | Out-Null
New-Item "$layout\RuntimeDlls" -ItemType Directory | Out-Null
New-Item "$layout\Assets"      -ItemType Directory | Out-Null

Write-Host "Building ARM layout from ilc/..."

# Manifest — strip Debug VCLibs dependency (not present in Release packages)
$manifest = Get-Content "$ilcDir\AppxManifest.xml" -Raw
$manifest = $manifest -replace '\s*<PackageDependency Name="Microsoft\.VCLibs\.140\.00\.Debug"[^/]*/>', ''
Set-Content "$layout\AppxManifest.xml" $manifest -Encoding UTF8

# App binaries
Copy-Item "$ilcDir\NokiDrome.UWP.exe"    "$layout\"
Copy-Item "$ilcDir\NokiDrome.UWP.dll"    "$layout\"
Copy-Item "$ilcDir\NokiDrome.UWP.xr.xml" "$layout\"
Copy-Item "$ilcDir\resources.pri"        "$layout\"
Copy-Item "$ilcDir\clrcompression.dll"   "$layout\"

# Runtime DLLs
Copy-Item "$ilcDir\RuntimeDlls\System.Private.CoreLib.dll" "$layout\RuntimeDlls\"
Copy-Item "$ilcDir\RuntimeDlls\clrjit.dll"                 "$layout\RuntimeDlls\"
Copy-Item "$ilcDir\RuntimeDlls\uwphost.dll"                "$layout\RuntimeDlls\"

# Assets
Copy-Item "$ilcDir\Assets\*" "$layout\Assets\" -Recurse

Write-Host "Packing..."
& $makeappx pack /d $layout /p $appxOut /o
if ($LASTEXITCODE -ne 0) { Write-Error "MakeAppx failed"; exit 1 }

Write-Host "Signing..."
& $signtool sign /fd SHA256 /f $pfxPath /p DevOnly $appxOut
if ($LASTEXITCODE -ne 0) { Write-Error "SignTool failed"; exit 1 }

Write-Host "Done. APPX at: $appxOut"
