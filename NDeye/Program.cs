/*
 * File: Program.cs
 * Project: TestNdiLib
 * Created Date: 2026-02-23 21:34:43
 * Author: Sven Wanzenried
 * -----
 * Copyright (c) 2026 Sven Wanzenried
 * 
 * MIT License
 * --------------------------------------------------------------
 */
// using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NDeye;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<NdiOptions>(
    builder.Configuration.GetSection("Ndi"));

builder.Services.AddSingleton<NdiFrameService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<NdiFrameService>());


var app = builder.Build();

app.MapNDeyeEndpoints();

try
{
    app.Run();
}
catch (Exception ex)
{

}
