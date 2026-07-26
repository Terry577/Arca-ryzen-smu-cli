# ryzen-smu-cli

[![CI](https://github.com/Terry577/Arca-ryzen-smu-cli/actions/workflows/ci.yml/badge.svg)](https://github.com/Terry577/Arca-ryzen-smu-cli/actions/workflows/ci.yml)
[![Latest release](https://img.shields.io/github/v/release/Terry577/Arca-ryzen-smu-cli)](https://github.com/Terry577/Arca-ryzen-smu-cli/releases/latest)
[![License: GPL-3.0](https://img.shields.io/github/license/Terry577/Arca-ryzen-smu-cli)](LICENSE)

`ryzen-smu-cli` is a Windows command-line interface for reading and changing
selected AMD Ryzen System Management Unit (SMU) settings. It uses
[ZenStates-Core](https://github.com/irusanov/ZenStates-Core) for hardware
access.

> [!WARNING]
> SMU and Curve Optimizer changes can make a system unstable, corrupt work in
> progress, or prevent Windows from booting normally. Overclocking may also
> affect warranty coverage. Make small changes, validate them under load, and
> keep a known-good BIOS profile. Core-disable changes take effect only after a
> reboot.

## Download

Download the latest version from
[GitHub Releases](https://github.com/Terry577/Arca-ryzen-smu-cli/releases/latest).

| Package | Runtime requirement | Intended use |
| --- | --- | --- |
| `win-x64-self-contained-with-pawnio.zip` | None | Easiest clean-system setup; includes the official interactive PawnIO installer |
| `win-x64-self-contained.zip` | None | Recommended for systems that already have PawnIO |
| `win-x64-framework-dependent.zip` | .NET 8 x64 runtime | Smaller download |
| `symbols.zip` | Not executable | Debugging and crash analysis |

Extract the complete ZIP. Do not copy only `ryzen-smu-cli.exe`;
`inpoutx64.dll` and the adjacent license files are part of the application.
The `with-pawnio` package also contains `PawnIO_setup-v2.2.0.exe`. Run that
installer interactively as an administrator before the first hardware command.
Do not select the unsigned unrestricted PawnIO edition.

Verify a downloaded archive from PowerShell:

```powershell
Get-FileHash .\ryzen-smu-cli-v0.2.1-win-x64-self-contained.zip -Algorithm SHA256
Get-Content .\checksums-sha256.txt
```

## Requirements

- Windows 10 or Windows 11 x64
- A supported AMD Ryzen processor and motherboard firmware
- The signed [PawnIO](https://pawnio.eu/) system driver; PawnIO modules are
  already embedded in the application
- The [.NET 8 x64 runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
  only when using the framework-dependent package
- The .NET 8 SDK for development
- An elevated terminal for every hardware operation

`--help` and `--version` do not initialize hardware and work without
administrator privileges. The program is Windows-only even when it is built
from WSL. See the [installation guide](docs/INSTALLATION.md) for clean-system,
upgrade, and troubleshooting instructions.

## Run

Open an Administrator PowerShell in the extracted release directory:

```powershell
.\ryzen-smu-cli.exe --get-pbo-scalar
.\ryzen-smu-cli.exe --get-enabled-cores
```

Start with reads. Review the enabled-core mapping before setting offsets or
changing core-disable masks.

## Build from source

Clone recursively because ZenStates-Core is a Git submodule:

```powershell
git clone --recurse-submodules https://github.com/Terry577/Arca-ryzen-smu-cli.git
cd Arca-ryzen-smu-cli
.\scripts\Test.ps1
```

From WSL, call the installed Windows SDK through the same helper:

```bash
powershell.exe -NoProfile -ExecutionPolicy Bypass \
  -File "$(wslpath -w "$PWD/scripts/Test.ps1")"
```

The framework-dependent publish directory is written to
`artifacts/win-x64`.

## Commands

```text
--offset <offsets>
    Set Curve Optimizer offsets for enabled cores. Use either a positional
    CSV (`-10,5,-20`) or explicit enabled-core assignments
    (`0:-10,1:5,2:-20`). Do not mix the two forms. Accepted CLI values are
    -50 through 50; CPU firmware is the final authority.

--get-offsets-terse
    Print enabled-core offsets as one CSV line with no heading.

--get-physical-cores
    Print the factory-fused state of every physical core slot.

--get-enabled-cores
    Print the current enabled-core to physical-slot mapping.

--disable-cores <indices>
    Set the complete list of physical core slots to disable. Unspecified
    slots are enabled. A reboot is required.

--enable-all-cores
    Enable every physical core slot. A reboot is required.

--set-pbo-scalar <1-10>
    Set the PBO scalar to a whole number from 1 through 10.

--get-pbo-scalar
    Print the current PBO scalar.
```

Run `ryzen-smu-cli.exe --help` for the canonical option descriptions. Multiple
non-conflicting operations may be supplied in one invocation. The command
stops at the first failed hardware operation and never prints a success message
for a rejected write.

Core terminology matters:

- An **enabled-core index** is the compact zero-based index used by
  `--offset`.
- A **physical core slot** is the fixed topology position used by
  `--disable-cores`.

Use `--get-enabled-cores` to see the mapping before changing either setting.

## Compatibility

Actual CPU support is determined by ZenStates-Core, the CPU's SMU command
table, motherboard firmware, and the motherboard's optional `AMD_ACPI` WMI
interface.

Release 0.2.x was validated with:

- AMD Ryzen 7 9800X3D;
- Windows 11 x64;
- PawnIO 2.2.0;
- all eight physical cores enabled;
- read-only PBO scalar, Curve Optimizer, fused-core, and enabled-core queries.

That validation does not guarantee support for every CPU or motherboard.
Unsupported SMU commands and missing downcore interfaces return nonzero exit
codes rather than being treated as success.

## Exit codes

| Code | Meaning |
| ---: | --- |
| 0 | Success |
| 1 | Command-line parse error or administrator privileges are missing |
| 2 | Hardware initialization failed |
| 3 | The CPU does not expose the requested SMU operation |
| 4 | Invalid request discovered after parsing |
| 5 | Core index is outside the detected topology |
| 6 | An SMU operation failed or was rejected |
| 7 | AMD ACPI downcore support is unavailable or failed |
| 8 | Enabled cores could not be mapped reliably |
| 9 | Unsupported operating system |

## Development

The solution separates command-line parsing, domain validation, hardware
adapters, and orchestration so most behavior can be tested without touching
real hardware. `scripts/HardwareSmokeTest.ps1` contains only read operations
for an elevated post-build check.

See
[CONTRIBUTING.md](https://github.com/Terry577/Arca-ryzen-smu-cli/blob/master/CONTRIBUTING.md)
for the workflow and
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for bundled dependency and
native-component details.

Additional documentation:

- [Changelog](CHANGELOG.md)
- [Installation](docs/INSTALLATION.md)
- [Architecture](https://github.com/Terry577/Arca-ryzen-smu-cli/blob/master/docs/ARCHITECTURE.md)
- [Release process](https://github.com/Terry577/Arca-ryzen-smu-cli/blob/master/docs/RELEASING.md)
- [Security policy](https://github.com/Terry577/Arca-ryzen-smu-cli/blob/master/SECURITY.md)
- [v0.2.1 release notes](https://github.com/Terry577/Arca-ryzen-smu-cli/blob/v0.2.1/docs/releases/v0.2.1.md)

The project is licensed under GPL-3.0. All credit for the hardware library
belongs to the ZenStates-Core contributors. The implementation also draws on
the public [SMUDebugTool](https://github.com/irusanov/SMUDebugTool) examples.
