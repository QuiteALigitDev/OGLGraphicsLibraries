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

namespace OpenGLEngine{
    public class MainCamera : Common.Component
    {
        [Disable] public float pitch;
        [Disable] public float yaw;

        [Hide] public Vector3 up = Vector3.UnitY;
        [Hide] public Vector3 front = new Vector3(0.0f, 0.0f, -1.0f);

        private GameObject? parent;

        //This is how to get another Component (ComponentType)ComponentManager.GetComponentByID(id);
        //private MainCamera camera = (MainCamera)ComponentManager.GetComponentByID(0);
        public override void Start()
        {
            
        }

        public override void Update()
        {
            parent = ComponentManager.GetParentGameObject(this);
            Vector3 cameraTarget = Vector3.Zero;
            Vector3 cameraDirection = Vector3.Normalize( - cameraTarget);
            Vector3 cameraRight = Vector3.Normalize(Vector3.Cross(up, cameraDirection));
            Vector3 cameraUp = Vector3.Cross(cameraDirection, cameraRight);

            var position = parent!.position;

            front.X =
                (float)Math.Cos(MathHelper.DegreesToRadians(pitch))
                * (float)Math.Cos(MathHelper.DegreesToRadians(yaw));
            front.Y = (float)Math.Sin(MathHelper.DegreesToRadians(pitch));
            front.Z =
                (float)Math.Cos(MathHelper.DegreesToRadians(pitch))
                * (float)Math.Sin(MathHelper.DegreesToRadians(yaw));
            front = Vector3.Normalize(front);

            //Console.WriteLine(position);
            Matrix4 view = Matrix4.LookAt(position, position + front, up);
            //ShaderManager.numOfShaders();
            ShaderManager.SetMat4ofAllShaders(view, "view");
        }

        public override void Draw()
        {
            
        }
    }
}
