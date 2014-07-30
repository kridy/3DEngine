using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using SharpDX;



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

        public void Render(Camera camera, Mesh[] meshes)
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


                Parallel.For(0, mesh.Faces.Length, faceIndex =>
                {
                    var face = mesh.Faces[faceIndex];
                    var pointA = Project(mesh.Vertices[face.A], transformMatrix);
                    var pointB = Project(mesh.Vertices[face.B], transformMatrix);
                    var pointC = Project(mesh.Vertices[face.C], transformMatrix);


                    var color = 0.25f + (faceIndex % mesh.Faces.Length) * 0.75f / mesh.Faces.Length;
                    DrawTriangle(pointA, pointB, pointC, new Color4(color, color, color, 1));
                });
            }
        }

        public void Present()
        {
            var rect = new Int32Rect(0, 0, renderWidth, renderHeight);

            surface.WritePixels(rect, backBuffer, 4 * renderWidth, 0);
        }

        private Vector3 Project(Vector3 point, Matrix transformMatrix)
        {
            var tPoint = Vector3.TransformCoordinate(point, transformMatrix);
            var x = tPoint.X * renderWidth + renderWidth / 2.0f;
            var y = -tPoint.Y * renderHeight + renderHeight / 2.0f;
            return (new Vector3(x, y, tPoint.Z));
        }

        private void DrawTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Color4 color)
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

            var sx = (int)Interpolate(pa.X, pb.X, gradient1);
            var ex = (int)Interpolate(pc.X, pd.X, gradient2);

            float z1 = Interpolate(pa.Z, pb.Z, gradient1);
            float z2 = Interpolate(pc.Z, pd.Z, gradient2);

            for (int x = sx; x < ex; x++)
            {
                float gradient = (x - sx) / (float)(ex - sx);

                var z = Interpolate(z1, z2, gradient);

                DrawPoint(new Vector3(x, y, z), color);
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

        //private void DrawBLine(Vector2 point0, Vector2 point1)
        //{
        //    var x0 = (int)point0.X;
        //    var y0 = (int)point0.Y;
        //    var x1 = (int)point1.X;
        //    var y1 = (int)point1.Y;

        //    var dx = Math.Abs(x1 - x0);
        //    var dy = Math.Abs(y1 - y0);
        //    var sx = (x0 < x1) ? 1 : -1;
        //    var sy = (y0 < y1) ? 1 : -1;
        //    var err = dx - dy;

        //    while (true)
        //    {
        //        DrawPoint(new Vector2(x0, y0), Color.White);

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

        private void DrawPoint(Vector3 point, Color4 color)
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