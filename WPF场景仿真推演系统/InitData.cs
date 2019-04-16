using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF场景仿真推演系统
{
    public class InitData
    {
        public int mTimeSpan;
        public InitData(int timeSpan)
        {
            mTimeSpan = timeSpan;
        }
        public List<string> Serialize()
        {
            List<string> ans = new List<string>();
            ans.Add($"TimeSpan {mTimeSpan}");
            return ans;
        }

        public void Deserialize(List<string> list)
        {
            if (list == null) return;
            foreach(string s in list)
            {
                string[] temp = s.Split(new char[] { ' ' });
                if(temp.Length>=2)
                {
                    if (temp[0] == "TimeSpan")
                    {
                        mTimeSpan = int.Parse(temp[1]);
                    }
                }
            }
        }
    }
}
