# Architecture

## Overview

`ryzen-smu-cli` is a Windows-only .NET 8 console application. The executable
owns command parsing and safety policy; ZenStates-Core owns CPU discovery,
family-specific SMU transport, and low-level hardware access.

```text
Program
  |
  v
CliApplication -------- argument parsing and early validation
  |
  v
CommandRunner ---------- privilege checks, ordering, exit codes, output
  |           |
  |           +-------- CoreMapper
  |
  v
IRyzenController
  |
  +-------- ZenStatesRyzenController ---- ZenStates-Core / PawnIO / InpOut
  |
  +-------- AmdAcpiDowncoreController --- root\wmi:AMD_ACPI
```

Tests replace `IRyzenController` and `IPrivilegeChecker` with deterministic
test doubles. This keeps CI isolated from real firmware and drivers.

The hardware adapter exposes capability flags for optional SMU operations,
including FMax reads and writes. `CommandRunner` rejects an unsupported
operation before issuing its SMU command and treats zero or rejected FMax
results as failures.

## Command lifecycle

1. System.CommandLine parses options and applies syntax-level validators.
2. Parser-only commands (`--help` and `--version`) return without checking
   Windows, elevation, or hardware. Hardware information such as `--info`
   continues through the normal read-only hardware path.
3. `CommandRunner` verifies Windows and administrator privileges.
4. Hardware is initialized through a factory. A missing PawnIO installation is
   reported with an installation URL and bundled-installer guidance.
5. Enabled-core mapping is created only when an operation requires it.
6. Every dynamic range and topology assumption is validated before the first
   mutation.
7. Operations execute in a deterministic order and stop on the first failure.
8. A success message is written only after the underlying operation succeeds.
9. The controller is disposed on every path.

## Core coordinates

The CLI exposes two distinct index spaces:

- **Enabled-core index**: compact index over cores that currently answer the
  Curve Optimizer read command. Used by `--offset`.
- **Physical core slot**: fixed slot in the CCD topology, including disabled
  and factory-fused positions. Used by `--disable-cores`.

Each physical slot is represented as a CCD index and an index within the CCD.
ZenStates-Core converts those coordinates to the correct family-specific SMU
mask. This is important on Zen and Zen 2, where an eight-core CCD consists of
two four-core CCXs, while later desktop families use a single eight-core CCX.

## Downcore writes

Downcore changes use the motherboard-provided `AMD_ACPI` WMI interface rather
than an SMU mailbox. The request is a complete desired disable mask for every
CCD. Unspecified slots are enabled, and the change requires a reboot.

The application rejects:

- negative, duplicate, or out-of-range physical slot indices;
- a request that disables every physical slot;
- missing AMD ACPI commands;
- incomplete per-CCD results;
- access, initialization, and WMI failures.

## Native and managed packaging

The managed application is published as a single file. `inpoutx64.dll` remains
adjacent because ZenStates-Core loads it by name and it embeds the required
driver. PawnIO is different: its signed kernel driver must be installed
system-wide, while the signed hardware modules loaded into that driver are
embedded in ZenStates-Core. License and provenance files are external so users
can inspect them without unpacking the managed executable.

Release automation produces:

- a smaller framework-dependent win-x64 package;
- a self-contained win-x64 package with the .NET 8 runtime;
- a self-contained package that also carries the verified official PawnIO
  installer for interactive administrator-approved installation;
- a separate symbols package;
- SHA-256 checksums for every archive.

See [THIRD-PARTY-NOTICES.md](../THIRD-PARTY-NOTICES.md) for the dependency
inventory.
