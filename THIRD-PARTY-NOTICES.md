# Third-party notices

This distribution includes or links the following components. The complete
license texts are copied into every publish directory.

## ZenStates-Core

- Project: <https://github.com/irusanov/ZenStates-Core>
- Pinned version: `v1.90`
- Commit: `999cdb6dec5ccb6dcde37a02ad1d6d878cb55cbf`
- License: GNU General Public License v3.0
- Published license file: `ZenStates-Core.LICENSE.txt`

## PawnIO modules

ZenStates-Core embeds signed PawnIO modules originating from
<https://github.com/namazso/PawnIO.Modules/releases/tag/0.2.4>.

- License supplied by ZenStates-Core: GNU Lesser General Public License v2.1
- Published license file: `PawnIO.COPYING.txt`

## PawnIO official installer

The optional `self-contained-with-pawnio` release package redistributes the
unmodified official PawnIO installer.

- Project: <https://pawnio.eu/>
- Installer source:
  <https://github.com/namazso/PawnIO.Setup/releases/tag/2.2.0>
- File: `PawnIO_setup-v2.2.0.exe`
- File and product version: `2.2.0.0`
- SHA-256:
  `1f519a22e47187f70a1379a48ca604981c4fcf694f4e65b734aaa74a9fba3032`
- Authenticode signer: `namazso.eu`
- Binary distribution license: Proprietary freeware; upstream permits
  redistribution of the official installer

The installer is not linked into the application and is not installed
silently. Users must explicitly run it with administrator privileges and
accept its installation choices.

## InpOut32/InpOutx64

- Author: Phil Gibbons / Highresolution Enterprises, with portions by
  logix4u.net
- Project page: <https://www.highrez.co.uk/downloads/inpout32/>
- Official binary distribution: `InpOutBinaries_1501.zip`
- Bundled file: `inpoutx64.dll`
- File version: `1.5.0.0`
- SHA-256:
  `5f27ed4d5cd58a1ee23deeb802e09e73f3a1d884ce2135f6e827f67b171269e7`
- License: MIT-style permission notice supplied with ZenStates-Core
- Published license file: `InpOut.LICENSE.txt`

InpOut is a legacy kernel-access component. Its upstream author no longer
actively supports it. The DLL is not Authenticode-signed, so its official
distribution hash is pinned above. It must remain next to
`ryzen-smu-cli.exe`; it installs/opens its embedded driver when hardware access
is initialized and therefore requires administrator privileges.

## NuGet dependencies

The shipped managed executable includes:

- System.CommandLine 2.0.10 — MIT;
- System.Management 10.0.10 — MIT;
- their transitive dependencies as recorded by NuGet restore.

Package metadata, source links, copyright notices, and license expressions are
available from each package's NuGet page and in the local NuGet package cache.
Test-only packages are not part of the release executable.

The self-contained release also includes the .NET 8 runtime. Its
`dotnet-LICENSE.txt` and `dotnet-ThirdPartyNotices.txt` files are copied into
that archive by the release packaging script.
