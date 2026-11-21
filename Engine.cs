using System.Text.Json;
using OpenGLEngine.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Dear_ImGui_Sample.Backends;
using ImGuiNET;
using System.Runtime.InteropServices;
using System.Reflection;
using OpenTK.Windowing.Common.Input;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;


namespace OpenGLEngine
{
    public static class Input
    {
        static Game? window;

        public static void Init(Game game) => window = game;

        public static bool IsKeyDown(Keys key) => window?.KeyboardState.IsKeyDown(key) ?? false;
        public static bool IsKeyPressed(Keys key) => window?.KeyboardState.IsKeyPressed(key) ?? false;   // true only the frame it goes down
        public static bool IsKeyReleased(Keys key) => window?.KeyboardState.IsKeyReleased(key) ?? false; // true only the frame it goes up

        public static bool IsMouseDown(MouseButton button) => window?.MouseState.IsButtonDown(button) ?? false;
        public static Vector2 MouseDelta => window?.MouseState.Delta ?? Vector2.Zero;
        public static Vector2 MousePosition => window?.MouseState.Position ?? Vector2.Zero;
        public static Vector2 PrevScroll => window?.MouseState.PreviousScroll ?? Vector2.Zero;
        public static Vector2 Scroll => window?.MouseState.Scroll ?? Vector2.Zero;
        public static void GrabMouse(bool b){ window?.CursorState = b ? CursorState.Grabbed : CursorState.Normal; }
        public static void ConfineMouse(bool b){ window?.CursorState = b ? CursorState.Confined : CursorState.Normal; }
        public static void HideMouse(bool b){ window?.CursorState = b ? CursorState.Hidden : CursorState.Normal; }
        public static void SetCursorShape(MouseCursor mc) { window?.Cursor = mc; }


    }


    public static class TimeKeeper
    {
        public static double time;
        public static bool IsRunning = false;
    }

    public class Game(int width, int height, string title) : GameWindow(
            GameWindowSettings.Default,
            new NativeWindowSettings() { ClientSize = (width, height), Title = title }
            )
    {
        // scene

        JsonNode? rootNode;
        string CurScene = "SampleScene";

        int FBO, renderedTexture, depthrenderbuffer;
        int quadVAO, quadVBO;
        Shader? screenShader;

        public StringWriter consoleCapture = new();
        Dictionary<(int, int), Dictionary<string, object?>> snapshot = new();

        //fps 
        double lastTime;
        int nbFrames = 0;
        float fps;

        Vector3 ViewportPos;

        float pitch = 0;
        float yaw = 0;
        float Sensitivity = 0.15f;
        bool mouseDragging = false;

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            if (TimeKeeper.IsRunning) {
                ComponentManager.UpdateAll(); 
            } 
            else 
            {

                ComponentManager.SceneUpdate(); 

                Vector3 up = Vector3.UnitY;
                Vector3 front = new Vector3(0.0f, 0.0f, -1.0f);
                Vector3 cameraTarget = Vector3.Zero;
                Vector3 cameraDirection = Vector3.Normalize( - cameraTarget);
                Vector3 cameraRight = Vector3.Normalize(Vector3.Cross(up, cameraDirection));
                Vector3 cameraUp = Vector3.Cross(cameraDirection, cameraRight);

                if (Input.IsMouseDown(MouseButton.Button1)){               
                    mouseDragging = true; 
                    Input.SetCursorShape(MouseCursor.PointingHand);

                    yaw += Input.MouseDelta.X * Sensitivity;
                    pitch -= Input.MouseDelta.Y * Sensitivity;

                    if (pitch > 89.0f)
                    {
                        pitch = 89.0f;
                    }
                    else if (pitch < -89.0f)
                    {
                        pitch = -89.0f;
                    }
                }
                else if (mouseDragging)
                {
                    Input.SetCursorShape(MouseCursor.Default);
                    mouseDragging = false;
                }

                if (Input.PrevScroll.X > Input.Scroll.X && Input.PrevScroll.Y > Input.Scroll.Y)
                {
                    ViewportPos += front * (5.5f * (float)TimeKeeper.time);
                }
                else
                {
                    ViewportPos -= front * (5.5f * (float)TimeKeeper.time);
                }

                if (KeyboardState.IsKeyDown(Keys.D0))
                {
                    try {
                        MainCamera? cam = ComponentManager.GetGameobjectByName("Camera")!.GetComponent<MainCamera>();
                        pitch = cam!.pitch;
                        yaw = cam!.yaw;
                        ViewportPos = ComponentManager.GetParentGameObject(cam)!.position;
                    } 
                    catch{ Console.WriteLine("No Main Camera In Scene! You Must Have a GameObject Named 'Camera'."); }
                }

                front.X =
                    (float)Math.Cos(MathHelper.DegreesToRadians(pitch))
                    * (float)Math.Cos(MathHelper.DegreesToRadians(yaw));
                front.Y = (float)Math.Sin(MathHelper.DegreesToRadians(pitch));
                front.Z =
                    (float)Math.Cos(MathHelper.DegreesToRadians(pitch))
                    * (float)Math.Sin(MathHelper.DegreesToRadians(yaw));
                front = Vector3.Normalize(front);

                //Console.WriteLine(position);
                Matrix4 view = Matrix4.LookAt(ViewportPos, ViewportPos + front, up);
                //ShaderManager.numOfShaders();
                ShaderManager.SetMat4ofAllShaders(view, "view");
            }
        }

        public void LoadScene(string scene)
        {
            ComponentManager.ClearComponents();

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText("EngineJson.json"));
            var root = document.RootElement;
            rootNode = JsonNode.Parse(document.RootElement.GetRawText())!;

            Title = root.GetProperty("WindowTitle").GetString();
            GL.Viewport(
                0,
                0,
                root.GetProperty("ScreenWidth").GetInt32(),
                root.GetProperty("ScreenHeight").GetInt32()
            );

            JsonArray sceneArray = rootNode?["Scenes"]?[CurScene]?.AsArray() ?? new JsonArray();

            foreach (var obj in sceneArray)
            {
                if (obj is not JsonArray) { continue; }
                JsonArray objArray = obj.AsArray();

                GameObject header = new GameObject();

                foreach (var script in objArray)
                {
                    string? typename = script?.GetValue<string>();

                    if (typename == null) { continue; }


                    Type? type = ScriptHost.Current?.GetType(typename) ?? Type.GetType(typename);

                    if (type != null)
                    {
                        object? instance = Activator.CreateInstance(type);

                        if (instance is Component) { header.AddComponent((Component)instance);}
                    }
                }

                ComponentManager.AddComponent(header);
            }
        }

        // This function runs once when the winodw opens.
        // Init Code Should Go Here!
        protected override void OnLoad()
        {
            base.OnLoad();

            lastTime = GLFW.GetTime();

            // Capture Console Output
            Console.SetOut(consoleCapture);

            // Setup Hot Reload
            ScriptHost.Watch("assets");
            ScriptHost.Compile("assets");

            LoadScene(CurScene);

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                IncludeFields = true // Required so ValueTuple (int, int) saves Item1 and Item2
            };

            // Background Color when we run GL.Clear() in OnRenderFrame.
            GL.ClearColor(0.1059f, 0.1059f, 0.1059f, 1.0f);

            //Frame Buffer Code from https://www.opengl-tutorial.org/intermediate-tutorials/tutorial-14-render-to-texture/
            // Set up Frame Buffer
            FBO = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);


            renderedTexture = GL.GenTexture(); // create a texture we're going to render to
            GL.BindTexture(TextureTarget.Texture2D, renderedTexture); // bind a texture to renderedTexture
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, FramebufferSize.X, FramebufferSize.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, 0); // give an empty image to opengl ( the last "0" )

            // poor filtering needed
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new int[] { (int)TextureMagFilter.Nearest });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new int[] { (int)TextureMagFilter.Nearest });
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, renderedTexture, 0);

            // GL depth buffer
            depthrenderbuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthrenderbuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, FramebufferSize.X, FramebufferSize.Y);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthrenderbuffer);

            var Drawbuffers = new DrawBuffersEnum[] { DrawBuffersEnum.ColorAttachment0 };
            GL.DrawBuffers(1, Drawbuffers);

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
                Console.WriteLine($"FBO incomplete: {status}");
    
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0); // back to screen

            float[] g_quad_vertex_buffer_data = {
                -1.0f, -1.0f, 0.0f,
                1.0f, -1.0f, 0.0f,
                -1.0f,  1.0f, 0.0f,
                -1.0f,  1.0f, 0.0f,
                1.0f, -1.0f, 0.0f,
                1.0f,  1.0f, 0.0f,
            };

            quadVAO = GL.GenVertexArray();
            GL.BindVertexArray(quadVAO);
            quadVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, quadVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, g_quad_vertex_buffer_data.Length * sizeof(float), g_quad_vertex_buffer_data, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.BindVertexArray(0);

            try
            {
                screenShader = new Shader("Common/Shaders/screen.vert", "Common/Shaders/screen.frag");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating screen shader: {ex.Message}");
            }

            GL.Enable(EnableCap.DepthTest);

            // ImGUI set up, backend for openTK provided by NogginBops https://github.com/NogginBops/ImGui.NET_OpenTK_Sample/blob/opentk4.0/Dear%20ImGui%20Sample/Window.cs 36-50
            ImGui.CreateContext();
            ImGuiIOPtr io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            //io.ConfigFlags |= ImGuiConfigFlags.
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            //io.ConfigFlags |= ImGuiConfigFlags.;

            unsafe{
                ImFontConfigPtr cfg = ImGuiNative.ImFontConfig_ImFontConfig();
                ImFontConfigPtr glyphCfg = ImGuiNative.ImFontConfig_ImFontConfig();
                cfg.OversampleH = 2;
                cfg.OversampleV = 2;
                cfg.PixelSnapH = true;
                glyphCfg.PixelSnapH = true;
                glyphCfg.MergeMode = true;
                ushort[] iconRanges = [0xEA60, 0xEC84, 0xF000, 0xF2FF, 0];
                var _iconRangeHandle = GCHandle.Alloc(iconRanges, GCHandleType.Pinned);
                nint rangePtr = _iconRangeHandle.AddrOfPinnedObject();   //icons_ranges[] = { 0xea60, 0xec84, 0 };
                io.Fonts.AddFontFromFileTTF("fonts/0xProtoNerdFont-Regular.ttf", 25.0f, cfg);
                io.Fonts.AddFontFromFileTTF("fonts/0xProtoNerdFont-Regular.ttf", 25.0f, glyphCfg, rangePtr);

                cfg.Destroy();
            }

            ImGui.StyleColorsDark();

            ImGuiStylePtr style = ImGui.GetStyle();
            if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
            {
                style.ScaleAllSizes(1.0f);
                style.WindowRounding = 0.0f;
                style.Colors[(int)ImGuiCol.WindowBg].W = 1.0f;
            }

            ImguiImplOpenTK4.Init(this);
            ImguiImplOpenGL3.Init();

            // init input
            Input.Init(this);

            // Create The Shader!
            ShaderManager.SendNativeRefs(this);
            ShaderManager.createGlobalShader();

            //ComponentManager.StartAll();
        }
        // This runs everytime we render a frame.
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            TimeKeeper.time = e.Time;

            // Rendering ImGUI from https://github.com/NogginBops/ImGui.NET_OpenTK_Sample/blob/opentk4.0/Dear%20ImGui%20Sample/Window.cs 60 - 80
            ImguiImplOpenGL3.NewFrame();
            ImguiImplOpenTK4.NewFrame();
            ImGui.NewFrame();

            ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);

            // This is where our UI lives. 
            BaseUI();

            ImGui.Begin("Viewport");

            var obj = ComponentManager.components;

            if (!TimeKeeper.IsRunning){
                if (ImGui.Button("Run"))
                {
                    ComponentManager.started = false;

                    snapshot.Clear();

                    for (int g = 0; g < obj.Count; g++)
                    {
                        var o = obj[g];
                        var t = o.GetType();
                        var cap = t.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => !f.IsLiteral && !f.IsInitOnly).ToDictionary(f => f.Name, f => f.GetValue(o));
                        
                        snapshot[(g, -1)] = cap;

                        for (int c = 0; c < obj[g].childComponents.Count; c++)
                        {
                            var co = obj[g].childComponents[c];
                            var ct = co.GetType();
                            var cCap = ct.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => !f.IsLiteral && !f.IsInitOnly).ToDictionary(f => f.Name, f => f.GetValue(co));
                            
                            snapshot[(g, c)] = cCap;
                        }
                    }
                    TimeKeeper.IsRunning = true;

                    ComponentManager.StartAll();
                }
            }
            else
            {
                if (ImGui.Button("Stop"))
                {
                    ComponentManager.started = false;

                    for (int g = 0; g < obj.Count; g++)
                    {
                        if (snapshot.TryGetValue((g, -1), out var goVals))
                        {
                            foreach (var f in obj[g].GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => !f.IsLiteral && !f.IsInitOnly))
                            {
                                if(goVals.TryGetValue(f.Name, out var v) && (v == null || f.FieldType.IsInstanceOfType(v)))
                                {
                                    f.SetValue(obj[g], v);
                                }
                            }
                        }

                        for (int c = 0; c < obj[g].childComponents.Count; c++)
                        {
                            if (snapshot.TryGetValue((g, c), out var CgoVals))
                            {
                                foreach (var f in obj[g].childComponents[c].GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => !f.IsLiteral && !f.IsInitOnly))
                                {
                                    if(CgoVals.TryGetValue(f.Name, out var v) && (v == null || f.FieldType.IsInstanceOfType(v)))
                                    {
                                        f.SetValue(obj[g].childComponents[c], v);
                                    }
                                }
                            }
                        }
                    }
                    TimeKeeper.IsRunning = false;

                    ComponentManager.SceneStart();
                }
            }

            var ScaleXY = ImGui.GetContentRegionAvail() / new System.Numerics.Vector2(FramebufferSize.X, FramebufferSize.Y);

            float scale = Math.Min(ScaleXY.X, ScaleXY.Y);


            ImGui.Image((IntPtr)renderedTexture, new System.Numerics.Vector2(FramebufferSize.X * scale, FramebufferSize.Y * scale), new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));

            ImGui.End();

            ImGui.Render();


            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
            GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
            GL.Enable(EnableCap.DepthTest);
            // Clears the screen and set the base color to be the one set in OnLoad().
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); 

            if (ScriptHost.ShouldReload()) ScriptHost.ReloadScripts();

            if (TimeKeeper.IsRunning){ ComponentManager.DrawAll(); } else { ComponentManager.SceneDraw(); }


            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
            GL.Disable(EnableCap.DepthTest);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            screenShader?.Use();
            screenShader?.SetInt("screenTexture", 0);
            screenShader?.SetFloat("time", (float)TimeKeeper.time);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, renderedTexture);
            // GL.BindVertexArray(quadVAO);
            // GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            
            // ImGUI part 2 
            ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

            if (ImGui.GetIO().ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
            {
                ImGui.UpdatePlatformWindows();
                ImGui.RenderPlatformWindowsDefault();
                Context.MakeCurrent();
            }

            // pushes these edits to the screen and gives us a new canvas for new edits.
            // "Buffers" Contain these edits, were swaping them.

            SwapBuffers();

            base.OnRenderFrame(e);
        }

        // runs when the framebuffers resized. makes the NDC (Normalized Device Coordinance) be correctly scaled to the screen.
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            if (renderedTexture != 0)
            {
                GL.BindTexture(TextureTarget.Texture2D, renderedTexture);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, FramebufferSize.X, FramebufferSize.Y, 0,
                            PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthrenderbuffer);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24,
                                    FramebufferSize.X, FramebufferSize.Y);
            }

            GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
            float aspect = FramebufferSize.X / (float)Math.Max(1, FramebufferSize.Y);
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45f), aspect, 0.1f, 100f);
            ShaderManager.SetMat4ofAllShaders(projection, "projection");
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            File.WriteAllText("EngineJson.json", rootNode?.ToJsonString() ?? "");

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                IncludeFields = true
            };

            ImguiImplOpenGL3.Shutdown();
            ImguiImplOpenTK4.Shutdown();

            ShaderManager.disposeAllShaders();
            ModelManager.DelAllModels();
            
        }

        // UI

        int selectedindex = 0;
        float maxFPS = 60.0f;
        readonly Queue<float> recentFPS = new(800);

        // Assets

        bool checkedFolder = false;
        string path = "assets";
        string[] files = null!;
        string[] dirs = null!;

        public void BaseUI()
        {
            // File Browser
            ImGui.Begin("Assets");

            if (!checkedFolder){ files = Directory.GetFiles(path); dirs = Directory.GetDirectories(path); }
            Console.WriteLine();

            if(ImGui.Button("Back")){ try {path = Directory.GetParent(path).FullName;} catch { Console.WriteLine("Cannot Go Back! No further Directories"); } }

            if ((files.Length + dirs.Length) != 0)
            {
                foreach (var d in dirs)
                {
                    var dir = new DirectoryInfo(d).Name;
                    if(ImGui.Selectable($"\uea83 {dir}"))
                    {
                        path += $"/{dir}";
                    }
                }
                foreach (var f in files)
                {
                    string extension = Path.GetExtension(f);
                    string icon;

                    switch (extension){
                        case ".obj":
                            icon = "\uf1b2";
                            break;
                        case ".png":
                            icon = "\uf03e";
                            break;
                        case ".cs":
                            icon = "\uf1c9";
                            break;
                        default:
                            icon = "\uea7b";
                            break;
                    }

                    ImGui.Selectable($"{icon} {Path.GetFileName(f)}");
                }
            }

            ImGui.End();

            // Console
            ImGui.Begin("Console");

            if (ImGui.Button("Clear")){consoleCapture.GetStringBuilder().Clear();}

            string[] items = consoleCapture.ToString().Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);

            if (items.Length != 0){
                if (ImGui.BeginTable("Console", 1)){
                    ImGui.TableSetupColumn("Output...");
                    ImGui.TableHeadersRow();

                    foreach (var i in items)
                    {
                        ImGui.TableNextRow();

                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text(i);
                    }
                    ImGui.EndTable();
                }
            }

            ImGui.End();

            ImGui.ShowDemoWindow();
            // Performance Metrics
            ImGui.Begin("Performance Metrics");

            bool newFpsSample = false;

            if (TimeKeeper.IsRunning){
                double currentTime = GLFW.GetTime();
                nbFrames++;
                newFpsSample = false;
                if (currentTime - lastTime >= 1.0){
                    fps = nbFrames / (float)(currentTime - lastTime);
                    nbFrames = 0;
                    lastTime += 1.0;
                    newFpsSample = true;
                }   
            }
            else
            {
                recentFPS.Clear();
            }

            ImGui.Text($"FPS: {fps}");
            ImGui.Text($"Frame Time: {1000.0f / fps} ms");
            ImGui.Text($"Max FPS: {maxFPS}");
            ImGui.Separator();

            maxFPS = Math.Max(maxFPS, fps);

            if (newFpsSample)
            {
                if (recentFPS.Count >= 800)
                {
                    recentFPS.Dequeue(); // Removes the oldest item
                }
                recentFPS.Enqueue(fps); // One sample per second
            }
            if (recentFPS.Count > 0)
            {
                float[] fpsValues = recentFPS.ToArray();
                float recentMinFPS = fpsValues.Min();
                float recentMaxFPS = fpsValues.Max();
                float fpsPadding = Math.Max((recentMaxFPS - recentMinFPS) * 0.2f, 1.0f);
                ImGui.PlotLines(" ", ref fpsValues[0], fpsValues.Length, 0, "FPS Graph", recentMinFPS - fpsPadding, recentMaxFPS + fpsPadding, new System.Numerics.Vector2(0, 160));
            }

            ImGui.End();

            // Heirarchy (script viewer)
            ImGui.Begin("Objects");

            JsonArray sceneArray = rootNode?["Scenes"]?[CurScene]?.AsArray() ?? new JsonArray();

            if(ImGui.Button("Add...")){ ImGui.OpenPopup("addGameObjectPopup"); }
            ImGui.SameLine();
            if(ImGui.Button("Remove Selected")){ ComponentManager.components.Remove(ComponentManager.components.ElementAt(selectedindex)); sceneArray.RemoveAt(selectedindex); rootNode?["Scenes"]?[CurScene] = sceneArray; selectedindex = 0; }
            ImGui.Separator();

            if(ImGui.BeginPopup("addGameObjectPopup"))
            {
                if (ImGui.Button("Add GameObject"))
                {
                    sceneArray.Add(new JsonArray());
                    rootNode?["Scenes"]?[CurScene] = sceneArray;
                    ComponentManager.AddComponent(new GameObject());
                }
                ImGui.EndPopup();
            }

            ImGui.BeginMultiSelect(ImGuiMultiSelectFlags.SingleSelect);
            

            foreach(var (c, i) in ComponentManager.components.Select((value, i) => (value, i)))
            {
                bool isSelected = selectedindex == i;

                if(ImGui.Selectable($"{c.name}##", isSelected))
                {
                    selectedindex = i;
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndMultiSelect();
            ImGui.End();

            // Inspector
            ImGui.Begin("Inspector");

            try{
                var selC = ComponentManager.components.ElementAt(selectedindex);

                ImGui.SeparatorText($"{selC.GetType().Name}");
                
                Type type = selC.GetType();
                

                // Retrieves both instance and static public fields
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                foreach (var field in fields)
                {
                    RenderInspectorVars(field, selC);
                }
                ImGui.Separator();

                Component? toRemove = null; // use your actual base type

                foreach (var component in selC.childComponents)
                {
                    ImGui.SeparatorText($"{component.GetType().Name}");
                    ImGui.SameLine();
                    AlignIMGUI(ImGui.CalcTextSize("Remove").X, 1.0f);

                    if (ImGui.Button($"Remove##Remove{component.GetType().Name}"))
                    {
                        toRemove = component;
                    }

                    FieldInfo[] childFields = component.GetType().GetFields(
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                    foreach (var field in childFields)
                    {
                        RenderInspectorVars(field, component);
                    }
                }

                if (toRemove != null)
                {
                    int idx = selC.childComponents.IndexOf(toRemove);
                    selC.RemoveComponent(toRemove);
                    if (idx >= 0) sceneArray[selectedindex]?.AsArray().RemoveAt(idx);
                }

                ImGui.Dummy(new System.Numerics.Vector2(0, 15));

                AlignIMGUI(ImGui.CalcTextSize("Add Component").X, 0.5f);
                if (ImGui.Button("Add Component")) { ImGui.OpenPopup("addComponent"); }

                if (ImGui.BeginPopupContextItem("addComponent"))
                {
                    foreach (var Ctype in ComponentManager.GetComponentTypes())
                    {
                        if (ImGui.Selectable(Ctype.Name))
                        {
                            var cmpnt = (Component)Activator.CreateInstance(Ctype)!;

                            sceneArray[selectedindex]?.AsArray().Add(Ctype.FullName);
                            rootNode?["Scenes"]?[CurScene] = sceneArray;

                            selC.AddComponent(cmpnt);
                            cmpnt.Start();
                            ImGui.CloseCurrentPopup();
                        }
                    }
                    ImGui.EndPopup();
                }

                ImGui.End();
            }
            catch (ArgumentOutOfRangeException)
            {
                ImGui.Text("No Component Selected");
                ImGui.End();
                return;
            }
        }

        public void AlignIMGUI(float width, float alignment)
        {
            float avail = ImGui.GetContentRegionAvail().X;
            float off = (avail - width) * alignment;
            if (off > 0.0f){ ImGui.SetCursorPosX(ImGui.GetCursorPosX() + off); }
        
        }


        public void RenderInspectorVars(FieldInfo field, Component selC)
        {
            string name = field.Name;
            // Pass null for static fields, or the object instance for instance fields
            object? value = field.GetValue(field.IsStatic ? null : selC);

            bool IsReadOnly = field.IsLiteral || field.IsInitOnly;

            bool isDisabled = false;
            if (field.IsDefined(typeof(DisableAttribute), inherit: true)) {isDisabled = true; ImGui.BeginDisabled();}

            if (field.IsDefined(typeof(HideAttribute), inherit: true)) {return;}

            if (field.IsDefined(typeof(HeaderAttribute), inherit : true)) {ImGui.SeparatorText($"{field.GetCustomAttribute<HeaderAttribute>()!.Header}##{selC.GetType().Name}"); return; }

            if (field.IsDefined(typeof(DescriptionAttribute), inherit : true)) {ImGui.TextWrapped($"{field.GetCustomAttribute<DescriptionAttribute>()!.Description}##{selC.GetType().Name}"); return; }

            if (value == null && field.FieldType == typeof(string)){
                value = "";}

            switch (value)
            {
                case string s when !IsReadOnly:
                    if (ImGui.InputText($"{name}##{selC.GetType().Name}", ref s, 256)) { field.SetValue(selC, s); }
                    break;
                case int i when !IsReadOnly:
                    if (ImGui.DragInt($"{name}##{selC.GetType().Name}", ref i)) { field.SetValue(selC, i); }
                    break;
                case float f when !IsReadOnly:
                    if (ImGui.DragFloat($"{name}##{selC.GetType().Name}", ref f, 0.05f)) { field.SetValue(selC, f); }
                    break;
                case bool b when !IsReadOnly:
                    if (ImGui.Checkbox($"{name}##{selC.GetType().Name}", ref b)) { field.SetValue(selC, b); }
                    break;
                case Vector2 v2 when !IsReadOnly:
                    var sv = new System.Numerics.Vector2(v2.X, v2.Y);
                    if (ImGui.DragFloat2($"{name}##{selC.GetType().Name}", ref sv, 0.05f)) { field.SetValue(selC, new Vector2(sv.X, sv.Y)); }
                    break;
                case Vector3 v3 when !IsReadOnly:
                    var sv3 = new System.Numerics.Vector3(v3.X, v3.Y, v3.Z);
                    if (ImGui.DragFloat3($"{name}##{selC.GetType().Name}", ref sv3, 0.05f)) { field.SetValue(selC, new Vector3(sv3.X, sv3.Y, sv3.Z)); }
                    break;
                default:
                    ImGui.Text($"{name}: {(value?.ToString() ?? "null")} ({field.FieldType.Name})");
                    break;

            }
            if (isDisabled){ ImGui.EndDisabled(); }

            if (field.IsDefined(typeof(TooltipAttribute), inherit : true)) {ImGui.SetItemTooltip(field.GetCustomAttribute<TooltipAttribute>()!.Tooltip); return; }

        }

        public readonly static DebugProc DebugProcCallback = Window_DebugProc;
        private static void Window_DebugProc(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr messagePtr, IntPtr userParam)
        {
            string message = Marshal.PtrToStringAnsi(messagePtr, length);

            bool showMessage = true;

            switch (source)
            {
                case DebugSource.DebugSourceApplication:
                    showMessage = false;
                    break;
                case DebugSource.DontCare:
                case DebugSource.DebugSourceApi:
                case DebugSource.DebugSourceWindowSystem:
                case DebugSource.DebugSourceShaderCompiler:
                case DebugSource.DebugSourceThirdParty:
                case DebugSource.DebugSourceOther:
                default:
                    showMessage = true;
                    break;
            }

            if (showMessage)
            {
                switch (severity)
                {
                    case DebugSeverity.DontCare:
                        Console.WriteLine($"[DontCare] [{source}] {message}");
                        break;
                    case DebugSeverity.DebugSeverityNotification:
                        //Logger?.LogDebug($"[{source}] {message}");
                        break;
                    case DebugSeverity.DebugSeverityHigh:
                        Console.Error.WriteLine($"Error: [{source}] {message}");
                        break;
                    case DebugSeverity.DebugSeverityMedium:
                        Console.WriteLine($"Warning: [{source}] {message}");
                        break;
                    case DebugSeverity.DebugSeverityLow:
                        Console.WriteLine($"Info: [{source}] {message}");
                        break;
                    default:
                        Console.WriteLine($"[default] [{source}] {message}");
                        break;
                }
            }
        }
    }
}
