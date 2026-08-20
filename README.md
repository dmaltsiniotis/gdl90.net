# GDL90.NET - a .NET GDL90 Implementation

This code repository contains a .NET 10.0 code implementation of the Garmin GDL90 specification/protocol. It was designed to work in tandem with a [Stratux device](https://stratux.me/), but isn't required to do so.

The solution is split into four focused projects:

* `GDL90.Core` - Protocol message types, framing, CRC handling, and parsing pipeline.
* `GDL90.Adapters` - Stream and transport adapters that move raw bytes into parser channels.
* `GDL90.Console` - Runnable reference application demonstrating end-to-end usage.
* `GDL90.Tests` - Unit and integration coverage for protocol parsing and adapters.

## Project description

This project is structured so you can use Visual Studio 2026 or Visual Studio Code to build, run, and debug. It _may_ work for lower versions of .NET, the main requirement being the use of System.Threading.Channels, which was introduced in .NET Core 3.0.

### GDL90.Core

This is the primary protocol library. It contains:

* Message models for supported GDL90 message types.
* Framing and byte-level parsing logic.
* CRC computation and validation logic.
* Channel-based parser flow for converting byte chunks into typed messages.

This project is where core protocol behavior lives: no transport assumptions, no device-specific networking policy, and no application UI concerns.

### GDL90.Adapters

This project contains adapter components that acquire raw bytes and feed them into parser channels.

Current implementations include file and UDP adapters.

### GDL90.Tests

This project is the test harness for `GDL90.Core` and `GDL90.Adapters`. It includes:

* Unit tests for message parsing behavior.
* Integration-style tests that stream real fixture data through the channel pipeline.
* Fixture archives under `GDL90.Tests/data/` that are copied to test output as part of build.

The data folder contains zip-compressed binary files of GLD90 UDP messages received by a Stratux device. The zip format helps keep the repository size down, and the the stream is decompressed on the fly by the tests that need that data.

### GDL90.Console

The console application is a reference host that demonstrates how to wire adapters and parser channels together in a real process.

It currently supports:

* Listening on a UDP port for GDL90 messages (default port 4000, common with Stratux).
* Reading GDL90 byte streams from a file.
* Writing received raw framed bytes back to an output file.
* Verbose logging and optional processing statistics.

Use this project as a practical integration example, not as the authoritative source of protocol behavior.

## Build And Test

Build the full solution in Release mode:

```cmd
dotnet build GDL90.sln -c Release
```

Run the unit tests:

```cmd
dotnet test
```

If you only want to build the library project that is published by CI:

```cmd
dotnet build GDL90.Core/GDL90.Core.csproj -c Release
```

### Running

Run the example console application with default parameters that listen on UDP port 4000 for GDL90 messages:

```cmd
dotnet run --project GDL90.Console
```

Run the example console application and output the received data to file as well:

```cmd
dotnet run --project GDL90.Console --outfile "gdl90data.bin"
```

Run the example console application, reading data from a saved file:

```cmd
dotnet run --project GDL90.Console --infile "gdl90data.bin"
```

Run with debug logging on:

```cmd
dotnet run --project GDL90.Console --loglevel 4
```

Run in "benchmark" mode (must unzip the test data first):

```cmd
dotnet run --project GDL90.Console --infile GDL90.Tests\data\ADS-B_TEST-DATA-SMALL-TRAFFIC.bin --benchmark --stats --loglevel 3
```

Show command-line usage:

```cmd
dotnet run --project GDL90.Console -- --help
```

## Notes

* The parser pipeline is channel-based in current implementation.
* Test fixture archives in `GDL90.Tests/data/` are copied to output for deterministic test execution.
