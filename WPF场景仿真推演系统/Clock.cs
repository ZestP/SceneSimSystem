using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF场景仿真推演系统
{
    public class Clock
    {
        public float CurrentTime;
        public int TimeSpan;
        public bool IsPlaying;
        // Start is called before the first frame update
        public Clock(int ts)
        {
            CurrentTime = 0;
            TimeSpan = ts;
            IsPlaying = false;
        }

    }
}
