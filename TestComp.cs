//Component Id Constants    
// Source - https://stackoverflow.com/a
// Posted by Jimmeh, modified by community. See post 'Timeline' for change history
// Retrieved 2026-01-06, License - CC BY-SA 2.5

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenGLEngine.Common;

namespace OpenGLEngine{
    public class TestComp : Common.Component
    {

        //[Header("Model")]
        public Model windmill = null!;

        [Tooltip("Changes The Scale Of The Dragon Model")]
        public float size;
        Model windmillTwo = null!;
        Shader normalShader = null!;
        public override void Start()
        {
            normalShader = new Shader("Shaders/shader.vert", "Shaders/normals.frag");
            ShaderManager.initShader(normalShader);


            windmillTwo = new Model("assets/FixedDragon.obj", "assets/watermill_diffuse.png", normalShader);
            windmill = new Model("assets/watermill.obj", "assets/watermill_diffuse.png");

        }

        public override void Update()
        {

        }

        public override void Draw()
        {
            windmill.Draw(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f);

            windmill.Draw(-15.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, size);

            windmillTwo.Draw(15.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, size);

        }
    }
}
