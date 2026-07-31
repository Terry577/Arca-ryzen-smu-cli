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
including FMax reads and writes and mapped CPU-rail Vcore telemetry.
`CommandRunner` rejects an unsupported operation before issuing its SMU
command and treats invalid or rejected results as failures.

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

## CPU identity and topology information

CPU name, CPUID, logical-processor count, and threads per core are read from
the CPU/CPUID data initialized by ZenStates-Core rather than Windows processor
enumeration. `--info` always emits separate `Logical` and `SMT` rows so
consumers do not need to infer SMT from a localized operating-system label.

The physical-slot fuse map remains authoritative only where the per-core
topology is qualified. On a fail-closed Phoenix-family, heterogeneous Family
1Ah, or Fire Range path, the `--info` physical-core count prefers CPUID
`topology.cores`; the unqualified fused-slot and enabled-core values remain
diagnostic evidence and are not presented as the user-facing physical count.
The Vcore diagnostic JSON records both the user-facing physical/logical/SMT
values and the raw reported slot/enabled values.

## Vcore telemetry

Desktop-die Vcore is read from an exact SMU PM-table layout. Mobile and APU
silicon with no qualified PM-table rail uses a family/model-specific SMN SVI
register. Neither path substitutes per-core/current VID for the CPU supply
rail.

`VcoreTelemetryLayout` keys PM mappings by CPU family, model, package class,
table version, and structure size. `SviTfnVcoreTelemetry` records the exact
read-only register, VID bit field, optional status register, platform, and
mapping confidence. Unknown metadata is unsupported rather than an invitation
to guess an index or nearby register.

`--get-vcore` performs one mapped read. `--stream-vcore` initializes PawnIO,
ZenStates-Core, and the SMU once, then refreshes the selected PM or SVI source
in the same controller session. This persistent mode is required for sampling
intervals such as 150 ms; starting a new process for every sample would make
the timing and overhead unsuitable.

`--diagnose-vcore` selects a fixed raw-register whitelist from CPU
family/model/package. Each sample reads that whitelist under one PCI-bus lock
through the signed PawnIO `AMDFamily17` read-only SMN module. The CLI accepts
no arbitrary address argument. Only descriptors typed as `core-plane` are
decoded as VID candidates; status, unknown, and other-plane registers remain
raw evidence. For a selected SVI source, the reported voltage is derived from
the same captured raw value. The JSON also records typed source/confidence,
hardware and firmware identity, physical/logical core counts, threads per core
and SMT state, raw slot/enabled counts, topology qualification, timing,
cancellation, and success/failure counts.

Stream stdout is a headerless tab-separated protocol:

```text
VCORE	sequence	elapsed_ms	utc_timestamp	volts
```

All numeric fields use invariant culture, every record is flushed
immediately, and diagnostics use stderr. Ctrl+C cancels the stream cleanly.
Read failures stop the stream with exit code 6; unsupported layouts fail
before the first stdout record with exit code 3. A closed stdout pipe also
terminates through the normal failure path, disposes the controller, and
releases the PawnIO session; the PCI mutex is held only around an individual
PM-table refresh, SVI read, or diagnostic batch, never between samples.

## Core coordinates

The CLI exposes two distinct index spaces:

- **Enabled-core index**: compact index over cores that currently answer the
  Curve Optimizer read command. Used by `--offset`.
- **Physical core slot**: fixed slot in the CCD topology, including disabled
  and factory-fused positions. Used by `--disable-cores`.

Each qualified physical slot is represented as a CCD index and an index within
the CCD. ZenStates-Core converts those coordinates to the correct
family-specific SMU mask. This is important on Zen and Zen 2, where an
eight-core CCD consists of two four-core CCXs, while later desktop families
use a single eight-core CCX. Phoenix-family APUs, Family 1Ah heterogeneous
mobile parts, and Fire Range are not forced into an unverified desktop
eight-slots-per-CCD model: package-level information and Vcore remain
available, while per-core commands fail closed until their fuse topology and
selectors are hardware-qualified.

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
