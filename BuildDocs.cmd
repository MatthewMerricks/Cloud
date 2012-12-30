del /S html\*.*
rd /S html
pause Press a key...
doxygen doxyfile
cd html
7z a -tzip CloudSdkSyncSample.zip ./
cd .. 
