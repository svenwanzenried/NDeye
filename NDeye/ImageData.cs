/*
 * File: ImageData.cs
 * Project: NDeye
 * Created Date: 2026-02-24 22:45:58
 * Author: Sven Wanzenried
 * -----
 * Copyright (c) 2026 Sven Wanzenried
 * 
 * MIT License
 * --------------------------------------------------------------
 */
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NDeye;

public class ImageData(Image<Bgra32> image, byte[] frame)
{
    public Image<Bgra32> Image { get; set;} = image.Clone();
    public byte[] Frame { get; } = frame;
}
