require 'colorize'
require 'rspec'
require 'rspec/expectations'

class Testy

@@accepted_input = { "y" => 1, "yes" =>1, "1" => 1}

  def init
    @number_of_tests = 0
    @number_of_passes = 0
    @test_descriptions = []
    @test_failures = []
    puts "Enter Yes or y for Pass, No for failed".cyan.underline
  end

  def describe(description, test_id)
    puts "#{test_id} : A Syncbox #{description}\n".yellow
    @test_descriptions << "#{test_id} : A Syncbox #{description}"
  end

  def increment_test_count(response)
    if response.to_i == 0
      @test_failures << @number_of_tests
    end
    @number_of_passes += response.to_i
    @number_of_tests += 1
  end

  def number_of_tests_passed
    puts "Number of tests: " + "#{@number_of_tests}\n" + 
      "Failed:#{@number_of_tests - @number_of_passes}\n".red + 
      "Number of Success: #{@number_of_passes}".green

    puts "\nRunning on platform: " + RUBY_PLATFORM
    if @test_failures.count > 0
      puts "\n\n=========== FAILED TEST ===========\n".red
      failed_tests = ""
      @test_failures.each { |v| 
        puts @test_descriptions[v].red + "\n"
        failed_tests += "+\t__#{@test_descriptions[v].split(':')[0]}__ #{@test_descriptions[v].split(':')[1]}\n"
      }
      puts "Do you want to create a new test report?".green.underline
      if @@accepted_input[STDIN.gets.chomp.downcase]
        time = Time.now
        time = time.strftime("%m-%d-%Y %H:%M:%S")
        test_report_path = "#{File.expand_path(File.dirname(__FILE__))}/../Test_Report #{time}.md"
        File.open(test_report_path, "w") { |io| 
          io.write("Test Report - #{time}\n")
          io.write("----\n")
          io.write("###Failed Tests\n")
          io.write(failed_tests);
        }
        puts "Test report created at path\n#{File.expand_path(test_report_path)}".yellow
      end
    end
  end

  def get_input
    increment_test_count @@accepted_input[STDIN.gets.chomp.downcase]
  end

  def large_file_prompt
    puts "How large, in megs do you want this file to be?"
    STDIN.gets.chomp  
  end

  def assert(description)
    puts description.cyan
  end
end