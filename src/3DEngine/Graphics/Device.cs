using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using SharpDX;
using _3DEngine.Window;


namespace _3DEngine
{
    public static class Vector3Util
    {
        public static Vector2 XY(this Vector3 vector)
        {
            return new Vector2(vector.X, vector.Y);
        }
    }

    public class Device
    {
        private readonly WriteableBitmap surface;
        private readonly byte[] backBuffer;
        private readonly float[] depthBuffer;

        private readonly object[] lockBuffer;

        private readonly int renderWidth;
        private readonly int renderHeight;

        public Device(WriteableBitmap bmp)
        {
            surface = bmp;

            renderWidth = surface.PixelWidth;
            renderHeight = surface.PixelHeight;

            backBuffer = new byte[renderWidth * renderHeight * 4];
            depthBuffer = new float[renderWidth * renderHeight];
            lockBuffer = new object[renderWidth * renderHeight];

            for (var i = 0; i < lockBuffer.Length; i++)
            {
                lockBuffer[i] = new object();
            }
        }

        public void Clear(byte r, byte g, byte b, byte a)
        {
            for (int i = 0; i < backBuffer.Length; i += 4)
            {
                backBuffer[i] = b;
                backBuffer[i + 1] = g;
                backBuffer[i + 2] = r;
                backBuffer[i + 3] = a;
            }

            for (var index = 0; index < depthBuffer.Length; index++)
            {
                depthBuffer[index] = float.MaxValue;
            }
        }

        public void Render(Camera camera, Mesh[] meshes, IRender render)
        {

            var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
            var projectionMatrix = Matrix.PerspectiveFovLH(
                0.5f,
                (float)renderWidth / renderHeight,
                0.01f,
                1.0f);


            foreach (var mesh in meshes)
            {

                var worldMatrex = Matrix.RotationYawPitchRoll(
                    mesh.Rotation.X,
                    mesh.Rotation.Y,
                    mesh.Rotation.Z)*
                                  Matrix.Translation(mesh.Position);

                var transformMatrix = worldMatrex*viewMatrix*projectionMatrix;

                render.Render(this,mesh,transformMatrix);
                
            }
        }

        public void Present()
        {
            var rect = new Int32Rect(0, 0, renderWidth, renderHeight);

            surface.WritePixels(rect, backBuffer, 4 * renderWidth, 0);
        }

        public Vector3 Project(Vector3 point, Matrix transformMatrix)
        {
            var tPoint = Vector3.TransformCoordinate(point, transformMatrix);
            var x = tPoint.X * renderWidth + renderWidth / 2.0f;
            var y = -tPoint.Y * renderHeight + renderHeight / 2.0f;
            return (new Vector3(x, y, tPoint.Z));
        }



        //private void DrawBLine(Vector3 point0, Vector3 point1, Color4 color)
        //{
        //    var x0 = (int)point0.X;
        //    var y0 = (int)point0.Y;
        //    var z0 = (int)point0.Z;
        //    var x1 = (int)point1.X;
        //    var y1 = (int)point1.Y;
        //    var z1 = (int)point1.Z;


        //    var dx = Math.Abs(x1 - x0);
        //    var dy = Math.Abs(y1 - y0);
        //    var dz = Math.Abs(z1 - z0);
        //    var sx = (x0 < x1) ? 1 : -1;
        //    var sy = (y0 < y1) ? 1 : -1;
        //    var sz = (z0 < z1) ? 1 : -1;
        //    var err = dx - dy;

        //    while (true)
        //    {
        //        DrawPoint(new Vector3(x0, y0, z0), color);

        //        if ((x0 == x1) && (y0 == y1)) break;
        //        var e2 = 2 * err;
        //        if (e2 > -dy) { err -= dy; x0 += sx; }
        //        if (e2 < dx) { err += dx; y0 += sy; }
        //    }
        //}

        //private void DrawLine(Vector2 point0, Vector2 point1)
        //{
        //    var dist = (point0 - point1).Length();
        //    if (dist < 2)
        //        return;

        //    Vector2 middlePoint = point0 + (point1 - point0) / 2;

        //    DrawPoint(middlePoint, Color.White);
        //    DrawLine(point0, middlePoint);
        //    DrawLine(middlePoint, point1);
        //}

        public void DrawPoint(Vector3 point, Color4 color)
        {
            if (!(point.X >= 0) ||
                !(point.Y >= 0) ||
                !(point.X < renderWidth) ||
                !(point.Y < renderHeight))
                return;

            var index = ((int)point.X + (int)point.Y * renderWidth);

            lock (lockBuffer[index])
            {
                if (depthBuffer[index] < point.Z)
                    return;

                depthBuffer[index] = point.Z;

                index *= 4;

                backBuffer[index] = (byte)(color.Blue * 255);
                backBuffer[index + 1] = (byte)(color.Green * 255);
                backBuffer[index + 2] = (byte)(color.Red * 255);
                backBuffer[index + 3] = (byte)(color.Alpha * 255);
            }
        }
    }
}