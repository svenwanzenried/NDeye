/*
 * File: NdiReceiver.cs
 * Project: TestNdiLib
 * Created Date: 2026-02-23 21:38:04
 * Author: Sven Wanzenried
 * -----
 * Copyright (c) 2026 Sven Wanzenried
 * 
 * MIT License
 * --------------------------------------------------------------
 */
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NewTek;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NDeye;
public class NdiReceiver : IDisposable
{
    private IntPtr _recvInstance;
    private IntPtr _ptrReceiverName;
    private bool _running;

    private string _sourceName;
    private readonly ILogger<NdiReceiver> _logger;

    public NdiReceiver(string sourceName, ILogger<NdiReceiver> logger)
    {
        _sourceName = sourceName;
        _logger = logger;

        // if (!NDIlib.initialize())
        //     throw new Exception("NDI init failed");
    }

    public bool Connect()
    {

        var findSettings = new NDIlib.find_create_t()
        {
            show_local_sources = true
        };

        var find = NDIlib.find_create_v2(ref findSettings);
        Thread.Sleep(2000);

        uint count = 0;
        var sources = NDIlib.find_get_current_sources(find, ref count);

        for (int i = 0; i < count; i++)
        {
            var src = Marshal.PtrToStructure<NDIlib.source_t>(sources + i * Marshal.SizeOf<NDIlib.source_t>());

            var name = Marshal.PtrToStringUTF8(src.p_ndi_name);

            if (name is not null && name.Contains(_sourceName))
            {
                _logger.LogDebug("Found matching NDI Stream with name {Name}", name);
                _ptrReceiverName = Marshal.StringToHGlobalAnsi("test-receiver");
                var recvSettings = new NDIlib.recv_create_v3_t
                {
                    p_ndi_recv_name = _ptrReceiverName,
                    source_to_connect_to = src,
                    allow_video_fields = false, // NDI will de-interlace from interlaced sources
                    bandwidth = NDIlib.recv_bandwidth_e.recv_bandwidth_highest,
                    // color_format = NDIlib.recv_color_format_e.recv_color_format_RGBX_RGBA,
                    color_format = NDIlib.recv_color_format_e.recv_color_format_BGRX_BGRA
                };

                _recvInstance = NDIlib.recv_create_v3(ref recvSettings);
                // NDIlib.recv_connect(_recvInstance, src);
                _running = true;
                break;
            }
        }

        if (!_running)
        {
            return false;
            // throw new Exception("NDI source not found");
        }

        return true;
    }

    public Task<ImageData?> GetFrameAsync(CancellationToken ct)
    {
        return Task.Run(() => CaptureFrameUnsafe(), ct);
    }



    private unsafe ImageData? CaptureFrameUnsafe()
    {
        if (!_running)
            return null;

        var frame = new NDIlib.video_frame_v2_t();
        var audio = new NDIlib.audio_frame_v3_t();
        var meta = new NDIlib.metadata_frame_t();

        while (true)
        {
            var type = NDIlib.recv_capture_v3(
                _recvInstance,
                ref frame,
                ref audio,
                ref meta,
                100);

            switch (type)
            {
                case NDIlib.frame_type_e.frame_type_video:
                    try
                    {
                        var span = new Span<byte>(
                            (void*)frame.p_data,
                            frame.yres * frame.line_stride_in_bytes);

                        using var image = Image.LoadPixelData<Bgra32>(
                            span.ToArray(),
                            frame.xres,
                            frame.yres);

                        using var ms = new System.IO.MemoryStream();
                        _logger.LogDebug("New image created.");
                        image.SaveAsPng(ms);
                        return new ImageData(image.Clone(), ms.ToArray());
                    }
                    finally
                    {
                        NDIlib.recv_free_video_v2(_recvInstance, ref frame);
                    }
                    break;

                case NDIlib.frame_type_e.frame_type_audio: // not used
                    NDIlib.recv_free_audio_v3(_recvInstance, ref audio);
                    break;

                case NDIlib.frame_type_e.frame_type_metadata: // not used
                    NDIlib.recv_free_metadata(_recvInstance, ref meta);
                    break;

                case NDIlib.frame_type_e.frame_type_status_change: // not used
                    break;

                case NDIlib.frame_type_e.frame_type_error:
                    // apparently there isn't any more info available
                    break;

                // "source changed" ... 101 is newer than the bindings package
                case (NDIlib.frame_type_e)101:
                    break;

                default: // frame type "none"
                    break;
            }
        }


    }

    public void Dispose()
    {
        _running = false;

        if (_recvInstance != IntPtr.Zero)
        {
            NDIlib.recv_destroy(_recvInstance);
            _recvInstance = IntPtr.Zero;
        }

        NDIlib.destroy();
    }
}