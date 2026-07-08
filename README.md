# ScaleSwitcher

**English** | [日本語](./README.ja.md)

ScaleSwitcher is a lightweight Windows system tray utility built with WPF and .NET 10.
It allows you to quickly change display scaling (DPI) and screen resolutions for multiple monitors.

![ScaleSwitcher Demo](./ScaleSwitcher.png)

## Features

- **Left-Click to Cycle Scaling**: Instantly cycle through predefined scaling factors (e.g., 100% -> 150% -> 100%) on a selected display with a single click.
- **Modifier Chord Shortcut**: Instantly cycle through scaling percentages without using the mouse by pressing both left and right keys of a modifier (Shift, Ctrl, or Alt) simultaneously.
- **Dynamic Context Menu (Right-Click)**:
  - Dynamically generated submenus for changing scaling factor (DPI) per monitor.
  - Dynamically generated submenus for changing screen resolution per monitor.
  - Toggle "Run at Startup" to launch the app automatically on Windows login.
- **Settings Window**:
  - Select the target display for left-click/shortcut rotation.
  - Configure which scaling percentages are included in the rotation cycle via checkboxes.
  - Choose which modifier key is used for the shortcut key behavior.
- **Native DPI Awareness**: DPI Awareness (`PerMonitorV2`) ensures accurate display information detection.
- **Localization**: Displays in Japanese on Japanese OS environments, and defaults to English on others.

## System Requirements

- **OS**: Windows 10 / 11
- **Framework/Runtime**: .NET 10.0 Runtime (WPF enabled)

## Usage

1. Run the application to place it in the Windows system tray.
2. **Left-click** the system tray icon, or press your configured **modifier keys simultaneously** (e.g., left Shift + right Shift) to cycle the scaling percentage on your designated monitor.
3. **Right-click** the icon to access the context menu for changing individual display settings, accessing the settings UI, or managing startup execution.
4. Open the Settings window to configure the target monitor, select which scaling options to include in the cycle, and define the shortcut key configuration.

## Build & Run

### Run in Development
```bash
dotnet run
```

### Build the Project
```bash
dotnet build
```

### Create a Release Build
```bash
dotnet build -c Release
```
The compiled binary will be located in: `bin/Release/net10.0-windows/ScaleSwitcher.exe`.

## Configuration & Setup

### Settings File
User settings are saved in JSON format at the following path:

- **Path**: `%LOCALAPPDATA%\ScaleSwitcher\settings.json`

#### Configuration Example
```json
{
  "TargetMonitorIndex": 0,
  "ActiveDpiPercentages": [
    100,
    200
  ],
  "DisplayNumberSource": "TargetId",
  "UseCustomDisplayName": false,
  "CustomDisplayName": "",
  "KeyboardSwitchMode": "Shift",
  "UiLanguage": "auto"
}
```

#### Field Descriptions
- `TargetMonitorIndex`: 0-based index of the target monitor for scaling cycle.
- `ActiveDpiPercentages`: List of integer percentages representing scaling factors in the cycle.
- `DisplayNumberSource`: The identification logic used for monitor mapping.
  - `TargetId` (default): Identification based on target display IDs.
  - `PathOrder`: Identification based on monitor path configuration order.
  - `GdiDeviceName`: Identification based on GDI Device Names.
- `UseCustomDisplayName`: Toggles custom display name usage.
- `CustomDisplayName`: Custom display name text.
- `KeyboardSwitchMode`: Which keyboard modifier keys are tapped simultaneously to cycle scaling.
  - `Shift` (default): Left Shift + Right Shift.
  - `Control`: Left Ctrl + Right Ctrl.
  - `Alt`: Left Alt + Right Alt.
  - `Off`: Disables the keyboard chord shortcut.
- `UiLanguage`: User interface language setting. Accepts `"auto"`, `"ja"`, or `"en"`.

## Technical Details

- **Tech Stack**: C# / WPF (.NET 10)
- **API Integration**: Win32 APIs via P/Invoke (`user32.dll`, `shcore.dll`)
- **DPI Control**: Native Windows DPI Awareness configurations via `app.manifest`
- **Tray Icon**: Windows Forms `NotifyIcon` wrapper (zero third-party dependencies)

## License

This project is licensed under the [MIT License](./LICENSE).
