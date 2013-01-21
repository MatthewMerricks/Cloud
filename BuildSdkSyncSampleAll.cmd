if exist 3rdParty\bin\release\badgecom.dll (
    echo del 3rdParty\bin\release\badgecom.dll
    del 3rdParty\bin\release\badgecom.dll
    if ErrorLevel 1 goto Error
)
echo "C:\Windows\Microsoft.NET\Framework\v4.0.30319\Msbuild" win-client.sln /t:clean /p:configuration=Release64
"C:\Windows\Microsoft.NET\Framework\v4.0.30319\Msbuild" win-client.sln /t:clean /p:configuration=Release64
if ErrorLevel 1 goto Error
echo "C:\Windows\Microsoft.NET\Framework\v4.0.30319\Msbuild" win-client.sln /t:clean /p:configuration=ReleaseSampleAppOnly
"C:\Windows\Microsoft.NET\Framework\v4.0.30319\Msbuild" win-client.sln /t:clean /p:configuration=ReleaseSampleAppOnly
if ErrorLevel 1 goto Error
echo "C:\Windows\Microsoft.NET\Framework\v4.0.30319\Msbuild" win-client.sln /p:configuration=Release64
"C:\Windows\Microsoft.NET\Framework\v4.0.30319\Msbuild" win-client.sln /p:configuration=Release64
if ErrorLevel 1 goto Error
echo "C:\Windows\Microsoft.NET\Framework\v4.0.30319\Msbuild" win-client.sln /p:configuration=ReleaseSampleAppOnly
"C:\Windows\Microsoft.NET\Framework\v4.0.30319\Msbuild" win-client.sln /p:configuration=ReleaseSampleAppOnly
if ErrorLevel 1 goto Error

REM Place at bottom 
goto OK
:Error
Echo BUILD ERROR
goto Exit
:OK
Echo BUILD OK
:Exit


