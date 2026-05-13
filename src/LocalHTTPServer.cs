using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ClaudeUsageWidget;

/// Tiny HTTP server bound to 127.0.0.1:9123. Only serves the latest cached
/// snapshot — never triggers a fresh fetch (which would burn DPAPI prompts
/// and steal turns from the tray's polling loop).
public sealed class LocalHTTPServer : IDisposable
{
    public const int Port = 9123;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private UsageSnapshot? _lastSnapshot;

    public void SetSnapshot(UsageSnapshot? s) => _lastSnapshot = s;

    public void Apply(bool enabled)
    {
        if (enabled && _listener == null) Start();
        else if (!enabled && _listener != null) Stop();
    }

    private void Start()
    {
        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Start();
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => AcceptLoop(_cts.Token));
        }
        catch
        {
            _listener = null;
            _cts = null;
        }
    }

    private void Stop()
    {
        _cts?.Cancel();
        try { _listener?.Stop(); } catch { }
        try { _listener?.Close(); } catch { }
        _listener = null;
        _cts = null;
    }

    private async Task AcceptLoop(CancellationToken ct)
    {
        while (_listener != null && _listener.IsListening && !ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try { ctx = await _listener.GetContextAsync(); }
            catch { return; }
            _ = Task.Run(() => Handle(ctx));
        }
    }

    private void Handle(HttpListenerContext ctx)
    {
        try
        {
            var path = ctx.Request.Url?.AbsolutePath ?? "/";
            string body; int status; string contentType = "application/json";
            switch (path.TrimEnd('/'))
            {
                case "":
                case "/usage":
                    body = EncodeSnapshot(_lastSnapshot);
                    status = 200;
                    break;
                case "/healthz":
                    body = "{\"ok\":true}";
                    status = 200;
                    break;
                default:
                    body = "{\"error\":\"not found\"}";
                    status = 404;
                    break;
            }
            var bytes = Encoding.UTF8.GetBytes(body);
            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = contentType;
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.Headers["Cache-Control"] = "no-store";
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
        }
        catch { }
        finally
        {
            try { ctx.Response.Close(); } catch { }
        }
    }

    private static string EncodeSnapshot(UsageSnapshot? s)
    {
        if (s == null) return "{\"error\":\"no snapshot yet\"}";
        var dict = new Dictionary<string, object>
        {
            ["weekly_utilization_pct"] = (int)Math.Round(s.WeeklyUtilization),
            ["weekly_resets_at"]       = s.WeeklyResetsAt.ToString("O"),
            ["fetched_at"]             = s.FetchedAt.ToString("O"),
        };
        if (s.FiveHourUtilization is double fu)  dict["five_hour_utilization_pct"] = (int)Math.Round(fu);
        if (s.FiveHourResetsAt is DateTime fr)   dict["five_hour_resets_at"]       = fr.ToString("O");
        if (s.SonnetUtilization is double su)    dict["sonnet_utilization_pct"]    = (int)Math.Round(su);
        if (!string.IsNullOrEmpty(s.DisplayName)) dict["display_name"]             = s.DisplayName;
        if (!string.IsNullOrEmpty(s.PlanLabel))   dict["plan"]                     = s.PlanLabel;
        return JsonSerializer.Serialize(dict);
    }

    public void Dispose() => Stop();
}
