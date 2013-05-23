require 'fileutils'

def random_alphanumeric(size=16)
  s = ""
  size.times { s << (i = Kernel.rand(62); i += ((i < 10) ? 48 : ((i < 36) ? 55 : 61 ))).chr }
  s
end

def create_file_named(file_name='File.txt',size=16)
  file_name ||= "File_#{Kernel.rand(32)}.txt"
  File.open(file_name, 'w') do |file|
    file.write("This file was created at #{Time.now}\nNamed:#{file_name}\n")
    file.write(random_alphanumeric(size))
  end
  file_name
end

def create_large_file(file_name,size=10)
  fills = '1'*1024876
  file_name ||= "Large_File.txt"
  File.open(file_name, 'w') do |f|
    f.write(Time.now)
    f.write(file_name)
    size.to_i.times { f.write(fills) }
  end
end

def create_files_and_folders(folder_count = 10, file_count= 10, size=Kernel.rand(100000))
  folder_count ||= 10
  file_count ||= 10
  size ||= 10
  
  folder_count.to_i.times { |folder_index|
    puts folder_index
    folder_name = "Folder_#{sprintf("%04d", folder_index)}"

    Dir.mkdir(folder_name)
    Dir.chdir(folder_name)

    file_count.to_i.times { |file_index| 
      create_file_named("File_#{sprintf("%05d", file_index)}.txt",size)
    }

    Dir.chdir('..')
  }
end

def copy_with_path(src,dest)
  FileUtils.mkdir(dest)
  FileUtils.cp_r(src,dest)
end