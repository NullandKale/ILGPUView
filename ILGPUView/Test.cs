using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILGPUViewTest
{
    public static class Test
    {
        static Action<Index2, DeviceData, ArrayView<byte>> frameBufferToBitmap;
        static Action<Index1, DeviceData> particleProcessingKernel;

        static DeviceData c;
        static MemoryBuffer2D<Vec3> canvasData;
        static MemoryBuffer<Particle> particleData;
        static MemoryBuffer<byte> bitmapData;

        // DO NOT CHANGE FUNCTION PARAMETERS
        // width and height are the output bitmap size
        // the code will be unloaded on resize
        // setup is always called once before loop
        public static void setup(Accelerator accelerator, int width, int height)
        {
            canvasData = accelerator.Allocate<Vec3>(width, height);
            int numParticles = 10_000;
            Particle[] particles = new Particle[numParticles];
            Random rng = new Random();
            for (int i = 0; i < numParticles; i++)
            {
                Vec3 pos = new Vec3((float)rng.NextDouble() * width, (float)rng.NextDouble() * height, 1);
                particles[i] = new Particle(pos);
            }
            particleData = accelerator.Allocate(particles);
            c = new DeviceData(canvasData, particleData, width, height);

            bitmapData = accelerator.Allocate<byte>(width * height * 3);

            frameBufferToBitmap = accelerator.LoadAutoGroupedStreamKernel<Index2, DeviceData, ArrayView<byte>>(DeviceData.CanvasToBitmap);
            particleProcessingKernel = accelerator.LoadAutoGroupedStreamKernel<Index1, DeviceData>(particleKernel);
        }

        // DO NOT CHANGE FUNCTION PARAMETERS
        // bitmap is a 24bpp RGB bitmap of size width * height * 3
        // loop is called until it returns false
        public static bool loop(Accelerator accelerator, ref byte[] bitmap)
        {
            particleProcessingKernel((Index1)particleData.Extent, c);
            frameBufferToBitmap(canvasData.Extent, c, bitmapData);

            accelerator.Synchronize();

            bitmapData.CopyTo(bitmap, 0, 0, bitmap.Length);

            return true;
        }

        public static void dispose()
        {
            canvasData.Dispose();
            bitmapData.Dispose();
        }

        public static void particleKernel(Index1 index, DeviceData c)
        {
            c.particles[index].update(c, index);
            Index2 position = new Index2((int)c.particles[index].position.x, (int)c.particles[index].position.y);
            c.setColor(position, new Vec3(1, 1, 1));
        }
    }

    public struct Vec3
    {
        public float x;
        public float y;
        public float z;

        public Vec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vec3 operator +(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        public static Vec3 operator -(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }

        public static Vec3 operator *(Vec3 v1, float v)
        {
            return new Vec3(v1.x * v, v1.y * v, v1.z * v);
        }

        public float length()
        {
            return XMath.Sqrt(x * x + y * y + z * z);
        }
    }

    public struct Particle
    {
        public Vec3 position;
        public Vec3 velocity;
        public Vec3 acceleration;

        public Particle(Vec3 position)
        {
            this.position = position;
            velocity = new Vec3();
            acceleration = new Vec3();
        }

        private void updateAcceleration(DeviceData d, int ID)
        {
            acceleration = new Vec3();

            for (int i = 0; i < d.particles.Length; i++)
            {
                Vec3 otherPos;
                float mass;

                if (i == ID)
                {
                    //creates a mass at the center of the screen
                    otherPos = new Vec3(0.5f * d.width, 0.5f * d.height, 0);
                    mass = (float)d.particles.Length;
                }
                else
                {
                    otherPos = d.particles[i].position;
                    mass = 1f;
                }

                float deltaPosLength = (position - otherPos).length();
                float temp = (d.gc * mass) / XMath.Pow(deltaPosLength, 3f);
                acceleration += (otherPos - position) * temp;
            }
        }

        private void updatePosition(DeviceData d)
        {
            position = position + velocity + acceleration * 0.5f;
        }

        private void updateVelocity()
        {
            velocity = velocity + acceleration;
        }

        public void update(DeviceData d, int ID)
        {
            updateAcceleration(d, ID);
            updatePosition(d);
            updateVelocity();
        }
    }

    public struct DeviceData
    {
        public ArrayView2D<Vec3> canvas;
        public ArrayView<Particle> particles;
        public int width;
        public int height;
        public float gc;

        public DeviceData(ArrayView2D<Vec3> canvas, ArrayView<Particle> particles, int width, int height)
        {
            this.canvas = canvas;
            this.particles = particles;
            this.width = width;
            this.height = height;
            gc = 0.0001f;
        }

        public void setColor(Index2 index, Vec3 c)
        {
            if ((index.X >= 0) && (index.X < canvas.Width) && (index.Y >= 0) && (index.Y < canvas.Height))
            {
                canvas[index] = c;
            }
        }

        public static void CanvasToBitmap(Index2 index, DeviceData c, ArrayView<byte> bitmap)
        {
            Vec3 color = c.canvas[index];

            int bitmapIndex = ((index.Y * c.width) + index.X) * 3;

            bitmap[bitmapIndex] = (byte)(255.99f * color.x);
            bitmap[bitmapIndex + 1] = (byte)(255.99f * color.y);
            bitmap[bitmapIndex + 2] = (byte)(255.99f * color.z);

            c.canvas[index] = new Vec3(0, 0, 0);
        }
    }
}



