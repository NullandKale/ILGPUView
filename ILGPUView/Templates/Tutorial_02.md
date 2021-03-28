# Tutorial 02 MemoryBuffers and ArrayViews

Welcome to the seccond ILGPU tutorial. In this tutorial we will cover the basics
 of the Memory in ILGPU. In the best case C# programmers will think of memory 
in terms of stack and heap objects, ref / in / out parameters, and GC. Once you
introduce a coprocessor like a GPU memory gets a little more complex. 

[Preface: A rant about memory.](Tutorial_02_Preface.md) 

I wrote a preface to this tutorial that I believe will help you better understand how this 
works in hardware. It is not necessary to read if you just want to learn how to use ILGPU.

Before we get into this tutorial we need a bit of jargon.

* Device: the GPU or a GPU
* Host: the computer that contains the device

In most computers the host and device each have there own seperate memory. There are some ways
to pretend that they share memory in ILGPU, like ExchangeBuffers (more on that in a more advanced memory tutorial), but in general
it is faster and uses less memory to manage both sides manually. 

# MemoryBuffer\<T\>
The MemoryBuffer is the host side copy of memory allocated on the device. 

* always obtained from an Accelerator
* requires: using ILGPU.Runtime;
* basic constructing: MemoryBuffer\<int\> OnDeviceInts = accelerator.Allocate\<int\>(1000);

# ArrayView\<T\>
The ArrayView is the device side copy of memory allocated on the device via the host.

* always obtained from a MemoryBuffer
* requires: using ILGPU.Runtime;
* basic constructing: ArrayView\<int\> ints = OnDeviceInts.View;


### Sample 02|01
All device side memory management happens in the host code through the MemoryBuffer.
The sample goes over the basics of managing memory via MemoryBuffers.

```C#
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
        public static readonly bool debug = false;
        static void Main()
        {
            // We still need the Context and Accelerator boiler plate.
            Console.WriteLine("Hello Tutorial 02!");
            Context context = new Context();
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

            // Gets array of 1000 doubles on host.
            double[] doubles = new double[1000];

            // Gets MemoryBuffer on device with same size and contents as doubles.
            MemoryBuffer<double> doublesOnDevice = accelerator.Allocate<double>(doubles);

            // What if we change the doubles on the host and need to update the device side memory?
            for (int i = 0; i < doubles.Length; i++) { doubles[i] = i * Math.PI; }

            // We call MemoryBuffer.CopyFrom which copies any linear slice of doubles into the device side memory.
            doublesOnDevice.CopyFrom(doubles, 0, 0, doubles.Length);

            // What if we change the doublesOnDevice and need to write that data into host memory?
            doublesOnDevice.CopyTo(doubles, 0, 0, doubles.Length);

            // You can copy data to and from MemoryBuffers into any array / span / memorybuffer that allocates the same
            // type. for example:
            double[] doubles2 = new double[doublesOnDevice.Length];
            doublesOnDevice.CopyTo(doubles2, 0, 0, doubles2.Length);

            // There are also helper functions, but be aware of what a function does.
            // As an example this function is shorthand for the above two lines.
            // It does a relatively slow memory allocation on the host.
            double[] doubles3 = doublesOnDevice.GetAsArray();

            // Notice that you cannot access memory in a MemoryBuffer or an ArrayView from host code.
            // If you uncomment the following lines they should crash.
            // doublesOnDevice[1] = 0;
            // double d = doublesOnDevice[1];

            // There is not much we can show with ArrayViews currently, but in the 
            // Kernels Tutorial it will go over much more.
            ArrayView<double> doublesArrayView = doublesOnDevice.View;

            // do not forget to dispose of everything in the reverse order you constructed it.
            doublesOnDevice.Dispose(); 
            // note the doublesArrayView is now invalid, but does not need to be disposed.
            accelerator.Dispose();
            context.Dispose();
        }
    }
}
```