##Functional Tests


####CFT_0000 CREATE A FILE
	
>A Syncbox should be able to sync files being added to the sync folder 
		
####CFT_0001 RENAME A FILE
		
>A Syncbox should be able to sync a file that has just been renamed
		
####CFT_0002 DELETE A FILE

>A Syncbox should be able to handle a file deletion

####CFT_0003 CREATE A FOLDER

>A syncbox should sync a newly created folder
		
####CFT_0004 RENAME A FOLDER

>A syncbox should be able to handle a folder rename
		
####CFT_0005 DELETE A FOLDER

>A syncbox should be able to handle a folder deletion
		
		
####CFT_0006 QUICK RENAME FOLDER

>A syncbox should be able to handle the quick rename of folders
		
####CFT_0007 QUICK RENAME THEN DELETE FOLDER

>A syncbox should be able to handle a quick rename of a folder then a deletion of that folder
		
####CFT_0008 MULTI RENAME OF FOLDER

>A syncbox should be able to handle multiple folder renames
		
####CFT_0009 QUICK MOVE FOLDER

>Syncbox should handle quickly moving a folder into another folder
		
####CFT_0010 FOLDER UPLOAD WITH CONTENTS COPIED

>A Syncbox should be able to handle a folder being copied while uploading
		
####CFT_0011 COPY FILE INTO FOLDER THAT CONTAINS SAME NAME FILE

>Syncbox should handle a file copied into a folder that contains a file named the same as the copied file
		
####CFT_0012 LARGE FILE UPLOAD

>Syncbox should handle large file uploads
		
####CFT_0013 DELETE LARGE FILE

>Syncbox should handle deletion of large files
		
####CFT_0014 DELETE LARGE FILE DURING UPLOAD

>Syncbox should be able to handle a large file being deleted during upload
		
####CFT_0015 MOVE LARGE FILE DURING UPLOAD

>Syncbox should be able to handle a large file being moved during upload
		
####CFT_0016 MOVE FOLDER DURING UPLOAD

>Syncbox should handle moving a folder while it's contents are uploading
		
####CFT_0017 CHANGE FILE PERMISSIONS

>Syncbox should be able to handle a file changing permissions
		
####CFT_0018 SYMLINK TESTS

>Syncbox should handle symlinks created inside the syncbox

####CFT_0018_2

>Syncbox should handle the symlink being renamed
		
####CFT_0019 CREATE A LOT OF SMALL FILES

>Syncbox should be able to handle the addition of a lot of small files
		
####CFT_0020 DELETE A LOT OF SMALL FILES

>Syncbox should be able to handle the deletion of a lot of small files one by one
		
####CFT_0021 MULTI EDITS ON SAME FILE

>Syncbox should be able to handle multiple edits on the same file
		
####CFT_0022 MAKE AND RENAME LOTS OF FOLDERS

>Syncbox should be able to handle a lot of new folders being created

>__Cloud Issue: 000093__

####CFT_0023 DELETE FIRST FILE IN UPLOAD QUEUE
	
>Syncbox should be able to handle a delete of a file uploading at anytime

>__Cloud Issue: 000108__ 

####CFT_0024 MOVE CONTENTS AFTER SYNC

>Syncbox should be able to handle a move of any folder
>__Cloud Issue: 000124__

####CFT_0025 LARGE FILE NAMES

>Syncbox should be able to handle large file names of 190 characters (less than what the max is allowed on HFS)

>__Cloud Issue: 000101__

####CFT_0026 ZERO BYTE FILES

>Syncbox should be able to handle zero byte files

>__Cloud Issue: 000089__

####CFT_0027 FOLDER MOVES WITH CONTENTS

>Syncbox should be able to handle various moves into different subfolders

####CFT_0028 MOVE FILE THAT EXISTS IN ROOT

>Syncbox should be able to handle a file moved into the root that already contains a file with the same name, when 'replace' is selected it should update across all syncboxes

>__Cloud Issue: 000096__

####CFT_0029 MOVE FOLDER WITH CONTENTS

>Syncbox should be able to handle a folder with contents being moved into another folder

>__Cloud Issue: 000124__

####CFT_0030 FOLDER RENAME WHILE CONTENTS ARE BEING ADDED TO FOLDER

>Syncbox Should be able to handle a folder rename while contents are being added to folder

####CFT_0031 FOLDER RENAME WHILE CONTENTS ARE BEING ADDED OUTSIDE OF FOLDER

>Syncbox Should be able to handle a folder rename while contents in another folder are uploading

####CFT_0032 FILE SIZE CHANGE DURING UPLOAD

>Should be able to upload a file that starts at one size but ends at another
		

