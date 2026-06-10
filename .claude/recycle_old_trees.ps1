Add-Type -AssemblyName Microsoft.VisualBasic
$targets = @(
    'C:\Users\VitaKaninen\Desktop\Projects\GitHub\Rimworld mods\CarryCapacityFromBionics\Standalone\1.6',
    'C:\Users\VitaKaninen\Desktop\Projects\GitHub\Rimworld mods\CarryCapacityFromBionics\Standalone\Defs\SettingsMenuDef.xml',
    'C:\Users\VitaKaninen\Desktop\Projects\GitHub\Rimworld mods\CarryCapacityFromBionics\VEF'
)
foreach ($t in $targets) {
    if (Test-Path $t -PathType Container) {
        [Microsoft.VisualBasic.FileIO.FileSystem]::DeleteDirectory($t, 'OnlyErrorDialogs', 'SendToRecycleBin')
    } elseif (Test-Path $t) {
        [Microsoft.VisualBasic.FileIO.FileSystem]::DeleteFile($t, 'OnlyErrorDialogs', 'SendToRecycleBin')
    }
    Write-Output "Recycled: $t"
}
