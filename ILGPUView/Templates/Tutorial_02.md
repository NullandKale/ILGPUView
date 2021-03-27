# Tutorial 02 MemoryBuffers and ArrayViews

Welcome to the seccond ILGPU tutorial. In this tutorial we will cover the basics
 of the Memory in ILGPU. In the best case C# programmers will think of memory 
in terms of stack and heap objects, ref / in / out parameters, and GC. Once you
introduce a coprocessor like a GPU memory gets a little more complex. 

## Memory
Ok... I am about to go on a bit of a rant about memory. If you just want to get to the 
programming you can skip ahead to MemoryBuffers. The following is my understanding of 
the performance quirks with GPU's and memory and cache and coalescent memory access.

Ok, buckle up.

#### Computers need memory, and memory is slow. (Like, really slow)
Back in the day (I assume, the first computer I remember using had DDR-200) computer memory
 was FAST. Most of the time the limiting factor was the CPU, though correctly timing video output was also
a driving force. As an example, the C64 ran the memory at 2x the CPU frequency so the VIC-II 
graphics chip could share the CPU memory by stealing half the cycles. Since then, humanity 
has gotten much better at making silicon and precious metals do our bidding. Feeding 
data into the CPU from memory has become the slow part. Memory is slow.

Why is memory slow? To be honest, it seems to me that it's caused by two things:

1. Physics<br/>
Programmers like to think of computers as an abstract thing, a platonic ideal. 
But here in the real world there are no spherical cows, no free lunch. Memory values are ACTUAL
ELECTRONS traveling through silicon and precious metals. In general, the farther from the thing doing the math the ACTUAL
ELECTRONS are the slower it is to access. Wow... computers are magic.

2. We ~~need~~ want a lot of memory.<br/>
We can make very fast memory, but it must literally be directly made into the processor cores in silicon. 
Not only is this is very expensive, the more memory in silicon the less room for processor stuff. 

This leads to an optimization problem. Modern processor designers use a complex system of tiered 
memory consisting of several layers of small, fast, on die memory and large, slow, distant, off die memory.

The processors also perform a few tricks to help us deal with the fact that memory is slow. 
If a program uses memory in one spot it probably will use the memory around that spot so processors 
*prefetch* more memory than you ask for at first and put it in the cache, closer to the processor. 

I am getting off topic. For a more detailed explaination, see this thing I found on [google](https://formulusblack.com/blog/compute-performance-distance-of-data-as-a-measure-of-latency/).

What does this mean for the ILGPU?

#### GPU's have memory, and memory is slow. 

GPU's on paper have TONS of memory bandwidth, my GPU has around 10x the memory bandwidth my CPU does. Right? Yeah... 

###### Kinda
If we go back into spherical cow territory and ignore a ton of important details, we can illustrate an 
important quirk in GPU design that is directly impacts performance.

My Ryzen 5 3600 with dual channel DDR4 it gets around 34 GB/s of memory bandwidth. The GDDR6 in my RTX 2060 gets around 336 GB/s of memory bandwidth.

But lets compare bandwidth per thread.

Ryzen 5 3600 34 GB/s / 12 threads = 2.83 GB/s per thread

I thought this would be easy, but after double checking, I found that the question "How many threads can a GPU run at once?"
 is a hard question to answer. According to the cuda manual at maximum an SM (Streaming Multiprocessor) can 
have 16 warps executing simultaneously and 32 threads per warp so it can issue at minimum 512 memory accesses per 
cycle. You can schedule more warps than that but a minimum estimate will do.

RTX 2060 336 GB/s / (30 SM's * 512 threads) = 0.0218 GB/s or just *22.4 MB/s per thread*

#### So what?
In the end computers need memory because programs need memory. There are a few things I think about as I program that I think help

1. If your code scans through memory linearly the GPU can optimize it by prefetching the data. This leads to the "struct of arrays"
 approach, more on that in the structs tutorial.
2. GPU's take prefetching to the next level by having coalescent memory access, which I need a more in depth explaination of, but
basically if threads are accessing memory in a linear way that the GPU can detect it can send one memory access for the whole chunk
of threads. 

Again, this all boils down to the very simple notion that memory is slow, and it gets slower the farther it gets from the processor

