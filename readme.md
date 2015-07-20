# HmLib HomeMatic library for .NET
An easy to use .NET API for communicating with the HomeMatic CCU.


### Getting started
More information about getting started will be added to this document in the future. In the mean time, check the HmLib.Commandline project for examples of usage.

### Rationale
I started this library for fun and education.
First of all, I wanted to experiment with my HomeMatic device at home.
Second, this project is a way to experiment with the (currently) new Visual Studio 2015 and dnx project structure.
But the main purpose is, to create a reusable library with a strong focus towards an architecture and to learn from architecting a library.

If you discover a bug or would like to propose a new feature, please open a new issue.


### Thanks
I would like to thank the Homegear community for their [clear documentation](https://www.homegear.eu/index.php/Homegear_Reference) about the HomeMatic binary RPC protocol.
Also I took a some inspiration from the [HomegearLib.NET project](https://github.com/Homegear/HomegearLib.NET) for converting doubles to/from bin rpc formatted bytes.

## Status
[![Build status](https://ci.appveyor.com/api/projects/status/gqannv7xga01jau4?svg=true)](https://ci.appveyor.com/project/flwn/hmlib)

Version 1 alpha: not done, expect API changes.


### Features
Support only Bin RPC protocol. Can convert to messages to strong typed request/response objects.
Also a simple JSON writer is available for serializing binary messages to a JSON format. This could make debugging a lot easier.

Support sending messages to the HomeMatic CCU (tested v1 only) and also incomming events are supported.

### Roadmap

This project is not finished yet...

TODO:

* More documentation of course...
* Extend the proxy with more types.
* Write unit tests for request handlers.
* Add more events to server/client/requesthandler classes for logging and other use cases.
* Configure CI environment for testing and publishing NuGet package.