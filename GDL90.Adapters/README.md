# GDL90 Adapters

This purpose of this library is to provide common help functions that consumers of GLD90.Core might otherwise need to implement themselves, such as listening to a UDP socket, or reading from messages from a file.

Adapters are built in such a way to chain seamlessly with and into the byte-stream message parsing functionality of GDL90.Core.

## Adapters

These adapters are provided today:

- UDP Listener
- File Loader
