using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PoeShared.WinCaptureAudio.API;
using PoeShared.WinCaptureAudio.Extensions;
using IAudioClient = NAudio.CoreAudioApi.Interfaces.IAudioClient;

namespace PoeShared.WinCaptureAudio;

/// <summary>
///     This code is from https://github.com/bozbez/win-capture-audio
/// </summary>
internal class ProcessAudioCapture : DisposableReactiveObjectWithLogger, IWaveIn
{
    private static readonly TimeSpan CaptureThreadTerminationTimeout = TimeSpan.FromMilliseconds(5000);
    private static long globalIdx;
    
    private readonly nint[] events = new nint[2];

    private readonly WaveFormat mixWaveFormat;
    private readonly int bytesPerFrame;
    private readonly string instanceId = $"PAC#{Interlocked.Increment(ref globalIdx)}";

    private IAudioCaptureClient captureClient;
    private Thread captureThread;
    private IAudioClient client;

    public ProcessAudioCapture(Process process)
    {
        ProcessId = process.Id;
        ProcessName = process.ProcessName;
        Log.AddSuffix(instanceId);
        Log.AddSuffix(() => $"'{ProcessName}' PID #{ProcessId}{(IsInitialized ? "/Initialized" : string.Empty)}{(IsRunning ? "/Running" : string.Empty)}");

        Log.Debug($"Initializing capture process");
        using (var defaultLoopbackDevice = WasapiLoopbackCapture.GetDefaultLoopbackCaptureDevice())
        {
            mixWaveFormat = defaultLoopbackDevice.AudioClient.MixFormat;
            bytesPerFrame = WaveFormat.Channels * WaveFormat.BitsPerSample / 8;
        }
        Log.Debug($"Retrieved MixFormat: {mixWaveFormat}");

        for (var i = 0; i < events.Length; i++)
        {
            events[i] = Kernel32Api.CreateEvent(nint.Zero, false, false, null);
        }

        Anchors.Add(() =>
        {
            if (IsRunning)
            {
                Log.Debug($"Capture is running, stopping it");
                StopRecording();
            }
        });
    }

    public int ProcessId { get; }
    
    public string ProcessName { get; }
    
    public bool IsRunning { get; private set; }
    
    public bool IsInitialized { get; private set; }

    public WaveFormat WaveFormat
    {
        get => mixWaveFormat;
        set => throw new NotSupportedException();
    }

    public event EventHandler<WaveInEventArgs> DataAvailable;
    
    public event EventHandler<StoppedEventArgs> RecordingStopped;

    public void StartRecording()
    {
        if (IsRunning)
        {
            return;
        }
        Log.Debug($"Starting recording");
        captureThread = new Thread(CaptureThreadDoWork)
        {
            IsBackground = true,
            Name = $"AudioCapture#{ProcessId}"
        };
        captureThread.Start();
        IsRunning = true;
        Log.Debug($"Recording process has started, thread: {new { captureThread.Name, captureThread.ManagedThreadId, captureThread.IsAlive, captureThread.IsBackground }}");
    }

    public void StopRecording()
    {
        if (!IsRunning)
        {
            return;
        }
        Log.Debug($"Stopping recording");
        Kernel32Api.SetEvent(events[(int) HelperEvents.Shutdown]);
        IsRunning = false;
        if (captureThread != null && Environment.CurrentManagedThreadId != captureThread.ManagedThreadId && captureThread.IsAlive)
        {
            Log.Debug($"Awaiting for capture thread termination for {CaptureThreadTerminationTimeout}");
            if (!captureThread.Join(CaptureThreadTerminationTimeout))
            {
                throw new InvalidStateException($"Failed to terminated capture thread in {CaptureThreadTerminationTimeout}");
            }
        }
        captureThread = null;
        RecordingStopped?.Invoke(this, new StoppedEventArgs());
        Log.Debug($"Recording has stopped");
    }

    [STAThread]
    private void InitializeClient()
    {
        Log.Debug("Initializing audio client");
        var guid = Guid.Empty;

        var audioActivationParams = CreateAudioActivationParams(ProcessId);
        var propVariant = GetPropVariant(audioActivationParams, out var paramsPtr);

        try
        {
            Log.Debug($"Activating audio interface using {audioActivationParams}");

            var completionHandler = new CompletionHandler();
            Marshal.ThrowExceptionForHR(MMDevApi.ActivateAudioInterfaceAsync(
                MMDevApi.VirtualAudioDeviceProcessLoopback,
                typeof(IAudioClient).GUID,
                propVariant,
                completionHandler,
                out var _
            ));
            Log.Debug($"Awaiting for activation...");

            var waitResult = Kernel32Api.WaitForSingleObject(completionHandler.eventFinished, uint.MaxValue);
            Marshal.ThrowExceptionForHR(waitResult);
            Marshal.ThrowExceptionForHR((int)completionHandler.Result);
        
            client = completionHandler.AudioClient;
            Log.Debug($"Initializing audio client: {client}");

            Marshal.ThrowExceptionForHR(client.Initialize(
                AudioClientShareMode.Shared,
                AudioClientStreamFlags.Loopback | AudioClientStreamFlags.EventCallback,
                0,
                0,
                WaveFormat,
                ref guid));

            Marshal.ThrowExceptionForHR(client.SetEventHandle(events[0]));
        }
        finally
        {
            Marshal.FreeHGlobal(paramsPtr);
            Marshal.FreeHGlobal(propVariant);
        }
    }

    private void InitializeCapture()
    {
        Log.Debug($"Initializing instance of {typeof(IAudioCaptureClient)}");
        InitializeClient();
        Marshal.ThrowExceptionForHR(client.GetService(typeof(IAudioCaptureClient).GUID, out var obj));
        if (obj is not IAudioCaptureClient audioCaptureClient)
        {
            throw new InvalidStateException($"Something went wrong - failed to cast object {obj.DumpToString()} to {typeof(IAudioCaptureClient)}");
        }
        captureClient = audioCaptureClient;
        IsInitialized = true;
        Log.Debug($"Initialized instance of {typeof(IAudioCaptureClient)}: {captureClient}");
    }

    private void HandlePacket(ref byte[] buffer)
    {
        Marshal.ThrowExceptionForHR(captureClient.GetNextPacketSize(out var numFrames));

        while (numFrames > 0)
        {
            Marshal.ThrowExceptionForHR(captureClient.GetBuffer(out var dataPtr, out numFrames, out var flags, out var _, out var qpcPosition));

            if (flags.HasFlag(AudioClientBufferFlags.DataDiscontinuity))
            {
                Log.Debug("DataDiscontinuity flag is set");
            }

            if (flags.HasFlag(AudioClientBufferFlags.TimestampError))
            {
                Log.Debug("TimestampError flag is set");
            }
            
            if (!flags.HasFlag(AudioClientBufferFlags.Silent))
            {
                var bytesToRead = (int)numFrames * bytesPerFrame;
                if (buffer.Length < bytesToRead)
                {
                    Log.Debug($"Growing buffer from {buffer.Length} to {bytesToRead}");
                    buffer = new byte[bytesToRead];
                }

                Marshal.Copy(dataPtr, buffer, 0, bytesToRead);
                DataAvailable?.Invoke(this, new WaveInEventArgs(buffer, bytesToRead));
            }

            Marshal.ThrowExceptionForHR(captureClient.ReleaseBuffer(numFrames));
            Marshal.ThrowExceptionForHR(captureClient.GetNextPacketSize(out numFrames));
        }
    }

    private void Capture()
    {
        if (!IsInitialized)
        {
            InitializeCapture();
        }

        var shutdown = false;

        Marshal.ThrowExceptionForHR(client.Start());

        var buffer = Array.Empty<byte>();

        Log.Debug("Starting packet capture loop");
        while (!shutdown && IsRunning)
        {
            var eventId = (HelperEvents) Kernel32Api.WaitForMultipleObjects((uint) events.Length, events, false, uint.MaxValue);

            switch (eventId)
            {
                case HelperEvents.PacketReady:
                    HandlePacket(ref buffer);
                    break;

                case HelperEvents.Shutdown:
                    shutdown = true;
                    break;

                default:
                    Log.Warn($"Wait operation has failed with result: {eventId}");
                    shutdown = true;
                    break;
            }
        }

        Marshal.ThrowExceptionForHR(client.Stop());
    }

    private void CaptureThreadDoWork()
    {
        try
        {
            Log.Debug("Capture thread has started");
            Capture();
            Log.Debug("Capture thread has completed gracefully");
        }
        catch (Exception ex)
        {
            Log.Error("Failed to capture", ex);
            StopRecording();
        }
        finally
        {
            Log.Debug("Capture thread has stopped");
        }
    }
    
    private static AudioClientActivationParams CreateAudioActivationParams(int processId)
    {
        var mode = ProcessLoopbackMode.ProcessLoopbackModeIncludeTargetProcessTree;
        return new AudioClientActivationParams
        {
            ActivationType = AudioClientActivationType.AudioClientActivationTypeProcessLoopback,
            ProcessLoopbackParams = new AudioClientProcessLoopbackParams
            {
                TargetProcessId = processId,
                ProcessLoopbackMode = mode
            }
        };
    }

    private static nint GetPropVariant(AudioClientActivationParams @params, out nint paramsPtr)
    {
        var size = Marshal.SizeOf<AudioClientActivationParams>();
        var propSize = Marshal.SizeOf<PropVariant>();

        paramsPtr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(@params, paramsPtr, false);
        var prop = new PropVariant
        {
            inner = new TagInnerPropVariant
            {
                vt = (ushort) VarEnum.VT_BLOB,
                blob = new Blob
                {
                    cbSize = (ulong) size,
                    pBlobData = paramsPtr
                }
            }
        };
        var propPtr = Marshal.AllocHGlobal(propSize);
        Marshal.StructureToPtr(prop, propPtr, false);

        return propPtr;
    }
}