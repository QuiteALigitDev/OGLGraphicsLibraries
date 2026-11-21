using System;
using JeremyAnsel.Media.WavefrontObj;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;

namespace OpenGLEngine.Common
{
    public class ModelManager
    {
        private static readonly List<Model> models = new();

        public static void AddModelToDelBuffer(Model m)
        {
            models.Add(m);
        }

        public static void NumOfModels()
        {
            Console.WriteLine(models.Count());
        }

        public static void DelAllModels()
        {
            foreach (Model m in models)
            {
                m.Dispose();
            }
        }
    }

    public class Model
    {
        private readonly int VertexArrayObject;
        private readonly int ElementBufferObject;
        private readonly int VertexBufferObject;
        private readonly Shader _shader;
        private readonly Texture _texture;
        private uint[] _indicies;

        public void Draw(float x, float y, float z, float rx, float ry, float rz, float scale)
        {
            // Shader
            _shader.Use();

            // Texture
            _texture.Use(TextureUnit.Texture0);

            // Transformations
            Matrix4 Translation = Matrix4.CreateTranslation(x, y, z);
            Matrix4 RotationX = Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(rx));
            Matrix4 RotationY = Matrix4.CreateRotationY((float)MathHelper.DegreesToRadians(ry));
            Matrix4 RotationZ = Matrix4.CreateRotationZ((float)MathHelper.DegreesToRadians(rz));
            Matrix4 Scale = Matrix4.CreateScale(scale);

            var rotation = RotationX * RotationY * RotationZ;
            var modelTransform = Scale * rotation * Translation;

            _shader.SetMat4("model", modelTransform);

            GL.BindVertexArray(VertexArrayObject);
            //GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            GL.DrawElements(
                PrimitiveType.Triangles,
                _indicies.Length,
                DrawElementsType.UnsignedInt,
                0
            );
        }

        public Model(string path, string TexturePath, Shader shdr = null!)
        {

            ModelManager.AddModelToDelBuffer(this);
            // Loading Models
            uint[] indices;
            float[] vertices;

            (vertices, indices) = ParseOBJ(path);

            // Creating and Binding Objects from OpenGL
            VertexBufferObject = GL.GenBuffer();
            ElementBufferObject = GL.GenBuffer();
            VertexArrayObject = GL.GenVertexArray();

            if (shdr == null){
                _shader = ShaderManager.globalShader;
            }
            else
            {
                _shader = shdr;
            }

            _indicies = indices;

            GL.BindVertexArray(VertexArrayObject);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);

            //EBO can only exist if a VAO is bound
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                indices.Length * sizeof(uint),
                indices,
                BufferUsageHint.StaticDraw
            );

            // Assigning Buffer Data
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                vertices.Length * sizeof(float),
                vertices,
                BufferUsageHint.StaticDraw
            );

            // The first parameter specifies which vertex attribute we want to configure. Remember that we specified the location of the position vertex attribute in the vertex shader with layout (location = 0). This sets the location of the vertex attribute to 0 and since we want to pass data to this vertex attribute, we pass in 0.
            // The next argument specifies the size of the vertex attribute. The vertex attribute is a vec3 so it is composed of 3 values.
            // The third argument specifies the type of the data, which is float (a vec* in GLSL consists of floating point values).
            // The next argument specifies if we want the data to be normalized. If we're inputting integer data types (int, byte) and we've set this to true, the integer data is normalized to 0 (or -1 for signed data) and 1 when converted to float. This is not relevant for us, so we'll leave this as false.
            // The fifth argument is known as the stride and tells us the space between consecutive vertex attributes. Since the next set of position data is located exactly 3 times the size of a float away we specify that value as the stride. Note that since we know that the array is tightly packed (there is no space between the next vertex attribute value) we could've also specified the stride as 0 to let OpenGL determine the stride (this only works when values are tightly packed). Whenever we have more vertex attributes we have to carefully define the spacing between each vertex attribute but we'll get to see more examples of that later on.
            // The last parameter is the offset of where the position data begins in the buffer. Since the position data is at the start of the data ar
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(
                0,
                3,
                VertexAttribPointerType.Float,
                false,
                8 * sizeof(float),
                0
            );

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(
                1,
                2,
                VertexAttribPointerType.Float,
                false,
                8 * sizeof(float),
                3 * sizeof(float)
            );

            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(
                2,
                3,
                VertexAttribPointerType.Float,
                false,
                8 * sizeof(float),
                5 * sizeof(float)
            );

            // Texture Loading

            _texture = new(TexturePath);
            _texture.Use(TextureUnit.Texture0);
            _shader.SetInt("texture1", 0);
        }

        // Model Loading
        public static (float[] vertices, uint[] indices) ParseOBJ(string path)
        {
            ObjFile objFile = ObjFile.FromFile(path);

            List<float> finalVertices = new List<float>();
            List<uint> finalIndices = new List<uint>();

            // Maps "vertexIndex/textureIndex" -> flattened index, so repeated
            // combinations get reused instead of duplicated.
            Dictionary<(int, int, int), uint> vertexLookup =
                new Dictionary<(int, int, int), uint>();

            foreach (ObjFace face in objFile.Faces)
            {
                List<uint> faceIndices = new List<uint>();

                foreach (ObjTriplet point in face.Vertices)
                {
                    // Library indices are already 1-based-converted; still need -1 for 0-based C# lists.
                    int vertexIndex = point.Vertex - 1;
                    int textureIndex = point.Texture - 1;
                    int normalIndex = point.Normal - 1;

                    var key = (vertexIndex, textureIndex, normalIndex);

                    if (!vertexLookup.TryGetValue(key, out uint flattenedIndex))
                    {
                        ObjVertex v = objFile.Vertices[vertexIndex];
                        finalVertices.Add(v.Position.X);
                        finalVertices.Add(v.Position.Y);
                        finalVertices.Add(v.Position.Z);

                        if (textureIndex >= 0)
                        {
                            ObjVector3 uv = objFile.TextureVertices[textureIndex];
                            finalVertices.Add(uv.X);
                            finalVertices.Add(uv.Y);
                        }
                        else
                        {
                            // No UV data for this face/vertex — fall back to 0,0.
                            finalVertices.Add(0f);
                            finalVertices.Add(0f);
                        }

                        if (normalIndex >= 0)
                        {
                            ObjVector3 normal = objFile.VertexNormals[normalIndex];
                            finalVertices.Add(normal.X);
                            finalVertices.Add(normal.Y);
                            finalVertices.Add(normal.Z);
                        }
                        else
                        {
                            finalVertices.Add(0f);
                            finalVertices.Add(0f);
                            finalVertices.Add(0f);
                        }

                        flattenedIndex = (uint)(finalVertices.Count / 8 - 1);
                        vertexLookup[key] = flattenedIndex;
                    }

                    faceIndices.Add(flattenedIndex);
                }

                // Fan-triangulate in case of quads/n-gons.
                for (int i = 1; i < faceIndices.Count - 1; i++)
                {
                    finalIndices.Add(faceIndices[0]);
                    finalIndices.Add(faceIndices[i]);
                    finalIndices.Add(faceIndices[i + 1]);
                }
            }

            return (finalVertices.ToArray(), finalIndices.ToArray());
        }

        // Model Disposal
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                GL.DeleteBuffer(ElementBufferObject);
                GL.DeleteBuffer(VertexBufferObject);
                GL.DeleteVertexArray(VertexArrayObject);

                disposedValue = true;
            }
        }

        ~Model()
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
    }
}
