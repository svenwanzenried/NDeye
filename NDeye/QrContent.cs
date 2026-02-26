/*
 * File: QrContent.cs
 * Project: TestNdiLib
 * Created Date: 2026-02-23 21:38:04
 * Author: Sven Wanzenried
 * -----
 * Copyright (c) 2026 Sven Wanzenried
 * 
 * MIT License
 * --------------------------------------------------------------
 */
namespace NDeye;

internal class QrContent
{
    public QrContent(QrContentType type, string content, DateTimeOffset? timestamp = null)
    {
        if (timestamp is null) { Timestamp = DateTimeOffset.UtcNow; }
        else { Timestamp = (DateTimeOffset)timestamp; }

        Type = type;
        Content = content;
    }

    public QrContentType Type { get; }
    public DateTimeOffset Timestamp { get; }
    public string Content { get; }
}

