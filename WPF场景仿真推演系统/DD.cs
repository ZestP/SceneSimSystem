using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF场景仿真推演系统
{
    class DD
    {
        private List<Position> positions;
        private Dictionary<int, int> timeIndex;
        public DD(Position initPos)
        {
            positions = new List<Position>();
            timeIndex = new Dictionary<int, int>();
            AddPos(initPos);

        }
        public void AddPos(Position npos)
        {
            if(ValidTime(npos))
            {
                positions.Add(npos);
                timeIndex.Add(npos.T, positions.Count - 1);
            }
        }
        public bool ValidTime(Position npos)
        {
            return !timeIndex.ContainsKey(npos.T);
        }
        public bool ValidTime(int currentTime)
        {
            return !timeIndex.ContainsKey(currentTime);
        }
        public ObservableCollection<ParamsData> GetParamsList(int currentTime)
        {
            if (!ValidTime(currentTime)) return null;
            ObservableCollection<ParamsData> ans = new ObservableCollection<ParamsData>();
            ans.Add(new ParamsData("PosX", positions[timeIndex[currentTime]].X));
            ans.Add(new ParamsData("PosY", positions[timeIndex[currentTime]].Y));
            ans.Add(new ParamsData("PosZ", positions[timeIndex[currentTime]].Z));
            return ans;
        }
    }
}
