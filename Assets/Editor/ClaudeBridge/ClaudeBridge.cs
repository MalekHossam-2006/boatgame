// ClaudeBridge: tiny local HTTP server that lets an external tool (Claude Code)
// drive the Unity Editor. Listens on 127.0.0.1 only and requires a token that is
// stored in UserSettings/ClaudeBridge.token (not committed to git).
//
// Requests:  POST http://127.0.0.1:17891/<command>?key=value   header X-Token: <token>
// Body:      optional raw text (passed to "invoke" as the string argument)
// Response:  plain text / JSON-ish text. HTTP 200 on success, 500 on error.
//
// Commands:
//   ping                         editor state (playing, compiling, scene)
//   refresh                      AssetDatabase.Refresh (triggers recompile)
//   compile_errors               errors from the last script compilation
//   logs?since=N                 console messages captured since index N
//   clear_logs                   clear captured console messages
//   invoke?type=T&method=M       call static method T.M() or T.M(string body)
//   menu?path=File/Save          EditorApplication.ExecuteMenuItem
//   play | stop | pause          play mode control
//   save                         save open scenes
//   open_scene?path=Assets/..    open a scene
//   hierarchy                    dump active scene tree with components
//   screenshot?path=&w=&h=&view=game|scene   render a camera to PNG
#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ClaudeBridge
{
    const int Port = 17891;
    const string TokenPath = "UserSettings/ClaudeBridge.token";

    class Job
    {
        public string Command;
        public Dictionary<string, string> Args;
        public string Body;
        public string Result;
        public bool Failed;
        public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
    }

    static HttpListener listener;
    static Thread thread;
    static string token;
    static readonly ConcurrentQueue<Job> jobs = new ConcurrentQueue<Job>();
    static readonly List<string> logs = new List<string>();
    static readonly object logLock = new object();
    static readonly List<string> compileErrors = new List<string>();

    static ClaudeBridge()
    {
        token = LoadOrCreateToken();
        Application.logMessageReceivedThreaded += OnLog;
        EditorApplication.update += Pump;
        AssemblyReloadEvents.beforeAssemblyReload += Stop;
        EditorApplication.quitting += Stop;
        CompilationPipeline.compilationStarted += _ => { lock (compileErrors) compileErrors.Clear(); };
        CompilationPipeline.assemblyCompilationFinished += (asm, msgs) =>
        {
            lock (compileErrors)
                compileErrors.AddRange(msgs.Where(m => m.type == CompilerMessageType.Error)
                    .Select(m => $"{m.file}({m.line},{m.column}): {m.message}"));
        };
        StartServer();
    }

    // ------------------------------------------------------------------ server
    static string LoadOrCreateToken()
    {
        if (File.Exists(TokenPath)) return File.ReadAllText(TokenPath).Trim();
        Directory.CreateDirectory(Path.GetDirectoryName(TokenPath));
        var t = Guid.NewGuid().ToString("N");
        File.WriteAllText(TokenPath, t);
        return t;
    }

    static void StartServer()
    {
        try
        {
            listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            listener.Start();
            thread = new Thread(Listen) { IsBackground = true, Name = "ClaudeBridge" };
            thread.Start();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ClaudeBridge] could not start on port {Port}: {e.Message}");
        }
    }

    static void Stop()
    {
        try { listener?.Stop(); listener?.Close(); } catch { }
        listener = null;
    }

    static void Listen()
    {
        while (listener != null && listener.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = listener.GetContext(); }
            catch { break; }
            ThreadPool.QueueUserWorkItem(_ => Handle(ctx));
        }
    }

    static void Handle(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        string text;
        int status = 200;
        try
        {
            if (req.Headers["X-Token"] != token)
            {
                status = 401;
                text = "bad token";
            }
            else
            {
                string body;
                using (var r = new StreamReader(req.InputStream, Encoding.UTF8)) body = r.ReadToEnd();
                var job = new Job
                {
                    Command = req.Url.AbsolutePath.Trim('/'),
                    Args = req.QueryString.AllKeys.Where(k => k != null)
                        .ToDictionary(k => k, k => req.QueryString[k]),
                    Body = body,
                };
                jobs.Enqueue(job);
                if (!job.Done.Wait(TimeSpan.FromSeconds(120)))
                {
                    status = 504;
                    text = "timeout waiting for editor main thread";
                }
                else
                {
                    status = job.Failed ? 500 : 200;
                    text = job.Result ?? "";
                }
            }
        }
        catch (Exception e)
        {
            status = 500;
            text = e.ToString();
        }
        var bytes = Encoding.UTF8.GetBytes(text);
        res.StatusCode = status;
        res.ContentType = "text/plain; charset=utf-8";
        res.ContentLength64 = bytes.Length;
        try { res.OutputStream.Write(bytes, 0, bytes.Length); res.Close(); } catch { }
    }

    static void Pump()
    {
        while (jobs.TryDequeue(out var job))
        {
            try { job.Result = Run(job); }
            catch (Exception e)
            {
                job.Failed = true;
                job.Result = (e is TargetInvocationException && e.InnerException != null ? e.InnerException : e).ToString();
            }
            job.Done.Set();
        }
    }

    static void OnLog(string msg, string stack, LogType type)
    {
        var line = $"[{type}] {msg}";
        if (type == LogType.Exception || type == LogType.Error) line += "\n" + stack;
        lock (logLock)
        {
            logs.Add(line);
            if (logs.Count > 2000) logs.RemoveRange(0, 500);
        }
    }

    // ------------------------------------------------------------------ commands
    static string Arg(Job j, string key, string fallback = null) =>
        j.Args.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v : fallback;

    static string Run(Job j)
    {
        switch (j.Command)
        {
            case "ping":
                return $"{{\"ok\":true,\"unity\":\"{Application.unityVersion}\",\"playing\":{Bool(EditorApplication.isPlaying)}," +
                       $"\"compiling\":{Bool(EditorApplication.isCompiling)},\"updating\":{Bool(EditorApplication.isUpdating)}," +
                       $"\"scene\":\"{SceneManager.GetActiveScene().path}\",\"logCount\":{LogCount()}}}";

            case "refresh":
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                CompilationPipeline.RequestScriptCompilation();
                return "refresh requested";

            case "compile_errors":
                lock (compileErrors) return compileErrors.Count == 0 ? "none" : string.Join("\n", compileErrors);

            case "logs":
            {
                int since = int.Parse(Arg(j, "since", "0"));
                lock (logLock)
                    return string.Join("\n", logs.Skip(Math.Min(since, logs.Count)));
            }

            case "clear_logs":
                lock (logLock) logs.Clear();
                return "cleared";

            case "invoke":
                return Invoke(Arg(j, "type"), Arg(j, "method"), j.Body);

            case "menu":
                return EditorApplication.ExecuteMenuItem(Arg(j, "path")) ? "ok" : "menu item not found";

            case "play":
                EditorApplication.isPlaying = true;
                return "entering play mode";

            case "stop":
                EditorApplication.isPlaying = false;
                return "exiting play mode";

            case "pause":
                EditorApplication.isPaused = !EditorApplication.isPaused;
                return $"paused={EditorApplication.isPaused}";

            case "save":
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
                return "saved";

            case "open_scene":
                EditorSceneManager.OpenScene(Arg(j, "path"));
                return "opened " + Arg(j, "path");

            case "hierarchy":
                return Hierarchy();

            case "screenshot":
                return Screenshot(Arg(j, "path", "ClaudeTools/out/shot.png"),
                    int.Parse(Arg(j, "w", "1280")), int.Parse(Arg(j, "h", "720")), Arg(j, "view", "game"));

            default:
                throw new ArgumentException("unknown command: " + j.Command);
        }
    }

    static string Bool(bool b) => b ? "true" : "false";

    static int LogCount()
    {
        lock (logLock) return logs.Count;
    }

    static string Invoke(string typeName, string methodName, string body)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(typeName, false))
            .FirstOrDefault(t => t != null)
            ?? AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(t => t.Name == typeName);
        if (type == null) throw new ArgumentException("type not found: " + typeName);

        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var candidates = type.GetMethods(flags).Where(m => m.Name == methodName).ToArray();
        var method = candidates.FirstOrDefault(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string))
                     ?? candidates.FirstOrDefault(m => m.GetParameters().Length == 0);
        if (method == null) throw new ArgumentException($"static method {typeName}.{methodName}() or ({nameof(String)}) not found");

        var result = method.GetParameters().Length == 1
            ? method.Invoke(null, new object[] { body ?? "" })
            : method.Invoke(null, null);
        return result?.ToString() ?? "ok";
    }

    static string Hierarchy()
    {
        var sb = new StringBuilder();
        var scene = SceneManager.GetActiveScene();
        sb.AppendLine($"scene: {scene.path}");
        foreach (var root in scene.GetRootGameObjects()) Dump(root.transform, 0, sb);
        return sb.ToString();
    }

    static void Dump(Transform t, int depth, StringBuilder sb)
    {
        var comps = t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name);
        var p = t.localPosition;
        sb.Append(' ', depth * 2)
          .Append(t.gameObject.activeSelf ? "" : "(off) ")
          .Append(t.name)
          .Append($"  pos({p.x:0.##},{p.y:0.##},{p.z:0.##})");
        var list = string.Join(", ", comps);
        if (list.Length > 0) sb.Append("  [").Append(list).Append(']');
        sb.AppendLine();
        foreach (Transform c in t) Dump(c, depth + 1, sb);
    }

    static string Screenshot(string path, int w, int h, string view)
    {
        Camera cam = view == "scene" ? SceneView.lastActiveSceneView?.camera : Camera.main;
        if (cam == null) cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (cam == null) throw new InvalidOperationException("no camera found");

        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
        // render twice: the first pass after scene edits can miss newly created objects
        for (int pass = 0; pass < 2; pass++)
        {
            var request = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, request))
                RenderPipeline.SubmitRenderRequest(cam, request);
            else
            {
                var prev = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = prev;
            }
        }

        var prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prevActive;

        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full));
        File.WriteAllBytes(full, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);
        return full;
    }
}
#endif
