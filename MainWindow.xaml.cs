using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace TwinAudio;

public sealed record AudioEndpoint(string Id, string Name);

public partial class MainWindow : Window
{
    private readonly AudioRouter _router = new();
    private readonly DispatcherTimer _diagnosticsTimer;
    private bool _loadingDevices;
    private bool _isRunning;

    public MainWindow()
    {
        InitializeComponent();
        _diagnosticsTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _diagnosticsTimer.Tick += (_, _) =>
        {
            if (_isRunning) DetailText.Text = _router.GetDiagnostics();
        };
        RefreshDevices();
    }

    private void RefreshDevices()
    {
        var sourceId = (SourceCombo.SelectedItem as AudioEndpoint)?.Id;
        var firstId = (OutputACombo.SelectedItem as AudioEndpoint)?.Id;
        var secondId = (OutputBCombo.SelectedItem as AudioEndpoint)?.Id;
        _loadingDevices = true;
        try
        {
            var endpoints = AudioRouter.GetActiveRenderEndpoints(out var defaultId);
            var items = new ObservableCollection<AudioEndpoint>(endpoints);
            SourceCombo.ItemsSource = items;
            OutputACombo.ItemsSource = items;
            OutputBCombo.ItemsSource = items;

            SourceCombo.SelectedItem = items.FirstOrDefault(x => x.Id == sourceId)
                ?? items.FirstOrDefault(x => x.Id == defaultId)
                ?? items.FirstOrDefault();
            OutputACombo.SelectedItem = items.FirstOrDefault(x => x.Id == firstId)
                ?? items.FirstOrDefault(x => x.Id == defaultId)
                ?? items.FirstOrDefault();
            OutputBCombo.SelectedItem = items.FirstOrDefault(x => x.Id == secondId && x.Id != (OutputACombo.SelectedItem as AudioEndpoint)?.Id)
                ?? items.FirstOrDefault(x => x.Id != (OutputACombo.SelectedItem as AudioEndpoint)?.Id);

            StatusText.Text = items.Count switch
            {
                0 => "Не найдено активных устройств вывода.",
                1 => "Найдено одно устройство. Для общего звука нужны два разных выхода.",
                _ => $"Найдено аудиоустройств: {items.Count}. Выбери две пары наушников или других выхода."
            };
            DetailText.Text = items.Count == 0 ? "Подключи наушники или Bluetooth-донгл и обнови список." : string.Empty;
        }
        catch (Exception ex)
        {
            StatusText.Text = "Не удалось прочитать список аудиоустройств.";
            DetailText.Text = ex.Message;
        }
        finally
        {
            _loadingDevices = false;
        }

        UpdateControls();
    }

    private void UpdateControls()
    {
        var source = SourceCombo.SelectedItem as AudioEndpoint;
        var first = OutputACombo.SelectedItem as AudioEndpoint;
        var second = OutputBCombo.SelectedItem as AudioEndpoint;
        var validPair = first is not null && second is not null && first.Id != second.Id;
        var enabled = !_isRunning;

        SourceCombo.IsEnabled = enabled;
        OutputACombo.IsEnabled = enabled;
        OutputBCombo.IsEnabled = enabled;
        RefreshButton.IsEnabled = enabled;
        StartButton.IsEnabled = _isRunning || (source is not null && validPair);
        TestButton.IsEnabled = !_isRunning && (first is not null || second is not null);
        StartButton.Content = _isRunning ? "■   Остановить звук" : "▶   Запустить общий звук";
        PairCountText.Text = validPair ? "2 ИЗ 2 ВЫБРАНО" : first is not null || second is not null ? "1 ИЗ 2 ВЫБРАНО" : "0 ИЗ 2 ВЫБРАНО";

        RouteStateA.Text = RouteLabel(first, source);
        RouteStateB.Text = RouteLabel(second, source);
        VolumeA.IsEnabled = first is not null && first.Id != source?.Id;
        VolumeB.IsEnabled = second is not null && second.Id != source?.Id;
        VolumeAText.Text = first is null ? "—" : first.Id == source?.Id ? "Windows" : $"{(int)VolumeA.Value}%";
        VolumeBText.Text = second is null ? "—" : second.Id == source?.Id ? "Windows" : $"{(int)VolumeB.Value}%";
    }

    private static string RouteLabel(AudioEndpoint? output, AudioEndpoint? source)
    {
        if (output is null) return "НЕ ВЫБРАНО";
        return output.Id == source?.Id ? "ПРЯМОЙ СИГНАЛ WINDOWS" : "КОПИЯ ПОТОКА";
    }

    private void Selection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingDevices) return;
        UpdateControls();
        var source = SourceCombo.SelectedItem as AudioEndpoint;
        SourceHint.Text = source is null
            ? "Сначала подключи устройство вывода."
            : $"Звук будет захвачен с устройства «{source.Name}». Если YouTube играет через другой выход, измени его в микшере громкости Windows.";
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => RefreshDevices();

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            StopRouter();
            return;
        }

        if (SourceCombo.SelectedItem is not AudioEndpoint source ||
            OutputACombo.SelectedItem is not AudioEndpoint first ||
            OutputBCombo.SelectedItem is not AudioEndpoint second)
        {
            return;
        }

        try
        {
            _router.Start(source.Id, first, second, (float)(VolumeA.Value / 100), (float)(VolumeB.Value / 100));
            _isRunning = true;
            StatusText.Text = "Общий звук передаётся на выбранные устройства.";
            DetailText.Text = source.Id == first.Id || source.Id == second.Id
                ? "Источник звучит напрямую; второе устройство получает синхронную копию."
                : "Исходный системный выход продолжает звучать параллельно.";
            SetStatus(true, "ТРАНСЛЯЦИЯ");
            UpdateControls();
            _diagnosticsTimer.Start();
        }
        catch (Exception ex)
        {
            StopRouter();
            StatusText.Text = "Не удалось запустить аудиомаршрут.";
            DetailText.Text = ex.Message;
            SetStatus(false, "ОШИБКА");
        }
    }

    private void StopRouter()
    {
        _router.Stop();
        _diagnosticsTimer.Stop();
        _isRunning = false;
        SetStatus(false, "ОСТАНОВЛЕНО");
        StatusText.Text = "Аудиомаршрут остановлен.";
        DetailText.Text = string.Empty;
        UpdateControls();
    }

    private async void TestButton_Click(object sender, RoutedEventArgs e)
    {
        var outputs = new[]
        {
            (OutputACombo.SelectedItem as AudioEndpoint, (float)(VolumeA.Value / 100)),
            (OutputBCombo.SelectedItem as AudioEndpoint, (float)(VolumeB.Value / 100))
        }
        .Where(x => x.Item1 is not null)
        .Select(x => (x.Item1!, x.Item2))
        .DistinctBy(x => x.Item1.Id)
        .ToArray();
        if (outputs.Length == 0) return;
        TestButton.IsEnabled = false;
        StatusText.Text = outputs.Length == 1 ? "Короткий тестовый сигнал на выбранном выходе…" : "Короткий тестовый сигнал на обоих выходах…";
        DetailText.Text = "Уровень сигнала низкий; длительность около двух секунд.";
        try
        {
            await AudioRouter.PlayTestToneAsync(outputs.Select(x => (x.Item1.Id, x.Item2)).ToArray());
            StatusText.Text = "Проверка завершена.";
            DetailText.Text = outputs.Length == 1
                ? "Тестовый сигнал воспроизведён на выбранном устройстве."
                : "Если сигнал был слышен в обеих парах, устройства готовы.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Тестовый сигнал не удалось воспроизвести.";
            DetailText.Text = ex.Message;
        }
        finally
        {
            TestButton.IsEnabled = !_isRunning && (OutputACombo.SelectedItem is AudioEndpoint || OutputBCombo.SelectedItem is AudioEndpoint);
        }
    }

    private void VolumeA_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VolumeAText is null) return;
        VolumeAText.Text = $"{(int)VolumeA.Value}%";
        _router.SetVolume((OutputACombo.SelectedItem as AudioEndpoint)?.Id, (float)(VolumeA.Value / 100));
    }

    private void VolumeB_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VolumeBText is null) return;
        VolumeBText.Text = $"{(int)VolumeB.Value}%";
        _router.SetVolume((OutputBCombo.SelectedItem as AudioEndpoint)?.Id, (float)(VolumeB.Value / 100));
    }

    private void SetStatus(bool active, string text)
    {
        StatusBadge.Text = text;
        StatusBadge.Foreground = active ? Brushes.DarkGreen : Brushes.SlateGray;
        StatusDot.Fill = active ? new SolidColorBrush(Color.FromRgb(19, 127, 117)) : new SolidColorBrush(Color.FromRgb(147, 161, 159));
    }

    protected override void OnClosed(EventArgs e)
    {
        _router.Dispose();
        base.OnClosed(e);
    }
}

internal sealed class AudioRouter : IDisposable
{
    private MMDeviceEnumerator? _activeEnumerator;
    private WasapiLoopbackCapture? _capture;
    private MMDevice? _sourceDevice;
    private readonly List<OutputRoute> _routes = [];
    private long _capturedBytes;
    private long _packetCount;
    private long _lastPacketTicks;
    private int _inputPeakBits;
    private string? _routeError;

    public static List<AudioEndpoint> GetActiveRenderEndpoints(out string? defaultId)
    {
        using var enumerator = new MMDeviceEnumerator();
        using var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        defaultId = defaultDevice.ID;
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        var endpoints = new List<AudioEndpoint>(devices.Count);
        foreach (var device in devices)
        {
            endpoints.Add(new AudioEndpoint(device.ID, device.FriendlyName));
            device.Dispose();
        }
        return endpoints;
    }

    public void Start(string sourceId, AudioEndpoint first, AudioEndpoint second, float volumeA, float volumeB)
    {
        Stop();
        if (first.Id == second.Id) throw new InvalidOperationException("Для двух слушателей выбери два разных устройства.");

        _activeEnumerator = new MMDeviceEnumerator();
        _routeError = null;
        try
        {
            _sourceDevice = _activeEnumerator.GetDevice(sourceId);
            _capture = new WasapiLoopbackCapture(_sourceDevice);
            AddRoute(_activeEnumerator, sourceId, first, volumeA);
            AddRoute(_activeEnumerator, sourceId, second, volumeB);

            _capture.DataAvailable += (_, args) =>
            {
                Interlocked.Add(ref _capturedBytes, args.BytesRecorded);
                Interlocked.Increment(ref _packetCount);
                Interlocked.Exchange(ref _lastPacketTicks, DateTime.UtcNow.Ticks);
                var peak = MeasurePeak(args.Buffer, args.BytesRecorded, _capture.WaveFormat);
                Interlocked.Exchange(ref _inputPeakBits, BitConverter.SingleToInt32Bits(peak));
                foreach (var route in _routes)
                    route.Buffer.AddSamples(args.Buffer, 0, args.BytesRecorded);
            };

            _capture.StartRecording();
            foreach (var route in _routes) route.Player.Play();
        }
        catch
        {
            Stop();
            throw;
        }
    }

    private void AddRoute(MMDeviceEnumerator enumerator, string sourceId, AudioEndpoint endpoint, float volume)
    {
        if (endpoint.Id == sourceId) return;
        var device = enumerator.GetDevice(endpoint.Id);
        var buffer = new BufferedWaveProvider(_capture!.WaveFormat)
        {
            BufferDuration = TimeSpan.FromSeconds(2),
            DiscardOnBufferOverflow = true,
            ReadFully = true
        };
        var player = new WasapiOut(device, AudioClientShareMode.Shared, true, 180) { Volume = Math.Clamp(volume, 0, 1) };
        player.PlaybackStopped += (_, args) =>
        {
            if (args.Exception is not null)
                Volatile.Write(ref _routeError, $"{endpoint.Name}: {args.Exception.Message}");
        };
        player.Init(buffer);
        _routes.Add(new OutputRoute(endpoint.Id, endpoint.Name, player, buffer, device));
    }

    public void SetVolume(string? endpointId, float volume)
    {
        if (endpointId is null) return;
        var route = _routes.FirstOrDefault(x => x.EndpointId == endpointId);
        if (route is not null) route.Player.Volume = Math.Clamp(volume, 0, 1);
    }

    public string GetDiagnostics()
    {
        var lastTicks = Interlocked.Read(ref _lastPacketTicks);
        var packetIsFresh = lastTicks != 0 && DateTime.UtcNow - new DateTime(lastTicks, DateTimeKind.Utc) < TimeSpan.FromMilliseconds(500);
        var inputPeak = packetIsFresh ? BitConverter.Int32BitsToSingle(Interlocked.CompareExchange(ref _inputPeakBits, 0, 0)) : 0;
        var packetCount = Interlocked.Read(ref _packetCount);
        var rates = _routes.Select((route, index) =>
        {
            var peak = 0f;
            try { peak = route.Device.AudioMeterInformation.MasterPeakValue; } catch { }
            var bufferedMs = route.Buffer.BufferedBytes * 1000d / route.Buffer.WaveFormat.AverageBytesPerSecond;
            return $"выход {index + 1} {peak:P0}/{bufferedMs:F0} мс";
        });
        var error = Volatile.Read(ref _routeError);
        return error is not null
            ? $"Ошибка потока: {error}"
            : $"Вход {inputPeak:P0} · пакеты {packetCount:N0} · {string.Join(" · ", rates)}";
    }

    private static float MeasurePeak(byte[] buffer, int bytesRecorded, WaveFormat format)
    {
        var peak = 0f;
        if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
        {
            for (var offset = 0; offset + sizeof(float) <= bytesRecorded; offset += sizeof(float))
                peak = Math.Max(peak, Math.Abs(BitConverter.ToSingle(buffer, offset)));
        }
        else if (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample == 16)
        {
            for (var offset = 0; offset + sizeof(short) <= bytesRecorded; offset += sizeof(short))
                peak = Math.Max(peak, Math.Abs(BitConverter.ToInt16(buffer, offset) / 32768f));
        }
        return peak;
    }

    public static async Task PlayTestToneAsync(IReadOnlyList<(string Id, float Volume)> outputs)
    {
        if (outputs.Count == 0) throw new InvalidOperationException("Не выбрано ни одного устройства.");
        using var enumerator = new MMDeviceEnumerator();
        var devices = new List<MMDevice>();
        var players = new List<WasapiOut>();
        try
        {
            foreach (var output in outputs.DistinctBy(x => x.Id))
            {
                var device = enumerator.GetDevice(output.Id);
                devices.Add(device);
                players.Add(CreateTestPlayer(device, output.Volume));
            }
            foreach (var player in players) player.Play();
            await Task.Delay(TimeSpan.FromSeconds(1.8));
        }
        finally
        {
            foreach (var player in players)
            {
                try { player.Stop(); } catch { }
                player.Dispose();
            }
            foreach (var device in devices) device.Dispose();
        }
    }

    private static WasapiOut CreateTestPlayer(MMDevice device, float volume)
    {
        var tone = new SignalGenerator(44100, 2)
        {
            Gain = 0.06,
            Frequency = 440,
            Type = SignalGeneratorType.Sin
        };
        var player = new WasapiOut(device, AudioClientShareMode.Shared, true, 180) { Volume = Math.Clamp(volume, 0, 1) };
        player.Init(tone);
        return player;
    }

    public void Stop()
    {
        try { _capture?.StopRecording(); } catch { }
        _capture?.Dispose();
        _capture = null;
        _sourceDevice?.Dispose();
        _sourceDevice = null;
        foreach (var route in _routes)
        {
            try { route.Player.Stop(); } catch { }
            route.Player.Dispose();
            route.Device.Dispose();
        }
        _routes.Clear();
        _activeEnumerator?.Dispose();
        _activeEnumerator = null;
        Interlocked.Exchange(ref _packetCount, 0);
        Interlocked.Exchange(ref _capturedBytes, 0);
        Interlocked.Exchange(ref _lastPacketTicks, 0);
        Interlocked.Exchange(ref _inputPeakBits, 0);
        _routeError = null;
    }

    public void Dispose() => Stop();

    private sealed record OutputRoute(string EndpointId, string Name, WasapiOut Player, BufferedWaveProvider Buffer, MMDevice Device);
}

