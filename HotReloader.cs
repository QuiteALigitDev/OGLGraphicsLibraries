using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using OpenGLEngine;
using OpenGLEngine.Common;

public static class ScriptHost
{
    class scriptContext : AssemblyLoadContext
    {
        public scriptContext() : base(isCollectible: true) { }
    }    

    public const string Prefix = "Scripts_";
    public static Assembly? Current;

    static scriptContext? ctx;
    static FileSystemWatcher? watcher;
    static long dirtyAt;

    public static void Watch(string folder)
    {
        watcher = new FileSystemWatcher(folder, "*.cs")
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        FileSystemEventHandler h = (_, _) => dirtyAt = Environment.TickCount64;
        watcher.Changed += h; watcher.Created += h; watcher.Deleted += h;
        watcher.Renamed += (_, _) => dirtyAt = Environment.TickCount64;
        dirtyAt = Environment.TickCount64;
    }

    public static bool ShouldReload()
    {
        if (dirtyAt == 0 || Environment.TickCount64 - dirtyAt < 250) return false;

        dirtyAt = 0;
        return true;
    }

    public static bool Compile(string folder)
    {
        var trees = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories).Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f));
        var refs = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location)).Select(a => MetadataReference.CreateFromFile(a.Location));
        var compilation = CSharpCompilation.Create(Prefix + Guid.NewGuid().ToString("N"), trees, refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            foreach (var d in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)){
                Console.WriteLine($"Script Error: {d}");}
            
            return false;
        }

        ms.Position = 0;
        var newCtx = new scriptContext();
        Current = newCtx.LoadFromStream(ms);
        ctx?.Unload();
        ctx = newCtx;
        return true;

    }

    public static void ReloadScripts()
    {
        if (!ScriptHost.Compile("assets")) return;

        Console.WriteLine("RELOAD!");
        
        foreach (var go in ComponentManager.components)
        {
            for (int i = go.childComponents.Count - 1; i >= 0; i--)
            {
                var old = go.childComponents[i];
                var oldType = old.GetType();

                // Only touch components that came from a script assembly
                if (oldType.Assembly.GetName().Name?.StartsWith(ScriptHost.Prefix) != true) continue;

                var newType = ScriptHost.Current!.GetType(oldType.FullName!);
                if (newType == null) { go.RemoveComponent(old); continue; } // script deleted

                var fresh = (Component)Activator.CreateInstance(newType)!;

                // Carry over public field values by name + type
                foreach (var f in oldType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    var nf = newType.GetField(f.Name, BindingFlags.Public | BindingFlags.Instance);
                    if (nf != null && nf.FieldType == f.FieldType)
                        nf.SetValue(fresh, f.GetValue(old));
                }

                go.childComponents[i] = fresh;   // in place, so order matches your JSON
                if (TimeKeeper.IsRunning) fresh.Start();
            }
        }
    }


}