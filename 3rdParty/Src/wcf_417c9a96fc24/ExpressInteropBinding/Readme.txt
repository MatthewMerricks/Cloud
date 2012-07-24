Microsoft WCF Interop Bindings
------------------------------

Last Changed
6/10/2011

1. System Requirements
----------------------

- .NET Framework 4.0
- The extension installs on Visual studio 2010 Standard and above
- To build, Visual Studio SDK 2010 is required. You can download it from
http://www.microsoft.com/downloads/en/details.aspx?FamilyID=47305cf4-2bea-43c0-91cd-1b853602dcc5&displaylang=en


2. Usage instructions
---------------------

The installer drops two files in the selected installation folder, 
a- Microsoft.ServiceModel.Interop.dll, which is the assembly that contains the WCF interop bindings and it is automatically registered in the GAC, 
b- A Visual Studio extension "Microsoft.ServiceModel.Interop.Extension.vsix" that you can run to register the configuration wizard and project templates into Visual Studio 2010. 

The visual extension registers two new project templates under the "WCF" category that you can use to create a new service configured with one of the provided interop bindings for Oracle Weblogic, IBM Websphere, Metro or Apache Axis 2. Those project templates are "Express Interop WCF Service Library" for creating a new WCF service as part of the service library, or "Express Interop WCF Application" for creating a new WCF service as part of a Web Application. The templates will automatically launch a configuration wizard for selecting the desired interop scenario.

 
3. System Requirements
----------------------
This projects lives in wcf.codeplex.com


4. Feedback
----------------------
Contact Abu.Obeida@microsoft.com or soaptest@microsoft.com

