using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Devices;
using System.Windows.Threading;
using fft;
using Microsoft.Phone.Shell;

namespace Heartbeat
{
    public partial class MainPage : PhoneApplicationPage
    {
        DispatcherTimer timer;
        DispatcherTimer drawtimer;
        Processor proc = new Processor();
        // Конструктор
        public MainPage()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1);
            timer.Tick += new EventHandler(timer_Tick);
            drawtimer = new DispatcherTimer();
            drawtimer.Interval = TimeSpan.FromSeconds(1);
            drawtimer.Tick += new EventHandler(drawtimer_Tick);
            proc.canvaz = canvas2;
        }

        void drawtimer_Tick(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { updateCanvas(); }));
        }
        byte[] ARGBPx;

        long tickcounter = 0;

        float calc_y()
        {
            cam.GetPreviewBufferY(ARGBPx);
            int maxY = (int)(cam.PreviewResolution.Height * cam.PreviewResolution.Height);
            long sy = 0;
            int cn = 0;
            for (int x = 0; x < maxY; x += 4)
            {
                sy += ARGBPx[x];                
                cn++;
            }
            return ((float)sy) / ((float)cn);
        }

        float calc_cr()
        {
            cam.GetPreviewBufferYCbCr(ARGBPx);
            int cn = 0;
            int cro = ybrlayout.CrOffset;
            int cbo = ybrlayout.CbOffset;
            int mx = (int)cam.PreviewResolution.Width / 2;
            int my = (int)cam.PreviewResolution.Height / 2;
            long sum = 0;
            for (int yi = 0; yi < my; ++yi)
            {
                cro = ybrlayout.CrOffset + ybrlayout.CrPitch * yi;
                for (int xi = 0; xi < mx; ++xi)
                {
                    int cb, cr;
                    byte y;
                    //int cro = ybrlayout.CrOffset + yi * layout.CrPitch + (xFramePos / 2) * layout.CrXPitch;
                    //GetYCbCrFromPixel(ybrlayout, ARGBPx, xi, yi, out y, out cr, out cb);
                    sum += ARGBPx[cro];
                    cro += ybrlayout.CrXPitch;
                    ++cn;
                }
            }
            return ((float)sum) / ((float)cn);
        }

        float calc_cb()
        {
            cam.GetPreviewBufferYCbCr(ARGBPx);
            int cn = 0;
            int cro = ybrlayout.CrOffset;
            int cbo = ybrlayout.CbOffset;
            int mx = (int)cam.PreviewResolution.Width / 2;
            int my = (int)cam.PreviewResolution.Height / 2;
            long sum = 0;
            for (int yi = 0; yi < my; ++yi)
            {
                cbo = ybrlayout.CbOffset + ybrlayout.CbPitch * yi;
                for (int xi = 0; xi < mx; ++xi)
                {
                    int cb, cr;
                    byte y;
                    //int cro = ybrlayout.CrOffset + yi * layout.CrPitch + (xFramePos / 2) * layout.CrXPitch;
                    //GetYCbCrFromPixel(ybrlayout, ARGBPx, xi, yi, out y, out cr, out cb);
                    sum += ARGBPx[cbo];
                    cbo += ybrlayout.CbXPitch;
                    ++cn;
                }
            }
            return ((float)sum) / ((float)cn);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            try
            {
                proc.ntick = DateTime.Now.Ticks;
                float sum = calc_cr();
                proc.add(sum);
                tickcounter++;
                if (tickcounter % 1 == 0)
                {
                    Dispatcher.BeginInvoke(new Action(() => { updateBeat(sum); }));
                }
            }
            catch (Exception ex)
            {
                //ignore here
                timer.Stop();
            }
        }
        double lastval = -1;
        double ds = 0;
        void updateBeat(double val)
        {
            if (lastval>-1)
            {
                double df = val-lastval;
                if (df>0)
                {
                    //inflate
                    ds+=10;
                } else
                    ds-=5;
            }
            lastval = val;
            if (ds>50)
                ds = 50;
            if (ds<0)
                ds = 0;
            ScaleTransform st = new ScaleTransform();
            st.CenterX = 0.5;
            st.CenterY = 0.5;
            st.ScaleX = (50+ds)/50;
            st.ScaleY = (50+ds)/50;
            ellipse1.RenderTransform = st;
        }
        static double[] phann = null;
        static void hann(double[] src)
        {
            if (phann==null)
            {
                phann = new double[src.Length];
                for(int i=0;i<src.Length;++i)
                {
                    phann[i] = 0.5*(1-Math.Cos(2*Math.PI*i/(src.Length-1)));
                }
            }
            for (int i = 0; i < src.Length; ++i)
            {
                src[i] *= phann[i];
            }
        }
        void updateCanvas()
        {
            textBlock2.Text = proc.calc().ToString("0");
            if (proc.quality)
            {
                textBlock2.Foreground = new SolidColorBrush(Colors.White);
            } else
                textBlock2.Foreground = new SolidColorBrush(Color.FromArgb(255,132,33,53));
        }

        PhotoCamera cam;

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PhotoCamera.IsCameraTypeSupported(CameraType.Primary) == true)
            {
                cam = new PhotoCamera(CameraType.Primary);
                viewfinderBrush.SetSource(cam);
                cam.Initialized += new EventHandler<CameraOperationCompletedEventArgs>(cam_Initialized);
            }
        }

        protected override void OnNavigatingFrom
          (System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            timer.Stop();
            drawtimer.Stop();
            if (cam != null)
            {
                if (cam.IsFlashModeSupported(FlashMode.Off))
                {
                    cam.FlashMode = FlashMode.Off;
                }
                cam.Dispose();
            }
        }


        void cam_Initialized(object sender, CameraOperationCompletedEventArgs e)
        {
            if (cam.IsFlashModeSupported(FlashMode.On))
            {
                cam.FlashMode = FlashMode.On;
            }
            Dispatcher.BeginInvoke(new Action(() =>
            {
                //ARGBPx = new byte[(int)cam.PreviewResolution.Width * (int)cam.PreviewResolution.Height];
                ARGBPx = new byte[cam.YCbCrPixelLayout.RequiredBufferSize];
                ybrlayout = cam.YCbCrPixelLayout;
                timer.Start();
                drawtimer.Start();
            }));
        }

        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Instructions.xaml", UriKind.Relative));
        }

        private void ApplicationBarMenuItem_Click_1(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));

        }

        YCbCrPixelLayout ybrlayout;

        private void GetYCbCrFromPixel(YCbCrPixelLayout layout, byte[] currentPreviewBuffer, int xFramePos, int yFramePos, out byte y, out int cr, out int cb)
        {
            // Find the bytes corresponding to the pixel location in the frame.
            int yBufferIndex = layout.YOffset + yFramePos * layout.YPitch + xFramePos * layout.YXPitch;
            int crBufferIndex = layout.CrOffset + (yFramePos / 2) * layout.CrPitch + (xFramePos / 2) * layout.CrXPitch;
            int cbBufferIndex = layout.CbOffset + (yFramePos / 2) * layout.CbPitch + (xFramePos / 2) * layout.CbXPitch;

            // The luminance value is always positive.
            y = currentPreviewBuffer[yBufferIndex];

            // The preview buffer contains an unsigned value between 255 and 0.
            // The buffer value is cast from a byte to an integer.
            cr = currentPreviewBuffer[crBufferIndex];

            // Convert to a signed value between 127 and -128.
            cr -= 128;

            // The preview buffer contains an unsigned value between 255 and 0.
            // The buffer value is cast from a byte to an integer.
            cb = currentPreviewBuffer[cbBufferIndex];

            // Convert to a signed value between 127 and -128.
            cb -= 128;
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            BuildAppBar();
        }

        private void BuildAppBar()
        {
            ApplicationBar = new ApplicationBar();
            ApplicationBarMenuItem instr = new ApplicationBarMenuItem(AppResources.strInstruction);
            instr.Click += ApplicationBarMenuItem_Click;
            ApplicationBarMenuItem sett = new ApplicationBarMenuItem(AppResources.strSettings);
            sett.Click += ApplicationBarMenuItem_Click_1;
            ApplicationBar.MenuItems.Add(instr);
            ApplicationBar.MenuItems.Add(sett);
        }


    }
}