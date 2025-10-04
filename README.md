# EDID Reset Tool

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A lightweight, open-source Windows utility to fix intermittent audio dropouts caused by EDID (Extended Display Identification Data) handshake failures in multi-device chains (e.g., GPU → Monitor → eARC Soundbar like Sonos Beam). It works across all graphics drivers publishing High Definition Audio Devices (e.g., AMD, NVIDIA, Intel) and monitors supporting DDC/CI (Display Data Channel/Command Interface) for input toggling.

Built with C# (.NET 8+ must be installed), it embeds Microsoft's devcon.exe and NirSoft's ControlMyMonitor.exe for device management and monitor control. No installation required—portable EXE.

## Features

- **Automatic EDID Reset**: Toggles monitor inputs (e.g., DP ↔ HDMI) to force EDID renegotiation, rescans hardware, cycles audio devices, and restarts audio services.
- **Cross-Driver Compatibility**: Detects and targets any High Definition Audio Device (HDAUDIO-based).
- **DDC/CI Monitor Support**: Works with any DDC/CI-enabled monitor for non-invasive input switches.
- **Interactive Setup Wizard**: First-run detection of monitors and audio devices, with configurable delays.
- **CLI-Friendly**: Command-line switches for config viewing/editing, resets, and silent runs.
- **Scheduled Task Integration**: Optional auto-run on logon, unlock, or sleep resume (Kernel-Power Event ID 107).
- **Portable & Self-Contained**: Single EXE (<1MB) with embedded tools—no dependencies or installs (excluding .NET 8+).

## Quick Start

1. **Download**: Grab the latest release from [Releases](https://github.com/Flawkee/EDID-Reset-Tool/releases) (e.g., `EDIDResetTool.exe`).
2. **Run as Administrator**: Right-click → Run as administrator (required for cycle audio devices & restart audio service).
3. **First Run**: The Setup Wizard auto-launches:
   - Detects monitors via ControlMyMonitor (lists Device Name & Monitor ID).
   - Detects audio devices via devcon (lists names like "AMD High Definition Audio Device" with cleaned IDs).
   - Prompts for input toggle values (VCP Code 60, e.g., 15=HDMI, 17=DP) and sleep delays (in seconds).
   - Saves to `config.ini` in the EXE directory.
4. **Run Reset**: Run `EDIDResetTool.exe` via CLI with -r or --reset to execute the reset sequence.
5. **Verify**: Test audio after boot/sleep—should restore without cable unplugging.

**Note**: Place the EXE in a persistent folder (e.g., `C:\Tools`). Config is per-directory.

## Commands

Run `EDIDResetTool.exe --help` (or `-h`) for full usage. On first run, the wizard executes automatically.

```
=== EDID Reset Tool ===

Usage: EDIDResetTool.exe [options]

On first run, EDID Reset Tool Setup Wizard will execute automatically.

Options:
  -c, --config                   Prints current configuration.
  -r, --reset                    Reset current configuration and runs the Setup Wizard.
  -cst, --createscheduledtask    Creates a scheduled task to run EDID Reset Tool on user logon and computer wake from sleep.
  -dst, --deletescheduledtask    Deletes EDID Reset Tool Scheduled Task if exists.
  -q, --quiet                    Execute the program without user prompts.
  -v, --version                  Show Version.

  -s, --set                      Set configuration value (Key=Value), Available Keys:
                                     MonitorId="\\.\DISPLAY1\Monitor0"
                                     AlternateInput=15
                                     OriginalInput=17
                                     AudioId="HDAUDIO\FUNC_01&VEN_1002&DEV_AA01"
                                     SleepAfterFirstToggle=5
                                     SleepAfterSecondToggle=5
                                     SleepAfterRescan=5
                                     SleepAfterDisable=5
                                     SleepAfterEnable=5
```

- **Examples**:
  - `EDIDResetTool.exe --quiet`: Silent mode (no output/prompts) - works only when configuration is already set.
  - `EDIDResetTool.exe --config`: View current settings.
  - `EDIDResetTool.exe --set AudioId=HDAUDIO\FUNC_01&VEN_1002&DEV_AA01`: Update a single key-value pair.
  - `EDIDResetTool.exe --createscheduledtask`: Set up auto-run on logon/unlock/sleep resume.

## Configuration

- **File**: `config.ini` (auto-created in EXE directory).
- **Wizard**: Guides detection and setup (monitors via Device Name/Monitor ID; audio via name/cleaned ID like `HDAUDIO\FUNC_01&VEN_1002&DEV_AA01`).
- **Manual Edits**: Use `--set Key=Value` for updates (e.g., sleep values in seconds, converted to ms internally).
- **Keys** (from help):
  - `MonitorId`: e.g., `\\.\DISPLAY1\Monitor0` (from wizard).
  - `AlternateInput` / `OriginalInput`: VCP 60 values (e.g., 15=HDMI, 17=DP; verify in monitor OSD).
  - `AudioId`: Cleaned HDAUDIO ID (no SUBSYS/REV/instance).
  - Sleep keys: Seconds (e.g., 5 → 5000ms delays between steps).

Example `config.ini`:
```
[Settings]
MonitorId=\\.\DISPLAY1\Monitor0
AlternateInput=15
OriginalInput=17
AudioId=HDAUDIO\FUNC_01&VEN_1002&DEV_AA01
SleepAfterFirstToggleMs=5000
SleepAfterSecondToggleMs=2000
SleepAfterRescanMs=2000
SleepAfterDisableMs=3000
SleepAfterEnableMs=2000
```

## Scheduled Tasks

- **Create**: `--createscheduledtask` (or `-cst`).
  - **General**: Author: "EDID Reset Tool"; Description: "Running EDID Reset Tool in quiet mode. Created by EDID Reset Tool."; Run with highest privileges; Only when user logged on.
  - **Triggers**: At logon; On Workstation Unlock; On Event (System log, Microsoft-Windows-Kernel-Power, ID 107).
  - **Action**: Starts `EDIDResetTool.exe --quiet` from EXE location.
- **Delete**: `--deletescheduledtask` (or `-dst`).

Requires Admin rights. Ideal for auto-fixing on boot/sleep resume.

## License

This project is licensed under the [MIT License](LICENSE). See the [LICENSE](LICENSE) file for details.

Free to fork, modify, and distribute. Contributions welcome via pull requests!

**Third-Party Components**:
- **devcon.exe**: Microsoft Windows Driver Kit (WDK) - [License](https://docs.microsoft.com/en-us/windows-hardware/drivers/devtest/devcon).
- **ControlMyMonitor.exe**: NirSoft Freeware - [License](https://www.nirsoft.net/utils/control_my_monitor.html).

## Contributing

1. Fork the repo.
2. Create a feature branch (`git checkout -b feature/amazing-feature`).
3. Commit changes (`git commit -m 'Add amazing feature'`).
4. Push to branch (`git push origin feature/amazing-feature`).
5. Open a Pull Request.

Report issues or suggest features in [Issues](https://github.com/Flawkee/EDID-Reset-Tool/issues).

## Support

- **Tested On**: Windows 11 (x64), AMD/NVIDIA/Intel GPUs, DDC/CI monitors.
- **Known Issues**: Requires Admin for tasks/device enables; some monitors may need manual VCP verification.
- **Contact**: Open an issue on GitHub.

Thanks for using EDID Reset Tool—happy audio troubleshooting! 🎧