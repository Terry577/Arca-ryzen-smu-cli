# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Fixed

- Vcore streaming now skips missed cadence slots and waits for the next
  future slot instead of immediately catching up, preventing duplicate
  monotonic millisecond timestamps after a slow hardware read.

## [0.3.4] - 2026-08-01

### Changed

- Fire Range (`1Ah/44h`, mobile package) is no longer rejected solely because
  of its package class. Its desktop-die per-core selector is now admitted only
  after the existing CCD-count, eight-slots-per-CCD, enabled-core-count, and
  complete fuse-map checks pass.

### Tests

- Fire Range now has an explicit platform-gate regression test, while the
  heterogeneous Family 1Ah mobile families remain fail-closed.
- The terse Curve Optimizer output contract explicitly covers a positive
  factory offset of `+10` as a valid signed read-back value.

## [0.3.3] - 2026-08-01

### Added

- Silicon-specific SMU SVI CPU-core rail mappings for Phoenix, Phoenix 2,
  Hawk Point, Strix Point, Krackan Point / refresh, and Strix Halo. These are
  recorded as structural candidates until synchronized hardware captures
  qualify each layout.
- Exact PM-table version-and-size mappings for known Raphael, Dragon Range,
  and Family 1Ah model 44h revisions, with verified and structural confidence
  recorded separately.
- `--diagnose-vcore`, `--samples`, and diagnostic use of `--interval-ms` for a
  read-only JSON report containing a typed source and confidence, CPU,
  motherboard, BIOS, firmware and SMU identity, topology qualification,
  selected samples, raw SVI candidates, and selected/register success and
  failure counts, together with the producing CLI version.
- A family/model/package-specific diagnostic register whitelist for supported
  Zen 4/5 platforms. Diagnostics never probe arbitrary SMN addresses.
- Platform, confidence, telemetry-decoder, sentinel-value, and diagnostic JSON
  tests covering the Zen 4/5 desktop, mobile, APU, and desktop-die mobile
  paths.
- Stable `Logical` and `SMT` rows in `--info`, sourced from CPU/CPUID topology;
  the Vcore diagnostic JSON carries the same logical-processor, threads-per-
  core, and SMT fields.

### Changed

- Vcore source selection now uses CPU family/model/package class plus exact
  PM-table metadata instead of marketing names or PM-table version alone. OEM
  and regional SKUs on the same silicon share a selector without a
  product-name allowlist.
- Each diagnostic sample reads its complete fixed whitelist under one PCI-bus
  lock through the official signed PawnIO `AMDFamily17` read-only SMN channel,
  including the Family 1Ah `0x000730xx` range.
- Diagnostic register decoding is role-aware: only `core-plane` candidates
  are converted from VID to volts. Status, unknown, and other-plane values
  remain typed raw evidence and are not presented as Vcore candidates.
- Known PM-table versions with an unexpected structure size now fail closed.
- `--info` now labels telemetry sources as `verified` or `structural mapping`.
- Family 1Ah model 44h mobile-package diagnostics are reported as Fire Range
  instead of Granite Ridge.
- Family 1Ah mobile package-level information and Vcore diagnostics no longer
  depend on an unqualified heterogeneous per-core map; per-core operations
  still fail closed until those selectors are hardware-qualified.
- Phoenix-family APUs and Fire Range now also fail closed for per-core
  operations until their fuse maps and SMU selectors are hardware-qualified;
  package-level information and Vcore diagnostics remain available.
- On fail-closed Phoenix, heterogeneous Family 1Ah, and Fire Range platforms,
  `--info` prefers the CPUID-derived physical-core count over the unqualified
  fuse-slot map.

### Fixed

- Granite Ridge PM tables `0x00620105` and `0x00620205` now read the
  live VDDCR CPU telemetry value at entry 49. Version 0.3.2 incorrectly read
  the peak/limit metric at entry 18, which systematically overstated Vcore.
- Raphael PM table `0x00540104` now reads live VDDCR at entry 47 instead of
  the peak/limit-style value at entry 18.

## [0.3.2] - 2026-07-31

### Added

- `--get-vcore` for a single invariant-format PM-table Vcore telemetry sample.
- `--stream-vcore` and `--interval-ms` for low-overhead sampling through one
  persistent PawnIO/SMU session.
- PM-table version, size, and selected Vcore mapping to `--info`.
- Explicit PM-table layout whitelisting, telemetry-value validation, stable
  stream framing, and automated output/error contract tests.

### Changed

- ZenStates-Core initialization diagnostics are redirected to standard error
  so machine-readable command output remains clean.

## [0.3.1] - 2026-07-29

### Added

- `--info` to print CPU identity and topology, motherboard and BIOS details,
  CPU firmware revision, and SMU version.
- An exact-output contract test and a read-only hardware smoke-test entry for
  the new information command.

## [0.3.0] - 2026-07-27

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

[Unreleased]: https://github.com/Terry577/Arca-ryzen-smu-cli/compare/v0.3.4...HEAD
[0.3.4]: https://github.com/Terry577/Arca-ryzen-smu-cli/compare/v0.3.3...v0.3.4
[0.3.3]: https://github.com/Terry577/Arca-ryzen-smu-cli/compare/v0.3.2...v0.3.3
[0.3.2]: https://github.com/Terry577/Arca-ryzen-smu-cli/compare/v0.3.1...v0.3.2
[0.3.1]: https://github.com/Terry577/Arca-ryzen-smu-cli/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/Terry577/Arca-ryzen-smu-cli/compare/v0.2.1...v0.3.0
[0.2.1]: https://github.com/Terry577/Arca-ryzen-smu-cli/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/Terry577/Arca-ryzen-smu-cli/compare/0.1.3...v0.2.0
[0.1.3]: https://github.com/Terry577/Arca-ryzen-smu-cli/compare/0.1.2...0.1.3
[0.1.2]: https://github.com/Terry577/Arca-ryzen-smu-cli/compare/0.1.0...0.1.2
[0.1.0]: https://github.com/Terry577/Arca-ryzen-smu-cli/releases/tag/0.1.0
