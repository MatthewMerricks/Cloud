mkdir c:\Symbols
copy *.pdb c:\Symbols
if Not Defined ProgramFiles(x86) goto Exit
   copy X64\*.pdb c:\Symbols
:Exit
copy *.ini %LOCALAPPDATA%\Cloud
pause Press a key to continue...


