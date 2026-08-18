# GDL90.NET - a .NET GDL90 Implementation

This repository contains .NET 10.0 code for an implementation of the GDL90 specification/protocol. There are three folders that comprise this project: `gdl90_lib`, `gdl90_tests`, `gdl90_console`.

## Project description

This project is structured such that you may use Visual Studio 2026 or Visual Studio Code, to build, run, and debug the project. The only hard requirement is .NET 10.0 (although the project is known to work with .NET 8.0 and lower).


### gdl90_lib

This is the primary project, its output is a .NET library and contains the actual GLD90 implementation. Information specific to the library and how to use it are in the [README file under that project folder](./gdl90_lib/README.md).

### gdl90_tests

This project is a test harness for the `gdl90_lib` project. It contains unit tests for the library and is primary intended for local development and CI pipelines.

### gdl90_console

The Console project is a fully featured example project demonstrating usage of the `gdl90_lib` library. It is a .NET console application that implements two primary modes:

* Listening on a UDP port for GDL90 messages from a [Stratux](http://stratux.me/) device.
* Reading from a raw binary GDL90 messages from a file.

It reads all available GDL90 messages and outputs them to the console window. The console app can also save data received from UDP to a file, see the command usage with `--help` for details on the specific arguments.

<!-- ### gdl90_minimal

The gdl90_minimal project is the smallest possible usage and implementation of the GLD90.net library. If you're looking for the quickest way to get started, this is it. -->

## Build And Test

Use the .NET SDK to build the solution and run the test project from the repository root.

Build the full solution in Release mode:

```bash
dotnet build gdl90.sln -c Release
```

Run the unit tests:

```bash
dotnet test gdl90.sln
```

If you only want to build the library project that is published by CI:

```bash
dotnet build gdl90_lib/gdl90_lib.csproj -c Release
```

### Running

Run the example console application with default parameters that listen on UDP port 4000 for GDL90 messages:

```bash
dotnet run --project gdl90_console
```

Run the example console application and output the received data to file as well:

```bash
dotnet run --project gdl90_console --outfile="gdl90data.bin"
```

Run the example console application, reading data from a saved file:

```bash
dotnet run --project gdl90_console --infile="gdl90data.bin"
```

Run with debug logging on:

```bash
dotnet run --project gdl90_console --loglevel 4
```
