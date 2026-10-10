# SONORA для Windows

**SONORA** передаёт звук с выбранного устройства воспроизведения Windows одновременно на два разных активных аудиовыхода. Приложение предназначено, например, для совместного прослушивания в двух парах наушников, в том числе Bluetooth-наушниках, уже подключённых к Windows.

> Реализацию приложения выполнил **OpenAI Codex (ИИ-ассистент)** по идее и требованиям владельца репозитория. Владелец также проверил работу приложения на своих устройствах.

## Возможности

- Показывает активные аудиовыходы Windows и позволяет выбрать источник и два разных устройства назначения.
- Копирует выбранный поток через WASAPI loopback capture и shared-mode playback (NAudio).
- Включает короткий тестовый сигнал, отдельную регулировку громкости выходов и базовую диагностику уровня и буфера.
- Не меняет устройство вывода Windows по умолчанию и не устанавливает виртуальный аудиодрайвер.
- Готовая автономная сборка для Windows x64 не требует отдельно устанавливать .NET Runtime.

## Ограничения

- Это небольшой прототип, проверенный на ограниченном наборе устройств, а не сертифицированный или широко протестированный аудиопродукт. Результат зависит от версии Windows, драйверов, Bluetooth-адаптеров, кодеков и выбранных устройств.
- У Bluetooth-устройств может различаться задержка. SONORA не синхронизирует отдельные наушники на уровне аудиосэмплов, поэтому возможны эхо или заметное смещение.
- Приложение захватывает один выбранный аудиовыход Windows, а не звук отдельного приложения. Если браузер или плеер выводит звук на другое устройство, выбери его источником в SONORA или настрой вывод браузера в микшере громкости Windows.
- Исходное устройство продолжает воспроизводить звук напрямую. Если оно выбрано одним из двух выходов, Windows оставляет прямой сигнал, а SONORA копирует звук на второй выход. Иначе исходный звук может звучать дополнительно к двум копиям.
- Отображаются только активные устройства воспроизведения Windows. Сначала подключи Bluetooth-устройства в Windows; если доступны несколько профилей, выбирай стереовыход.
- Сборка предназначена только для Windows x64. Нет многоплатформенной поддержки, отдельной маршрутизации для каждого приложения, сведения микрофона или гарантии работы с любой комбинацией форматов и устройств.

## Скачать и запустить

Скачай `SONORA-Windows-x64.exe` из [последнего релиза](https://github.com/Black-Foxx3/SONORA-Windows/releases/latest) и запусти файл. Сборка автономная, отдельно устанавливать .NET Runtime не нужно. Windows SmartScreen может предупредить о запуске, так как прототип не подписан цифровым сертификатом.

## Быстрый запуск

1. Подключи обе пары наушников или других устройств вывода в Windows.
2. Запусти SONORA и при необходимости обнови список устройств.
3. В поле **Источник** выбери выход, через который сейчас воспроизводится нужный звук. Для браузера проверь его устройство в микшере громкости Windows.
4. Выбери два разных устройства вывода. Пока оба выхода не выбраны, трансляцию запустить нельзя.
5. Проверь устройства тестовым сигналом, настрой громкость и запусти общий звук.

## Сборка из исходников

Требования: Windows x64 и .NET 10 SDK.

```powershell
dotnet restore TwinAudio.csproj
dotnet build TwinAudio.csproj -c Release
dotnet publish TwinAudio.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Опубликованная сборка появится в `bin/Release/net10.0-windows/win-x64/publish/`. Диагностическая утилита в `tools/AudioCaptureProbe` не нужна для работы приложения.

## Приватность

В коде приложения нет входа в аккаунт, аналитики, телеметрии или отправки данных по сети. Захват и воспроизведение звука выполняются локально через аудиоинтерфейсы Windows. NuGet используется только при восстановлении зависимостей и сборке исходников.

Локальные сборки, отладочные символы, настройки IDE и конфигурация конкретного компьютера исключены из репозитория. Перед публикацией публичной копии дополнительно проверь содержимое форка.

## Зависимости

- [.NET 10](https://dotnet.microsoft.com/)
- [NAudio 2.2.1](https://github.com/naudio/NAudio)

NAudio распространяется по собственной лицензии; см. репозиторий проекта. Для исходного кода SONORA отдельная лицензия пока не предоставлена.

## Сообщить о проблеме

Укажи версию Windows, модели аудиоустройств, выбранный источник и выходы, а также точное описание проблемы. Не прикладывай логи и скриншоты с личными данными, предварительно их не проверив.

---

# SONORA for Windows

**SONORA** routes audio from a selected Windows playback device to two different active outputs at the same time. It is designed for sharing audio on two pairs of headphones, including Bluetooth headphones already connected to Windows.

> The application was implemented by **OpenAI Codex (AI assistant)** based on the repository owner's idea and requirements. The owner also tested it with their devices.

## Features

- Lists active Windows playback devices and lets you choose a source and two distinct outputs.
- Copies the selected stream using WASAPI loopback capture and shared-mode playback (NAudio).
- Includes a short test tone, independent output volume controls, and basic live level/buffer diagnostics.
- Does not change the Windows default playback device or install a virtual audio driver.
- The standalone Windows x64 build does not require a separate .NET Runtime installation.

## Limitations

- This is a small prototype tested with a limited set of devices, not a certified or extensively validated audio product. Results depend on Windows, drivers, Bluetooth adapters, codecs, and the selected devices.
- Bluetooth devices can have different latency. SONORA does not synchronize separate headphones at sample level, so echo or an audible offset may occur.
- The app captures one selected Windows playback endpoint, not an individual application. If a browser or player uses another output, select that endpoint in SONORA or change the app's output in the Windows volume mixer.
- The source endpoint continues to play audio directly. If it is selected as one of the outputs, Windows keeps that direct signal while SONORA copies audio to the second output. Otherwise, the source may remain audible alongside both copies.
- Only active Windows playback endpoints are listed. Pair and connect Bluetooth devices in Windows first; choose the stereo playback endpoint if Windows exposes multiple profiles.
- This build targets Windows x64 only. It does not provide cross-platform support, per-application routing, microphone mixing, or a guarantee that every audio format/device combination will work.

## Download and run

Download `SONORA-Windows-x64.exe` from the [latest release](https://github.com/Black-Foxx3/SONORA-Windows/releases/latest) and run it. The build is self-contained and does not require a separate .NET Runtime. Windows SmartScreen may show a warning because the prototype is not digitally signed.

## Quick start

1. Connect both pairs of headphones or other output devices in Windows.
2. Start SONORA and refresh the device list if needed.
3. Under **Source**, select the endpoint currently playing the audio. For a browser, check its output in the Windows volume mixer.
4. Select two different output devices. Streaming stays disabled until both are selected.
5. Test the outputs, adjust their volume, and start the shared stream.

## Build from source

Requirements: Windows x64 and the .NET 10 SDK.

```powershell
dotnet restore TwinAudio.csproj
dotnet build TwinAudio.csproj -c Release
dotnet publish TwinAudio.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The published build is placed under `bin/Release/net10.0-windows/win-x64/publish/`. The diagnostic utility in `tools/AudioCaptureProbe` is not required by the desktop app.

## Privacy

The application code contains no account sign-in, analytics, telemetry, or network upload functionality. Audio capture and playback happen locally through Windows audio APIs. NuGet is used only when restoring dependencies and building the source project.

Local build output, debug symbols, IDE state, and machine-specific configuration are excluded from the repository. Review a fork before making it public.

## Dependencies and licensing

- [.NET 10](https://dotnet.microsoft.com/)
- [NAudio 2.2.1](https://github.com/naudio/NAudio)

NAudio is distributed under its own license; see its upstream repository. No separate license has been granted for the SONORA source code yet.

## Reporting an issue

Include your Windows version, audio device models, selected source and outputs, and the exact symptom. Review logs and screenshots for personal information before attaching them.

