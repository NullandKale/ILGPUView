# Primer 01: Code
This tutorial will provide a quick rundown the basics of how kernels (think GPU programs) run.
If you are already familiar with cuda or openCL programs you can probably skip this.

## A GPU is not a CPU
If you will allow a little bit of **massive oversimplification**, this is pretty easy to understand.

A traditional processor has a very simple cycle: fetch, decode, execute. It grabs an instruction
from memory (fetch), figures out how to perform said instruction (decode), and does the instruction 
(execute). This linearity is fine for most programs because CPUs are super fast, and CPUs also have 
multiple cores which can each do the cycle. What happens when you have an algorithm that can be processed 
in parallel? You can throw multiple cores at the problem, but in the end each core will be running 
a stream of instructions, likely the *same* stream of instructions. 

Both GPUs and CPUs try and exploit this fact, but use very different methods.

##### CPU | SIMD: Single Instruction Multiple Data.
CPUs have a trick for parallel programs called SIMD. These are a set of instructions
that allow you to have one instuction do operations on multiple peices of data at once.

Lets say a CPU has an add instruction: 
> ADD RegA RegB

Which would perform

> RegA = RegB + RegA

The SIMD version would be:
> ADD RegABCD RegEFGH

Which would perform
> RegA = RegE + RegA
> 
> RegB = RegF + RegB
> 
> RegC = RegG + RegC
>
> RegD = RegH + RegD

All at once.

A clever programmer can take these instructions and get a 3x-8x performance improvement
in very math heavy scenarios.

##### GPU | SIMT: Single Instruction Multiple Threads.
GPUs have SIMT. SIMT is the same idea as SIMD but instead of just doing the math instructions
 in parallel why not do **all** the instructions in parallel. 

The GPU assumes all the instructions you are going to fetch and decode for 32 threads are 
the same, it does 1 fetch and decode to setup 32 execute steps, then it does all 32 execute 
steps at once. This allows you to get 32 way multithreading per single core, if and only 
if all 32 threads want to do the same instruction. 

### Kernels
Kernels are GPU programs. When you launch a kernel in ILGPU you give it a 

When I was first learning about kernels I made an observation that make kernels kinda *click*
in my head. Kernels and Parallel.For have the same usage pattern. If you don't know about 
Parallel.For it is a function that provides a really easy way to run code on every core of 
the CPU. For both kernels and 
```C#
using System;
using System.Threading.Tasks;

namespace Primer_01
{
    class Program
    {
        static void Main(string[] args)
        {
            int[] data = { 0, 1, 2, 4, 5, 6, 7, 8, 9 };
            int[] output = new int[10_000];
                
            Parallel.For(0, output.Length, (int i) =>
            {
                output[i] = data[i % data.Length];
            });
        }
    }
}
```
Parallel.For is a widely used feature of C# and it allows you to run the inside of a for loop



# OLD STUFF
#### What does the GPU part mean?
GPUs are designed to be good at one task, rendering. In the very early days of 3D 
rendering (think Doom / StarFox / Elite) the CPU did all of the rendering serially.
Drawing triangles to the screen (or in Doom's case vertical lines, kinda) is a very
parallel algoritim, why not exploit this? 

The first few generations of GPU were fixed function shaders. They took in triangles, 
textures, and a bit of configuration data, and gave you shaded pixels on screen. As 
time went on people started wanting more control, fancier graphics, more pixels. After 
a while that "bit of configuration data" became full programs that just ran on the GPU's
 processing cores. 

#### The General Purpose part
This is where the General Purpose stuff started getting added. If you have an expensive, 
heavy, power hungry coprocessor in your computer why not exploit it for more than just 
games? There are many algorithms that can be speed up by just throwing threads at it.

#### "Embarrassingly Parallel" Algorithms
We have this coprocessor in our computers that is really good at parallel algorithms.
What can we use that for? The first super parallel algorithm that I learned happens to 
be a rendering algorithm: Monte Carlo Ray Tracing. This algorithm is so easy to make 
parallel because it simply repeats for each pixel. It's simple to have each thread 
follow what I like to call the "read whatever you want, but write your index only" pattern. 