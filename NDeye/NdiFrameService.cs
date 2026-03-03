/*
 * File: NdiFrameService.cs
 * Project: TestNdiLib
 * Created Date: 2026-02-23 21:38:04
 * Author: Sven Wanzenried
 * -----
 * Copyright (c) 2026 Sven Wanzenried
 * 
 * MIT License
 * --------------------------------------------------------------
 */
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ZXing;

namespace NDeye;

public partial class NdiFrameService : BackgroundService
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<NdiFrameService> _logger;
    private readonly NdiOptions _options;

    private readonly object _lock = new();

    private byte[]? _latestFrame;
    private IEnumerable<QrContent> _qrContents = [];
    private NdiReceiver? _receiver;

    public bool IsAvailable { get; private set; }
    public string? LastError { get; private set; }

    public NdiFrameService(
        ILoggerFactory loggerFactory,
        IOptions<NdiOptions> options)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<NdiFrameService>();
        _options = options.Value;
    }

    public byte[]? GetLatestFrame()
    {
        lock (_lock)
        {
            return _latestFrame;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NDI Frame Service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_receiver == null)
                {
                    _logger.LogDebug("No NDI receiver found. Creating a new one...");
                    _receiver = new NdiReceiver(_options.SourceName, _loggerFactory.CreateLogger<NdiReceiver>());
                }
                if (!IsAvailable)
                {

                    if (!_receiver.Connect())
                    {
                        _logger.LogError("NDI source not found");
                        IsAvailable = false;
                        LastError = "NDI source not found";
                        _logger.LogWarning("NDI source not found");
                        await Task.Delay(3000, stoppingToken);
                        continue;
                    }

                    _logger.LogInformation("Connected to NDI source");
                }

                var imageData = await _receiver.GetFrameAsync(stoppingToken);

                if (imageData is not null &&
                    imageData.Image is not null &&
                    imageData.Frame is not null)
                {

                    var links = DecodeQr(imageData.Image);
                    // var png = EncodePng(frame);
                    var png = imageData.Frame;

                    lock (_lock)
                    {
                        _latestFrame = png;
                        _qrContents = links
                                            .UnionBy(_qrContents, x => x.Content)                                       
                                            .Where(x => x.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-5));
                    }

                    IsAvailable = true;
                    LastError = null;
                }
                else
                {
                    IsAvailable = false;
                    LastError = "No frame received";
                }
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                LastError = ex.Message;
                _logger.LogError(ex, "NDI error");

                _receiver?.Dispose();
                _receiver = null;
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_options.FrameIntervalSeconds),
                stoppingToken);
        }
    }

    private static readonly List<string> s_validLinkSchemes = [
        Uri.UriSchemeHttp,
        Uri.UriSchemeHttps,
        Uri.UriSchemeMailto,
        Uri.UriSchemeFile,
    ];

    internal static List<QrContent> DecodeQr(Image<Bgra32> image)
    {
        var result = new List<QrContent>();
        var reader = new ZXing.ImageSharp.BarcodeReader<Bgra32>
        {
            AutoRotate = true,
            Options =
            {
                TryHarder = true,
                PossibleFormats = new[]
                {
                    BarcodeFormat.QR_CODE
                }
            }
        };

        using var ms = new MemoryStream();
        var thumbnail = image.Clone((ipc) => ipc.Resize(new ResizeOptions()
        {
            Size = new Size(256,256),
            Mode = ResizeMode.Max
        })).ToBase64String(PngFormat.Instance);
        

        var decoded = reader.DecodeMultiple(image);

        if (decoded is not null)
        {
            foreach (var qr in decoded)
            {
                if (Uri.TryCreate(qr.Text, uriKind: UriKind.Absolute, out var uriResult)
                && s_validLinkSchemes.Contains(uriResult.Scheme))
                {
                    result.Add(new(QrContentType.Link, qr.Text, thumbnail));
                }
                else
                {
                    result.Add(new QrContent(QrContentType.Text, qr.Text, thumbnail));
                }

            }
        }

        return result;
    }

    internal IEnumerable<QrContent> GetLatestQrContent()
    {
        lock (_lock)
        {
            return _qrContents;
        }
    }

}
