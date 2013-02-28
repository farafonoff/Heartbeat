using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fft
{
    class TimeBuffer<TElement>
    {
        public struct Measurement
        {
            public TElement value;
            public long tick;
        }
        const int SIZE = 8192;
        public Measurement[] _vals = new Measurement[SIZE];
        int addpos = 0;
        int getpos = 0;
        public void add(TElement t,long tick)
        {
            _vals[addpos].tick = tick;
            _vals[addpos].value = t;
            lasttick = tick;
            addpos = (addpos + 1) % SIZE;
        }

        int endpos;
        public void setZero(long tick)
        {
            int i=getpos;
            endpos = addpos-1;
            do
            {
                if (_vals[i].tick > tick)
                {
                    getpos = i;
                    break;
                }
                i=(i+1) % SIZE;
            } while (i != getpos);
        }
        public long lasttick;
        public int Length
        {
            get
            {
                if (getpos < addpos)
                {
                    return addpos - getpos;
                }
                else
                    return (SIZE - getpos) + addpos;
            }
        }
        public double getSecondsWindow()
        {
            return TimeSpan.FromTicks(_vals[endpos].tick - _vals[getpos].tick).TotalSeconds;
        }
        public float relativePosition(int idx)
        {
            return ((float)(get(idx).tick - _vals[getpos].tick)) / ((float)(_vals[endpos].tick - _vals[getpos].tick));
        }
        public Measurement get(int idx)
        {
            int i = (getpos + idx) % SIZE;
            return _vals[i];
        }
    }
}
