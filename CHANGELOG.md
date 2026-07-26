# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- `--get-fmax` to read the current maximum boost-frequency limit in MHz.
- `--set-fmax <MHz>` to set the maximum boost-frequency limit using validated
  25 MHz steps.
- FMax capability detection, result checking, automated tests, and a read-only
  hardware smoke-test query.

## [0.2.1] - 2026-07-27

### Added

- A self-contained release archive containing the unmodified official PawnIO
  2.2.0 installer for clean-system setup.
- A dedicated installation, upgrade, removal, and troubleshooting guide.
- Release-time SHA-256 and Authenticode verification for the redistributed
  PawnIO installer.
- An automated test for the missing-PawnIO diagnostic.

### Changed

- Hardware initialization now reports actionable PawnIO installation
  instructions when the required system driver is absent.
- README, architecture, security, contributor, release, and third-party
  documentation now distinguish the installed PawnIO driver from the embedded
  PawnIO modules.
- Release automation now publishes four ZIP archives plus the checksum file.

### Security

- PawnIO installation remains interactive and administrator-approved; the
  application never performs a silent kernel-driver installation.
- The optional installer is pinned to the official PawnIO.Setup 2.2.0 release,
  SHA-256
  `1f519a22e47187f70a1379a48ca604981c4fcf694f4e65b734aaa74a9fba3032`,
  with a required valid `namazso.eu` Authenticode signature.

## [0.2.0] - 2026-07-27

This is the first maintained release of the `Terry577/Arca-ryzen-smu-cli`
fork. It is a substantial reliability, safety, packaging, and maintainability
update over 0.1.3.

### Added

- Typed command request, domain, service, and hardware-adapter layers.
- 49 automated tests covering parsing, topology, core mapping, operation
  failures, output contracts, safety checks, disposal, and exit codes.
- Dynamic support for up to 16 CCDs instead of the previous two-CCD limit.
- Explicit exit codes for initialization, unsupported operations, invalid core
  indices, SMU failures, downcore failures, and mapping failures.
- Complete license and provenance records for ZenStates-Core, PawnIO modules,
  and InpOutx64.
- Windows CI for restore, Release build, tests, publish, and artifact upload.
- Tag-driven GitHub Release packaging with framework-dependent,
  self-contained, symbols, and SHA-256 assets.
- WSL-compatible PowerShell build and read-only hardware smoke-test scripts.
- Contributor, security, architecture, release, and support documentation.

### Changed

- Upgraded ZenStates-Core from v1.75 to v1.90 and switched its SMU path from
  the embedded WinRing0 implementation to PawnIO.
- Upgraded System.CommandLine to 2.0.10 and System.Management to 10.0.10.
- `--disable-cores` now explicitly uses physical core-slot indices.
- `--offset` now explicitly uses compact enabled-core indices and accepts
  either positional CSV or `core:offset` assignments.
- Curve Optimizer input validation now accepts `-50` through `50`; firmware
  remains the final authority.
- `--get-offsets-terse` now emits exactly one CSV line without a heading.
- Hardware initialization is lazy, so `--help`, `--version`, parse failures,
  and empty invocation do not load drivers or require elevation.
- Release output is a framework-dependent single-file executable with its
  required native DLL and license files beside it.

### Fixed

- Restored the missing `inpoutx64.dll` and made native asset packaging an
  explicit build responsibility.
- Removed a legacy post-build copy chain that could hide a missing DLL by
  returning the status of a later successful copy.
- Correctly checks every SMU and AMD ACPI result before reporting success.
- Returns nonzero process exit codes instead of always returning zero.
- Validates malformed, mixed, duplicate, empty, and out-of-range arguments
  before initializing hardware.
- Validates all core indices before performing the first mutation.
- Prevents conflicting `--disable-cores` and `--enable-all-cores` requests.
- Refuses requests that would disable every physical core slot.
- Detects incomplete factory-fuse and AMD ACPI result maps.
- Retries enabled-core mapping and fails explicitly when the topology cannot
  be mapped reliably.
- Correctly encodes CCD/CCX/core coordinates on legacy Zen and Zen 2 CPUs by
  delegating family-specific mask construction to ZenStates-Core.
- Correctly disposes hardware resources on both success and failure paths.
- Fixed the incompatible single-file compression settings that previously
  made `dotnet publish` fail.
- Prevented Release builds from consuming a Debug ZenStates-Core assembly.

### Security

- Removed WinIo32 and WinRing0 files that were unnecessary in the win-x64
  release package.
- Pinned and verified the official InpOutx64 binary by SHA-256.
- Added dependency vulnerability scanning to the release checklist.
- Added administrator and platform checks before hardware initialization.

### Breaking changes

- Scripts that parsed the old heading from `--get-offsets-terse` must now
  consume the CSV line directly.
- Scripts must distinguish enabled-core indices (`--offset`) from physical
  core-slot indices (`--disable-cores`).
- Failed operations now return meaningful nonzero exit codes.
- The release version moves from 0.1.3 to 0.2.0.

## [0.1.3] - 2025-04-30

### Fixed

- Corrected terse Curve Optimizer output for systems with disabled cores.
- Clarified bitmask terminology.

## [0.1.2] - 2025-04-09

### Fixed

- Corrected project version metadata.

## [0.1.0] - 2025-04-09

### Added

- Initial tagged CLI release based on ZenStates-Core.

[Unreleased]: https://github.com/Terry577/Arca-ryzen-smu-cli/compare/v0.2.1...HEAD
[0.2.1]: https://github.com/Terry577/Arca-ryzen-smu-cli/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/Terry577/Arca-ryzen-smu-cli/compare/0.1.3...v0.2.0
[0.1.3]: https://github.com/Terry577/Arca-ryzen-smu-cli/compare/0.1.2...0.1.3
[0.1.2]: https://github.com/Terry577/Arca-ryzen-smu-cli/compare/0.1.0...0.1.2
[0.1.0]: https://github.com/Terry577/Arca-ryzen-smu-cli/releases/tag/0.1.0
