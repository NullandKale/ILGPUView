﻿using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using System;
using System.Linq;

namespace ILGPUViewTest
{
    public static class Test
    {
        static Action<Index2, FloatBitmapCanvas, ArrayView<byte>> outputKernel;
        static Action<Index2, FloatBitmapCanvas> userKernel;

        static bool dir = true;
        static FloatBitmapCanvas c;
        static MemoryBuffer2D<Vector3> canvasData;
        static MemoryBuffer<byte> bitmapData;

        // DO NOT CHANGE FUNCTION PARAMETERS
        // width and height are the output bitmap size
        // the code will be unloaded on resize
        // setup is always called once before loop
        public static void setup(Accelerator accelerator, int width, int height)
        {
            canvasData = accelerator.Allocate<Vector3>(width, height);
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

        public static void Tutorial02()
        {
            Context context = new Context();

            bool debug = false;
            Accelerator accelerator = null;
            if (CudaAccelerator.CudaAccelerators.Length > 0 && !debug)
            {
                accelerator = new CudaAccelerator(context);
            }
            else if (CLAccelerator.AllCLAccelerators.Length > 0 && !debug)
            {
                accelerator = new CLAccelerator(context, CLAccelerator.AllCLAccelerators.FirstOrDefault());
            }
            else
            {
                accelerator = new CPUAccelerator(context);
            }

            MemoryBuffer<float> floatBuffer = accelerator.Allocate<float>(10_000_00);
            floatBuffer.MemSetToZero();

            int[] intData = new int[100_000];
            MemoryBuffer<int> intBuffer = accelerator.Allocate(intData);
            
            intBuffer.CopyTo(intData, 0, 0, intData.Length);

            intData[1] = 1;
            
            intBuffer.CopyFrom(intData, 0, 0, intData.Length);


            float[] output = floatBuffer.GetAsArray();

            accelerator.Synchronize();


            intBuffer.Dispose();
            floatBuffer.Dispose();
            accelerator.Dispose();

            for(int i = 0; i < floatBuffer.Length; i++)
            {
                Console.Write(output[i] + " ");
                if(i % 25 == 24)
                {
                    Console.WriteLine();
                }
            }
        }

        public static float[] Tutorial03()
        {
            Context context = new Context();

            bool debug = false;
            Accelerator accelerator = null;
            if (CudaAccelerator.CudaAccelerators.Length > 0 && !debug)
            {
                accelerator = new CudaAccelerator(context);
            }
            else if (CLAccelerator.AllCLAccelerators.Length > 0 && !debug)
            {
                accelerator = new CLAccelerator(context, CLAccelerator.AllCLAccelerators.FirstOrDefault());
            }
            else
            {
                accelerator = new CPUAccelerator(context);
            }

            Action<Index1, ArrayView<float>, float> addKernel =
                accelerator.LoadAutoGroupedStreamKernel<Index1, ArrayView<float>, float>(AddKernel);

            MemoryBuffer<float> buffer = accelerator.Allocate<float>(10_000_00);
            buffer.MemSetToZero();
            addKernel(buffer.Length, buffer.View, 10.0f);
            float[] output = buffer.GetAsArray();
            accelerator.Synchronize();

            buffer.Dispose();
            accelerator.Dispose();

            return output;
        }

        static void AddKernel(Index1 index, ArrayView<float> output, float val)
        {
            output[index] += val;
        }

        public static void dispose()
        {
            canvasData.Dispose();
            bitmapData.Dispose();
        }

        public static void kernel(Index2 index, FloatBitmapCanvas c)
        {
            c.setColor(index, new Vector3((float)index.X / (float)c.width, (float)index.Y / (float)c.height, (float)c.tick / 255f));
        }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    public struct FloatBitmapCanvas
    {
        public ArrayView2D<Vector3> canvas;
        public int width;
        public int height;
        public int tick;

        public FloatBitmapCanvas(ArrayView2D<Vector3> canvas, int width, int height)
        {
            this.canvas = canvas;
            this.width = width;
            this.height = height;
            tick = 0;
        }

        public void setColor(Index2 index, Vector3 c)
        {
            canvas[index] = c;
        }

        public static void CanvasToBitmap(Index2 index, FloatBitmapCanvas c, ArrayView<byte> bitmap)
        {
            Vector3 color = c.canvas[index];

            int bitmapIndex = ((index.Y * c.width) + index.X) * 3;

            bitmap[bitmapIndex] = (byte)(255.99f * color.x);
            bitmap[bitmapIndex + 1] = (byte)(255.99f * color.y);
            bitmap[bitmapIndex + 2] = (byte)(255.99f * color.z);
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
        public static string getTutorial(int ID)
        {
            switch (ID)
            {
                case 1:
                    return Tutorial01Template;
                case 2:
                    return Tutorial02Template;
                default:
                    return terminalTemplate;
            }
        }

        public static readonly string Tutorial01Template = @"

";

        public static readonly string Tutorial02Template = @"
using System;
using System.Linq;

using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;

namespace Tutorial
{
    class Program
    {
        static void Main()
        {

// Welcome to the second ILGPU tutorial. In this tutorial we will cover the 
// basics of the Memory: MemoryBuffers and ArrayViews

// Just in case you forgot, we need a quick run down on the structure of this tutorial.

//                   (This is a section header.)
// 
// There will be some important sections with important tips / tricks
// !!!!!! I will note them with exclaimation points !!!!!

// !!!!!! Note the line below that is tabbed over a few times. !!!!!!!
// This is a code example. Uncomment them, compile, and run to test them. 
// You can try it with the following line.

            //Console.WriteLine(""Hello Tutorial 02!"");

// Ok, back to the tutorial.

//                   (GPU's and Memory)
// 
// Normal CPU programs have a single uniform chunk of memory. However GPU's are
// different. Most Dedicated GPU's use a non-unified memory architecture. This 
// is just a fancy way of saying that the GPU has its own memory. If we want programs to run on the 
// GPU we need to put stuff in that memory for the GPU programs to access.

// One of the ways ILGPU helps you think about this is by having a clear division
// between the CPU side memory reference (the MemoryBuffer) and the GPU side memory
// reference (the ArrayView).

// First things first, as we learned in the first tutorial we need a
// context and an accelerator.

            using Context context = new Context();
            Console.WriteLine(""Context: "" + context.ToString());

            bool debug = false;
            Accelerator accelerator = null;
            if(CudaAccelerator.CudaAccelerators.Length > 0 && !debug)
            {
                accelerator = new CudaAccelerator(context);
            }
            else if(CLAccelerator.AllCLAccelerators.Length > 0 && !debug)
            {
                accelerator = new CLAccelerator(context, CLAccelerator.AllCLAccelerators.FirstOrDefault());
            }
            else
            {
                accelerator = new CPUAccelerator(context);
            }
            accelerator.PrintInformation();

//Ok, back to the tutorial

//                          (MemoryBuffer)
//
// All GPU memory is allocated using MemoryBuffer. Once you have a 

            //using Context context = new Context();
            //Console.WriteLine(""Context: "" + context.ToString());

// Most instances of classes that require a context reference
// as well as the Context object itself require dispose calls to
// prevent memory leaks. In most simple cases you can use the using
// pattern to make it harder to mess up.

// You can also use the ContextFlags enum to change many settings.
// but for now all we need is a basic context.
        }
    }
}
";


        public static readonly string terminalTemplate = @"
using System;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using System.Linq;

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