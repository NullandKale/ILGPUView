using ILGPU;
using ILGPU.Runtime;
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

    public struct FloatBitmapCanvas
    {
        public ArrayView2D<Color> canvas;
        public int sizeX;
        public int sizeY;
        public int tick;

        public FloatBitmapCanvas(ArrayView2D<Color> canvas, int sizeX, int sizeY)
        {
            this.canvas = canvas;
            this.sizeX = sizeX;
            this.sizeY = sizeY;
            tick = 0;
        }

        public void setColor(Index2 index, Color c)
        {
            canvas[index] = c;
        }

        public static void CanvasToBitmap(Index2 index, FloatBitmapCanvas c, ArrayView<byte> bitmap)
        {
            int newIndex = ((index.Y * c.sizeX) + index.X) * 3;
            Color color = c.canvas[index];

            bitmap[newIndex] = (byte)(255.99f * color.r);
            bitmap[newIndex + 1] = (byte)(255.99f * color.g);
            bitmap[newIndex + 2] = (byte)(255.99f * color.b);
        }
    }

    public static class Test
    {
        static Action<Index2, FloatBitmapCanvas, ArrayView<byte>> outputKernel;
        static Action<Index2, FloatBitmapCanvas> userKernel;

        static bool dir = true;
        static FloatBitmapCanvas c;
        static MemoryBuffer2D<Color> canvasData;
        static MemoryBuffer<byte> bitmapData;

        // DO NOT CHANGE FUNCTION PARAMETERS
        // width and height are the output bitmap size
        // the code will be unloaded on resize
        // setup is always called once before loop
        public static void setup(Accelerator accelerator, int width, int height)
        {
            canvasData = accelerator.Allocate<Color>(width, height);
            c = new FloatBitmapCanvas(canvasData, width, height);

            bitmapData = accelerator.Allocate<byte>(width * height * 3);

            outputKernel = accelerator.LoadAutoGroupedStreamKernel<Index2, FloatBitmapCanvas, ArrayView<byte>>(FloatBitmapCanvas.CanvasToBitmap);
            userKernel = accelerator.LoadAutoGroupedStreamKernel<Index2, FloatBitmapCanvas>(kernel);
        }

        // DO NOT CHANGE FUNCTION PARAMETERS
        // bitmap is a 24bpp RGB bitmap of size width * height * 3
        // loop is called until it returns false
        public static bool loop(Accelerator accelerator, ref byte[] bitmap)
        {
            userKernel(canvasData.Extent, c);
            outputKernel(canvasData.Extent, c, bitmapData);

            accelerator.Synchronize();

            bitmapData.CopyTo(bitmap, 0, 0, bitmap.Length);

            if (dir)
            {
                c.tick++;
                if (c.tick > 255)
                {
                    dir = !dir;
                }
            }
            else
            {
                c.tick--;
                if (c.tick <= 0)
                {
                    dir = !dir;
                }
            }

            return true;
        }

        public static void dispose()
        {
            canvasData.Dispose();
            bitmapData.Dispose();
        }

        public static void kernel(Index2 index, FloatBitmapCanvas c)
        {
            c.setColor(index, new Color((float)index.X / (float)c.sizeX, (float)index.Y / (float)c.sizeY, (float)c.tick / 255f));
        }
    }
}


namespace ILGPUView
{
    public delegate void setupDelegate(Accelerator accelerator, int width, int height);
    public delegate bool loopDelegate(Accelerator accelerator, ref byte[] bitmap);
    public delegate void disposeDelegate();

    public delegate void terminalDelegate();

    public class Templates
    {
        public static readonly string terminalTemplate = @"
using System;

namespace Terminal
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine(""Hello World!"");
        }
}
}
";

        public static readonly string bitmapTemplate = @"
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using System;

namespace Bitmap
{
    public static class Program
    {
        // Called once per pixel
        public static void kernel(Index2 pixelIndex, FloatBitmapCanvas c)
        {
            c.setColor(pixelIndex, new Color((float)pixelIndex.X / (float)c.sizeX, (float)pixelIndex.Y / (float)c.sizeY, (float)c.tick / 255f));
        }

        // the rest of the Owl 
        // https://i.kym-cdn.com/photos/images/original/000/572/078/d6d.jpg
        static Action<Index2, FloatBitmapCanvas, ArrayView<byte>> outputKernel;
        static Action<Index2, FloatBitmapCanvas> userKernel;

        static bool dir = true;
        static FloatBitmapCanvas c;
        static MemoryBuffer2D<Color> canvasData;
        static MemoryBuffer<byte> bitmapData;

        // DO NOT CHANGE FUNCTION PARAMETERS
        // width and height are the output bitmap size
        // the code will be unloaded on resize
        // setup is always called once before loop
        public static void setup(Accelerator accelerator, int width, int height) 
        {
            canvasData = accelerator.Allocate<Color>(width, height);
            c = new FloatBitmapCanvas(canvasData, width, height);

            bitmapData = accelerator.Allocate<byte>(width * height * 3);

            outputKernel = accelerator.LoadAutoGroupedStreamKernel<Index2, FloatBitmapCanvas, ArrayView<byte>>(FloatBitmapCanvas.CanvasToBitmap);
            userKernel = accelerator.LoadAutoGroupedStreamKernel<Index2, FloatBitmapCanvas>(kernel);
        }

        // DO NOT CHANGE FUNCTION PARAMETERS
        // bitmap is a 24bpp RGB bitmap of size width * height * 3
        // loop is called until it returns false
        public static bool loop(Accelerator accelerator, ref byte[] bitmap)
        {
            userKernel(canvasData.Extent, c);
            outputKernel(canvasData.Extent, c, bitmapData);
            
            accelerator.Synchronize();

            bitmapData.CopyTo(bitmap, 0, 0, bitmap.Length);

            if (dir)
            {
                c.tick++;
                if (c.tick > 255)
                {
                    dir = !dir;
                }
            }
            else
            {
                c.tick--;
                if(c.tick <= 0)
                {
                    dir = !dir;
                }
            }

            return true;
        }

        public static void dispose()
        {
            canvasData.Dispose();
            bitmapData.Dispose();
        }
    }

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

    public struct FloatBitmapCanvas
    {
        public ArrayView2D<Color> canvas;
        public int sizeX;
        public int sizeY;
        public int tick;

        public FloatBitmapCanvas(ArrayView2D<Color> canvas, int sizeX, int sizeY)
        {
            this.canvas = canvas;
            this.sizeX = sizeX;
            this.sizeY = sizeY;
            tick = 0;
        }

        public void setColor(Index2 index, Color c)
        {
            canvas[index] = c;
        }

        public static void CanvasToBitmap(Index2 index, FloatBitmapCanvas c, ArrayView<byte> bitmap)
        {
            int newIndex = ((index.Y * c.sizeX) + index.X) * 3;
            Color color = c.canvas[index];

            bitmap[newIndex] = (byte)(255.99f * color.r);
            bitmap[newIndex + 1] = (byte)(255.99f * color.g);
            bitmap[newIndex + 2] = (byte)(255.99f * color.b);
        }
    }
}
";
    }
}