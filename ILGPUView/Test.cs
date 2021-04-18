using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using System;

namespace ILGPUViewTest
{
    public static class Test
    {
        static Action<Index2, DeviceData, ArrayView<byte>> frameBufferToBitmap;
        static Action<Index1, DeviceData> particleProcessingKernel;

        static int particleCount = 50_000;
        static DeviceData c;
        static MemoryBuffer2D<Vec3> canvasData;
        static MemoryBuffer<byte> bitmapData;

        //static HostParticleSystem h_particleSystem;
        static HostParticleSystemStructOfArrays h_particleSystem;

        public static void particleKernel(Index1 index, DeviceData c)
        {
            Vec3 pos = c.particles.update(index);
            Index2 position = new Index2((int)pos.x, (int)pos.y);
            c.setColor(position, new Vec3(1, 1, 1));
        }

        public struct DeviceData
        {
            public ArrayView2D<Vec3> canvas;
            public int width;
            public int height;
            //public ParticleSystem particles;
            public ParticleSystemStructOfArrays particles;

            public DeviceData(ArrayView2D<Vec3> canvas, ParticleSystemStructOfArrays particles, int width, int height)
            {
                this.canvas = canvas;
                this.width = width;
                this.height = height;
                this.particles = particles;
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


        public class HostParticleSystemStructOfArrays : IDisposable
        {
            public int particleCount;
            public MemoryBuffer<Vec3> positions;
            public MemoryBuffer<Vec3> velocities;
            public MemoryBuffer<Vec3> accelerations;
            public ParticleSystemStructOfArrays deviceParticleSystem;

            public HostParticleSystemStructOfArrays(int particleCount, Accelerator device, int width, int height)
            {
                this.particleCount = particleCount;
                Vec3[] poses = new Vec3[particleCount];
                Random rng = new Random();
                for (int i = 0; i < particleCount; i++)
                {
                    poses[i] = new Vec3((float)rng.NextDouble() * width, (float)rng.NextDouble() * height, 1);
                }

                positions = device.Allocate(poses);
                velocities = device.Allocate<Vec3>(particleCount);
                accelerations = device.Allocate<Vec3>(particleCount);

                velocities.MemSetToZero();
                accelerations.MemSetToZero();

                deviceParticleSystem = new ParticleSystemStructOfArrays(positions, velocities, accelerations, width, height);
            }

            public void Dispose()
            {
                positions.Dispose();
                velocities.Dispose();
                accelerations.Dispose();
            }
        }

        //Struct of Arrays Method
        public struct ParticleSystemStructOfArrays
        {
            public ArrayView<Vec3> positions;
            public ArrayView<Vec3> velocities;
            public ArrayView<Vec3> accelerations;
            public float gc;
            public Vec3 centerPos;
            public float centerMass;

            public ParticleSystemStructOfArrays(ArrayView<Vec3> positions, ArrayView<Vec3> velocities, ArrayView<Vec3> accelerations, int width, int height)
            {
                this.positions = positions;
                this.velocities = velocities;
                this.accelerations = accelerations;
                gc = 0.001f;
                centerPos = new Vec3(0.5f * width, 0.5f * height, 0);
                centerMass = (float)positions.Length;
            }

            private void updateAcceleration(int ID)
            {
                accelerations[ID] = new Vec3();

                for (int i = 0; i < positions.Length; i++)
                {
                    Vec3 otherPos;
                    float mass;

                    if (i == ID)
                    {
                        //creates a mass at the center of the screen
                        otherPos = centerPos;
                        mass = centerMass;
                    }
                    else
                    {
                        otherPos = positions[i];
                        mass = 1f;
                    }

                    float deltaPosLength = (positions[ID] - otherPos).length();
                    float temp = (gc * mass) / XMath.Pow(deltaPosLength, 3f);
                    accelerations[ID] += (otherPos - positions[ID]) * temp;
                }
            }

            private void updatePosition(int ID)
            {
                positions[ID] = positions[ID] + velocities[ID] + accelerations[ID] * 0.5f;
            }

            private void updateVelocity(int ID)
            {
                velocities[ID] = velocities[ID] + accelerations[ID];
            }

            public Vec3 update(int ID)
            {
                updateAcceleration(ID);
                updatePosition(ID);
                updateVelocity(ID);
                return positions[ID];
            }
        }

        //Array of structs method
        public class HostParticleSystem : IDisposable
        {
            public int particleCount;
            public MemoryBuffer<Particle> particleData;
            public ParticleSystem deviceParticleSystem;

            public HostParticleSystem(int particleCount, Accelerator device, int width, int height)
            {
                this.particleCount = particleCount;
                Particle[] particles = new Particle[particleCount];
                Random rng = new Random();
                for (int i = 0; i < particleCount; i++)
                {
                    Vec3 pos = new Vec3((float)rng.NextDouble() * width, (float)rng.NextDouble() * height, 1);
                    particles[i] = new Particle(pos);
                }

                particleData = device.Allocate(particles);
                deviceParticleSystem = new ParticleSystem(particleData, width, height);
            }

            public void Dispose()
            {
                particleData.Dispose();
            }
        }

        public struct ParticleSystem
        {
            public ArrayView<Particle> particles;
            public float gc;
            public Vec3 centerPos;
            public float centerMass;

            public ParticleSystem(ArrayView<Particle> particles, int width, int height)
            {
                this.particles = particles;
                gc = 0.001f;
                centerPos = new Vec3(0.5f * width, 0.5f * height, 0);
                centerMass = (float)particles.Length;
            }

            public Vec3 update(int ID)
            {
                particles[ID].update(this, ID);
                return particles[ID].position;
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

            private void updateAcceleration(ParticleSystem d, int ID)
            {
                acceleration = new Vec3();

                for (int i = 0; i < d.particles.Length; i++)
                {
                    Vec3 otherPos;
                    float mass;

                    if (i == ID)
                    {
                        //creates a mass at the center of the screen
                        otherPos = d.centerPos;
                        mass = d.centerMass;
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

            private void updatePosition()
            {
                position = position + velocity + acceleration * 0.5f;
            }

            private void updateVelocity()
            {
                velocity = velocity + acceleration;
            }

            public void update(ParticleSystem particles, int ID)
            {
                updateAcceleration(particles, ID);
                updatePosition();
                updateVelocity();
            }
        }

        // DO NOT CHANGE FUNCTION PARAMETERS
        // width and height are the output bitmap size
        // the code will be unloaded on resize
        // setup is always called once before loop
        public static void setup(Accelerator accelerator, int width, int height)
        {
            canvasData = accelerator.Allocate<Vec3>(width, height);

            //h_particleSystem = new HostParticleSystem(particleCount, accelerator, width, height);
            h_particleSystem = new HostParticleSystemStructOfArrays(particleCount, accelerator, width, height);

            c = new DeviceData(canvasData, h_particleSystem.deviceParticleSystem, width, height);

            bitmapData = accelerator.Allocate<byte>(width * height * 3);

            frameBufferToBitmap = accelerator.LoadAutoGroupedStreamKernel<Index2, DeviceData, ArrayView<byte>>(DeviceData.CanvasToBitmap);
            particleProcessingKernel = accelerator.LoadAutoGroupedStreamKernel<Index1, DeviceData>(particleKernel);
        }

        // DO NOT CHANGE FUNCTION PARAMETERS
        // bitmap is a 24bpp RGB bitmap of size width * height * 3
        // loop is called until it returns false
        public static bool loop(Accelerator accelerator, ref byte[] bitmap)
        {
            particleProcessingKernel(particleCount, c);
            frameBufferToBitmap(canvasData.Extent, c, bitmapData);

            accelerator.Synchronize();

            bitmapData.CopyTo(bitmap, 0, 0, bitmap.Length);

            return true;
        }

        public static void dispose()
        {
            h_particleSystem.Dispose();
            canvasData.Dispose();
            bitmapData.Dispose();
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
}




