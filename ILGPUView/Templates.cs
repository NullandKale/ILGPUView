using ILGPU;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILGPUView
{
    public delegate int setupDelegate(Context context, Accelerator accelerator, int width, int height);
    public delegate int loopDelegate(ref byte[] bitmap);

    public class Templates
    {

        public static readonly string usingStatements = @"

                using System;
                using System.Diagnostics;
                using ILGPU;
                using ILGPU.Runtime;
                using ILGPU.Runtime.CPU;

";
        public static readonly string codeTemplate = usingStatements + @"


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
            int newIndex = ((index.Y * c.sizeY) + index.X) * 3;
            Color color = c.canvas[index];

            bitmap[newIndex] = (byte)(255.99f * color.r);
            bitmap[newIndex + 1] = (byte)(255.99f * color.g);
            bitmap[newIndex + 2] = (byte)(255.99f * color.b);
        }

        public static int setup(Context context, Accelerator accelerator, int width, int height)
        {
            canvasData = accelerator.Allocate<Color>(height, width);
            c = new Canvas(canvasData, height, width);

            bitmapData = accelerator.Allocate<byte>(width * height * 3);

            outputKernel = accelerator.LoadAutoGroupedStreamKernel<Index2, Canvas, ArrayView<byte>>(CanvasToBitmap);
            userKernel = accelerator.LoadAutoGroupedStreamKernel<Index2, Canvas>(kernel);

            return 1;
        }

        public static int loop(ref byte[] bitmap)
        {
            userKernel(canvasData.Extent, c);
            outputKernel(canvasData.Extent, c, bitmapData);
            bitmapData.CopyTo(bitmap, 0, 0, bitmap.Length);
            return 1;
        }

        public static void dispose()
        {
            canvasData.Dispose();
            bitmapData.Dispose();
        }

        public static void kernel(Index2 index, Canvas c)
        {
            c.setColor(index, new Color(index.X / c.sizeX, index.Y / c.sizeY, 0));
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
            int newIndex = ((index.Y * c.sizeY) + index.X) * 3;
            Color color = c.canvas[index];

            bitmap[newIndex] = (byte)(255.99f * color.r);
            bitmap[newIndex + 1] = (byte)(255.99f * color.g);
            bitmap[newIndex + 2] = (byte)(255.99f * color.b);
        }

        public static int setup(Context context, Accelerator accelerator, int width, int height)
        {
            canvasData = accelerator.Allocate<Color>(height, width);
            c = new Canvas(canvasData, height, width);

            bitmapData = accelerator.Allocate<byte>(width * height * 3);

            outputKernel = accelerator.LoadAutoGroupedStreamKernel<Index2, Canvas, ArrayView<byte>>(CanvasToBitmap);
            userKernel = accelerator.LoadAutoGroupedStreamKernel<Index2, Canvas>(kernel);

            return 1;
        }

        public static int loop(ref byte[] bitmap)
        {
            userKernel(canvasData.Extent, c);
            outputKernel(canvasData.Extent, c, bitmapData);
            bitmapData.CopyTo(bitmap, 0, 0, bitmap.Length);
            return 1;
        }

        public static void dispose()
        {
            canvasData.Dispose();
            bitmapData.Dispose();
        }

        public static void kernel(Index2 index, Canvas c)
        {
            c.setColor(index, new Color(index.X / c.sizeX, index.Y / c.sizeY, 0));
        }
    }
}
