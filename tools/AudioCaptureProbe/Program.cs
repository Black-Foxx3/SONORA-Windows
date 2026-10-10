using NAudio.CoreAudioApi;
using NAudio.Wave;

var query = args.FirstOrDefault();
using var enumerator = new MMDeviceEnumerator();
var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
Console.WriteLine("Active render endpoints:");
foreach (var endpoint in endpoints)
    Console.WriteLine($"- {endpoint.FriendlyName} | {endpoint.ID}");

var selected = query is null
    ? enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
    : endpoints.FirstOrDefault(device => device.FriendlyName.Contains(query, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"No active output matched '{query}'.");

Console.WriteLine($"\nCapturing 8 seconds from: {selected.FriendlyName}");
using var capture = new WasapiLoopbackCapture(selected);
long capturedBytes = 0;
long packetCount = 0;
double peak = 0;
double sumSquares = 0;
long sampleCount = 0;
var measurementLock = new object();

capture.DataAvailable += (_, packet) =>
{
    Interlocked.Add(ref capturedBytes, packet.BytesRecorded);
    Interlocked.Increment(ref packetCount);
    var localPeak = 0d;
    var localSquares = 0d;
    var localCount = 0;
    if (capture.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat && capture.WaveFormat.BitsPerSample == 32)
    {
        for (var offset = 0; offset + sizeof(float) <= packet.BytesRecorded; offset += sizeof(float))
        {
            var sample = Math.Abs(BitConverter.ToSingle(packet.Buffer, offset));
            localPeak = Math.Max(localPeak, sample);
            localSquares += sample * sample;
            localCount++;
        }
    }
    else if (capture.WaveFormat.Encoding == WaveFormatEncoding.Pcm && capture.WaveFormat.BitsPerSample == 16)
    {
        for (var offset = 0; offset + sizeof(short) <= packet.BytesRecorded; offset += sizeof(short))
        {
            var sample = Math.Abs(BitConverter.ToInt16(packet.Buffer, offset) / 32768d);
            localPeak = Math.Max(localPeak, sample);
            localSquares += sample * sample;
            localCount++;
        }
    }

    lock (measurementLock)
    {
        peak = Math.Max(peak, localPeak);
        sumSquares += localSquares;
        sampleCount += localCount;
    }
};

capture.StartRecording();
for (var second = 1; second <= 10; second++)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    double capturedPeak;
    lock (measurementLock) capturedPeak = peak;
    float endpointPeak;
    try { endpointPeak = selected.AudioMeterInformation.MasterPeakValue; }
    catch { endpointPeak = -1; }
    Console.WriteLine($"t={second}s endpoint peak={endpointPeak:F4} capture peak={capturedPeak:F4}");
}
capture.StopRecording();

var rms = sampleCount == 0 ? 0 : Math.Sqrt(sumSquares / sampleCount);
Console.WriteLine($"Format: {capture.WaveFormat}");
Console.WriteLine($"Packets: {packetCount}, bytes: {capturedBytes}");
Console.WriteLine($"Peak: {peak:F5}, RMS: {rms:F5}");

