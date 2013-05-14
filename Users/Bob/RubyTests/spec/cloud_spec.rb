require "#{File.expand_path(File.dirname(__FILE__))}/../operations/file_operations"
require "#{File.expand_path(File.dirname(__FILE__))}/../operations/test_operations"
require 'sqlite3'

describe "File Sync" do 

    def clean_up
        if @clean_up
            FileUtils.rm_rf Dir.glob('*')
            if @index
                db = SQLite3::Database.new(@index)
                rows = db.execute( "select * from ZFILESYSTEMITEM")
                if rows.count != 2
                    raise "Index does not contain 2 items i.e. / & .cldeletedfolder count is #{rows.count}"
                end
            end
        end
    end

	before(:all) do |t|
        @skippable = ENV["skippable"]
        @index = ENV["index"]
		@testy = Testy.new
		@testy.init
	end

	after(:all) do
		@testy.number_of_tests_passed
	end

    before(:each) do |t|
        @clean_up = true
    end

	after(:each) do
        if !@skipped
		  @testy.get_input
        end
        clean_up
	end

	def desc(task,id)
		@testy.describe task.example.description, id
        if @skippable
            puts "Skip this test? type 's' ".light_blue
            response = STDIN.gets.chomp
            if response.to_s == "s"
                @skipped = true
                pending "skipped test: #{id}"
            end
        end
	end

	def assert(desc)
		@testy.assert desc
	end

    def pend(description)
        @skipped = true
        pending description
    end

	it "should be able to add a file to a syncbox", :CFT_0000 => true do |t|
		desc t, "CFT_0000"
		file_name = 'File_1.txt'
		create_file_named file_name
		assert "Was the file /#{file_name} created and synced properly?"
        @clean_up = false
	end

	it "should be able to handle a file rename", :CFT_0001 => true do |t|
		desc t, "CFT_0001"
		file_name = "File_1.txt"
      	file_rename = 'File_Renamed.txt'
      	File.rename(file_name,file_rename)
      	assert "Was the file /#{file_name} renamed to /#{file_rename}?"
        @clean_up = false
	end

	it "should be able to handle a file deletion", :CFT_0002 => true do |t|
		desc t, "CFT_0002"
		file_name = 'File_Renamed.txt'
      	File.delete(file_name)
      	assert "Was the file named #{file_name} deleted across all syncboxes"
	end

	it "should sync a newly created folder", :CFT_0003 => true do |t|
		desc t, "CFT_0003"
		folder_name = 'Folder'
        Dir.mkdir(folder_name)
        assert "was the folder /#{folder_name} synced across all syncboxes?"
        @clean_up = false
	end

	it "should be able to handle a folder rename", :CFT_0004 => true do |t|
		desc t, "CFT_0004"
		folder_name = 'Folder'
        folder_rename = 'Folder_Renamed'
        File.rename(folder_name,folder_rename)
        assert "was the folder /#{folder_name} renamed to /#{folder_rename} across all syncboxes"
        @clean_up = false
	end

	it "should be able to handle a folder deletion", :CFT_0005 => true do |t|
		desc t, "CFT_0005"
		folder_name = 'Folder_Renamed'
        FileUtils.rm_r(folder_name)
        assert "was the folder /#{folder_name} deleted across all syncboxes"
	end

	it "should be able to handle the quick rename of folders", :CFT_0006 => true do |t|
		desc t, "CFT_0006"
		folder_name = "Folder_to_be_renamed"
        folder_rename = "Bacon_Pancakes!"
        Dir.mkdir(folder_name)
        File.rename(folder_name,folder_rename)
        assert "was the folder /#{folder_name} renamed to /#{folder_rename} synced to all syncboxes"
	end

	it "should be able to handle a quick rename of a folder then a deletion of that folder", :CFT_0007 => true do |t|
		desc t, "CFT_0007"
		folder_name = "Folder_01"
        folder_rename = "Folder_02"
        Dir.mkdir(folder_name)
        File.rename(folder_name,folder_rename)
        sleep(1)
        FileUtils.rm_r(folder_rename)
        assert "was the folder /#{folder_name} renamed to /#{folder_rename} deleted across all syncboxes"
	end

	it "should be able to handle multiple folder renames", :CFT_0008 => true do |t|
		desc t, "CFT_0008"
		folder_name = 'Folder_Quick_Rename_0'
        Dir.mkdir(folder_name)
        puts "Renaming folder /#{folder_name} 25 times"
        25.times { |i| 
          folder_rename = "Folder_Quick_Rename_#{i}"
          File.rename(folder_name,folder_rename)
          folder_name = folder_rename
          puts "Folder renamed to /#{folder_rename}"
          sleep(1)
        }
        assert "Was the folder /#{folder_name} renamed 25 times properly synced across all syncboxes"
	end

	it "should handle quickly moving a folder into another folder", :CFT_0009 => true do |t|
		desc t, "CFT_0009"
	    puts "making a folder and placing a subfolder into it very quickly"
        untitled_folder = 'untitled folder'
        folder_1 = 'foo'
        folder_2 = 'bar'
        Dir.mkdir(untitled_folder)
        FileUtils.mv untitled_folder, folder_1
        Dir.mkdir(untitled_folder)
        FileUtils.mv untitled_folder, folder_2
        FileUtils.mv folder_2, folder_1
        assert "Folder /#{folder_1}/#{folder_2} should be synced across syncboxes"
	end

	it "should be able to handle a folder being copied while uploading", :CFT_0010 => true do |t|
		desc t, "CFT_0010"
		puts "Making a folder to copy with files in it"
        folder_name = 'Folder_to_be_copied'
        folder_copy = 'Folder_copy'
        Dir.mkdir(folder_name)
        Dir.chdir(folder_name)
        create_files_and_folders 1, 10, 100
        Dir.chdir('..')
        puts 'sleeping for 2 seconds to make sure upload has started'
        sleep(2)
        copy_with_path folder_name, folder_copy
        assert "was the folder /#{folder_name} synced and was its copy /#{folder_copy} properly synced"
	end

	it "should handle a file copied into a folder that contains a file named the same as the copied file", :CFT_0011 => true do |t|
		desc t, "CFT_0011"
        puts "Making a File to be copied on top of another file in another folder"
        folder_name = 'Contains_File_A'
        file_name = 'File_A.txt'
        Dir.mkdir(folder_name)
        Dir.chdir(folder_name)
        create_file_named file_name
        Dir.chdir('..')
        puts 'sleeping for 2 second to make sure file is uploaded'
        sleep(2)
        create_file_named file_name, 32
        FileUtils.cp file_name, folder_name
        assert "Is the file /#{file_name} in /#{folder_name}/#{file_name} the same file as /#{file_name}"
	end

	it "should handle large file uploads", :CFT_0012 => true do |t|
    	desc t, "CFT_0012"
    	file_size = @testy.large_file_prompt
        puts "Making a large file, this could take a while"
        file_name = "The_Large_File.file"
        create_large_file file_name, file_size
        sleep(3)
        assert "Was the file /#{file_name} of size #{file_size}MB uploaded successfully?"
        @clean_up = false
	end

	it "should handle deletion of large files", :CFT_0013 => true do |t|
		desc t, "CFT_0013"
		File.delete("The_Large_File.file")
        assert 'Was the file /The_Large_File.file deleted across all syncboxes'
	end

	it "should be able to handle a large file being deleted during upload", :CFT_0014 => true do |t|
		desc t, "CFT_0014"
		file_size = @testy.large_file_prompt
        puts "Making a large file, this could take a while"
        file_name = "Large_file_to_be_DELETED"
        create_large_file file_name, file_size
        puts "Large File Made\nSleeping for 15 seconds to make sure the upload is in process"
        sleep(15)
        puts "deleting file"
        File.delete(file_name)
        assert "was the file /#{file_name} that was deleted during upload properly deleted across all syncboxes?"
	end

	it "should be able to handle a large file being moved during upload", :CFT_0015 => true do |t|
		desc t, "CFT_0015"
		file_size = @testy.large_file_prompt
        puts "Making a large file, this could take a while"
        file_name = "Large_file_to_be_MOVED"
        create_large_file file_name, file_size
        puts "sleeping for 15 seconds to make sure the upload is in process"
        sleep(15)
        dest = "Large_File_Move_destination"
        Dir.mkdir(dest)
        puts "moving /#{file_name} to /#{dest}"
        FileUtils.mv file_name, dest
        assert "was the file /#{file_name} moved into /#{dest} across all syncboxes?"
	end

    it "should be able to create a file in a folder, move it to the root, rename the folder, then move the file into the renamed folder during upload", :CFT_0016 => true do |t|
        begin
            desc t, "CFT_0016"
            file_size = @testy.large_file_prompt
    		puts 'Making a folder which will be renamed during uploading'
            move_folder = 'Folder_to_be_moved_during_upload'
            move_folder_dest = 'Folder_to_be_moved_during_upload_destination'
            file_name = "Large_file_to_be_MOVED"
            Dir.mkdir(move_folder)
            puts "Making a large file /#{move_folder}/#{file_name}, this could take a while"
            Dir.chdir(move_folder)
            create_large_file "#{file_name}", file_size
            Dir.chdir('..')
            puts 'sleeping for 15 seconds to make sure the upload is in process'
            sleep(15)
            puts 'After sleeping.  Move the file out to the root.'
            puts "File.rename #{move_folder}/#{file_name}, #{file_name}"
            File.rename "#{move_folder}/#{file_name}", "#{file_name}"
            puts 'sleeping for 1 second'
            sleep(1)
            puts 'after sleeping for 1 seconds.  Rename the folder.'
            FileUtils.mv move_folder, move_folder_dest
            puts 'sleeping for 1 second'
            sleep(1)
            puts 'after sleeping for 1 seconds.  Move the uploading file back into the folder.'
            File.rename "#{file_name}", "#{move_folder_dest}/#{file_name}"
            assert "Was the file /#{file_name} properly uploaded in folder /#{move_folder_dest} across all syncboxes?"
        rescue
          puts "rescue: " + @error_message="#{$!}"
        end
    end

    # @@@@@@@@@@@@@@@@@@@ Doesn't work on Windows.  Replaced by CFT_0016 above.
	it "should handle moving a folder while it's contents are uploading", :CFT_0016A => true do |t|
		desc t, "CFT_0016A"
		puts 'Making a folder filled with files to be moved during uploading'
        move_folder = 'Folder_to_be_moved_during_upload'
        move_folder_dest = 'Folder_to_be_moved_during_upload_destination'
        Dir.mkdir(move_folder)
        Dir.chdir(move_folder)
        10.times { |i|
          create_large_file("FileWillBeMovedDuringUplod-#{i}",5)
        }
        Dir.chdir('..')
        puts 'Folder to be moved during upload'
        puts "Sleeping for 10 seconds"
        sleep(10)
        puts "Done sleeping for 10 seconds.  Move the tile from folder from #{move_folder} to #{move_folder_dest}."
        #
        # FileUtils.mv move_folder, move_folder_dest
        puts "After the move"
        assert "Rename the folder manually now."
        assert "Was the folder /#{move_folder} moved properly to /#{move_folder_dest} across all syncboxes?"
	end

    # @@@@@@@@@@@@@@@@@@@ Doesn't work on Windows.  Must be reworked.
	it "should be able to handle a file changing permissions", :CFT_0017 => true do |t|
		desc t, "CFT_0017"
        permissions_file = 'file_with_permissions'
		puts "making file /#{permissions_file}"
        create_file_named permissions_file
        FileUtils.chmod(0777,permissions_file)
        assert "Was the file /#{permissions_file} synced with the permission 777?"
	end

    # @@@@@@@@@@@@@@@@@@@ Doesn't work on Windows.  Must be reworked.
	it "should handle symlinks", :CFT_0018 => true do |t|
	    desc t, "CFT_0018"
        puts "Creating symlink target folder and file"
        symlink_target_path = "Symlink Target Folder"
        symlink_target_file = "Target File.txt"
        symlink_file = "symlink_file"
        Dir.mkdir(symlink_target_path)
        Dir.chdir(symlink_target_path)
        create_file_named(symlink_target_file)
        Dir.chdir("..")
        puts "Creating symlink"
        File.symlink("#{symlink_target_path}/#{symlink_target_file}", symlink_file)
        assert "was the symlink file /#{symlink_file} created, and its target is /#{symlink_target_path}/#{symlink_target_file}"
	    @clean_up = false
    end

    # @@@@@@@@@@@@@@@@@@@ Doesn't work on Windows.  Must be reworked.
    it "Should handle symlink renames", :CFT_0018_2 => true do |t|
        desc t, "CFT_0018_2"
        symlink_file = "symlink_file"
        symlink_file_renamed = "symlink_file_renamed"
        FileUtils.mv(symlink_file, symlink_file_renamed)
        assert "Was the symlink /#{symlink_file} renamed to /#{symlink_file_renamed} still work?"
    end


	it "should be able to handle the addition of a lot of small files", :CFT_0019 => true do |t|
		desc t, "CFT_0019"
		puts "Making a folder to contain these small files"
		small_files_path = 'small_files'
        Dir.mkdir(small_files_path)
        Dir.chdir(small_files_path)
        puts "Enter an amount of files to create"
        file_count = STDIN.gets.chomp
        file_count.to_i.times { |i|
           create_file_named("file_#{i}",1)
        }
        Dir.chdir('..')
        assert "were #{file_count} files located in /#{small_files_path} created and synced across all syncboxes"
	   @clean_up = false
    end

	it "should be able to handle the deletion of a lot of small files one by one", :CFT_0020 => true do |t|
		desc t, "CFT_0020"
		Dir.chdir('small_files')
        file_count = Dir[File.join('.', '**', '*')].count { |file| File.file?(file) }
        file_count.times { |i|
          File.delete("file_#{i}")
        }
        Dir.chdir('..')
        assert "were #{file_count} small files located in /small_files deleted across all syncboxes"
	end

	it "should be able to handle multiple edits on the same file", :CFT_0021 => true do |t|
		desc t, "CFT_0021"
		puts "Create a new file to edit"
        file_name = "File_Edited_5_times.txt"
        5.times { |i| 
          unless i == 0 
            puts "editing file for #{i}'th time"
          else
            puts "creating file"
          end
          File.open(file_name, "w") { |f| 
            f.write("This file has been edited #{i} times\n\n");
            f.write(random_alphanumeric(i * 64 + 1))
          } 
          puts 'sleeping for a 15 seconds to allow this to be uploaded to server'
          sleep(15)
        }
        assert "Was the file /#{file_name} edited 5 times and properly synced?"
	end

	it "should be able to handle a lot of new folders being created : ISSUE: 000093", :CFT_0022 => true do |t|
		desc t, "CFT_0022"
		container = 'Folder_containing_a_lot_of_folders'
        puts "creating a lot of folder in directory /#{container}"
        Dir.mkdir(container)
        Dir.chdir(container)
        puts "sleeping for 5 seconds to guarentee /#{container} is uploaded"
        sleep(5)
        20.times { |i| 
          Dir.mkdir("untitled folder")
          puts "mkdir #{container}/untitled folder/ & Sleeping for 5 seconds"
          sleep(5)
          puts "mv #{container}/untitled folder/ #{container}/Folder_#{i}"
          FileUtils.mv "untitled folder", "Folder_#{i}"
        }
        Dir.chdir('..')
        puts "This test may or may not fail, make sure #{container}/untitled folder does not exist on any other syncboxes".red
        assert "where 20 folders synced properly to all syncboxes"
	end

	it "should be able to handle a delete of a file uploading at anytime : ISSUE: 000108", :CFT_0023 => true do |t|
		desc t, "CFT_0023"
        folder_name = "Upload_queue"
        file_name = "File_to_be_DELETED.txt"
        file_size = 120
        Dir.mkdir(folder_name)
        Dir.chdir(folder_name)
        puts "created a #{file_size} MB file in /#{folder_name}/#{file_name}"
        create_large_file file_name, file_size
        puts "sleeping for 15 seconds to start upload of large file"
        sleep(15)
        puts "creating 50 smaller files in /#{folder_name} to add to the upload queue"  
        50.times { |i|
          create_large_file "File_#{i}.txt", 2
        }  
        puts "sleeping for 10 seconds to let first upload start"
        sleep(10)
        File.delete(file_name)
        Dir.chdir('..')
        assert "Were the remaining 50 files in /#{folder_name} properly uploaded after #{file_name} was deleted?"
	end

	it "should be able to handle a move of any folder : ISSUE 000124", :CFT_0024 => true do |t|
		desc t, "CFT_0024"
		folder_name = "Move_After_upload"
        Dir.mkdir(folder_name)
        Dir.chdir(folder_name)
        puts "creating 20 files in /#{folder_name}"
        20.times { |i|
          create_large_file "File_to_be_moved_#{i}.txt", 2
        }
        puts "Hit enter when all files are synced"
        Dir.chdir('..')
        STDIN.gets.chomp
        containing_folder = "Containg_Move_after_upload"
        Dir.mkdir(containing_folder)
        FileUtils.mv folder_name, "#{containing_folder}"
        puts "moved /#{folder_name} to /#{containing_folder}/#{folder_name}"
        assert "does /#{containing_folder}/#{folder_name} contain all 20 files across syncboxes"
	end

    # @@@@@@@@@@@@@@@@@@@ Doesn't work on Windows.  Must be reworked.
	it "should be able to handle large file names of 255 characters (allowed on HFS) : ISSUE 000101", :CFT_0025 => true do |t|
		desc t, "CFT_0025"
		file_name = random_alphanumeric 255
        create_file_named(file_name,32)
        assert "was the file named\n#{file_name}\nCreated and synced across all syncboxes"
    	end

  	it "should be able to handle zero byte files : ISSUE 000089", :CFT_0026 => true do |t|
    		desc t, "CFT_0026"
    		folder_name = "Zero_Byte_Files"
        Dir.mkdir(folder_name)
        Dir.chdir(folder_name)
        20.times { |i|
          FileUtils.touch "Zero_Byte_file_#{i}.txt"
        }
        Dir.chdir('..')
        assert "Were 20 zero byte files created and synced across all syncboxes in /#{folder_name}"
	end

	it "should be able to handle various moves into different subfolders", :CFT_0027 => true do |t|
		desc t, "CFT_0027"
		container = "Contains_Moves_tests"
        child = "sub_folder"
        Dir.mkdir(container)
        Dir.chdir(container)
        puts "Making dir #{container}"
        Dir.mkdir(child)
        Dir.chdir(child)
        puts "Making dir #{container}/#{child}"
        sleep(5)
        5.times { |i|
          Dir.mkdir "#{child} child #{i}"
          puts "Making dir #{container}/#{child}/#{i}"
        }
        sleep(5)
        Dir.chdir('..')
        puts "moving #{container}/#{child} to ../#{child}"
        FileUtils.mv "#{child}", '..'
        sleep(5)
        puts "making new /#{container}/#{child}"
        Dir.mkdir(child)
        sleep(5)
        puts "moving ../#{child} to #{container}/#{child}"
        FileUtils.mv "../#{child}", "#{child}", :force => true
        Dir.chdir('..')
        assert "Folder structure of /#{container} should match across all syncboxes"
	end

	it "should be able to handle a file moved into the root that already contains a file with the same name, when 'replace' is selected it should update across all syncboxes\nISSUE 000096", :CFT_0028 => true do |t|
        desc t, "CFT_0028"
    	folder_name = "Folder_that_contains_B"
        file_name = "B.txt"
        Dir.mkdir(folder_name)
        puts "created folder /#{folder_name}\nsleeping for 5 seconds"
        sleep(5)
        Dir.chdir(folder_name)
        create_file_named file_name
        puts "created file /#{folder_name}/#{file_name}\nsleeping for 5 seconds"
        sleep(5)
        Dir.chdir('..')
        create_file_named file_name
        puts "created file /#{file_name} and sleeping for 5 seconds"
        sleep(5)
        FileUtils.mv "#{folder_name}/#{file_name}", '.', :force => true
        puts "moving /#{folder_name}/#{file_name} to / "
        sleep(5)
        assert "is the file /#{file_name} synced across all syncboxes and #{folder_name}/#{file_name} does not exist"
	end		

	it "should be able to handle a folder with contents being moved into another folder : ISSUE 000124", :CFT_0029 => true do |t|
		desc t, "CFT_0029"
        folder_name = "folder_with_contents"
        dest = "folder_with_contents_DESTINATION"
        puts "making a folder /#{folder_name} that will have 10, 1mb files"
        Dir.mkdir folder_name
        Dir.chdir folder_name
        10.times { |i|
          create_large_file "Some_File-#{i}", 1
        }
        Dir.chdir '..'
        puts "Press ENTER when all contents /#{folder_name} are synced"
        STDIN.gets.chomp
        puts "making a destination folder /#{dest}"
        Dir.mkdir dest
        sleep(3)
        puts "moving /#{folder_name} to /#{dest}/#{folder_name}"
        FileUtils.mv folder_name, dest
        assert "Was the move presested across all syncboxes and /#{dest}/#{folder_name} contains 10 files?"
	end	

    it "Should be able to handle a folder rename while contents are being added to folder", :CFT_0030 => true do |t|
        desc t, "CFT_0030"
        folder_name  = "Rename_while_contents_being_added"
        folder_rename = "Renamed_while_contents_being_added"
        tmp_name = folder_name
        Dir.mkdir(folder_name)
        sleep(3)
        puts "created /folder_name, adding 5 1 meg files to it"
        5.times { |i|
            create_large_file "#{tmp_name}/#{i}.txt", 1
            if (i % 3 == 0 && i > 0)
                puts "Renaming /#{folder_name} to /#{folder_rename}"
                FileUtils.mv folder_name, folder_rename
                tmp_name = folder_rename
            end
        }
        assert "Was the /#{folder_name} renamed to /#{folder_rename} and its contents synced?"
    end													

    it "Should be able to handle a folder rename while contents in another folder are uploading", :CFT_0031 => true do |t|
        desc t, "CFT_0031"
        folder_name = "Rename_me_while_contents_in_another_folder_uploading"
        folder_rename = "Renamed_folder_contents_in_another_folder_uploading"
        external_folder_name = "contents_folder"
        Dir.mkdir(folder_name)
        Dir.mkdir(external_folder_name)
        puts "created /#{folder_name} and /#{external_folder_name} and adding 5 files to /#{external_folder_name}"
        5.times { |i|
            create_large_file "#{external_folder_name}/#{i}.txt", 1
            if (i % 3 == 0 && i > 0)
                puts "Renaming /#{folder_name} to /#{folder_rename}"
                FileUtils.mv folder_name, folder_rename
            end
        }
        assert "Was the /#{folder_name} renamed to /#{folder_rename} and the contents of #{external_folder_name} uploaded"
    end

    it "Should be able to upload a file that starts at one size but ends at another", :CFT_0032 => true do |t|
        desc t, "CFT_0032"
        puts "Writing a file that takes a while to fully write into"
        file_name = "File.txt"
        File.open(file_name, "w") { |file|
            1000000.times { |i|
                file.write("#{Time.now}")
                file.write("#{i}")
            }
        }
        assert "Was the file fully uploaded and synced across syncboxes?"
    end

    it "Should be able to handle a lot of folders with 1 file in each of them", :CFT_0033 => true do |t|
        desc t, "CFT_0033"
        container = "Contains_500_folders_with_1_file"
        Dir.mkdir(container)
        Dir.chdir(container)
        500.times { |i|
            Dir.mkdir("#{i}")
            create_file_named("#{i}/File-#{i}.txt", 256)
        }
        Dir.chdir('..')
        assert "Were all 500 folders and 500 files synced across all syncboxes?"
    end

    it "Should be able to handle 500 folders being deleted while indexing", :CFT_0034 => true do |t|
        desc t, "CFT_0034"
        pend "THIS TEST IS NOT READY YET"
        container = "Contains_500_Folders_and_Files_TOBE_Deleted_WHILE_uploading"
        Dir.mkdir(container)
        Dir.chdir(container)
        500.times { |i|
            Dir.mkdir("#{i}")
            create_file_named("#{i}/File-#{i}.txt",264)
        }
        Dir.chdir('..')
        puts "Sleeping for 15 seconds and deleting /#{container}"
        sleep(15)
        FileUtils.rm_rf container
        assert "All 500 files should not be synced "
    end

    it "Should be able to handle 50 nested directorys each with the same file in them, ISSUE: 00178", :CFT_0035 => true do |t|
        desc t, "CFT_0035"
        container = "Contains_50_Nested_folders"
        pwd = Dir.pwd
        file_name = "File_for_copy"
        Dir.mkdir(container)
        Dir.chdir(container)
        create_file_named(file_name,256)
        50.times { |i|
            Dir.mkdir("Folder_#{i}")
            FileUtils.cp file_name, "Folder_#{i}"
            Dir.chdir("Folder_#{i}")
        }
        Dir.chdir(pwd)
        assert "Were all 50 subfolders contained in /#{container} each with the same file synced across all syncboxes"
    end

    it "Should be able to handle a file being deleted and a folder being added of the same name", :CFT_0036 => true do |t|
        desc t, "CFT_0036"
        pending "WE ARN'T ready for this test yet"
        file_name = "File.txt"
        puts "creating file /#{file_name}"
        create_file_named(file_name, 255)
        puts "Sleeping for 15 seconds to make sure file has been uploaded"
        sleep(15)
        FileUtils.rm_rf file_name
        Dir.mkdir(file_name)
        assert "the directory /#{file_name} should be synced across all syncboxes"
    end

    it "Should be able to handle a folder being deleted and a file added with the same name", :CFT_0037 => true do |t|
        desc t, "CFT_0037"
        pending "WE ARN'T ready for this test yet"
        folder_name = "The_Awesome_Folder"
        Dir.mkdir(folder_name)
        puts "Created /#{folder_name}\nSleeping for 10 seconds"
        sleep(10)
        puts "Deleting /#{folder_name} & creating /#{folder_name} as a new file"
        FileUtils.rm_rf folder_name
        create_file_named(folder_name,64)
        assert "is the file /#{folder_name} synced across all syncboxes?"
    end
end