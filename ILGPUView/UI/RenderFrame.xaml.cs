using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ILGPUView.UI
{
    /// <summary>
    /// Interaction logic for OutputFrame.xaml
    /// </summary>
    public partial class RenderFrame : UserControl
    {
        public double scale = 1;

        public int width;
        public int height;
        public int scaledWidth;
        public int scaledHeight;
        public WriteableBitmap wBitmap;
        public Int32Rect rect;

        public byte[] framebuffer;

        public Action<int, int> onResolutionChanged;
        public double frameTime;
        public double frameRate;

        public RenderFrame()
        {
            InitializeComponent();

            SizeChanged += RenderFrame_SizeChanged;
        }

        private void RenderFrame_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            width = (int)e.NewSize.Width;
            height = (int)e.NewSize.Height;
            UpdateResolution();
        }

        public void UpdateResolution()
        {
            if (scale > 0)
            {
                scaledHeight = (int)(height * scale);
                scaledWidth = (int)(width * scale);
            }
            else
            {
                scaledHeight = (int)(height / -scale);
                scaledWidth = (int)(width / -scale);
            }

            scaledWidth += ((scaledWidth * 3) % 4);

            wBitmap = new WriteableBitmap(scaledWidth, scaledHeight, 96, 96, PixelFormats.Rgb24, null);
            Frame.Source = wBitmap;
            rect = new Int32Rect(0, 0, scaledWidth, scaledHeight);
            framebuffer = new byte[scaledWidth * scaledHeight * 3];
            onResolutionChanged(scaledWidth, scaledHeight);
        }
        public void update(ref byte[] data)
        {
            if (data.Length == wBitmap.PixelWidth * wBitmap.PixelHeight * 3)
            {
                wBitmap.Lock();
                IntPtr pBackBuffer = wBitmap.BackBuffer;
                Marshal.Copy(data, 0, pBackBuffer, data.Length);
                wBitmap.AddDirtyRect(rect);
                wBitmap.Unlock();
            }
        }
    }
}
