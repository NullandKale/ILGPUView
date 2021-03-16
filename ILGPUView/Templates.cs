using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPUViewTest;
using System;

namespace ILGPUView
{
    public delegate void setupDelegate(Accelerator accelerator, int width, int height);
    public delegate bool loopDelegate(Accelerator accelerator, ref byte[] bitmap);
    public delegate void disposeDelegate();
    public class Templates
    {
        public static readonly string codeTemplate = @"
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using System;

namespace ILGPUViewTest
{
    public struct Color
    {
        public float r;
        public float g;
        public float b;

        public Color(float r, float g, float b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }
    }

    public struct Canvas
    {
        public ArrayView2D<Color> canvas;
        public int sizeX;
        public int sizeY;

        public Canvas(ArrayView2D<Color> canvas, int sizeX, int sizeY)
        {
            this.canvas = canvas;
            this.sizeX = sizeX;
            this.sizeY = sizeY;
        }

        public void setColor(Index2 index, Color c)
        {
            canvas[index] = c;
        }
    }

    public static class Test
    {
        static Action<Index2, Canvas, ArrayView<byte>> outputKernel;
        static Action<Index2, Canvas> userKernel;

        static Canvas c;
        static MemoryBuffer2D<Color> canvasData;
        static MemoryBuffer<byte> bitmapData;

        public static void CanvasToBitmap(Index2 index, Canvas c, ArrayView<byte> bitmap)
        {
            int newIndex = ((index.Y * c.sizeX) + index.X) * 3;
            Color color = c.canvas[index];

            bitmap[newIndex] = (byte)(255.99f * color.r);
            bitmap[newIndex + 1] = (byte)(255.99f * color.g);
            bitmap[newIndex + 2] = (byte)(255.99f * color.b);
        }

        public static void setup(Accelerator accelerator, int width, int height)
        {
            canvasData = accelerator.Allocate<Color>(width, height);
            c = new Canvas(canvasData, width, height);

            bitmapData = accelerator.Allocate<byte>(width * height * 3);

            outputKernel = accelerator.LoadAutoGroupedStreamKernel<Index2, Canvas, ArrayView<byte>>(CanvasToBitmap);
            userKernel = accelerator.LoadAutoGroupedStreamKernel<Index2, Canvas>(kernel);
        }

        public static bool loop(Accelerator accelerator, ref byte[] bitmap)
        {
            userKernel(canvasData.Extent, c);
            outputKernel(canvasData.Extent, c, bitmapData);
            accelerator.Synchronize();
            bitmapData.CopyTo(bitmap, 0, 0, bitmap.Length);
            return true;
        }

        public static void dispose()
        {
            canvasData.Dispose();
            bitmapData.Dispose();
        }

        public static void kernel(Index2 index, Canvas c)
        {
            c.setColor(index, new Color((float)index.X / (float)c.sizeX, (float)index.Y / (float)c.sizeY, 0));
        }
    }
}
";
    }
}


namespace ILGPUViewTest
{
    public struct Color
    {
        public float r;
        public float g;
        public float b;

        public Color(float r, float g, float b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }
    }

    public struct Canvas
    {
        public ArrayView2D<Color> canvas;
        public int sizeX;
        public int sizeY;

        public Canvas(ArrayView2D<Color> canvas, int sizeX, int sizeY)
        {
            this.canvas = canvas;
            this.sizeX = sizeX;
            this.sizeY = sizeY;
        }

        public void setColor(Index2 index, Color c)
        {
            canvas[index] = c;
        }
    }

    public static class Test
    {
        static Action<Index2, Canvas, ArrayView<byte>> outputKernel;
        static Action<Index2, Canvas> userKernel;

        static Canvas c;
        static MemoryBuffer2D<Color> canvasData;
        static MemoryBuffer<byte> bitmapData;

        public static void CanvasToBitmap(Index2 index, Canvas c, ArrayView<byte> bitmap)
        {
            int newIndex = ((index.Y * c.sizeX) + index.X) * 3;
            Color color = c.canvas[index];

            bitmap[newIndex] = (byte)(255.99f * color.r);
            bitmap[newIndex + 1] = (byte)(255.99f * color.g);
            bitmap[newIndex + 2] = (byte)(255.99f * color.b);
        }

        public static void setup(Accelerator accelerator, int width, int height)
        {
            canvasData = accelerator.Allocate<Color>(width, height);
            c = new Canvas(canvasData, width, height);

            bitmapData = accelerator.Allocate<byte>(width * height * 3);

            outputKernel = accelerator.LoadAutoGroupedStreamKernel<Index2, Canvas, ArrayView<byte>>(CanvasToBitmap);
            userKernel = accelerator.LoadAutoGroupedStreamKernel<Index2, Canvas>(kernel);
        }

        public static bool loop(Accelerator accelerator, ref byte[] bitmap)
        {
            userKernel(canvasData.Extent, c);
            outputKernel(canvasData.Extent, c, bitmapData);
            accelerator.Synchronize();
            bitmapData.CopyTo(bitmap, 0, 0, bitmap.Length);
            return true;
        }

        public static void dispose()
        {
            canvasData.Dispose();
            bitmapData.Dispose();
        }

        public static void kernel(Index2 index, Canvas c)
        {
            c.setColor(index, new Color((float)index.X / (float)c.sizeX, (float)index.Y / (float)c.sizeY, 0));
        }
    }
}