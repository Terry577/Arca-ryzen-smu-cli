# Security policy

## Supported versions

| Version | Supported |
| --- | --- |
| 0.2.x | Yes |
| 0.1.x and older | No |

Only the latest release line receives security fixes.

## Reporting a vulnerability

Do not open a public issue for a vulnerability that could permit arbitrary
kernel access, unsafe SMU writes, privilege escalation, or distribution of a
modified native binary.

Use GitHub's **Report a vulnerability** flow under the repository Security
tab when it is available. If private vulnerability reporting is unavailable,
contact the maintainer through the
[Terry577 GitHub profile](https://github.com/Terry577) and ask for a private
reporting channel without including exploit details in the initial message.

Include:

- affected version and commit;
- Windows, CPU, motherboard, BIOS, and AGESA versions when hardware-specific;
- reproduction steps and observed impact;
- whether administrator access is already required;
- hashes and provenance for any native files involved;
- a proposed fix, if one is available.

An acknowledgement is targeted within seven days. Confirmed issues will be
fixed privately, validated, and disclosed with an updated release and
appropriate credit unless coordinated disclosure requires a different plan.

## Security boundaries

This utility intentionally performs privileged hardware access. Administrator
access is required for every hardware operation. It is not a sandbox or a
security boundary.

Release packages contain `inpoutx64.dll`, an unsupported legacy component
whose DLL is not Authenticode-signed. The repository pins the official binary
by SHA-256, publishes that hash, and keeps the upstream license adjacent to the
binary. Verify `checksums-sha256.txt` before running a downloaded release.

The optional `self-contained-with-pawnio` package contains the unmodified,
official PawnIO installer. Release packaging pins its source and SHA-256 and
requires a valid `namazso.eu` Authenticode signature. The application never
installs the kernel driver silently; installation remains an explicit,
administrator-approved operation.

Automated tests and CI never execute hardware writes. The checked-in hardware
smoke test contains read operations only.
