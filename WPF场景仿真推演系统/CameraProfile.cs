using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF场景仿真推演系统
{
    class CameraProfile : UnitProfile
    {
        public CameraProfile(int id, int type, MainWindow mw) : base(id, type, mw)
        {
            canRotate = true;
        }
        public override ObservableCollection<ParamsData> GetParamsAtTime(int time)
        {
            ObservableCollection<ParamsData> oc=base.GetParamsAtTime(time);
            oc.Add(new ParamsData("Roll", mTargets[mTimeDict[time]].RotX));
            oc.Add(new ParamsData("Yaw", mTargets[mTimeDict[time]].RotY));
            oc.Add(new ParamsData("Pitch", mTargets[mTimeDict[time]].RotZ));
            return oc;
        }
    }
}
