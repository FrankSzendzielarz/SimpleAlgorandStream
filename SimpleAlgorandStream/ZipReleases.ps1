
$rootDir = Join-Path -Path $PSScriptRoot -ChildPath "./bin/Release/net7.0"


$directories = Get-ChildItem -Directory -Path $rootDir


foreach ($dir in $directories) {
    # Set the zip file name
    $zipfile = Join-Path -Path $rootDir -ChildPath "$($dir.Name).zip"

    # Set the source directory
    $sourceDir = Join-Path -Path $dir.FullName -ChildPath "publish"

    # Create the zip file
    Compress-Archive -Path $sourceDir\* -DestinationPath $zipfile
}
