using System.Reflection;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenGLEngine.Common
{
    public static class ComponentManager
    {
        //this is probably not the most effcient way of doing this but it was a good learning experiance
        public static readonly List<GameObject> components =
            new List<GameObject>();
        public static bool started = false;

        public static void AddComponent(GameObject c)
        {
            components.Add(c);
        }

        public static void ClearComponents()
        {
            components.Clear();
        }

        public static GameObject? GetGameobjectByName(string name)
        {
            foreach (var c in components)
            {
                if (c.name == name) { return c;} 
            }
            Console.WriteLine("Gameobject does not exist.");
            return null;
        }
        public static GameObject? GetParentGameObject(Component self)
        {
            foreach (var c in components)
            {
                if (c.childComponents.Contains(self)) { return c;} 
            }
            Console.WriteLine("Gameobject does not exist.");
            return null;
        }
        public static void SceneStart()
        {
            foreach (var c in components)
                c.Start();
        }
        public static void SceneUpdate()
        {
            foreach (var c in components)
                c.Update();
        }
        public static void SceneDraw()
        {
            foreach (var c in components)
                c.Draw();
        }

        public static void StartAll()
        {
            if (started)
                return;
            started = true;

            foreach (var c in components){
                c.Start();
                foreach (var child in c.childComponents)
                    child.Start();
            }
        }

        public static void UpdateAll()
        {
            foreach (var c in components){
                c.Update();
                foreach (var child in c.childComponents)
                    child.Update();
            }
        }

        public static void DrawAll()
        {
            foreach (var c in components){
                c.Draw();
                foreach (var child in c.childComponents)
                    child.Draw();
            }
        }

        public static Type[] GetComponentTypes()
        {
            var prefix = ScriptHost.Prefix;
            IEnumerable<Assembly> asms = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => prefix == null || a.GetName().Name?.StartsWith(prefix) != true);

            if (ScriptHost.Current != null)
                asms = asms.Append(ScriptHost.Current);

            return asms
                .SelectMany(a =>
                {
                    try { return a.GetTypes().AsEnumerable(); }
                    catch (ReflectionTypeLoadException e) { return e.Types.OfType<Type>(); }
                })
                .Where(t => t.IsClass && !t.IsAbstract && typeof(Component).IsAssignableFrom(t))
                .ToArray();
        }
    }

    //base class for Components,
    public abstract class Component
    {
        public virtual void Start() { }

        public virtual void Update() { }

        public virtual void Draw() { }
    }
}
