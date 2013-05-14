#Cloud-FunctionalTest-Scripts
============================
----

Make sure that you have Ruby 1.9.3 + on your system.
 
Bundle requires the bundle gem.

		$ gem install bundler

Then do

		$ bundle install

This will install any gems that are required to run the scripts in this repo.


Usage
-----
run all tests within a specific syncbox

	$ rake tests:all_tests[/Path/To/Syncbox] 
	
run one specific test using the test case id (i.e. CFT_0001) in a syncbox
	
	$ rake tests:run_test[/Path/To/Syncbox,TEST_CASE_ID]

Options
----

Skip test while executing
	
	skippable=true

Check local index after each test clean-up

	index=/path/to/local/index

