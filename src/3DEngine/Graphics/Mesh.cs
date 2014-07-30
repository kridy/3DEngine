using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SharpDX;

namespace _3DEngine
{
    public class Mesh
    {
        public string Name { get; set; }
        public Vector3[] Vertices { get; set; }
        public Face[] Faces { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }

        public Mesh(string name, int verticesCount, int facesCount)
        {
            Vertices = new Vector3[verticesCount];
            Faces = new Face[facesCount];
            Name = name;
        }

        public static List<Mesh> LoadMeshes(string path)
        {
            var meshes = new List<Mesh>();

            dynamic jsonObject = JsonConvert.DeserializeObject(File.ReadAllText(path));

            for (var meshIndex = 0; meshIndex < jsonObject.meshes.Count; meshIndex++)
            {
                var verticesArray = jsonObject.meshes[meshIndex].vertices;

                var indicesArray = jsonObject.meshes[meshIndex].indices;

                var uvCount = jsonObject.meshes[meshIndex].uvCount.Value;
                var verticesStep = 1;

                switch ((int) uvCount)
                {
                    case 0:
                        verticesStep = 6;
                        break;
                    case 1:
                        verticesStep = 8;
                        break;
                    case 2:
                        verticesStep = 10;
                        break;
                }

                var verticesCount = verticesArray.Count/verticesStep;
                // number of faces is logically the size of the array divided by 3 (A, B, C)
                var facesCount = indicesArray.Count/3;
                var mesh = new Mesh(jsonObject.meshes[meshIndex].name.Value, verticesCount, facesCount);

                // Filling the Vertices array of our mesh first
                for (var index = 0; index < verticesCount; index++)
                {
                    var x = (float) verticesArray[index*verticesStep].Value;
                    var y = (float) verticesArray[index*verticesStep + 1].Value;
                    var z = (float) verticesArray[index*verticesStep + 2].Value;
                    mesh.Vertices[index] = new Vector3(x, y, z);
                }

                // Then filling the Faces array
                for (var index = 0; index < facesCount; index++)
                {
                    var a = (int) indicesArray[index*3].Value;
                    var b = (int) indicesArray[index*3 + 1].Value;
                    var c = (int) indicesArray[index*3 + 2].Value;
                    mesh.Faces[index] = new Face(a, b, c);
                }

                // Getting the position you've set in Blender
                var position = jsonObject.meshes[meshIndex].position;
                mesh.Position = new Vector3((float) position[0].Value, (float) position[1].Value, (float) position[2].Value);
                meshes.Add(mesh);
            }
            return meshes;
        }

        public static Mesh LoadMesh(string path, int index)
        {
            return LoadMeshes(path)[index];
        }
    }

    public class Face
    {
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }

        public Face(int a, int b, int c)
        {
            A = a;
            B = b;
            C = c;
        }
    }
}