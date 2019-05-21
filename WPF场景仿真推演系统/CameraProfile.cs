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
        public CameraProfile(int id, int type,int team, MainWindow mw) : base(id, type,team, mw)
        {
            canRotate = true;
            canFire = false;
            if (mw.mUnitMan.mCamMan.mCamKeys == null)
            {
                mw.mUnitMan.mCamMan.AddTarget(id, 0);
                mw.mUnitMan.mCamMan.UpdateDopesheet();
            }
        }
        public override ObservableCollection<ParamsData> GetParamsAtTime(int time)
        {
            if (mTimeDict.ContainsKey(time))
            {
                ObservableCollection<ParamsData> ans = new ObservableCollection<ParamsData>();
                ans.Add(new ParamsData("名称", mName.ToString()));
                ans.Add(new ParamsData("类型", mType.ToString()));
                ans.Add(new ParamsData("X坐标", mTargets[mTimeDict[time]].X));
                ans.Add(new ParamsData("Y坐标", mTargets[mTimeDict[time]].Y));
                ans.Add(new ParamsData("Z坐标", mTargets[mTimeDict[time]].Z));
                ans.Add(new ParamsData("俯仰角", mTargets[mTimeDict[time]].RotX));
                ans.Add(new ParamsData("偏航角", mTargets[mTimeDict[time]].RotY));
                ans.Add(new ParamsData("翻滚角", mTargets[mTimeDict[time]].RotZ));
                return ans;
            }
            else
            {
                Position tp = Move(time);

                ObservableCollection<ParamsData> ansOb = new ObservableCollection<ParamsData>();
                ansOb.Add(new ParamsData("名称", mName.ToString()));
                ansOb.Add(new ParamsData("类型", mType.ToString()));
                ansOb.Add(new ParamsData("X坐标", tp.X));
                ansOb.Add(new ParamsData("Y坐标", tp.Y));
                ansOb.Add(new ParamsData("Z坐标", tp.Z));
                ansOb.Add(new ParamsData("俯仰角", tp.RotX));
                ansOb.Add(new ParamsData("偏航角", tp.RotY));
                ansOb.Add(new ParamsData("翻滚角", tp.RotZ));
                return ansOb;
            }
            
        }
    }
}
