// See https://aka.ms/new-console-template for more information
//
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;


namespace OpenGLEngine
{
    public static class Program
    {
        private static void Main()
        {
            GLFWProvider.SetErrorCallback((errorCode, description) =>
            {
                if (errorCode == ErrorCode.FeatureUnavailable)
                {
                    // Ignore Wayland window position/icon warnings
                    return;
                }
                throw new GLFWException(description, errorCode);
            });


            using (Game game = new Game(800, 600, "LearnOpenTK"))
            {
                game.VSync = VSyncMode.On;
                game.Run();
            }
        }
    }
}
