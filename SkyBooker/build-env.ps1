# Path to your .env file (change if needed)
$envFilePath = ".env"

if (-Not (Test-Path $envFilePath)) {
    Write-Error "❌ .env file not found at path: $envFilePath"
    exit 1
}

# Read the file and process each non-comment, non-empty line
Get-Content $envFilePath | ForEach-Object {
    $line = $_.Trim()

    # Skip empty lines or comments
    if ($line -eq "" -or $line.StartsWith("#")) {
        return
    }

    # Split line into key and value
    $parts = $line -split '=', 2
    if ($parts.Count -ne 2) {
        Write-Warning "⚠️ Skipping invalid line: $line"
        return
    }

    $key = $parts[0].Trim()
    $value = $parts[1].Trim()

    # Remove quotes if present
    $value = $value -replace '^["''](.*)["'']$', '$1'

    # Set environment variable at user level
    [Environment]::SetEnvironmentVariable($key, $value, "User")
    Write-Host "✅ Set: $key"
}

Write-Host "`n🎉 All environment variables from .env file have been set for the current user."
Write-Host "🔄 Restart your terminal or IDE to see the changes take effect."
