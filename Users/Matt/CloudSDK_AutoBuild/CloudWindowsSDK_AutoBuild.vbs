'Citrix - Cloud Group
'Created By Matthew Merricks, 06/2013
'Auto Build Script for the Windows SDK Solution.  Used for preparing for a staging release.
'See the original document for further clarification, "Preparing for a staging realease.docx"
'All steps in this script will be proceded by the text explanation from the original how to document.
Option Explicit

'
'Variables
'
'User Defined
Dim Source_FolderPath : Source_FolderPath = "C:\Cloud\CloudSDK-Windows\"
Dim Version_Major : Version_Major = "9"
Dim Version_Minor : Version_Minor = "0"
'typically only update this value.
Dim Version_Build : Version_Build = "9"
'typically always 0 for a Release.
Dim Version_Revision : Version_Revision = "5"
Dim Signing_UseKey : Signing_UseKey = True
'
'Constants
Dim currFile, currFolder, Source_Folder, FSO, WSNetworkObject 
'Solution
Dim Cloud_Solution_Filename : Cloud_Solution_Filename = "CloudSDK.sln"
'Projects in Solution
Dim Cloud_FolderName_SampleLiveSync : Cloud_FolderName_SampleLiveSync = "CloudSdkSyncSample"
Dim Cloud_ProjectName_SampleLiveSync : Cloud_ProjectName_SampleLiveSync = "Sample-Live-Sync"
Dim Cloud_ProjectName_CloudApiPublic : Cloud_ProjectName_CloudApiPublic = "CloudApiPublic"
Dim Cloud_ProjectName_CloudSetupSdkSyncSample : Cloud_ProjectName_CloudSetupSdkSyncSample = "CloudSetupSdkSyncSample"
Dim Cloud_ProjectName_CloudSetupSdkSyncSampleSupport : Cloud_ProjectName_CloudSetupSdkSyncSampleSupport = "CloudSetupSdkSyncSampleSupport"
Dim Cloud_ProjectName_ObfuscateCloud : Cloud_ProjectName_ObfuscateCloud = "ObfuscateCloud"
'Version
Dim Cloud_Solution_CurrentVersion : Cloud_Solution_CurrentVersion = Version_Major & "." & Version_Minor & "." & Version_Build & "." & Version_Revision
Dim Version_AssemblyInfo_PathAndName : Version_AssemblyInfo_PathAndName = "\Properties\AssemblyInfo.cs"
Dim Version_AssemblyInfo_PathAndName_TEMP : Version_AssemblyInfo_PathAndName_TEMP = "\Properties\AssemblyInfoTemp.cs"
'Tag
Dim Cloud_Solution_TagName : Cloud_Solution_TagName = "12345"
'Signing
Dim CloudPlatformCodeSigning_Path : CloudPlatformCodeSigning_Path = "C:\CertBackup\CloudSigning\"
Dim CloudPlatformCodeSigning_File : CloudPlatformCodeSigning_File = "CloudPlatformCodeSigning.pfx"
'TODO: this password is hardcoded here.
Dim CloudPlatformCodeSigning_Password : CloudPlatformCodeSigning_Password = "cArd1n@l"
Dim CloudPlatformCodeSigning_MoveKeyProject : CloudPlatformCodeSigning_MoveKeyProject = "AddLicenseFiles\ZAddLicenseFiles.csproj"
Dim CloudPlatformCodeSigning_RemoveKeyProject : CloudPlatformCodeSigning_RemoveKeyProject = "DeleteLicenseFilesOnly\ZDeleteLicenseFilesOnly.csproj"
Dim CloudSigning_SignToolLocation : CloudSigning_SignToolLocation = "C:\Program Files (x86)\InstallShield\2012SpringLE\System\"
Dim CloudSigning_SignToolEXE : CloudSigning_SignToolEXE = "signtool.exe"
'Install
Dim CloudSDK_Install_Path : CloudSDK_Install_Path = "C:\CloudSetupSdkSyncSample\Express\SingleImage\DiskImages\DISK1\"
Dim CloudSDK_InstallEXE : CloudSDK_InstallEXE = "CloudSdkSetup.exe"
Dim CloudSDK_InstallZIP : CloudSDK_InstallZIP = "CloudSDKv" & Version_Major & Version_Minor & Version_Build & Version_Revision & ".zip"
Dim CloudSDK_Install_ArchivedReleaseFolder : CloudSDK_Install_ArchivedReleaseFolder = "C:\Source\Projects\ArchivedCloudSdkReleases\"
Dim CloudSDK_InstallDSOMap : CloudSDK_InstallDSOMap = "Cloud.dsomap"
'Obfuscation
Dim Obfuscate_DS_Configuration : Obfuscate_DS_Configuration = "C:\Cloud\CloudSDK-Windows\CloudApiPublic\Cloud.dsoconfig"
Dim Obfuscate_ProjectName : Obfuscate_ProjectName = "ObfuscateCloud"
'Visual Studio
Dim VisualStudio_Path : VisualStudio_Path = "C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\"
Dim VisualStudio_EXE : VisualStudio_EXE = "devenv.exe"
'Server
Dim ServerNameFile_CLDefinitions : ServerNameFile_CLDefinitions = "CloudApiPublic\Static\CLDefinitions.cs"
Dim ServerNameFile_CLDefinitions_TEMP : ServerNameFile_CLDefinitions_TEMP = "CloudApiPublic\Static\CLDefinitionsTemp.cs"
Dim Server_RemoteStorageForBackup_Path : Server_RemoteStorageForBackup_Path = "\\10.3.0.28\Builds\Windows\ArchivedCloudSdkReleases\"
Dim Server_RemoteStorageForBackup_PathDriveOnly : Server_RemoteStorageForBackup_PathDriveOnly = "\\10.3.0.28\Builds"
'TODO: should this be removed.  username and password
Dim Server_RemoteStorageForBackup_UserName : Server_RemoteStorageForBackup_UserName = "Cloud"
Dim Server_RemoteStorageForBackup_Password : Server_RemoteStorageForBackup_Password = "EverythingBurrito"
'Date
Dim todaysDate : todaysDate = CDate(Date)
Dim todaysDate_Month : todaysDate_Month = Month(todaysDate)
'The Date should be two digits long.
'Ie. January is 01 not 1
If Len( todaysDate_Month ) = 1 Then
    todaysDate_Month = "0" & todaysDate_Month
End If
Dim todaysDate_Day : todaysDate_Day = Day(todaysDate)
If Len( todaysDate_Day ) = 1 Then
    todaysDate_Day = "0" & todaysDate_Month
End If
Dim DateString_ForBranch : DateString_ForBranch = Year(todaysDate) & todaysDate_Month & todaysDate_Day
'Branch
Dim Release_BranchString : Release_BranchString = DateString_ForBranch & "Release" & Version_Major & "_" & Version_Minor & "_" & Version_Build & "_" & Version_Revision
Dim CloudSDK_Install_ArchivedReleaseFolderName : CloudSDK_Install_ArchivedReleaseFolderName = Release_BranchString
CloudSDK_Install_ArchivedReleaseFolder = CloudSDK_Install_ArchivedReleaseFolder & CloudSDK_Install_ArchivedReleaseFolderName & "\"


'Start up text.
WScript.Echo ""
WScript.Echo ""
WScript.Echo ""
WScript.Echo ""
WScript.Echo ""
WScript.Echo ""
WScript.Echo ""
WScript.Echo ""
WScript.Echo ""
WScript.Echo ""
WScript.Echo "Citrix - Cloud Group"
WScript.Echo ""
WScript.Echo ""
WScript.Echo "Auto Build Script for the Windows SDK Solution"
WScript.Echo ""
WScript.Echo "Used for preparing a Staging Release."
WScript.Echo "For further clarification see ""Preparing for a staging realease.docx"""
WScript.Echo ""
Set FSO = CreateObject("Scripting.FileSystemObject")
'Check that the Solution exists.
If FSO.FileExists( Source_FolderPath & Cloud_Solution_Filename ) Then
    WScript.Echo "The Solution was located at " & Source_FolderPath & Cloud_Solution_Filename
Else
    WScript.Echo "The Solution was NOT located!"
    WScript.Echo "The current Path is " & Source_FolderPath & Cloud_Solution_Filename
    WScript.Echo "You can change the path by editing the Source_FolderPath variable"
    WScript.Echo ""
    WScript.Echo "The Script will now EXIT"
    WScript.Quit()
End If
'Is this the correct version.
WScript.Echo "The Current Version is " & Cloud_Solution_CurrentVersion
WScript.Echo "The Release Branch is " & Release_BranchString 
'Check that Visual Studio exists.
If FSO.FileExists( VisualStudio_Path & VisualStudio_EXE ) Then
    WScript.Echo "Visual Studio was located at " & VisualStudio_Path & VisualStudio_EXE
Else
    WScript.Echo "Visual Studio was NOT located!"
    WScript.Echo "The current Path is " & VisualStudio_Path & VisualStudio_EXE
    WScript.Echo "You can change the path by editing the VisualStudio_Path variable"
    WScript.Echo ""
    WScript.Echo "The Script will now EXIT"
    WScript.Quit()
End If
'Check that the signing key exists.
If FSO.FileExists(CloudPlatformCodeSigning_Path & CloudPlatformCodeSigning_File) Then
    WScript.Echo "The key " & CloudPlatformCodeSigning_File & " was located at " & CloudPlatformCodeSigning_Path 
    Signing_UseKey = True
    'Check that the SignTool exists.
    If FSO.FileExists( CloudSigning_SignToolLocation & CloudSigning_SignToolEXE ) Then
        WScript.Echo "The signtool was located at " & CloudSigning_SignToolLocation & CloudSigning_SignToolEXE 
    Else
        WScript.Echo "The signtool was NOT located!"
        WScript.Echo "The current Path is " & CloudSigning_SignToolLocation & CloudSigning_SignToolEXE
        WScript.Echo "You can change the path by editing the CloudSigning_SignToolLocation variable"
        WScript.Echo ""
        WScript.Echo "The Script will now EXIT"
        WScript.Quit()
    End If
Else
WScript.Echo ""
    WScript.Echo "THE SIGNING KEY WAS NOT LOCATED!!!"
    WScript.Echo "If you continue the project will NOT be signed."
    Signing_UseKey = False
End If
WScript.Echo ""
WScript.Echo ""
WScript.Echo ""
WScript.Echo ">>>> Ready To Start"
WScript.Echo ""
WScript.Echo "Press Enter to Cotinue."
WScript.StdIn.Read(1)
WScript.Echo ""
WScript.Echo ""


'
'Start
'''
''STEP
'''
'1a)  Setup the Windows Shell so it can be used to run the commands for the AutoBuild proccess.
WScript.Echo ">>>> STEP"
WScript.Echo "1a)  Start the Windows Shell for command line proccessing."
Dim oShell
Set oShell = WScript.CreateObject("WScript.Shell")
oShell.CurrentDirectory = Source_FolderPath
AnalyzeReturnCode( 0 )
WScript.Echo ""
WScript.Echo ""


''
'STEP
''
'1b)  Remove all of the .PDB and .Zip files from the Solution heirarchy.
WScript.Echo ">>>> STEP"
WScript.Echo "1b)  Remove all of the .PDB and .Zip files from the Solution heirarchy."
'Check our return code, if we succeeded continue.  If not we should quit proccessing.
AnalyzeReturnCode( RemovePdbAndZipFiles( Source_FolderPath ) )
WScript.Echo ""
WScript.Echo ""


''
'STEP
''
'2a)  Check out the master branch:
'Make sure that all current work has been committed and staged to master.
'Make sure you are current on the master branch.
WScript.Echo ">>>> STEP"
WScript.Echo "2a)  Check out the master branch."
'AnalyzeReturnCode(oShell.Run( "git checkout master", 1, true ))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'2b)  Pull master from git.
WScript.Echo ">>>> STEP"
WScript.Echo "2b)  Pull master from git."
'TODO: uncomment this, temp change for Matt test
'AnalyzeReturnCode(oShell.run( "git pull", 1, true))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'3)  Make a new branch for the release.
WScript.Echo ">>>> STEP"
WScript.Echo "3)  Make a new branch for the release."
'AnalyzeReturnCode(oShell.run( "git branch " & Release_BranchString, 1, true))
'AnalyzeReturnCode(oShell.run( "git checkout " & Release_BranchString, 1, true))
'AnalyzeReturnCode(oShell.run( "git push  -u origin " & Release_BranchString, 1, true))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'4)  Change the define in CLDefinitions from development cliff servers to the cloud.com servers. 
'Edit the file and place the correct #define line in the file.
'CloudApiPublic\Static\CLDefinitions: define PRODUCTION_BACKEND.
WScript.Echo ">>>> STEP"
WScript.Echo "4)  Change the #define in CLDefinitions from development cliff servers to the cloud.com servers."
AnalyzeReturnCode(InsertPoundDefineProductionServerName())
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'5a)  Change the Sample-Live-Sync, CloudApiPublic and CloudSetupSdkSyncSampleSupport Assembly and File Versions to the current release.  E.g., 0.1.2.0
WScript.Echo ">>>> STEP"
WScript.Echo "5a)  Change the Sample-Live-Sync, CloudApiPublic and CloudSetupSdkSyncSampleSupport Assembly and File Versions to the current release."
'Write each AssemblyFile manually.
AnalyzeReturnCode(UpdateAssemblyFileVersion(Cloud_FolderName_SampleLiveSync))
'For these the Project name is the same as the folder name.
AnalyzeReturnCode(UpdateAssemblyFileVersion(Cloud_ProjectName_CloudApiPublic))
AnalyzeReturnCode(UpdateAssemblyFileVersion(Cloud_ProjectName_CloudSetupSdkSyncSampleSupport))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'5b)  For CloudSetupSdkSyncSample, click Project Assistant, and then Application Information.  Set the application version.
WScript.Echo ">>>> STEP"
WScript.Echo "5b)  For CloudSetupSdkSyncSample edit the current version.  This is done manually by going to the Project Assistant, and then Application Information.  Set the application version."
AnalyzeReturnCode(UpdateAssemblyFileVersion_ISLProject(Cloud_ProjectName_CloudSetupSdkSyncSample))
WScript.Echo ""
WScript.Echo ""


'If the Signing key was not located, the user can still select to run the AutoBuild without signing, this would occur for a local Build.
If Signing_UseKey  Then
'''
''STEP
'''
'6)  ReBuild ZAddLicenseFiles to move the key file to the correct projects for signing.
WScript.Echo ">>>> STEP"
WScript.Echo "6)  ReBuild ZAddLicenseFiles to move the key file to the correct projects for signing."
AnalyzeReturnCode(oShell.run( """" & VisualStudio_Path & VisualStudio_EXE & """   """ & Source_FolderPath & Cloud_Solution_Filename & """ /rebuild Debug /project """ & Source_FolderPath & CloudPlatformCodeSigning_MoveKeyProject & """", 1, true))
WScript.Echo ""
WScript.Echo ""


''
''STEP
'''
'7)  Set these projects for signing, CloudApiPublic, CloudSetupSdkSyncSampleSupport and CloudSetupSdkSyncSample.  The key CloudPlatformCodeSigning.pfx is located at C:\CertBackup\CloudSigning   
WScript.Echo ">>>> STEP"
WScript.Echo "7)  Set these projects for signing CloudApiPublic, CloudSetupSdkSyncSampleSupport and CloudSetupSdkSyncSample."
'For these the Project name is the same as the folder name.
AnalyzeReturnCode(SignAssembly(Cloud_ProjectName_CloudApiPublic))
AnalyzeReturnCode(SignAssembly(Cloud_ProjectName_CloudSetupSdkSyncSampleSupport))
WScript.Echo ""
WScript.Echo ""


''
''STEP
'''
'8)  For signing CloudSetupSdkSyncSample 
WScript.Echo ">>>> STEP"
WScript.Echo "8)  Signing the Install Shield project CloudSetupSdkSyncSample."
AnalyzeReturnCode(SignAssembly_ISLProject(Cloud_ProjectName_CloudSetupSdkSyncSample))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'9)  Push the release branch to remote.
'TODO: not sure if this should be completed here, this instruction appears twice in the document.
WScript.Echo ">>>> STEP"
WScript.Echo "9)  Push the release branch to remote."
'AnalyzeReturnCode(oShell.run( "git push  -u origin " & Release_BranchString, 1, true))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'10)  Run Rebuild on the project ZGacUninstall.  This project contains only a cmd file.  Just run the .cmd file.  This will remove all of our old DLLs from the GAC.
WScript.Echo ">>>> STEP"
WScript.Echo "10)  Run the GacUninstall.cmd file.  This will remove all of the old .dlls from the GAC."
AnalyzeReturnCode(oShell.run( Source_FolderPath & "GacUninstall.cmd", 1, true))
WScript.Echo ""
WScript.Echo ""
Else
'''
''STEP
'''
'6-10)  Signing has been turned off.  All Signing steps have been skipped (Steps 6-10).
WScript.Echo ">>>> STEP"
WScript.Echo "6-10)  Signing has been turned off.  All Signing steps have been skipped (Steps 6-10)."
WScript.Echo "SKIPPED"
WScript.Echo ""
WScript.Echo ""
End If


'''
''STEP
'''
'11a)  In Debug solution configuration, choose the clean solution selection from the build menu.  Check that it succeeded.
WScript.Echo ">>>> STEP"
WScript.Echo "11a)  Clean the solution using the Debug solution configuration."
AnalyzeReturnCode(oShell.run( """" & VisualStudio_Path & VisualStudio_EXE & """   """ & Source_FolderPath & Cloud_Solution_Filename & """ /clean Debug", 1, true))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'11b)  In ReleaseSampleAppOnly solution configuration, choose the clean solution selection from the build menu.  Check that it succeeded.
WScript.Echo ">>>> STEP"
WScript.Echo "11b)  Clean the solution using the ReleaseSampleAppOnly solution configuration."
AnalyzeReturnCode(oShell.run( """" & VisualStudio_Path & VisualStudio_EXE & """   """ & Source_FolderPath & Cloud_Solution_Filename & """ /clean ReleaseSampleAppOnly", 1, true))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'12a)  Build CloudApiPublic project.
WScript.Echo ">>>> STEP"
WScript.Echo "12a)  Build CloudApiPublic project."
AnalyzeReturnCode(oShell.run( """" & VisualStudio_Path & VisualStudio_EXE & """   """ & Source_FolderPath & Cloud_Solution_Filename & """ /build ReleaseSampleAppOnly  /project """ & Source_FolderPath & Cloud_ProjectName_CloudApiPublic & "\" & Cloud_ProjectName_CloudApiPublic & ".csproj""", 1, true))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'12b)  Set the password for the ObfuscateCloud project.
WScript.Echo ">>>> STEP"
WScript.Echo "12b)  Set the password for the ObfuscateCloud project."
AnalyzeReturnCode( SetPasswordForObfuscation( Obfuscate_ProjectName ))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'12c)  Build the ObfuscateCloud project.
WScript.Echo ">>>> STEP"
WScript.Echo "12c)  Build the ObfuscateCloud project."
AnalyzeReturnCode(oShell.run( """" & VisualStudio_Path & VisualStudio_EXE & """   """ & Source_FolderPath & Cloud_Solution_Filename & """ /build ReleaseSampleAppOnly  /project """ & Source_FolderPath & Cloud_ProjectName_ObfuscateCloud & "\" & Cloud_ProjectName_ObfuscateCloud & ".dsoprojx""", 1, true))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'13a)  Build Sample-Live-Sync project in Debug.
WScript.Echo ">>>> STEP"
WScript.Echo "13a)  Build Sample-Live-Sync project in Debug."
AnalyzeReturnCode(oShell.run( """" & VisualStudio_Path & VisualStudio_EXE & """   """ & Source_FolderPath & Cloud_Solution_Filename & """ /build Debug  /project """ & Source_FolderPath & Cloud_FolderName_SampleLiveSync & "\" & Cloud_ProjectName_SampleLiveSync & ".csproj""", 1, true))
WScript.Echo ""
WScript.Echo ""

 
'''
''STEP
'''
'13b)  Build Sample-Live-Sync project in ReleaseSampleAppOnly.
WScript.Echo ">>>> STEP"
WScript.Echo "13b)  Build Sample-Live-Sync project in ReleaseSampleOnly."
AnalyzeReturnCode(oShell.run( """" & VisualStudio_Path & VisualStudio_EXE & """   """ & Source_FolderPath & Cloud_Solution_Filename & """ /build ReleaseSampleAppOnly  /project """ & Source_FolderPath & Cloud_FolderName_SampleLiveSync & "\" & Cloud_ProjectName_SampleLiveSync & ".csproj""", 1, true))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'14)  Build the installer project CloudSetupSdkSyncSample project.
WScript.Echo ">>>> STEP"
WScript.Echo "14)  Build the Install Shield project CloudSetupSdkSyncSample project."
AnalyzeReturnCode(oShell.run( """" & VisualStudio_Path & VisualStudio_EXE & """   """ & Source_FolderPath & Cloud_Solution_Filename & """ /build ReleaseSampleAppOnly  /project """ & Source_FolderPath & Cloud_ProjectName_CloudSetupSdkSyncSample & "\" & Cloud_ProjectName_CloudSetupSdkSyncSample & ".isproj""  /out MMInstallShieldLog3.log  ", 1, true))
WScript.Echo ""
WScript.Echo ""


'If the Signing key was not located, the user can still select to run the AutoBuild without signing, this would occur for a local Build.
If Signing_UseKey  Then
'''
''STEP
'''
'15)  ReBuild ZDeleteLicenseKeyFiles to remove the key files.
WScript.Echo ">>>> STEP"
WScript.Echo "15b)  ReBuild ZDeleteLicenseKeyFiles to remove the key files."
AnalyzeReturnCode(oShell.run( """" & VisualStudio_Path & VisualStudio_EXE & """   """ & Source_FolderPath & Cloud_Solution_Filename & """ /rebuild Debug /project """ & Source_FolderPath & CloudPlatformCodeSigning_RemoveKeyProject & """", 1, true))
WScript.Echo ""
WScript.Echo ""
Else
'''
''STEP
'''
'15)  Signing has been turned off.  All Signing steps have been skipped (Step 15).
WScript.Echo ">>>> STEP"
WScript.Echo "15)  Signing has been turned off.  All Signing steps have been skipped (Step 15)."
WScript.Echo "SKIPPED"
WScript.Echo ""
WScript.Echo ""
End If


'''
''STEP
'''
'16)  Zip CloudSdkSetup.exe into a zip file with the naming convention CloudSDKv0_1_2_0.zip.  
WScript.Echo ">>>> STEP"
WScript.Echo "16)  Zip the CloudSdkSetup.exe file."
AnalyzeReturnCode(oShell.run( "7z.exe a -tzip """ & CloudSDK_Install_Path & CloudSDK_InstallZIP & """ """ & CloudSDK_Install_Path & CloudSDK_InstallEXE & """", 1, true))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'17)  Copy all of the necessary files into the ArchivedCloudSdkReleases folder.
WScript.Echo ">>>> STEP"
WScript.Echo "17)  Copy all of the necessary files into the ArchivedCloudSdkReleases folder."
AnalyzeReturnCode( CopyFilesToArchivedRelease( Source_FolderPath ))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'18)  Copy the ArchivedRelease folder onto the Build Server for backup.  (\\10.3.0.28\Builds\Windows)
WScript.Echo ">>>> STEP"
WScript.Echo "18)  Copy the ArchivedRelease folder onto the Build Server for backup.  (\\10.3.0.28\Builds\Windows)"
'This method is recursive so we have to pass in a variable for the first call
'even if it is an empty string.
AnalyzeReturnCode( CopyArchivedFolderToServerForBackup( "" ))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'19a)  Push the release branch to remote. 
WScript.Echo ">>>> STEP"
WScript.Echo "19a)  Push the release branch to remote."
'AnalyzeReturnCode(oShell.run( "git push  -u origin " & Release_BranchString, 1, true))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'19b)  Delete a local and remote tag named 12345.
WScript.Echo ">>>> STEP"
WScript.Echo "19b)  Delete a local and remote tag named " & Cloud_Solution_TagName & "."
'AnalyzeReturnCode(oShell.run( "git tag -d " & Cloud_Solution_TagName, 1, true))
'AnalyzeReturnCode(oShell.run( "git push origin :refs/tags/" & Cloud_Solution_TagName, 1, true))
WScript.Echo ""
WScript.Echo ""


'''
''STEP
'''
'19c)  Create a local tag named 12345 and push it to remote.
WScript.Echo ">>>> STEP"
WScript.Echo "19c)  Create a local tag named " & Cloud_Solution_TagName & " and push it to remote."
'confirm that all git commands are correct.
'AnalyzeReturnCode(oShell.run( "git tag " & Cloud_Solution_TagName, 1, true))
'AnalyzeReturnCode(oShell.run( "git push –tags", 1, true))
WScript.Echo ""
WScript.Echo ""


'Close Windows CMD 
Set oShell = Nothing
'
'Done
WScript.Echo ""
WScript.Echo ""
WScript.Echo ""
WScript.Echo ""
WScript.Echo "The Citrix Cloud Windows SDK AutoBuild has completed."
WScript.Echo ""
WScript.Echo ""
WScript.Echo ">>>> Finished"
'''
''FINISHED
'''


'
'Methods
Function RemovePdbAndZipFiles( stringInFolderName )
    'Do not remove any pdb files from the 3rd Party directory
    'as they would have to be rebuilt separately.
    If InStr( stringInFolderName, "3rdParty" ) > 0 Then
        Exit Function 
    End If
    If Trim(stringInFolderName) = "" Then
        WScript.Echo "No directory was entered."
        WScript.Quit()
    Else
        If FSO.FolderExists(stringInFolderName) Then
            ' Continue program
        Else
            WScript.Echo "Directory does not exist, " & stringInFolderName
            WScript.Quit()
        End If
    End If

    Set Source_Folder = FSO.GetFolder(stringInFolderName)

    For Each currFile In Source_Folder.Files
        If Right(currFile.name, 4) = ".pdb" Or Right(currFile.name, 4) = ".zip" Then
            FSO.DeleteFile(stringInFolderName & "\" & currFile.name)
        End If
    Next
        
    'Recursive calls to SubFolders
    For Each currFolder In Source_Folder.SubFolders
        RemovePdbAndZipFiles currFolder.Path   
    Next

    RemovePdbAndZipFiles = 0

End Function


Function CopyFilesToArchivedRelease( stringInFolderName )

    'Create the necessary directories.
    If Not FSO.FolderExists( CloudSDK_Install_ArchivedReleaseFolder ) Then
        FSO.CreateFolder CloudSDK_Install_ArchivedReleaseFolder 
    End If
    
    'Copy the exe and zip files.
    Dim returnValueAsInt
    returnValueAsInt = FSO.CopyFile( CloudSDK_Install_Path & CloudSDK_InstallEXE, CloudSDK_Install_ArchivedReleaseFolder & CloudSDK_InstallEXE, true)
    If returnValueAsInt <> 0 Then
        CopyFilesToArchivedRelease = returnValueAsInt
        Exit Function
    End If
    returnValueAsInt = FSO.CopyFile( CloudSDK_Install_Path & CloudSDK_InstallZIP, CloudSDK_Install_ArchivedReleaseFolder & CloudSDK_InstallZIP, true)
    If returnValueAsInt <> 0 Then
        CopyFilesToArchivedRelease = returnValueAsInt
        Exit Function
    End If
    
    'Create the necessary directories.
    If Not FSO.FolderExists( CloudSDK_Install_ArchivedReleaseFolder & "DSO\" ) Then
        FSO.CreateFolder CloudSDK_Install_ArchivedReleaseFolder & "DSO\" 
    End If
    
    If Not FSO.FolderExists( CloudSDK_Install_ArchivedReleaseFolder & "DSO\Release" ) Then
        FSO.CreateFolder CloudSDK_Install_ArchivedReleaseFolder & "DSO\Release" 
    End If
    
    'Copy the files from the Release folder in the ObfuscateCloud project.
    returnValueAsInt = CopyObfuscateCloudFilesToArchivedRelease()
    'If we failed, we are done.
    If returnValueAsInt <> 0 Then
        CopyFilesToArchivedRelease = returnValueAsInt
        Exit Function
    End If
    
    'Create the necessary directories.
    If Not FSO.FolderExists( CloudSDK_Install_ArchivedReleaseFolder & "PDBs\" ) Then
        FSO.CreateFolder CloudSDK_Install_ArchivedReleaseFolder & "PDBs\" 
    End If

    'Copy all of the PDB files for debugging.
    CopyFilesToArchivedRelease = CopyPdbFilesToArchivedRelease( stringInFolderName )

End Function

Function CopyObfuscateCloudFilesToArchivedRelease() 
    Set Source_Folder = FSO.GetFolder(Source_FolderPath & Cloud_ProjectName_ObfuscateCloud & "\bin\Release\")
    For Each currFile In Source_Folder.Files
        FSO.CopyFile Source_FolderPath & Cloud_ProjectName_ObfuscateCloud & "\bin\Release\" & currFile.name, CloudSDK_Install_ArchivedReleaseFolder & "DSO\Release\" & currFile.name, true
    Next
End Function

Function CopyPdbFilesToArchivedRelease( stringInFolderName ) 
    'Do not move any pdb files from the ObfuscateCloud directory
    'as they will be moved separately.
    If InStr( stringInFolderName, "ObfuscateCloud" ) > 0 Then
        Exit Function 
    End If
    If Trim(stringInFolderName) = "" Then
        WScript.Echo "No directory was entered."
        WScript.Quit()
    Else
        If FSO.FolderExists(stringInFolderName) Then
            ' Continue program
        Else
            WScript.Echo "Directory does not exist, " & Source_FolderPath
            WScript.Quit()
        End If
    End If

    Set Source_Folder = FSO.GetFolder(stringInFolderName)
    
    For Each currFile In Source_Folder.Files
        If Right(currFile.name, 4) = ".pdb" Then
            FSO.CopyFile stringInFolderName & "\" & currFile.name, CloudSDK_Install_ArchivedReleaseFolder & "PDBs\" & currFile.name, true
        End If
    Next

    'Recursive calls to SubFolders
    For Each currFolder In Source_Folder.SubFolders
        CopyPdbFilesToArchivedRelease currFolder.Path   
    Next

    'Success
    CopyPdbFilesToArchivedRelease = 0

End Function


Function CopyArchivedFolderToServerForBackup( stringInSubFolderName ) 
    'This method is recursive so we have to pass in a variable for the first call
    'even if it is an empty string.
    If Trim( stringInSubFolderName ) = "" Then
        'This is the first call to this method.
        'We must login to the Network to place
        'files on this drive.
        LoginToNetworkShareDrive()
        'Copy all files in the root folder.
        Set Source_Folder = FSO.GetFolder(CloudSDK_Install_ArchivedReleaseFolder)
        'Create destination directory if does not exist.
        If Not FSO.FolderExists( Server_RemoteStorageForBackup_Path & "\" & CloudSDK_Install_ArchivedReleaseFolderName ) Then
            FSO.CreateFolder Server_RemoteStorageForBackup_Path & "\" & CloudSDK_Install_ArchivedReleaseFolderName
        End If 
        'Copy all files.    
         For Each currFile In Source_Folder.Files
            FSO.CopyFile CloudSDK_Install_ArchivedReleaseFolder & currFile.name, Server_RemoteStorageForBackup_Path & "\" & CloudSDK_Install_ArchivedReleaseFolderName & "\" & currFile.name, true
        Next
    Else
        'For Sub Folders.
        Set Source_Folder = FSO.GetFolder(CloudSDK_Install_ArchivedReleaseFolder & stringInSubFolderName)
        'Create destination directory if does not exist.
        If Not FSO.FolderExists( Server_RemoteStorageForBackup_Path & "\" & CloudSDK_Install_ArchivedReleaseFolderName & "\" & stringInSubFolderName ) Then
            FSO.CreateFolder Server_RemoteStorageForBackup_Path & "\" & CloudSDK_Install_ArchivedReleaseFolderName & "\" & stringInSubFolderName
        End If 
        'Copy all files.    
        For Each currFile In Source_Folder.Files
            FSO.CopyFile CloudSDK_Install_ArchivedReleaseFolder & stringInSubFolderName & "\" & currFile.name, Server_RemoteStorageForBackup_Path & "\" & CloudSDK_Install_ArchivedReleaseFolderName & "\" & stringInSubFolderName & "\" & currFile.name, true
        Next
    End If

    'Recursive calls to all sub folders.
    For Each currFolder In Source_Folder.SubFolders
        'Grab the substring that is only the folder path relative to 
        'the ArchivedReleaseFolder.     
        CopyArchivedFolderToServerForBackup( Mid( currFolder.Path, ( Len( CloudSDK_Install_ArchivedReleaseFolder ) + 1 ), (Len(currFolder.Path ) - Len( CloudSDK_Install_ArchivedReleaseFolder )) ) )
    Next
    
    LogoutOfTheNetworkShareDrive()
    'Success
    CopyArchivedFolderToServerForBackup = 0
End Function

Function LoginToNetworkShareDrive()
    'The drive where the ArchivedRelease folder is saved needs
    'Network credentials to place files on it.
    'Login here.
    Set WSNetworkObject = CreateObject("WScript.Network")
    WSNetworkObject.MapNetworkDrive "", Server_RemoteStorageForBackup_PathDriveOnly, False, Server_RemoteStorageForBackup_UserName, Server_RemoteStorageForBackup_Password
End Function

Function LogoutOfTheNetworkShareDrive()
    'The drive where the ArchivedRelease folder is saved needs
    'Network credentials to place files on it.
    'Logout here, now that we are done copying files.
    If Not WSNetworkObject Is Nothing Then 
        WSNetworkObject.RemoveNetworkDrive Server_RemoteStorageForBackup_PathDriveOnly, True, False
    End If
    Set WSNetworkObject = Nothing
End Function


Function UpdateAssemblyFileVersion( ProjectFolderNameAsString )
    'VBScript open file attributes
    '<filename>, IOMode (1=Read,2=write,8=Append), Create (true,false), Format (-2=System Default,-1=Unicode,0=ASCII)
    Dim FileToRead, FileToWrite

    Set FileToWrite = FSO.OpenTextFile(Source_FolderPath & ProjectFolderNameAsString & Version_AssemblyInfo_PathAndName_TEMP, 2, True)
    Set FileToRead = FSO.OpenTextFile(Source_FolderPath & ProjectFolderNameAsString & Version_AssemblyInfo_PathAndName, 1)
    Dim strLine
    'Loop through the file
    Do While Not FileToRead.AtEndOfStream
        strLine = FileToRead.ReadLine()
        If InStr(strLine, "//") > 0 Then
            'We do not need to alter any comment lines.
            'Just keep the old line.
            FileToWrite.WriteLine(strLine)
        Else
            'Write all lines to the file except the Version lines.
            If InStr(strLine, "[assembly: AssemblyVersion") > 0 Then
                FileToWrite.WriteLine("[assembly: AssemblyVersion(""" & Cloud_Solution_CurrentVersion & """)]")
            Else
                'There are two slightly different Version lines.
                If InStr(strLine, "[assembly: AssemblyFileVersion") > 0 Then
                    FileToWrite.WriteLine("[assembly: AssemblyFileVersion(""" & Cloud_Solution_CurrentVersion & """)]")
                Else
                    'Just keep the old line.
                    FileToWrite.WriteLine(strLine)
                End If
            End If
        End If
    Loop

    FileToRead.Close()
    Set FileToRead = Nothing
    'Rename the newly created file to the old file.
    FSO.CopyFile Source_FolderPath & ProjectFolderNameAsString & Version_AssemblyInfo_PathAndName_TEMP, Source_FolderPath & ProjectFolderNameAsString & Version_AssemblyInfo_PathAndName, true
    FileToWrite.Close()
    Set FileToWrite = Nothing
    'Delete the temp file.
    FSO.DeleteFile Source_FolderPath & ProjectFolderNameAsString  & Version_AssemblyInfo_PathAndName_TEMP

    'Success
    UpdateAssemblyFileVersion = 0
End Function


Function UpdateAssemblyFileVersion_ISLProject( ProjectNameAsString )
    Dim FileToRead, FileToWrite
    'For an isproj file the file name is always the same as the Folder Name.
    Set FileToWrite = FSO.OpenTextFile(Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & "Temp.isproj", 2, True)
    Set FileToRead = FSO.OpenTextFile(Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & ".isproj", 1)
    Dim strLine
    'Loop through the file
    Do While Not FileToRead.AtEndOfStream
        strLine = FileToRead.ReadLine()
        'Write all lines to the file except the Version lines.
        'TODO: Could add additional test to make sure you are in the right table.
        '<table name="Property">
        If InStr(strLine, "<row><td>ProductVersion</td><td>") > 0 Then
            'TODO: is this version formatted correctly, appears slightly different in the VS settings.
            FileToWrite.WriteLine("<row><td>ProductVersion</td><td>" & Cloud_Solution_CurrentVersion & "</td><td/></row>")
        Else
            'Just keep the old line.
            FileToWrite.WriteLine(strLine)
        End If
    Loop

    FileToRead.Close()
    Set FileToRead = Nothing
    'Rename the newly created file to the old file.
    FSO.CopyFile Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & "Temp.isproj", Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & ".isproj", true
    FileToWrite.Close()
    Set FileToWrite = Nothing
    'Delete the temp file.
    FSO.DeleteFile Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & "Temp.isproj"

    'Success
    UpdateAssemblyFileVersion_ISLProject = 0
End Function


Function SignAssembly(ProjectNameAsString)

    Dim FileToRead, FileToWrite

    'For a csproj file the file name is always the same as the Folder Name.
    Set FileToWrite = FSO.OpenTextFile(Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & "Temp.csproj", 2, True)
    Set FileToRead = FSO.OpenTextFile(Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & ".csproj", 1)
    Dim strLine, strLine_NextLine
    Dim bool_SignAssembly_TagFound : bool_SignAssembly_TagFound = False
    Dim bool_AssemblyKeyFile_TagFound : bool_AssemblyKeyFile_TagFound = False
    'Loop through the file
    Do While Not FileToRead.AtEndOfStream
        strLine = FileToRead.ReadLine()
        'It is possible that the previous ReadLine was the last line in the file. 
        'If we do not do this check then we can read past the end of the file.
        If Not FileToRead.AtEndOfStream Then
            strLine_NextLine = FileToRead.ReadLine()
        Else
            FileToWrite.WriteLine(strLine)
            Exit Do
        End If 
        'Write all lines to the file except the Version lines.
        If InStr(strLine, "<PropertyGroup>") > 0 And InStr(strLine_NextLine, "<SignAssembly>") > 0 Then
            bool_SignAssembly_TagFound = True
            FileToWrite.WriteLine("<PropertyGroup>")
            FileToWrite.WriteLine("<SignAssembly>true</SignAssembly>")
            FileToWrite.WriteLine("</PropertyGroup>")
            'If the </SignAssembly> tag was on a different line then the opening
            '<SignAssembly> tag then the next line should be the </PropertyGroup> tag.
            strLine = FileToRead.ReadLine()
            If Not InStr(strLine, "</PropertyGroup>") > 0 Then
                 'Skip the </PropertyGroup> tag.
                 FileToRead.ReadLine()
            End If 
        Else
            'There are two Properties that have to be set.
            If InStr(strLine, "<PropertyGroup>") > 0 And InStr(strLine_NextLine, "<AssemblyOriginatorKeyFile>") > 0 Then
                bool_AssemblyKeyFile_TagFound = True
                FileToWrite.WriteLine("<PropertyGroup>")
                FileToWrite.WriteLine("<AssemblyOriginatorKeyFile>" & CloudPlatformCodeSigning_File & "</AssemblyOriginatorKeyFile>")
                FileToWrite.WriteLine("</PropertyGroup>")
                'If the </AssemblyOriginatorKeyFile> tag was on a different line then the opening
                '<AssemblyOriginatorKeyFile> tag then the next line should be the </PropertyGroup> tag.
                strLine = FileToRead.ReadLine()
                If Not InStr(strLine, "</PropertyGroup>") > 0   Then
                    'Skip the </PropertyGroup> tag.
                     FileToRead.ReadLine()
                End If 
            Else
                'Just keep the old lines.
                FileToWrite.WriteLine(strLine)
                FileToWrite.WriteLine(strLine_NextLine)
            End If
        End If
    Loop

    'If the tags were not found we will have to insert them. 
    If Not bool_AssemblyKeyFile_TagFound Or Not bool_SignAssembly_TagFound Then
        'Close the files down and reopen them so we can restart this proccess.
        FileToRead.Close()
        Set FileToRead = Nothing
        FileToWrite.Close()
        Set FileToWrite = Nothing
        Set FileToWrite = FSO.OpenTextFile(Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & "Temp.csproj", 2, True)
        Set FileToRead = FSO.OpenTextFile(Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & ".csproj", 1)
        'Loop through the file
        Do While Not FileToRead.AtEndOfStream
            strLine = FileToRead.ReadLine()
            'Insert the Version lines after the opening tags.         
            If InStr(strLine, "<Project") > 0 Then
                'We still need the opening <Project tag line.
                FileToWrite.WriteLine(strLine)
                If Not bool_SignAssembly_TagFound Then
                    bool_SignAssembly_TagFound = True
                    FileToWrite.WriteLine("<PropertyGroup>")
                    FileToWrite.WriteLine("<SignAssembly>true</SignAssembly>")
                    FileToWrite.WriteLine("</PropertyGroup>")
                End If
                'There are two Properties that have to be set.
                If Not bool_AssemblyKeyFile_TagFound Then
                    bool_AssemblyKeyFile_TagFound = True
                    FileToWrite.WriteLine("<PropertyGroup>")
                    FileToWrite.WriteLine("<AssemblyOriginatorKeyFile>" & CloudPlatformCodeSigning_File & "</AssemblyOriginatorKeyFile>")
                    FileToWrite.WriteLine("</PropertyGroup>")
                End If
            Else
                'Just keep the old line.
                FileToWrite.WriteLine(strLine)
            End If
        Loop
    End If

    FileToRead.Close()
    Set FileToRead = Nothing
    'Rename the newly created file to the old file.
    FSO.CopyFile Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & "Temp.csproj", Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & ".csproj", true
    FileToWrite.Close()
    Set FileToWrite = Nothing
    'Delete the temp file.
    FSO.DeleteFile Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & "Temp.csproj"

    'Success
    SignAssembly = 0
End Function


Function SignAssembly_ISLProject(ProjectNameAsString)
    Dim FileToRead, FileToWrite
    'For this isproj file the file name is the same as the Folder Name.
    Set FileToWrite = FSO.OpenTextFile(Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & "Temp.isproj", 2, True)
    Set FileToRead = FSO.OpenTextFile(Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & ".isproj", 1)
    Dim strLine
    'Loop through the file
    Do While Not FileToRead.AtEndOfStream
        strLine = FileToRead.ReadLine()
        'Write all lines to the file except the Version lines.
        'TODO: Could add additional test to make sure you are in the right table.
        '<table name="Property">
        If InStr(strLine, "<row><td>ProductVersion</td><td>") > 0 Then
            'Note: To set the Setup.exe and Windows Installer Package set this to <td>536980511</td>, the other settings have large int values to, I have not been able to decipher the code or algorithm for these, for now will use the int hopefully it is a constant.
            FileToWrite.WriteLine("<row><td>SingleImage</td><td>Express</td><td>c:\CloudSetupSdkSyncSample</td><td>PackageName</td><td>1</td><td>1033</td><td>0</td><td>1</td><td>Intel</td><td/><td>1033</td><td>0</td><td>0</td><td>0</td><td>0</td><td/><td>0</td><td/><td>MediaLocation</td><td/><td>http://</td><td/><td>" & CloudPlatformCodeSigning_Path & CloudPlatformCodeSigning_File & "</td><td/><td>Copyright (C) Cloud.com. All Rights Reserved.</td><td>536980511</td><td/><td/><td/><td>3</td></row>")
        Else
            'Just keep the old line.
            FileToWrite.WriteLine(strLine)
        End If
    Loop

    'Do not need to insert lines if the tags are not found.  These should always be there even if they do not contain data.

    FileToRead.Close()
    Set FileToRead = Nothing
    'Rename the newly created file to the old file.
    FSO.CopyFile Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & "Temp.isproj", Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & ".isproj", true
    FileToWrite.Close()
    Set FileToWrite = Nothing
    'Delete the temp file.
    FSO.DeleteFile Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & "Temp.isproj"
        
    'Success
    SignAssembly_ISLProject = 0
End Function


Function SetPasswordForObfuscation(ProjectNameAsString)
    Dim FileToRead, FileToWrite
    'For a csproj file the file name is always the same as the Folder Name.
    Set FileToWrite = FSO.OpenTextFile(Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & "Temp.dsoprojx", 2, True)
    Set FileToRead = FSO.OpenTextFile(Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & ".dsoprojx", 1)
    Dim strLine, strLine_NextLine
    Dim bool_DeepSea_TagFound : bool_DeepSea_TagFound = False
    'Loop through the file
    Do While Not FileToRead.AtEndOfStream
        strLine = FileToRead.ReadLine()
        'Loop till we find the KeyPassword tag.
        If InStr(strLine, "<KeyPassword>") > 0 Then
            bool_DeepSea_TagFound = True
            FileToWrite.WriteLine("<KeyPassword>" & CloudPlatformCodeSigning_Password & "</KeyPassword>")
            'If the </KeyPassword> tag was on a different line then the opening
            '<KeyPassword> tag then the next line should be the </KeyPassword> tag.
            strLine = FileToRead.ReadLine()
            If InStr(strLine, "</KeyPassword>") > 0 Then
                'Skip the </KeyPassword> tag.
            Else
                'Just keep the old line.
                FileToWrite.WriteLine(strLine)
            End If 
        Else
            'Just keep the old line.
            FileToWrite.WriteLine(strLine)
        End If
    Loop

    FileToRead.Close()
    Set FileToRead = Nothing
    'Rename the newly created file to the old file.
    FSO.CopyFile Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & "Temp.dsoprojx", Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & ".dsoprojx", true
    FileToWrite.Close()
    Set FileToWrite = Nothing
    'Delte the temp file.
    FSO.DeleteFile Source_FolderPath & ProjectNameAsString & "\" & ProjectNameAsString & "Temp.dsoprojx"

    'Success
    SetPasswordForObfuscation = 0
End Function


Function InsertPoundDefineProductionServerName()
    Dim FileToRead, FileToWrite
    Set FileToWrite = FSO.OpenTextFile(Source_FolderPath & ServerNameFile_CLDefinitions_TEMP, 2, True)
    Set FileToRead = FSO.OpenTextFile(Source_FolderPath & ServerNameFile_CLDefinitions, 1)
    Dim strLine
    'Loop through the file
    Do While Not FileToRead.AtEndOfStream
        strLine = FileToRead.ReadLine()
        'Add the #define to the correct line.
        If InStr(strLine, "// Back end definitions") > 0 Then
            FileToWrite.WriteLine(strLine)
            strLine = FileToRead.ReadLine()
            'If the #define line is already there, do not add it.
            If InStr(strLine, "#define PRODUCTION_BACKEND    // cloud.com") > 0 Then
                FileToWrite.WriteLine(strLine)            
            Else
                FileToWrite.WriteLine("#define PRODUCTION_BACKEND    // cloud.com")
            End If 
        Else
            'Just keep the old line.
            FileToWrite.WriteLine(strLine)
        End If
    Loop

    FileToRead.Close()
    Set FileToRead = Nothing
    'Rename the newly created file to the old file.
    FSO.CopyFile Source_FolderPath & ServerNameFile_CLDefinitions_TEMP, Source_FolderPath & ServerNameFile_CLDefinitions, true
    FileToWrite.Close()
    Set FileToWrite = Nothing
    'Delete the temp file.
    FSO.DeleteFile Source_FolderPath & ServerNameFile_CLDefinitions_TEMP
    
    'Success
    InsertPoundDefineProductionServerName = 0
End Function


Function FindEndOfPropertyGroup( FileToReadIn, FileToWriteIn )
    'The xml in the csproj file contains a Property group for each SLN configuration.
    'Before you can close this property group you need to get past all of the Deep Sea tags.
    'Sometimes the Deep Sea tags do not open and close on the same line, so we must account for that here.
    Dim tempStringFromCSProjFile
    tempStringFromCSProjFile = FileToReadIn.ReadLine()
    'Loop till we get to the last DeepSea tag.
    Do While InStr( tempStringFromCSProjFile, "DeepSea" ) > 0
        tempStringFromCSProjFile = FileToReadIn.ReadLine()
    Loop
    'The last line read will not be a DeepSea line so we need to keep it.
    FileToWriteIn.WriteLine( tempStringFromCSProjFile )
End Function 


Function AnalyzeReturnCode( returnCodeIn )
    If returnCodeIn = 0 Then
        WScript.Echo "PASS"
    Else
        WScript.Echo "FAIL"
        WScript.Echo "There was an error processing this step."
        WScript.Echo "The step Exited with an error code of " & returnCodeIn
        WScript.Echo ""
        WScript.Echo ""
        WScript.Echo "The AutoBuild will now EXIT."
        WScript.Echo ""
        WScript.Echo ""
        WScript.Quit()
    End If
End Function
