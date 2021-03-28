# ILGPUView Tutorials

## Beginner (How ILGPU works)

This series is meant to be a brief overview of ILGPU and how to use it. It assumes you have at least a little knowledge of how Cuda or OpenCL work. 
If you need a primer look to something like [this for Cuda](https://developer.nvidia.com/about-cuda) or [this for OpenCL](https://www.khronos.org/opencl/)

01 [Context and Accelerators](Tutorial_01.md) (0.10.0)
> This tutorial covers the creating the Context and Accelerator objects which setup ILGPU for use. 
> It's mostly boiler plate and does no computation but it does print info about your GPU if you have one.
> There is some advice about ILGPU in here that makes it worth the quick read.

02 [Preface: A rant about memory.](Tutorial_02_Preface.md) 
> This will hopefully give you a better understanding of how memory works in hardware and the performance
> implications. It is skippable.

02 [MemoryBuffers and ArrayViews](Tutorial_02.md) (0.10.0)
> This tutorial covers the basics for Host / Device memory management.

03 Kernels

04 Structs

05 Algorithms 1 Math

## Beginner II (Something more interesting)

Well at least I think. This is where I will put ILGPUView bitmap shader things I (or other people if they want to) eventually write. Below are the few I have planned / think would be easy.

1. Ray Tracing in One Weekend based raytracer
2. Cloud Simulation
2. 2D Physics Simulation
3. Other things I see on shadertoy

