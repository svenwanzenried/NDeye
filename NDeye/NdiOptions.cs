/*
 * File: NdiOptions.cs
 * Project: NDeye
 * Created Date: 2026-02-25 21:40:11
 * Author: Sven Wanzenried
 * -----
 * Copyright (c) 2026 Sven Wanzenried
 * 
 * MIT License
 * --------------------------------------------------------------
 */
namespace NDeye;
public class NdiOptions
{
    public string SourceName { get; set; } = string.Empty;
    public int FrameIntervalSeconds { get; set; } = 5;
}