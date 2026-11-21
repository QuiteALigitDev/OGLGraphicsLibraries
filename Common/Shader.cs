using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenGLEngine.Common{
    public class ShaderManager{
        private static readonly List<Shader> shaders = new();

        private static Game _game = null!;

        private static bool created = false;
        public static Shader globalShader = null!;

        public static void createGlobalShader()
        {
            if (created)
                return;
            created = true;

            globalShader = new Shader("Common/Shaders/global.vert", "Common/Shaders/global.frag");
            ShaderManager.initShader(globalShader);
        }

        public static void SendNativeRefs(Game game)
        {
            _game = game;
        }

        public static void initShader(Shader s)
        {
            s.Use();
            // Transform OBJ Verts from vertex Coordinantes to world space
            Matrix4 model = Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(-55.0f));
            // world space to view matrix
            Matrix4 view = Matrix4.LookAt(
                new Vector3(0.0f, 0.0f, 3.0f),
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 1.0f, 0.0f)
            );
            // projection matrix
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45.0f),
                _game.ClientSize.X / (float)_game.ClientSize.Y,
                0.1f,
                100.0f
            );

            s.SetMat4("model", model);
            s.SetMat4("view", view);
            s.SetMat4("projection", projection);

            Vector2 Resolution = _game.FramebufferSize;
            //Console.WriteLine(Resolution);
            s.SetVec2("Resolution", Resolution);


            shaders.Add(s);
        }

        public static void SetMat4ofAllShaders(Matrix4 mat4, string name){
            foreach (Shader s in shaders){
                s.SetMat4(name, mat4);
            }
        }

        public static void numOfShaders()
        {
            Console.WriteLine(shaders.Count());
        }

        public static void disposeAllShaders()
        {
            foreach (Shader s in shaders)
            {
                s.Dispose();
            }
        }
    }

    public class Shader
    {
        int Handle;

        public void Use()
        {
            GL.UseProgram(Handle);
        }

        public Shader(string vertexPath, string fragmentPath)
        {
            int VertexShader;
            int FragmentShader;

            // Load Source Code From Vertex And Fragment Shader Sources.
            string VertexShaderSource = File.ReadAllText(vertexPath);
            string FragmentShaderSource = File.ReadAllText(fragmentPath);

            // Create/Generate and Bind Our Shaders.
            VertexShader = GL.CreateShader(ShaderType.VertexShader); // Create
            GL.ShaderSource(VertexShader, VertexShaderSource); // Bind

            FragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(FragmentShader, FragmentShaderSource);

            GL.CompileShader(VertexShader);

            GL.GetShader(VertexShader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(VertexShader);
                Console.WriteLine(infoLog);
            }

            GL.CompileShader(FragmentShader);

            GL.GetShader(FragmentShader, ShaderParameter.CompileStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(FragmentShader);
                Console.WriteLine(infoLog);
            }

            Handle = GL.CreateProgram();

            GL.AttachShader(Handle, VertexShader);
            GL.AttachShader(Handle, FragmentShader);

            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(Handle);
                Console.WriteLine(infoLog);
            }

            // kill off individual vertex and frag shaders since theyre copied away.
            GL.DetachShader(Handle, VertexShader);
            GL.DetachShader(Handle, FragmentShader);
            GL.DeleteShader(FragmentShader);
            GL.DeleteShader(VertexShader);
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                GL.DeleteProgram(Handle);

                disposedValue = true;
            }
        }

        ~Shader()
        {
            if (disposedValue == false)
            {
                Console.WriteLine("GPU Resource leak! Did you forget to call Dispose()?");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void SetInt(string name, int value)
        {
            Use();
            int location = GL.GetUniformLocation(Handle, name);

            GL.Uniform1(location, value);
        }
        public void SetFloat(string name, float value)
        {
            Use();
            int location = GL.GetUniformLocation(Handle, name);

            GL.Uniform1(location, value);
        }

        public void SetMat4(string name, Matrix4 value)
        {
            Use();
            int location = GL.GetUniformLocation(Handle, name);

            GL.UniformMatrix4(location, true, ref value);
        }

        public void SetVec2(string name, Vector2 value){
            Use();
            int location = GL.GetUniformLocation(Handle, name);

            GL.Uniform2(location, ref value);
        }
    }
}
