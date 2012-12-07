mkdir c:\Trash
mkdir c:\Trash\DeploymentPackage
mkdir c:\Trash\DeploymentPackage\X64
del c:\Trash\DeploymentPackage\*.*
del c:\Trash\DeploymentPackage\X64\*.*
copy %WINCLIENTPATH%\TestDeploy\*.cmd c:\Trash\DeploymentPackage
copy %WINCLIENTPATH%\TestDeploy\*.ini c:\Trash\DeploymentPackage
copy %WINCLIENTPATH%\3rdParty\bin\Release\*.pdb c:\Trash\DeploymentPackage
copy %WINCLIENTPATH%\3rdParty\bin\Release64\*.pdb c:\Trash\DeploymentPackage\X64
copy %WINCLIENTPATH%\bin\Release\*.pdb c:\Trash\DeploymentPackage
copy %WINCLIENTPATH%\CloudSetup\CloudSetup\Express\SingleImage\DiskImages\DISK1\CloudSetup.exe c:\Trash\DeploymentPackage
7z a -tZIP -r c:\Trash\DeploymentPackage\CloudInstall.zip c:\Trash\DeploymentPackage\*.*
pause Press a key to continue...

