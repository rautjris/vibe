$assemblyPath = 'C:\Users\RistoRautio\.nuget\packages\microsoft.fluentui.aspnetcore.components\4.13.1\lib\net8.0\Microsoft.FluentUI.AspNetCore.Components.dll'
$asm = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
$asm.GetExportedTypes() | Where-Object { $_.Name -like '*Provider*' } | Select-Object -ExpandProperty FullName
