// Component Id Constants    
// Source - https://stackoverflow.com/a
// Posted by Jimmeh, modified by community. See post 'Timeline' for change history
// Retrieved 2026-01-06, License - CC BY-SA 2.5
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenGLEngine.Common;
using ImGuiNET;

namespace OpenGLEngine{
    public class GameObject : Component
    {
        public string name = "GameObject";
        public Vector3 position = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 rotation = new Vector3(0.0f, 0.0f, 0.0f);

        public float scale = 1.0f;

        public string modelPath = null!;
        public string texturePath = null!;
        public string FragmentShaderPath = null!;
        public string VertexShaderPath = null!;

        private Model model = null!;


        // scripts
        [Hide] public readonly List<Component> childComponents = new();

        public override void Start()
        {

            if (texturePath == null && modelPath == null) { return; }
            if (texturePath == "" && modelPath == "") { return; }

            if (VertexShaderPath != null && FragmentShaderPath != null & VertexShaderPath != "" && FragmentShaderPath != "")
            {
                var shdr = new Shader(VertexShaderPath, FragmentShaderPath!);
                ShaderManager.initShader(shdr);

                model = new Model(modelPath!, texturePath!, shdr);
            }
            else{
                model = new Model(modelPath!, texturePath!);
            }
        }

        public void AddComponent(Component c)
        {
            childComponents.Add(c);
        }
        public void RemoveComponent(Component c)
        {
            childComponents.Remove(c);
        }

        public T? GetComponent<T>() where T : Component
        {
            foreach (var c in childComponents)
            {
                if (c is T match){ return match; }
            }
            return null;
        }

        public override void Update()
        {

        }

        public override void Draw()
        {
            if (model == null) { return; }

            model.Draw(position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z, scale);
        }
    }
}
