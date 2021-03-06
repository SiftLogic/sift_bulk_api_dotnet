.Net Bulk API
=============

This demonstrates how to connect to the bulk server with .Net using C#. Currently, HTTP and FTP are supported for connections. Unlike FTP, HTTP requires no username just the password (auth token). The apikey and password/auth token are found in the UI: API Keys -\> Manage -\> Actions -\> Access Details. Once you have that you can try the executable file for an upload demo. For example, using HTTP:
<pre>
  <code>
    CSharpFTPExample\bin\Release\CSharpFTPExample.exe -f test.csv -l /tmp -p e261742d-fe2f-4569-95e6-312689d049 --poll 10
  </code>
</pre>
The CLI is described in more detail with <code>CSharpFTPExample\bin\Release\CSharpFTPExample.exe</code>

It is recommended to include the Operations file and use the methods in there to customize your process. The methods are described in file.

Licensing
=========

Copyright 2014 SiftLogic LLC

SiftLogic LLC hereby grants to SiftLogic Customers a revocable, non-exclusive, non-transferable, limited license for use of SiftLogic sample code for the sole purpose of integration with the SiftLogic platform.

Quick Start
===========

There is no installation step as all necessary files have been included. Although the solution needs to be built (F7) in Visual Studio.

* **Run the EXE on a CLI:** <code>CSharpFTPExample\bin\Release\CSharpFTPExample.exe</code> *The solution must be built for release mode to do this*
* **Load the solution into Visual Studio:** Open CSharpFTPExample\CSharpFTPExample.sln
* **Run the example:** With the solution open, click the start button
 * To configure command line options click on the CSharpFTPExample example properties->Debug->Edit Start Options
* **Run the tests:** With the solution open, TEST->Run->All Tests. Make sure to build (F7) first.
 * Note only with tests there may be an error: "NuGet Package restore failed for project Miscellaneous Files: Value cannot be null or an empty string. Parameter name: root." This is a NuGet configuration issue and is not a testing error.

Building
========

Make sure you are running C# \>= <b>5.5</b>.

Using Visual Studio Express 2013 Edition, but newer or slightly older versions of Visual Studio should be able to load the solution file in \CSharpFTPExample fine. The build is already configured you just have to run it (F7). We are using NuGet (built into modern Visual Studios) for package management.

Files And Folders
=================

* **CSharpFTPExample/:** Visual Studio project folder of the main code (also contains the solution). Important files and folders:
 * **bin/:** Contains the debug and release information and executables.
 * **packages/:** Contains packages used across the entire solution.
 * **Properties/:** Basic information about the project such as copyrights
 * **CSharpFTPExample.csproj:** The project file used to load FTP example code.
 * **CSharpFTPExample.sln:** The solution file for the entire project including the tests.
 * **Program.cs:** Used to run the program, accepts command line arguments.
 * **Operations.cs:** Object that interfaces with server connection modes.
 * **FtpOperations.cs:** Object that provides an FTP interface to the server.
 * **HttpOperations.cs:** Object that provides an HTTP interface to the server.
 * **I\*.cs:** An custom interface around a class. Since most mocking libraries including what we are using (Moq) cannot test non virtual methods, interfaces like this must be created around concrete classes.
 * **Wrapped\*.cs:** The implementation for the corresponding I\*.cs.
 * **packages.config:** XML Specification of the libraries this program uses.

* **CSharpFTPExampleTests/:** Visual Studio project folder of the tests for the main code. Important files and folders:
 * **bin/:** Contains the debug and release executables.
 * **Properties/:** Basic information about the project such as copyrights
 * **CSharpFTPExample.csproj:** The project file used to load FTP example test code.
 * **\*Test.js:** Unit tests of API functionality. It is recommended that you update these if you want to customize connection code.
 * **packages.config:** XML Specification of the libraries this program uses

* **test.csv:** A small sample records file. 
