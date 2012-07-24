echo off


REM **
echo ************
echo NOTE: This batch file must be run from a Visual Studio Command Prompt
echo ************

REM ** Set up the Certificates
set SERVER_NAME=HelloWorldService
set CLIENT_NAME=HelloWorldClient
set ROOT_NAME=SampleRoot

echo %ROOT_NAME%

certmgr -del -r LocalMachine -s Root -c -n %ROOT_NAME%

REM Server Certs
certmgr -del -r LocalMachine -s TrustedPeople -c -n %SERVER_NAME%

REM CLIENT Certs
certmgr -del -r LocalMachine -s TrustedPeople -c -n %CLIENT_NAME%

echo ************
echo Making Root cert
echo ************
makecert -pe -n CN=%ROOT_NAME% -ss Root -sr LocalMachine -a sha1 -sky signature

echo ************
echo Server cert setup starting
echo %SERVER_NAME%
echo ************
echo Making server cert
echo ************
makecert.exe -sr LocalMachine -ss TrustedPeople -a sha1 -n CN=%SERVER_NAME% -sky exchange -pe -is Root -ir LocalMachine -in %ROOT_NAME%

echo ************
echo CLIENT cert setup starting
echo %CLIENT_NAME%
echo ************
echo Making CLIENT cert
echo ************
makecert.exe -sr LocalMachine -ss TrustedPeople -a sha1 -n CN=%CLIENT_NAME% -sky exchange -pe -is Root -ir LocalMachine -in %ROOT_NAME%
echo ************

