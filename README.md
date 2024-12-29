# GDL90.NET - a .NET GDL90 Implementation

This repository contains .NET 8.0 code for an implementation of the GDL90 specification/protocol. There are three folders that comprise this project: `gdl90_lib`, `gdl90_tests`, `gdl90_console`.

## Project descriptions

This project is structured such that you may use Visual Studio 2022 or Visual Studio Code, to build, run, and debug the project. The only hard requirement is .NET 8.0.

### gdl90_lib

This is the primary project, its output is a .NET library and contains the actual GLD90 implementation. Information specific to the library and how to use it are in the [README file under that project folder](./gdl90_lib/README.md).

### gdl90_tests

This project is a test harness for the `gdl90_lib` project. It contains unit tests for the library and is primary intended for local development and CI pipelines.

### gdl90_console

The Console project is essentially an example project demonstrating usage of the `gdl90_lib` library. It is a .NET console application that implements two primary modes:

* Listening on a UDP port for GDL90 messages from a [Stratux](http://stratux.me/) device.
* Reading from a raw binary GDL90 messages from a file.

The console app can also save data received from UDP to a file, see the command usage with `--help` for details on the specific arguments.
