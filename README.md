# EasyDock 🚀

**Mac-Style Dock & Dynamic Island Status Pill for Windows 11**

EasyDock brings the fluid, physical beauty of macOS Sonoma/Sequoia and iOS Dynamic Island to Windows 11 with authentic frosted dark glass materials, zero-latency tactile response, and seamless desktop integration.

---

## Features

### 1. Top Dynamic Island Status Pill
- **Closed State:** A minimal, unobtrusive 74px Home Bar notch handle resting flush against the top-middle screen bezel.
- **Opened State:** Expands into a full macOS-style status bar with WWDC-inspired fluid spring physics:
  - **`‹` System Tray Arrow:** Instantly opens the Windows Notification Area Hidden Icons flyout.
  - **`🎚️` Control Center Sliders:** Two-track vector sliders launching Windows 11 Quick Settings (`Win + A`).
  - **`🔊` Sound & Volume:** Fluent speaker with sound waves launching Volume Mixer / Sound Settings.
  - **`🔋` Battery:** Apple System Green (`#34C759`) battery level fill with charging bolt `⚡`.
  - **Live Clock & Date:** Formatted as `Tue Nov 17 8:29 PM` using `Segoe UI Variable Display` optical typography.
  - **`🔔` Notification Center:** Fluent vector bell with Apple System Red (`#FF3B30`) badge dot launching Windows Notification Center (`Win + N`).
  - **`♫` Media Controls:** One-click launch for Spotify and active media controls.
  - **`^` Collapse Chevron:** Smoothly snaps the bar back into the closed notch handle.

### 2. Right-Side Glass Dock
- **Compact & Focused:** Scaled to a sleek 64px width (0.7x compact profile).
- **Golden Ratio Geometry:** Spans the middle **70% of screen height**, leaving **15% free margin at the top and bottom**.
- **Real Apps Only:** Automatically resolves and loads your real Windows 11 taskbar pinned apps (`%APPDATA%\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar`) and genuine open windows. No dummy apps!
- **macOS Pearl Indicator:** Running apps feature a luminous pearl white indicator pill on their edge with ambient glow.
- **Parabolic Magnification:** Hovering over an app lifts it with fluid `scale(1.16)` spring momentum and a glass chip reflection.
- **Zero-Latency Tactile Click:** Buttons depress to `scale(0.92)` on pointer-down and spring back on release.
- **Pin / Auto-Hide Mode Toggle:** Toggle between **Always Visible** (pinned open) and **Auto-Hide** with physical cursor edge detection.

---

## Tech Stack
* **Language & Framework:** C# / .NET 9 + WPF (XAML)
* **Architecture:** Win32 User32 & Shell32 Native Interop (P/Invoke)
* **Aesthetics:** Apple Human Interface Guidelines + Windows 11 Frosted Acrylic Glass

---

## Getting Started

### Prerequisites
* Windows 11 (or Windows 10 build 19041+)
* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Build & Run
```powershell
git clone https://github.com/Ummad-mk/easy-dock.git
cd easy-dock/EasyDock
dotnet run
```

### Exit
Right-click the EasyDock icon in your system tray and select **Exit EasyDock**, or press `Ctrl + C` in your terminal.
