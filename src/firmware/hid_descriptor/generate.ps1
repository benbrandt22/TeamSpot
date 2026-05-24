$baseName = "descriptor"

$headerFile = "${baseName}.h"
$pythonFile = "${baseName}.py"

# Delete previous output files so stale results aren't used if generation fails
Remove-Item -Path "${headerFile}" -ErrorAction SilentlyContinue
Remove-Item -Path "${pythonFile}" -ErrorAction SilentlyContinue

# Run Waratah to generate the C header
WaratahCmd --source "$baseName.wara" --destination "${baseName}"

# Abort if Waratah failed to produce the header
if (-not (Test-Path "${headerFile}")) {
    Write-Error "Waratah generation failed — ${headerFile} not found. Aborting."
    exit 1
}

# Convert the generated header to a Python descriptor
$lines = Get-Content $headerFile

# Find the lines between the opening brace and closing "};" of the descriptor array
$inArray = $false
$descriptorLines = @()

foreach ($line in $lines) {
    if ($line -match 'static const uint8_t hidReportDescriptor\[\]') {
        $inArray = $true
        continue
    }
    if ($inArray) {
        if ($line -match '^\s*\{') { continue } # opening brace line
        if ($line -match '^\s*\};') { break }   # closing brace — done
        $descriptorLines += $line
    }
}

# Convert each line: replace // comments with #
$pythonLines = $descriptorLines | ForEach-Object {
    $_ -replace '//', '#'
}

# Build the output file
$output = @("HID_REPORT_DESCRIPTOR = bytes((")
$output += $pythonLines
$output += "))"

Set-Content -Path $pythonFile -Value $output

# Copy to clipboard
$output -join "`n" | Set-Clipboard

Write-Host "Written to $pythonFile and copied to clipboard."