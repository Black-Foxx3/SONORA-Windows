# SONORA for Windows

**SONORA** sends audio from a selected Windows playback device to two different active output devices. It is intended for listening together on two pairs of headphones, including Bluetooth headphones already connected in Windows.

> This project was implemented with **OpenAI Codex (AI assistant)**. The project idea and requirements came from the repository owner, who also tested the application on real audio devices.

## What it does

- Lists active Windows audio outputs and lets you choose a capture source and two distinct destinations.
- Copies the selected source to the chosen output devices using WASAPI loopback capture and shared-mode playback (NAudio).
- Provides an output test tone, per-output volume controls, and basic live level/buffer diagnostics.
- Leaves the Windows default output unchanged and does not install a virtual audio driver.
- Includes a self-contained Windows x64 executable for users who do not want to install the .NET runtime separately.

## Limitations

- This is a small, personally tested prototype, not a certified or extensively validated audio product. Results depend on Windows, drivers, Bluetooth adapters, codecs, and the selected devices.
- Bluetooth devices may have different delays. SONORA does not synchronize separate headsets at sample level, so a noticeable echo or offset is possible.
- SONORA captures one selected Windows render endpoint, not an individual app. If a browser or player sends audio to another endpoint, select that endpoint as the source in SONORA or in the Windows volume mixer.
- The source endpoint remains audible through Windows. If it is also selected as one destination, Windows supplies that direct sound while SONORA copies to the other destination. Otherwise the source can remain audible in addition to both copies.
- Only active Windows playback endpoints are listed. Pair and connect Bluetooth devices in Windows first; select the stereo playback endpoint when Windows exposes multiple profiles.
- This build targets Windows x64. It does not provide multi-platform support, independent per-application routing, microphone mixing, or a guarantee that every audio format/device combination will work.

## Download and run

Download `SONORA-Windows-x64.exe` from the latest GitHub Release and run it. The release executable is self-contained and does not require a separately installed .NET runtime. Windows SmartScreen may show a warning because the prototype is not code-signed; only run software you trust.

## Quick start

1. Pair and connect both headphones or output devices in Windows.
2. Start SONORA and refresh the device list if needed.
3. Choose the endpoint currently playing the audio as **Source**. For browser audio, check the browser's output in the Windows volume mixer.
4. Select two different output devices. The app will not start until both are selected.
5. Use **Test** to confirm the destinations, adjust their levels, then start the shared stream.

## Build from source

Requirements: Windows x64 and the .NET 10 SDK.

```powershell
dotnet restore TwinAudio.csproj
dotnet build TwinAudio.csproj -c Release
dotnet publish TwinAudio.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The publish output is under `bin/Release/net10.0-windows/win-x64/publish/`. The diagnostic probe in `tools/AudioCaptureProbe` is not required by the desktop app.

## Privacy

The application code contains no account sign-in, analytics, telemetry, or network upload functionality. Audio capture and playback are handled locally through Windows audio APIs. NuGet is contacted only when restoring/building the source project.

The repository intentionally excludes local build output, debug symbols, IDE state, and machine-specific configuration. Please report any privacy concern before sharing a public fork.

## Dependencies

- [.NET 10](https://dotnet.microsoft.com/)
- [NAudio 2.2.1](https://github.com/naudio/NAudio)

NAudio is distributed under its own license; see its upstream repository. This repository currently does not grant an additional license for SONORA source code.

## Feedback

Include your Windows version, audio device models, source/destination selections, and the exact symptom when reporting a problem. Do not attach logs or screenshots containing personal information unless you have reviewed them first.
