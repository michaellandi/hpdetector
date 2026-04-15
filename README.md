# HPDetector

Last updated: April 15, 2026

**HPDetector** is a hidden port detector that catches inconsistencies between what the operating system reports as bound ports and what is actually bound — a common sign of a rootkit hiding a backdoor.

It works by attempting to bind to every TCP and UDP port (1–65535). If a port throws a binding exception (meaning something is already bound to it) but does not appear in the OS port list (`netstat` on Windows, `/proc/net/tcp` and `/proc/net/udp` on Linux), it is flagged as a hidden port.

Available for both **Windows** (.NET 2.0, GUI) and **Linux** (Java, CLI).

## How It Works

1. Parse the OS port list (Netstat / `/proc/net/tcp` and `/proc/net/udp`)
2. Attempt to bind a socket to every port from 1–65535
3. Any port that raises a bind exception but is absent from the OS list is reported as hidden
4. Redundant checks are performed to reduce false positives

A hidden port does not definitively confirm a rootkit, but it is a strong indicator worth investigating further. Follow-up steps include attempting to connect to the hidden port (e.g. via `telnet`) and running additional rootkit tools such as Rootkit Revealer.

## Windows

**Requirements:** .NET Framework 2.0

Run `HPDetector.exe` as Administrator. The GUI allows a full port scan (1–65535) or a base scan (1–1023) and displays any hidden TCP or UDP ports found.

## Linux

**Requirements:** JRE or `gcj` (GNU Compiler for Java), root privileges

**Run with JRE:**
```bash
su -
cd hpdetector_linux/class
java HPDetector
```

**Run pre-built binary:**
```bash
su -
cp hpdetector_linux/bin/hpdetector /bin/hpdetector
/bin/hpdetector
```

Root privileges are required to attempt binding to all ports.

## Project Structure

```
hpdetecor_windows/    # Windows GUI implementation (C#/.NET)
hpdetector_linux/
  src/                # Linux CLI implementation (Java)
  class/              # Compiled .class file
  bin/                # Pre-built native binary (gcj)
```

## License

MIT License. See [LICENSE](LICENSE) for details.
