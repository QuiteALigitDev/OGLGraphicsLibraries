//Component Id Constants    
// Source - https://stackoverflow.com/a
// Posted by Jimmeh, modified by community. See post 'Timeline' for change history
// Retrieved 2026-01-06, License - CC BY-SA 2.5
using System;
using OpenGLEngine.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenGLEngine{
    public class TestTwo : Component
    {

        public float Sensitivity = 0.15f;
        //[Description("This is a model. Lorem ipsum aenfjaeif aiefna neiafn aneiafn enaufn aneifa neiaf ")]

        public float speed = 5.5f;
        Vector2 lastPos;
        public bool canMouseMove;
        public bool firstMove;

        //This is how to get another Component (ComponentType)ComponentManager.GetComponentByID(id);
        //private GameObject obj = ComponentManager.GetComponentByID(0);
        public override void Start()
        {


        }

        public override void Update()
        {

            var camera = ComponentManager.GetGameobjectByName("Camera")?.GetComponent<MainCamera>();
            var parent = ComponentManager.GetParentGameObject(this);
            if (camera == null || parent == null) return;


            var mouse = Input.MousePosition;
            if (firstMove) { lastPos = new Vector2(mouse.X, mouse.Y); firstMove = false; }
            float deltaX = mouse.X - lastPos.X;
            float deltaY = mouse.Y - lastPos.Y;
            lastPos = new Vector2(mouse.X, mouse.Y);

            if (!canMouseMove) return;

            camera.yaw += deltaX * Sensitivity;
            camera.pitch = System.Math.Clamp(camera.pitch - deltaY * Sensitivity, -89f, 89f);

            float step = speed * (float)TimeKeeper.time;
            var right = Vector3.Normalize(Vector3.Cross(camera.front, camera.up));

            if (Input.IsKeyDown(Keys.W)) parent.position += camera.front * step;
            if (Input.IsKeyDown(Keys.S)) parent.position -= camera.front * step;
            if (Input.IsKeyDown(Keys.A)) parent.position -= right * step;
            if (Input.IsKeyDown(Keys.D)) parent.position += right * step;
            if (Input.IsKeyDown(Keys.Space)) parent.position += camera.up * step;
            if (Input.IsKeyDown(Keys.LeftShift)) parent.position -= camera.up * step;
        }

        public override void Draw()
        {
            
        }
    }
}
