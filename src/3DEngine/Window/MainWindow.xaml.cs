using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpDX;
using Matrix = SharpDX.Matrix;

namespace _3DEngine.Window
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private List<Mesh> meshes;
        private Camera camera;
        private Device device;
        

        public MainWindow()
        {
            InitializeComponent();

            var bmp = new WriteableBitmap(
                640, 480, 96, 96, 
                PixelFormats.Bgra32, 
                null);

            device = new Device(bmp);

            FrontBuffer.Source = bmp;

            meshes = new List<Mesh>();
#if false
            var m = new Mesh("triangle", 4, 4);

            m.Vertices[0] = new Vector3(0, 1f, 0f);
            m.Vertices[1] = new Vector3(1f, -1f, 0f);
            m.Vertices[2] = new Vector3(-1f, -1f, 0f);
            m.Vertices[3] = new Vector3(-0f, 0.0f, -1f);

            m.Faces[0] = new Face(0, 1, 2);
            m.Faces[1] = new Face(0, 3, 1);
            m.Faces[2] = new Face(0, 3, 2);
            m.Faces[3] = new Face(1, 3, 2);
            
            meshes.Add(m);
#else
            var mesh1 = Mesh.LoadMesh(@"resources\monkey.babylon", 0);
            mesh1.Position = new Vector3(-0.0f,0f,0f);
            mesh1.Rotation = new Vector3(-3.2f, 0f, 0f);
            meshes.Add(mesh1);

            meshes.Add(Mesh.LoadMesh(@"resources\monkey.babylon", 0));
            meshes.Add(Mesh.LoadMesh(@"resources\monkey.babylon", 0));
            //meshes.Add(Mesh.LoadMesh(@"resources\monkey.babylon", 0)); 
            //meshes.Add(Mesh.LoadMesh(@"resources\monkey.babylon", 0));
#endif
            camera = new Camera();
            camera.Position = new Vector3(0, 0, 10.0f);
            camera.Target = Vector3.Zero;

            render = new CompositeRender(new FaceRender());

            CompositionTarget.Rendering += CompositionTargetRendering;
        }

        void CompositionTargetRendering(object sender, EventArgs e)
        {
            Fps.Text = m_fps.ToString();

            device.Clear(0, 0, 0, 255);

            foreach (var mesh in meshes)
            {
                mesh.Rotation = new Vector3(mesh.Rotation.X + 0.01f, mesh.Rotation.Y, mesh.Rotation.Z);                
            }

            device.Render(camera, meshes.ToArray(), render);

            device.Present();
        }

        private readonly IRender render;

        private readonly Fps m_fps = new Fps();
    }

    public interface IRender
    {
        void Render(Device device, Mesh mesh, Matrix transformMatrix);
    }

    internal class FaceRender : IRender
    {
        private Device m_device;

        public void Render(Device device, Mesh mesh, Matrix transformMatrix)
        {
            m_device = device;

            Parallel.For(0, mesh.Faces.Length, faceIndex =>
            {
                var face = mesh.Faces[faceIndex];
                var pointA = m_device.Project(mesh.Vertices[face.A], transformMatrix);
                var pointB = m_device.Project(mesh.Vertices[face.B], transformMatrix);
                var pointC = m_device.Project(mesh.Vertices[face.C], transformMatrix);


                var color = 0.25f + (faceIndex % mesh.Faces.Length) * 0.75f / mesh.Faces.Length;
                DrawTriangle(pointA, pointB, pointC, new Color4(color, color, color, 1));
            });


        }

        public void DrawTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Color4 color)
        {
            if (p1.Y > p2.Y)
            {
                var temp = p2;
                p2 = p1;
                p1 = temp;
            }

            if (p2.Y > p3.Y)
            {
                var temp = p2;
                p2 = p3;
                p3 = temp;
            }

            if (p1.Y > p2.Y)
            {
                var temp = p2;
                p2 = p1;
                p1 = temp;
            }

            float dP1P2, dP1P3;

            if (p2.Y - p1.Y > 0)
                dP1P2 = (p2.X - p1.X) / (p2.Y - p1.Y);
            else
                dP1P2 = 0;

            if (p3.Y - p1.Y > 0)
                dP1P3 = (p3.X - p1.X) / (p3.Y - p1.Y);
            else
                dP1P3 = 0;

            if (dP1P2 > dP1P3)
            {
                for (var y = (int)p1.Y; y <= (int)p3.Y; y++)
                {
                    if (y < p2.Y)
                        ProcessScanLine(y, p1, p3, p1, p2, color);
                    else
                        ProcessScanLine(y, p1, p3, p2, p3, color);
                }
            }
            else
            {
                for (var y = (int)p1.Y; y <= (int)p3.Y; y++)
                {
                    if (y < p2.Y)
                        ProcessScanLine(y, p1, p2, p1, p3, color);
                    else
                        ProcessScanLine(y, p2, p3, p1, p3, color);
                }
            }
        }

        private void ProcessScanLine(int y, Vector3 pa, Vector3 pb, Vector3 pc, Vector3 pd, Color4 color)
        {
            var gradient1 = pa.Y != pb.Y ? (y - pa.Y) / (pb.Y - pa.Y) : 1;
            var gradient2 = pc.Y != pd.Y ? (y - pc.Y) / (pd.Y - pc.Y) : 1;

            var sx = (int)(pa.X + (pb.X - pa.X) * Clamp(gradient1));
            var ex = (int)(pc.X + (pd.X - pc.X) * Clamp(gradient2));

            float z1 = (pa.Z + (pb.Z - pa.Z) * Clamp(gradient1));
            float z2 = (pc.Z + (pd.Z - pc.Z) * Clamp(gradient2));

            for (int x = sx; x < ex; x++)
            {
                float gradient = (x - sx) / (float)(ex - sx);

                var z = (z1 + (z2 - z1) * Clamp(gradient));

                m_device.DrawPoint(new Vector3(x, y, z), color);
            }
        }

        private float Interpolate(float min, float max, float gradient)
        {
            return min + (max - min) * Clamp(gradient);
        }

        private float Clamp(float value, float min = 0, float max = 1)
        {
            return Math.Max(min, Math.Min(value, max));
        }
    }
    
    internal class VertexRender : IRender
    {
        public void Render(Device device, Mesh mesh, Matrix transformMatrix)
        {
            foreach ( var vertex in mesh.Vertices)
            {
                var pointA = device.Project( vertex, transformMatrix);

                device.DrawPoint(pointA, new Color4(1,0,0,1));   
            }
        }
    }

    internal class CompositeRender : IRender
    {
        private readonly List<IRender> renderList;

        public CompositeRender(params IRender[] renders)
        {
            renderList = new List<IRender>();
            renderList.AddRange(renders);
        }

        public void Render(Device device, Mesh mesh, Matrix transformMatrix)
        {
            foreach (var render in renderList)
            {
                render.Render(device,mesh,transformMatrix);
            }
        }
    }
}
