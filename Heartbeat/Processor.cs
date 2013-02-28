using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;

namespace fft
{
    class Processor
    {
        TimeBuffer<float> tb = new TimeBuffer<float>();
        public Canvas canvaz;
        long ticks5 = TimeSpan.FromSeconds(5).Ticks;
        long ticks10 = TimeSpan.FromSeconds(10).Ticks;
        long ticks20 = TimeSpan.FromSeconds(20).Ticks;
        long ticks30 = TimeSpan.FromSeconds(30).Ticks;
        long ticks1m = TimeSpan.FromSeconds(60).Ticks;
        long ticks01 = TimeSpan.FromSeconds(0.1).Ticks;
        const int FFTSIZE = 512;
        public void add(float v)
        {
            tb.add(v, ntick);
        }
        public long ntick
        {
            get;
            set;
        }
        long lpulse = 40;
        long hpulse = 300;
        public bool quality = false;
        public double lastValidMeasurement = 0;

        public double calc()
        {
            tb.setZero(ntick - ticks10);
            int len = tb.Length;
            double[] fftsrc = new double[FFTSIZE];
            int pos = 0;
            int ppos = 0;
            for (int i = 0; i < len; ++i)
            {
                var m = tb.get(i);
                pos = (int)(tb.relativePosition(i) * ((float)FFTSIZE-1.0));
                if (pos-ppos>1)
                {
                    for(int p=ppos+1;p<pos;++p)
                    {
                        fftsrc[p] = (m.value * ((float)pos-p) + fftsrc[ppos]*((float)p - ppos))/((float)pos-ppos);
                    }
                }
                fftsrc[pos] = m.value;
                ppos = pos;
            }
            source = createPline(Colors.Blue);
            buildFloatPolyline(source,fftsrc,FFTSIZE);
            alglib.complex[] ft;
            alglib.fftr1d(fftsrc, out ft);
            for (int i = 0; i < FFTSIZE; ++i)
            {
                fftsrc[i] = Math.Sqrt(ft[i].x * ft[i].x + ft[i].y * ft[i].y);
            }
            double wnd = tb.getSecondsWindow();
            int imin = (int)((float)lpulse * wnd / 60.0);
            int ilim = (int)((float)hpulse * wnd / 60.0);
            int imax = imin;
            for (int i = 0; i < imax; ++i)
            {
                fftsrc[i] = 0;
            }
            for (int i = imax; i < ilim; ++i)
            {
                if (fftsrc[i] > fftsrc[imax])
                    imax = i;
            }
            double result = (imax / wnd)*60.0;
            if (imax > 0 && fftsrc[imax] > 1.3 * fftsrc[imax - 1] && fftsrc[imax] > 1.3 * fftsrc[imax + 1])
            {
                quality = true;
            }
            for (int i = imin; i < ilim; ++i)
            {
                double pp = (i / wnd)*60.0;
                if (fftsrc[i] > (fftsrc[imax] / 2.0) && Math.Abs(result - pp) > 15)
                {
                    quality = false;
                }
            }
            if (quality) lastValidMeasurement = result;
            plft = createPline(Colors.Green);
            buildFloatPolyline(plft, fftsrc, FFTSIZE / 2);
            if (canvaz != null)
            {
                canvaz.Children.Clear();
                canvaz.Children.Add(source);
                canvaz.Children.Add(plft);
                double candValue = 0;
                for (int i = imin; i < ilim; ++i)
                {
                    if (i>3&&fftsrc[i] > 0.3 * fftsrc[imax] && 
                        fftsrc[i] > fftsrc[i-1]&&fftsrc[i] > fftsrc[i+1]&&
                        fftsrc[i] > fftsrc[i - 2] && fftsrc[i] > fftsrc[i + 2])
                    {
                        Ellipse CandidatePoint = new Ellipse();
                        CandidatePoint.Fill = new SolidColorBrush(Colors.Red);
                        CandidatePoint.Height = 3;
                        CandidatePoint.Width = 3;
                        CandidatePoint.Margin = new System.Windows.Thickness(i * canvaz.ActualWidth / FFTSIZE * 2, fftsrc[i] / fftsrc[imax] * canvaz.ActualHeight, 0, 0);
                        canvaz.Children.Add(CandidatePoint);
                        if (!quality)
                        {
                            double cresult = (i / wnd)*60.0;
                            if (Math.Abs(cresult - lastValidMeasurement) < 10 && fftsrc[i] > candValue)
                            {
                                result = cresult;
                                candValue = fftsrc[i];
                            }
                        }
                    }
                }
            }
            return result;
        }

        void buildFloatPolyline(Polyline pl,double[] src,int len)
        {
            for (int i = 0; i < len ; ++i)
            {
                pl.Points.Add(new System.Windows.Point(i, src[i]));
            }
        }

        Polyline createPline(Color cl)
        {
            if (canvaz != null)
            {
                Polyline res = new Polyline();
                res.Stroke = new SolidColorBrush(cl);
                res.Stretch = Stretch.Fill;
                res.Width = canvaz.ActualWidth;
                res.Height = canvaz.ActualHeight;
                return res;
            }
            return null;
        }

        Polyline source;
        Polyline plft;
        Polyline top;

        public void generate()
        {
            int period = 10;
            int spart = 4;
            for (int i = 0; i < 1000; ++i)
            {
                int px = i % period;
                if (px < spart)
                {
                    double arg = 2.0*Math.PI * px / spart;
                    double val = Math.Sin(arg);
                    add((float)(val*val));
                }
                else
                {
                    add(0);
                }
                ntick += ticks01;
            }
        }
    }
}
