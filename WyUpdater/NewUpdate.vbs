    option explicit
    '  NewUpdate.vbs
    '  Cloud Windows
    '
    '  Created by BobS.
    '  Copyright (c) Cloud.com. All rights reserved.
    '
    ' This script aids in the building of a new win-client update.
    '
    ' Parameters: none
    '
    ' SETUP:
    '  o Add the directory containing this script file to your path.  This file resides in the win-client\WyUpdater directory.
    '  o Add an environment variable (WINCLIENTPATH) to point to your win-client directory.  e.g.: set WINCLIENTPATH=c:\source\projects\win-client
    '  o Add an environment variable (WINCLIENTFTPROOTPATH) to provide the root directory of your local FTP root directory.  e.g.: set WINCLIENTFTPROOTPATH=c:\FtpRoot
    '  o Set the *.vbs file type association to use the program c:\windows\system32\cscript.exe.
    '
    ' USAGE:
    '  In a command window, type "NewUpdate".
 
    ' Constants  
    Const ForReading = 1
    Const ForWriting = 2
    Const ForAppending = 8
    Const OverwriteExisting = true
   
    ' Global variables
    Dim shouldTrace
    Dim logFileFullPath
    Dim objFileSys
    Dim objShell
    Dim programFilesX86Path
    Dim programFilesPath
    Dim commonProgramFilesX86Path
    Dim commonProgramFilesPath
    Dim programFilesX86CloudPath
    Dim commonProgramFilesX86CloudPath
    Dim commonProgramFilesCloudPath
    Dim winClientPath
    Dim wyUpdaterPath
    Dim wyUpdaterProgramFilesPath
    Dim wyUpdaterCommonFiles32BitPath
    Dim wyUpdaterCommonFiles64BitPath
    Dim wyBuildPath
    Dim version
    Dim versionUnderscore
    Dim ftpRootPath
   
    ' Trace function
    Sub WriteLog(LogMessage)
        Dim objLogFile
    
        If shouldTrace Then
           'wscript.echo Now() & ": " & LogMessage
           Set objLogFile = objFileSys.OpenTextFile(logFileFullPath, ForAppending, TRUE)
           objLogFile.Writeline(Now() & ": " & LogMessage)
           objLogFile.Close
           set objLogFile = Nothing
        End If
    End Sub
    
    ' Create the wyBuild XML file
    sub CreateXmlFile(parmVersion, parmNewVersion, parmChanges, parmCloudProgramFilesX86Path, _
                      parmCommonProgramFilesX86Path, parmCommonProgramFilesPath, _
                      parmOutputDirPath, parmOutputFilenameExt)
        Dim xmlDoc
        Dim objRoot
        Dim objAddVersion
        Dim objVersion
        Dim objChanges
        Dim tempFile
        Dim objInheritPrevRegistry
        Dim objInheritPrevActions
        Dim objFiles
        Dim objFolder
        
        WriteLog("NewUpdate: CreateXmlFile: Entry. parmVersion: <" & parmVersion & _
                                                   ">. parmNewVersion: <" & parmNewVersion & _
                                                   ">. parmChanges: <" & parmChanges & _
                                                   ">. parmCloudProgramFilesX86Path: <" & parmCloudProgramFilesX86Path & _
                                                   ">. parmCommonProgramFilesX86Path: <" & parmCommonProgramFilesX86Path & _
                                                   ">. parmCommonProgramFilesPath: <" & parmCommonProgramFilesPath & _
                                                   ">. parmOutputDirPath: <" & parmOutputDirPath & _
                                                   ">. parmOutputFilenameExt: <" & parmOutputFilenameExt & ">.")
                                                   
        ' Delete the target file if it is present.
        tempFile = parmOutputDirPath & parmOutputFilenameExt
        If objFileSys.FileExists(tempFile) Then
            WriteLog("NewUpdate: CreateXmlFile: Delete the target file: " & tempFile)
            objFileSys.DeleteFile tempFile, True
        End If
            
        WriteLog("NewUpdate: CreateXmlFile: Create the XMLDOM object.")
        Set xmlDoc = CreateObject("Microsoft.XMLDOM")  
          
        WriteLog("NewUpdate: CreateXmlFile: Add 'Versions'.")
        Set objRoot = xmlDoc.createElement("Versions")  
        xmlDoc.appendChild objRoot  
        
        WriteLog("NewUpdate: CreateXmlFile: Add 'AddVersion'.")
        Set objAddVersion = xmlDoc.createElement("AddVersion") 
        if parmNewVersion = false then
            objAddVersion.setAttribute "overwrite", "true"
        end if
        objRoot.appendChild objAddVersion 
          
        WriteLog("NewUpdate: CreateXmlFile: Add 'Version'.")
        Set objVersion = xmlDoc.createElement("Version")  
        objVersion.Text = parmVersion
        objAddVersion.appendChild objVersion  
        
        WriteLog("NewUpdate: CreateXmlFile: Add 'Changes'.")
        Set objChanges = xmlDoc.createElement("Changes")  
        objChanges.Text = parmChanges
        objAddVersion.appendChild objChanges  
        
        WriteLog("NewUpdate: CreateXmlFile: Add 'InheritPrevRegistry'.")
        Set objInheritPrevRegistry = xmlDoc.createElement("InheritPrevRegistry")  
        objAddVersion.appendChild objInheritPrevRegistry  
        
        WriteLog("NewUpdate: CreateXmlFile: Add 'InheritPrevActions'.")
        Set objInheritPrevActions = xmlDoc.createElement("InheritPrevActions")  
        objAddVersion.appendChild objInheritPrevActions  
        
        ' Add the Cloud x86 program files
        WriteLog("NewUpdate: CreateXmlFile: Add Cloud x86 program files.")
        Set objFiles = xmlDoc.createElement("Files")  
        objFiles.setAttribute "dir", "basedir"
        
        WriteLog("NewUpdate: CreateXmlFile: Add 'Folder'.")
        Set objFolder = xmlDoc.createElement("Folder")
        objFolder.setAttribute "source", parmCloudProgramFilesX86Path
        
        objFiles.appendChild objFolder 
        
        WriteLog("NewUpdate: CreateXmlFile: Append to AddVersion.")
        objAddVersion.appendChild objFiles  
        
        ' Add the Cloud common files x86 files
        WriteLog("NewUpdate: CreateXmlFile: Add the Cloud common files x86 files.")
        Set objFiles = xmlDoc.createElement("Files")  
        objFiles.setAttribute "dir", "commonfilesx86"

        WriteLog("NewUpdate: CreateXmlFile: Add 'Folder'.")
        Set objFolder = xmlDoc.createElement("Folder")
        objFolder.setAttribute "source", parmCommonProgramFilesX86Path & "\Cloud.com"
        
        objFiles.appendChild objFolder 
        
        WriteLog("NewUpdate: CreateXmlFile: Append to AddVersion.")
        objAddVersion.appendChild objFiles  
        
        ' Add the Cloud common files x64 files
        WriteLog("NewUpdate: CreateXmlFile: Add the Cloud common files x64 files.")
        Set objFiles = xmlDoc.createElement("Files")  
        objFiles.setAttribute "dir", "commonfilesx64"

        WriteLog("NewUpdate: CreateXmlFile: Add 'Folder'.")
        Set objFolder = xmlDoc.createElement("Folder")
        objFolder.setAttribute "source", parmCommonProgramFilesPath & "\Cloud.com"
        
        objFiles.appendChild objFolder 
        
        WriteLog("NewUpdate: CreateXmlFile: Append to AddVersion.")
        objAddVersion.appendChild objFiles  
        
        WriteLog("NewUpdate: CreateXmlFile: Save the XML file to location: " & parmOutputDirPath & "\" & parmOutputFilenameExt)
        xmlDoc.Save parmOutputDirPath & "\" & parmOutputFilenameExt
        
        set xmlDoc = Nothing
        set objRoot = Nothing
        set objAddVersion = Nothing
        set objVersion = Nothing
        set objChanges = Nothing
        set tempFile = Nothing
        set objInheritPrevRegistry = Nothing
        set objInheritPrevActions = Nothing
        set objFiles = Nothing
        set objFolder = Nothing

    end sub

    ' Initialize global variables
    Set objFileSys = wscript.CreateObject("Scripting.FileSystemObject") 
    set objShell = createObject("WScript.Shell")   
    
    shouldTrace = true
    logFileFullPath = objFileSys.GetSpecialFolder(2) & "\CloudTrace.log"

    ' @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@  MULTILINE INPUT DIALOG @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@    
    'The function will open an IE window and prompt the user for input  using IE. Function returns the user input.   
     'Argument: [str] myPrompt: This text will be displayed as instruction to the user on the input form 
     'Returns: [str] The text that was entered as input.  
     '------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ 
     Function MultiLineInput( myPrompt ) 
     ' Based on script Written by Rob van der Woude         Version:             2.10 
     ' http://www.robvanderwoude.com                Last modified:       2010-09-28 
     ' Error handling code written by Denis St-Pierre 
     '2011-2-9 RC: Changed to allow support for multi line input, converted to pure VBS  and various other tweaks 
         Dim objIE 
         ' Create an IE object 
         fnDebug "IE Obj Creation starting" 
         Set objIE = CreateObject( "InternetExplorer.Application" ) 
         fnDebug "IE Obj Created" 
         objIE.Visible = True 
         ' Wait till IE is ready 
         Do While objIE.Busy 
            fnDebug "Waiting for IE to be ready" 
             wait 0,200 
         Loop 
         
         ' Specify some of the IE window's settings 
         fnDebug "Specify window settings" 
         objIE.Navigate "about:blank" 
         objIE.Document.Title = "Input required " & String( 150, "." ) 
         objIE.ToolBar        = False 
         objIE.Resizable      = True 
         objIE.StatusBar      = False 
         objIE.Width          = 425 
         objIE.Height         = 600 
         fnDebug "Window settings err: " & Err 
         
         ' Center the dialog window on the screen 
         fnDebug "Center the dialog" 
         With objIE.Document.ParentWindow.Screen 
             objIE.Left = (.AvailWidth  - objIE.Width ) \ 2 
             objIE.Top  = (.Availheight - objIE.Height) \ 2 
         End With 
         fnDebug "Window center err: " & Err 
         
         ' Wait till IE is ready 
         fnDebug "Wait until ready" 
         Do While objIE.Busy 
            fnDebug "." 
             wait 0,200 
         Loop
      
         'original single line input  
         '    objIE.Document.Body.InnerHTML = "<div align=""center""><p>" & myPrompt & "</p>" & vbCrLf _ 
         '                                  & "<p><input type=""text"" size=""20"" " & "id=""UserInput""></p>" & vbCrLf _ 
         '                                  & "<p><input type=""hidden"" id=""OK"" " & "name=""OK"" value=""0"">" _ 
         '                                  & "<input type=""submit"" value="" OK "" " & "OnClick=""VBScript:OK.Value=1""></p></div>" 
         ' Multi line input                                  
         fnDebug "Set innerHTML" 
         objIE.Document.Body.InnerHTML = "<div align=""center""><p>" & myPrompt & "</p>" & vbCrLf _ 
                                       & "<p><textarea id=UserInput cols=40 rows=25 wrap = off ></textarea></p>" & vbCrLf _ 
                                       & "<p><input type=""hidden"" id=""OK"" " & "name=""OK"" value=""0"">" _ 
                                       & "<input type=""submit"" value="" OK "" " & "OnClick=""VBScript:OK.Value=1""></p></div>"                                   
          
         fnDebug "InnerHTML err: " & Err 
         objIE.Document.Body.Style.overflow = "auto"                        ' Hide the scrollbars 
         objIE.Visible = True                        ' Make the window visible 
         objIE.Document.All.UserInput.Focus                     ' Set focus on input field 
         fnDebug "Focus err: " & Err 
     
         ' Wait till the OK button has been clicked 
         fnDebug "Wait for user to enter text and press OK" 
         On Error Resume Next 
         Do While objIE.Document.All.OK.Value = 0  
             'fnDebug "Wait 200 ms" 
             wait 0,200 
             ' Error handling code by Denis St-Pierre 
             'fnDebug "Test err" 
             'If Err Then ' user clicked red X (or alt-F4) to close IE window 
             '    fnDebug "User clicked red X or alt-F4 to close." 
             '    fnDebug "Error number: " & Err.Number
             '    IELogin = Array( "", "" ) 
             '    objIE.Quit 
             '    Set objIE = Nothing 
             '    Exit Function 
             'End if 
            'fnDebug "Loop back" 
         Loop 
         On Error Goto 0 
         fnDebug "Get text entered." 
         MultiLineInput = objIE.Document.All.UserInput.Value                ' Read the user input from the dialog window 
         objIE.Quit                         ' Close and release the object 
         Set objIE = Nothing 
         fnDebug "Exit." 
     End Function           ' /MultiLineInput  
      
     Function fnDebug(txt) 
        WriteLog("NewUpdate: MultiLineInput: " & txt)
     End Function 

    
    ' @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@  MAIN PROGRAM @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
    
    Class NewUpdate
        ' Constructor
        Private Sub Class_Initialize
            Dim userResponse
            Dim userTextInput
            Dim tempDir
            Dim tempDir2
            Dim changes
            Dim returnCode
            Dim newVersion
            
            WriteLog("NewUpdate: Main: Constructor entry.")

            ' Set the directories
            programFilesX86Path = objShell.ExpandEnvironmentStrings("%PROGRAMFILES(X86)%")
            WriteLog("NewUpdate: Main: programFilesX86Path: " & programFilesX86Path)
            
            programFilesPath = objShell.ExpandEnvironmentStrings("%PROGRAMFILES%")
            WriteLog("NewUpdate: Main: programFilesPath: " & programFilesPath)
            
            commonProgramFilesX86Path = objShell.ExpandEnvironmentStrings("%COMMONPROGRAMFILES(X86)%")
            WriteLog("NewUpdate: Main: commonProgramFilesX86Path: " & commonProgramFilesX86Path)
            
            commonProgramFilesPath = objShell.ExpandEnvironmentStrings("%COMMONPROGRAMFILES%")
            WriteLog("NewUpdate: Main: commonProgramFilesPath: " & commonProgramFilesPath)
            
            programFilesX86CloudPath = programFilesX86Path & "\Cloud.com\Cloud"
            WriteLog("NewUpdate: Main: programFilesX86CloudPath: " & programFilesX86CloudPath)
            
            commonProgramFilesX86CloudPath = commonProgramFilesX86Path & "\Cloud.com\Cloud"
            WriteLog("NewUpdate: Main: commonProgramFilesX86CloudPath: " & commonProgramFilesX86CloudPath)
            
            commonProgramFilesCloudPath = commonProgramFilesPath & "\Cloud.com\Cloud"
            WriteLog("NewUpdate: Main: commonProgramFilesCloudPath: " & commonProgramFilesCloudPath)
            
            wyBuildPath = programFilesX86Path & "\WyBuild"
            WriteLog("NewUpdate: Main: wyBuildPath: " & wyBuildPath)
            
            ' check for the WINCLIENTPATH environment variable
            winClientPath = objShell.ExpandEnvironmentStrings("%WINCLIENTPATH%")
            if winClientPath = "%WINCLIENTPATH%" then
                call MsgBox("Please specify the 'WINCLIENTPATH' environment variable.  e.g.: 'set WINCLIENTPATH=c:\source\projects\win-client.", vbOk, "Error!")
                WriteLog("NewUpdate: Main: Cancel at enter version.  Exit code 1.")
                exit sub
            end if
            WriteLog("NewUpdate: Main: winClientPath: " & winClientPath)
            
            ' check for the WINCLIENTFTPURL environment variable
            ftpRootPath = objShell.ExpandEnvironmentStrings("%WINCLIENTFTPROOTPATH%")
            if ftpRootPath = "%WINCLIENTFTPROOTPATH%" then
                call MsgBox("Please specify the 'WINCLIENTFTPROOTPATH' environment variable.  e.g.: 'set WINCLIENTFTPROOTPATH=c:\FtpRoot.", vbOk, "Error!")
                WriteLog("NewUpdate: Main: Cancel at enter version.  Exit code 2.")
                exit sub
            end if
            WriteLog("NewUpdate: Main: ftpRootPath: " & ftpRootPath)
            
            wyUpdaterPath = winClientPath & "\WyUpdater"

            ' Enter the build number
            userTextInput = InputBox("Enter the version number in the format '1.2.3.4'.", "Version?", "")
            if userTextInput = "" then
                WriteLog("NewUpdate: Main: Cancel at enter version.  Exit code 3.")
                call MsgBox("ERROR: Code 3.", vbOk, "Error!")
                exit sub
            end if
            version = userTextInput
            WriteLog("NewUpdate: Main: Build number: " + version)
            
            ' Ask if this is a new version, or a replacement.
            userResponse = MsgBox("Is this a new version?  Yes for 'new', or No for a replacement version.", vbYesNo, "New Version?")
            if userResponse = vbYes then
                WriteLog("NewUpdate: Main: This is a new version.")
                newVersion = true
            else    
                WriteLog("NewUpdate: Main: This is a replacement version.")
                newVersion = false
            end if
            version = userTextInput
            WriteLog("NewUpdate: Main: Build number: " + version)

            ' Convert the version string into underscores.  e.g.: 1_2_3_4
            versionUnderscore = Replace(version, ".", "_", 1, -1)
            WriteLog "NewUpdate: Main: versionUnderscore: " & versionUnderscore
            
            ' Uninstall Cloud
            userResponse = MsgBox("Uninstall Cloud from the Start menu.", vbOkCancel, "Uninstall")
            if userResponse = vbCancel then
                WriteLog("NewUpdate: Main: Cancel at Uninstall Cloud.  Exit code 4.")
                call MsgBox("ERROR: Code 4.", vbOk, "Error!")
                exit sub
            end if
            WriteLog("NewUpdate: Main: Uninstalled.")
            
            ' Change the version of win-client in Visual Studio
            userResponse = MsgBox("Change the version of win-client in Visual Studio to: '" + version + "'.", vbOkCancel, "Change Version")
            if userResponse = vbCancel then
                WriteLog("NewUpdate: Main: Cancel at version change.  Exit code 5.")
                call MsgBox("ERROR: Code 5.", vbOk, "Error!")
                exit sub
            end if
            WriteLog("NewUpdate: Main: Version changed.")
            
            ' Re-Build configuration 'Release64'
            userResponse = MsgBox("Change the configuration to 'Release64' and Rebuild Solution.", vbOkCancel, "Rebuild Release64")
            if userResponse = vbCancel then
                WriteLog("NewUpdate: Main: Cancel at rebuild Release64.  Exit code 6.")
                call MsgBox("ERROR: Code 6.", vbOk, "Error!")
                exit sub
            end if
            WriteLog("NewUpdate: Main: Release64 rebuilt.")
            
            ' Re-Build configuration 'Release'
            userResponse = MsgBox("Change the configuration to 'Release' and Rebuild Solution.", vbOkCancel, "Rebuild Release")
            if userResponse = vbCancel then
                WriteLog("NewUpdate: Main: Cancel at rebuild Release.  Exit code 7.")
                call MsgBox("ERROR: Code 7.", vbOk, "Error!")
                exit sub
            end if
            WriteLog("NewUpdate: Main: Release rebuilt.")
            
            ' Install the new build.
            userResponse = MsgBox("Run CloudSetup.exe from '.\win-client\CloudSetup\CloudSetup\Express\SingleImage\DiskImages\DISK1' to install Cloud.", vbOkCancel, "Install")
            if userResponse = vbCancel then
                WriteLog("NewUpdate: Main: Cancel at install.  Exit code 8.")
                call MsgBox("ERROR: Code 8.", vbOk, "Error!")
                exit sub
            end if
            WriteLog("NewUpdate: Main: Installed.")
            
            ' Delete the trace.log file if it is present.
            If objFileSys.FileExists(programFilesX86CloudPath & "\trace.log") Then
               WriteLog "NewUpdate: Main: Delete file: " & programFilesX86CloudPath & "\trace.log"
               objFileSys.DeleteFile programFilesX86CloudPath & "\trace.log", True
            End If
            
            ' Delete the client.wyc file if it is present.
            If objFileSys.FileExists(programFilesX86CloudPath & "\client.wyc") Then
               WriteLog "NewUpdate: Main: Delete file: " & programFilesX86CloudPath & "\client.wyc"
               objFileSys.DeleteFile programFilesX86CloudPath & "\client.wyc", True
            End If
            
            ' Make the new WyUpdate directories.  First the DownloadVersion1_1_1_1 folder
            tempDir = wyUpdaterPath & "\DownloadVersion" & versionUnderscore
            WriteLog "NewUpdate: Main: Make directory: " & tempDir
            If Not objFileSys.FolderExists(tempDir) Then 
                objFileSys.CreateFolder tempDir
            End If 
            
            ' Make the Version1_1_1_1 folder
            tempDir = wyUpdaterPath & "\Version" & versionUnderscore
            WriteLog "NewUpdate: Main: Make directory: " & tempDir
            If Not objFileSys.FolderExists(tempDir) Then 
                objFileSys.CreateFolder tempDir
            End If 
            
            ' Make the Version1_1_1_1\ProgramFiles folder
            tempDir = wyUpdaterPath & "\Version" & versionUnderscore & "\ProgramFiles"
            wyUpdaterProgramFilesPath = tempDir
            WriteLog "NewUpdate: Main: Make directory: " & tempDir
            If Not objFileSys.FolderExists(tempDir) Then 
                objFileSys.CreateFolder tempDir
            End If 
            
            ' Make the Version1_1_1_1\CommonFiles32Bit folder
            tempDir = wyUpdaterPath & "\Version" & versionUnderscore & "\CommonFiles32Bit"
            wyUpdaterCommonFiles32BitPath = tempDir
            WriteLog "NewUpdate: Main: Make directory: " & tempDir
            If Not objFileSys.FolderExists(tempDir) Then 
                objFileSys.CreateFolder tempDir
            End If 
            
            ' Make the Version1_1_1_1\CommonFiles64Bit folder
            tempDir = wyUpdaterPath & "\Version" & versionUnderscore & "\CommonFiles64Bit"
            wyUpdaterCommonFiles64BitPath = tempDir
            WriteLog "NewUpdate: Main: Make directory: " & tempDir
            If Not objFileSys.FolderExists(tempDir) Then 
                objFileSys.CreateFolder tempDir
            End If 
            
            ' Copy the Program Files (x86) installation directory to the WyUpdater ProgramFiles directory
            tempDir = programFilesX86CloudPath
            tempDir2 = wyUpdaterProgramFilesPath
            WriteLog "NewUpdate: Main: Copy Cloud install dir: <" & tempDir & "> to WyUpdater ProgramFiles dir: <" & tempDir2 & ">."
            objFileSys.CopyFolder tempDir, tempDir2, OverwriteExisting

            ' Copy the Common Program Files (X86) installation directory to the WyUpdater CommonFiles32Bit directory
            tempDir = commonProgramFilesX86CloudPath
            tempDir2 = wyUpdaterCommonFiles32BitPath
            WriteLog "NewUpdate: Main: Copy Cloud X86 common files dir: <" & tempDir & "> to WyUpdater CommonFiles32Bit dir: <" & tempDir2 & ">."
            objFileSys.CopyFile tempDir & "\*.*" , tempDir2 & "\" , OverwriteExisting
            
            ' Copy the Common Program Files installation directory to the WyUpdater CommonFiles64Bit directory
            tempDir = commonProgramFilesCloudPath
            tempDir2 = wyUpdaterCommonFiles64BitPath
            WriteLog "NewUpdate: Main: Copy Cloud X86 common files dir: <" & tempDir & "> to WyUpdater CommonFiles32Bit dir: <" & tempDir2 & ">."
            objFileSys.CopyFile tempDir & "\*.*" , tempDir2 & "\" , OverwriteExisting
            
            ' Ask the user for the changes
            changes = MultiLineInput("Please enter the change list for version " & version)
            WriteLog "NewUpdate: Main: User entered changes: <" & changes & ">."
            
            ' Create the XML AddVersion file
            WriteLog "NewUpdate: Main: Call CreateXmlFile."
            call CreateXmlFile(version, newVersion, changes, programFilesX86CloudPath, _
                               commonProgramFilesX86Path, commonProgramFilesPath, _
                               wyUpdaterPath, "TempAddVersion.xml")
                               
            ' Now do the wyBuild.  This will add the new version, build the updates, build the wyUpdate.exe (which we don't use),
            ' and build the client.wyc file.  Hide the window (0) and wait for completion (true).
            WriteLog "NewUpdate: Main: Call wyBuild to build the version."
            returnCode =  objshell.Run("""" & wyBuildPath & "\wyBuild.cmd.exe"" """ & wyUpdaterPath & "\Cloud.wyp"" /bwu /bu -add=""" & wyUpdaterPath & "\TempAddVersion.xml""", 0, true)
            if returnCode <> 0 then
                WriteLog "NewUpdate: Main: ERROR: from wyBuild: " & returnCode & ". Exiting with code 9."
                call MsgBox("ERROR: Code 9.", vbOk, "Error!")
                exit sub
            end if
            
            ' Tell the user that we are done building the update.
            userResponse = MsgBox("wyBuilt version " & version & ".", vbOkCancel, "Build Complete!")
            if userResponse = vbCancel then
                WriteLog("NewUpdate: Main: Cancel at build with client.wyc complete.  Exit code 10.")
                call MsgBox("ERROR: Code 10.", vbOk, "Error!")
                exit sub
            end if
            WriteLog("NewUpdate: Main: wyBuild completed to produce client.wyc.")
            
            ' Copy the client.wyc file to the Update1_1_1_1 ProgramFiles directory.
            tempDir = wyUpdaterPath
            tempDir2 = wyUpdaterProgramFilesPath
            WriteLog "NewUpdate: Main: Copy client.wyc from dir: <" & tempDir & "> to WyUpdater ProgramFiles dir: <" & tempDir2 & ">."
            objFileSys.CopyFile tempDir & "\WyUpdate\client.wyc", tempDir2 & "\" , OverwriteExisting
            
            ' Tell the user to build with the new client.wyc file.
            userResponse = MsgBox("In Visual Studio, select the Release configuration and BUILD.  NOT Rebuild.", vbOkCancel, "Build Release")
            if userResponse = vbCancel then
                WriteLog("NewUpdate: Main: Cancel at Uninstall Cloud.  Exit code 11.")
                call MsgBox("ERROR: Code 11.", vbOk, "Error!")
                exit sub
            end if
            WriteLog("NewUpdate: Main: VS build completed.")
            
            ' Tell the user to uninstall from the Start menu.
            userResponse = MsgBox("Please use the Start menu to uninstall Cloud.", vbOk, "Please Uninstall")
            if userResponse = vbCancel then
                WriteLog("NewUpdate: Main: Cancel at uninstall after building with client.wyc.  Exit code 12.")
                call MsgBox("ERROR: Code 12.", vbOk, "Error!")
                exit sub
            end if
            WriteLog("NewUpdate: Main: Uninstalled after building with client.wyc.")
            
            ' Tell the user to reinstall from the Start menu.
            userResponse = MsgBox("Run CloudSetup.exe from '.\win-client\CloudSetup\CloudSetup\Express\SingleImage\DiskImages\DISK1' to install Cloud.", vbOkCancel, "Install")
            if userResponse = vbCancel then
                WriteLog("NewUpdate: Main: Cancel at install after building with client.wyc.  Exit code 13.")
                call MsgBox("ERROR: Code 13.", vbOk, "Error!")
                exit sub
            end if
            WriteLog("NewUpdate: Main: Installed after building with client.wyc.")
            
            ' Tell the user to check that client.wyc is the correct build in ProgramFiles.
            userResponse = MsgBox("Please check that the client.wyc is the correct file in " & programFilesX86CloudPath & ".", vbOkCancel, "Check client.wyc")
            if userResponse = vbCancel then
                WriteLog("NewUpdate: Main: Cancel at check correct client.wyc.  Exit code 13.")
                call MsgBox("ERROR: Code 13.", vbOk, "Error!")
                exit sub
            end if
            WriteLog("NewUpdate: Main: Installed after building with client.wyc.")
            
            ' Copy the Download files to the WyUpdater DownloadVersion1_1_1_1 directory
            tempDir = winClientPath & "\CloudSetup\CloudSetup\Express\SingleImage\DiskImages\DISK1"
            tempDir2 = wyUpdaterPath & "\DownloadVersion" & versionUnderscore
            WriteLog "NewUpdate: Main: Copy the download files from dir: <" & tempDir & "> to WyUpdater DownloadVersion dir: <" & tempDir2 & ">."
            objFileSys.CopyFile tempDir & "\*.*" , tempDir2 & "\" , OverwriteExisting
            
            ' Tell the user to change the CloudSetup.exe Resources.
            tempDir2 = wyUpdaterPath & "\DownloadVersion" & versionUnderscore
            userResponse = MsgBox("Please use Resource Hacker to change the resources in the file " & tempDir2 & "\CloudSetup.exe.", vbOkCancel, "Resource Hacker")
            if userResponse = vbCancel then
                WriteLog("NewUpdate: Main: Cancel at Resource Hacker.  Exit code 14.")
                call MsgBox("ERROR: Code 14.", vbOk, "Error!")
                exit sub
            end if
            WriteLog("NewUpdate: Main: Resource Hacker complete.")
            
            ' Copy the files to the localhost ftp root directory.  Start with CloudSetup.exe to the root.
            tempDir = wyUpdaterPath & "\DownloadVersion" & versionUnderscore & "\CloudSetup.exe"
            tempDir2 = ftpRootPath & "\"
            'If objFileSys.FileExists(tempDir2) Then
            '   WriteLog "NewUpdate: Main: Delete file: " & tempDir2
            '   objFileSys.DeleteFile tempDir2, True
            'End If
            WriteLog "NewUpdate: Main: Copy CloudSetup.exe from: <" & tempDir & "> to the FtpRoot at: <" & tempDir2 & ">."
            objFileSys.CopyFile tempDir, tempDir2, OverwriteExisting

            ' Copy the updates folder to the ftp root directory.
            tempDir = wyUpdaterPath & "\Updates"
            tempDir2 = ftpRootPath & "\updates"
            WriteLog "NewUpdate: Main: Copy updates dir: <" & tempDir & "> to FtpRoot dir: <" & tempDir2 & ">."
            objFileSys.CopyFolder tempDir, tempDir2, OverwriteExisting

            call MsgBox("Done!  Normal completion.", vbOk, "Done!")
            
        End Sub : Private Sub CatchErr : If Err.Number = 0 Then Exit Sub
            ' Catch
            WriteLog("NewUpdate: Main: Catch: Unhandled error " & Err.Number & " occurred.")
        Err.Clear : End Sub : Private Sub Class_Terminate : CatchErr
            ' Finally
            Call MsgBox("NewUpdate: Main: Finally: Done.", vbOk, "Done")
            WriteLog("NewUpdate: Main: Exiting constructor (finally).")
       End Sub 
    End Class    
    
    ' Drive the main program
    With New NewUpdate : End With
    
    ' No tracing after this
    set objShell = Nothing
    set objFileSys = Nothing
    wscript.Quit 0
    
