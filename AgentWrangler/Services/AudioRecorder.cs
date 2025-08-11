using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
#if WINDOWS
using NAudio.Wave;
#endif

namespace AgentWrangler.Services
{
    public static class AudioRecorder
    {
        public static async Task<string?> RecordAudioAsync(string outputPath, int seconds = 10, CancellationToken? cancellationToken = null)
        {

            cancellationToken ??= CancellationToken.None;
            if (OperatingSystem.IsWindows())
            {
#if WINDOWS
                // Record using NAudio
                try
                {
                    using var waveIn = new WaveInEvent();
                    waveIn.WaveFormat = new WaveFormat(16000, 1);
                    using var writer = new WaveFileWriter(outputPath, waveIn.WaveFormat);
                    void OnDataAvailable(object? s, WaveInEventArgs a)
                    {
                        if (a.Buffer != null && writer != null)
                        {
                            writer.Write(a.Buffer, 0, a.BytesRecorded);
                        }
                    }
                    waveIn.DataAvailable += OnDataAvailable;
                    waveIn.StartRecording();
                    try
                    {
                        await Task.Delay(seconds*1000, (CancellationToken)cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        // Recording was canceled, handle gracefully
                    }
                    finally
                    {
                        waveIn.StopRecording();
                        waveIn.DataAvailable -= OnDataAvailable;
                        writer.Flush();
                    }
                    return outputPath;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "AudioRecorder.RecordAudioAsync (Windows)");
                    return null;
                }
#endif
            }
            else if (OperatingSystem.IsLinux())
            {
                // Use ffmpeg (must be installed)
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-f alsa -i default -t {seconds} -ar 16000 -ac 1 -y '{outputPath}'",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                try
                {
                    using var proc = Process.Start(psi);
                    await proc.WaitForExitAsync((CancellationToken)cancellationToken);
                    return File.Exists(outputPath) ? outputPath : null;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "AudioRecorder.RecordAudioAsync (Linux)");
                    return null;
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                // Use ffmpeg (must be installed)
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-f avfoundation -i :0 -t {seconds} -ar 16000 -ac 1 -y '{outputPath}'",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                try
                {
                    using var proc = Process.Start(psi);
                    await proc.WaitForExitAsync((CancellationToken)cancellationToken);
                    return File.Exists(outputPath) ? outputPath : null;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "AudioRecorder.RecordAudioAsync (MacOS)");
                    return null;
                }
            }
            return null;
        }
    }
}
