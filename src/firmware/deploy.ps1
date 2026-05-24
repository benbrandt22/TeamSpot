# Copies any new/changed files to the CircuitPython device

# Find the CircuitPython device by volume label and boot_out.txt content
function Find-CircuitPyDrive {
    $drives = Get-PSDrive -PSProvider FileSystem | Where-Object { $_.Root -match '^[A-Z]:\\$' }
    
    foreach ($drive in $drives) {
        $root = $drive.Root
        
        # Check volume label
        $vol = Get-Volume -DriveLetter $drive.Name -ErrorAction SilentlyContinue
        if ($vol.FileSystemLabel -ne 'CIRCUITPY') { continue }
        
        # Check boot_out.txt exists and contains "CircuitPython"
        $bootFile = Join-Path $root 'boot_out.txt'
        if (Test-Path $bootFile) {
            $content = Get-Content $bootFile -Raw -ErrorAction SilentlyContinue
            if ($content -match 'CircuitPython') {
                return $root
            }
        }
    }
    
    return $null
}

$target = Find-CircuitPyDrive

if (-not $target) {
    Write-Error "CircuitPython device not found. Is it plugged in?"
    exit 1
}

Write-Host "Found CircuitPython device at: $target"

robocopy $PSScriptRoot/src $target /v /e