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

internal class QrContent(QrContentType type, string content)
{
    public QrContentType Type { get; } = type;
    public string Content { get; } = content;
}

