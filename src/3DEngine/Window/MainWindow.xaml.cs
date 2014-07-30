using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpDX;

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
#endif
            camera = new Camera();
            camera.Position = new Vector3(0, 0, 10.0f);
            camera.Target = Vector3.Zero;


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

            device.Render(camera, meshes.ToArray());

            device.Present();
        }

        private readonly Fps m_fps = new Fps();
    }
}
