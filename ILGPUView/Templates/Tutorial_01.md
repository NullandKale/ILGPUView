# Tutorial 01 Context and Accelerators

Welcome to the first ILGPU tutorial. In this tutorial we will cover the basics of the Context and Accelerator objects.

## Context
All ILGPU classes and functions rely on the global ILGPU Context.
The context's job is mainly to act as an interface for the ILGPU compiler. 
I believe it also stores some global state. 
* requires: using ILGPU;
* basic constructing: Context context = new Context();

### Sample 01|01 :
```C#
using System;
using ILGPU;

namespace Tutorial
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Hello Tutorial 01!");
            using Context context = new Context();
            // this doesnt really print anything special
            Console.WriteLine("Context: " + context.ToString());
        }
    }
}
```


A context object itself as well as most instances of classes that 
require a context reference require dispose calls to prevent memory 
leaks. In most simple cases you can use the using pattern as above to make it 
harder to mess up.

You can also use the ContextFlags enum to change many settings.
but for now all we need is a basic context.

## Accelerators
Every ILGPU program will require at least 1 Accelerator.
In ILGPU the accelerator repersents a hardware or software GPU.
Currently there are 3 Accelerator types CPU, Cuda, and OpenCL
Along with a generic Accelerator.

##### CPUAccelerator
requires no special hardware... well no more than c# does.
* requires: using ILGPU.CPU; and using ILGPU.Runtime;
* basic constructing: Accelerator accelerator = new CPUAccelerator(context);

In general CPU is best for debugging and as a fallback.
the CPU accelerator is slow, but is the only way to step through
device code with the debugger.

##### CudaAccelerator
* requires a Cuda capable gpu (TODO insert required version here)
* imports: using ILGPU.Cuda; using ILGPU.Runtime;
* basic constructing: Accelerator accelerator = new CudaAccelerator(context);

If you have one or more Nvida GPU's that are supported this is
the accelerator for you. 

##### CLAccelerator
* requires an OpenCL capable gpu (TODO insert required version here)
* imports: using ILGPU.OpenCL, using ILGPU.Runtime;
* basic constructing: Accelerator accelerator = new CLAccelerator(context, CLAccelerator.AllCLAccelerators.FirstOrDefault());

If you have one or more AMD or Intel GPU's that are supported this is
the accelerator for you. The CLAccelerator also supports some cpu's (TODO get more info on this)
Also as a bit of a disclaimer I do not have an OpenCL compatible gpu so
most of the OpenCL stuff is untested. Please let me know if there
are any issues.

##### Accelerator
Abstract class for storing and passing around more specific
accelerators.
* requires: using ILGPU.Runtime

### Sample 01|02
There is no guaranteed way to find the most powerful accelerator
usable from only software the following is a pretty simple way
to get what is likely the best accelerator. If you have multiple
GPU's or something you may need something more complex.


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
    // I normally have an easy to change bool or class parameter that forces
    // the cpu Accelerator
    public static readonly bool 
    class Program
    {
        static void Main()
        {
            Console.WriteLine(""Hello Tutorial 01!"");
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
            accelerator.Dispose();
        }
    }
}
```
Don't forget to dispose the accelerator. We do not have to call dispose 
of context because we used the using pattern. It is important to note 
that you must dispose objects in the reverse order from when you obtain them.

As you can see in the above sample the context is obtained first and then 
the accelerator. We dispose the accelerator explicitly by calling accelerator.Dispose();
and then only afterwards dispose the context automatically via the using pattern.

In more complex programs you will have a more complex tree of memory, kernels, streams, and accelerators
 to dispose of correctly.

Lets assume this is the structure of some program:
* Context
  * CPUAccelerator
    * Some Memory
    * Some Kernel
  * CudaAccelerator
    * Some Other Memory
    * Some Other Kernel

Anything created by the CPU accelerator must be disposed before the CPU accelerator
can be disposed. And then the CPU accelerator must be disposed before the context can
be disposed. However before we can dispose the context we must dispose the Cuda accelerator
 and everything that it owns.

Ok, this tutorial covers most of the boiler plate code needed.

The next tutorial covers memory, after that I PROMISE we will do something more interesting. I just have to write them first.