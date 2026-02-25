/*
 * File: Endpoints.cs
 * Project: NDeye
 * Created Date: 2026-02-25 21:43:11
 * Author: Sven Wanzenried
 * -----
 * Copyright (c) 2026 Sven Wanzenried
 * 
 * MIT License
 * --------------------------------------------------------------
 */

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace NDeye;

public static class NDeyeEndpoints
{
    public static void MapNDeyeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/snapshot", SnapshotHandler);
        app.MapGet("/qr", QrHandler);
    }

    private static async Task<IResult> SnapshotHandler(
        HttpContext context,
         [FromServices] NdiFrameService svc,
         [FromQuery] bool download = false)
    {
        if (!svc.IsAvailable)
        {
            return Results.Problem(
                title: "NDI stream not available",
                detail: svc.LastError ?? "Unknown error",
                statusCode: 503);
        }

        var img = svc.GetLatestFrame();

        if (img == null)
            return Results.Problem("No frame captured yet", statusCode: 503);

        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";

        if (download is true)
        {
            context.Response.Headers.ContentDisposition = "attachment; filename=\"snapshot.png\"";
        }

        return Results.File(img, "image/png");
    }


    private static async Task<IResult> QrHandler(
        HttpContext context,
        [FromServices] NdiFrameService svc,
        [FromQuery] bool redirect = false
    )
    {
        var qrContents = svc.GetLatestQrContent();

        if (redirect is true)
        {
            var link = qrContents.FirstOrDefault(x => x.Type == QrContentType.Link);
            if (link is not null)
            {
                return Results.Redirect(link.Content);
            }
            return Results.NotFound();
        }

        var htmlString = BuildQrHtml(qrContents);
        return Results.Content(htmlString, "text/html; charset=utf-8");
    }

    private static string BuildQrHtml(List<QrContent> qrContents)
    {
        var sb = new StringBuilder();

        sb.Append("""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1" />
                <title>QR Codes</title>
                <style>
                    body { font-family: Arial, sans-serif; margin: 2rem; }
                    h1 { margin-bottom: 1rem; }
                    .item { margin-bottom: 0.5rem; }
                </style>
            </head>
            <body>
                <h1>QR Codes</h1>
            """);

        foreach (var item in qrContents)
        {
            var encoded = WebUtility.HtmlEncode(item.Content);

            if (item.Type == QrContentType.Link)
            {
                sb.Append($"<div class=\"item\"><a href=\"{encoded}\" target=\"_blank\">{encoded}</a></div>");
            }
            else
            {
                sb.Append($"<div class=\"item\">{encoded}</div>");
            }
        }

        sb.Append("""
        </body>
        </html>
        """);

        return sb.ToString();
    }


}