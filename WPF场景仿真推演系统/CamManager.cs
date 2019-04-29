using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF场景仿真推演系统
{
    public class CamManager
    {
        public MainWindow mWindow;
        public ObservableCollection<DopesheetData> dopesheetList;
        public class CamKey
        {
            public int Camid,T;
            public CamKey(int cid,int ct)
            {
                Camid = cid;
                T = ct;
                
            }
        }
        public CamManager(MainWindow mw)
        {
            mWindow = mw;
            mCamKeys = new List<CamKey>();
            dopesheetList = new ObservableCollection<DopesheetData>();
            
        }
        public List<CamKey> mCamKeys;
        protected Dictionary<int, int> mTimeDict;
        public void AddTarget(int camid, int t)
        {
            while(mTimeDict!=null&&mTimeDict.ContainsKey(t))
            {
                t++;
            }
            CamKey target = new CamKey(camid,t);
            
            
            Console.WriteLine("Add camkey at" + target.Camid + ' ' + target.T);
            if (mCamKeys == null)
                mCamKeys = new List<CamKey>();

            mCamKeys.Add(target);
            mCamKeys.Sort((left, right) =>
            {
                if (left.T < right.T)
                    return -1;
                else if (left.T == right.T)
                    return 0;
                else
                    return 1;
            });
            
            RebuildTimeDict();
        }
        public void ClearTargets()
        {
            mCamKeys.Clear();
            mTimeDict.Clear();
        }
        public void ModifyTarget(CamKey nt)
        {
            mCamKeys[mTimeDict[nt.T]] = nt;
            mCamKeys.Sort((left, right) =>
            {
                if (left.T < right.T)
                    return -1;
                else if (left.T == right.T)
                    return 0;
                else
                    return 1;
            });
            RebuildTimeDict();

        }

        public void TimeshiftTarget(int old, int now)
        {
            mCamKeys[now] = mCamKeys[old];

            mCamKeys[mTimeDict[old]].T = now;
            mCamKeys.Sort((left, right) =>
            {
                if (left.T < right.T)
                    return -1;
                else if (left.T == right.T)
                    return 0;
                else
                    return 1;
            });
            RebuildTimeDict();
        }
        public void SwapTime(int a, int b)
        {

            mCamKeys[mTimeDict[a]].T = b;
            mCamKeys[mTimeDict[b]].T = a;
            mCamKeys.Sort((left, right) =>
            {
                if (left.T < right.T)
                    return -1;
                else if (left.T == right.T)
                    return 0;
                else
                    return 1;
            });
            RebuildTimeDict();
        }
        private void RebuildTimeDict()
        {
            if (mTimeDict == null)
                mTimeDict = new Dictionary<int, int>();
            mTimeDict.Clear();
            for (int i = 0; i < mCamKeys.Count; i++)
            {
                mTimeDict.Add(mCamKeys[i].T, i);
            }
        }
        public bool ContainsTargetAtSameTime(int b)
        {
            if (!mTimeDict.ContainsKey(b))
            {
                return false;
            }
            return true;
        }
        public void UpdateDopesheet()
        {
            dopesheetList.Clear();
            foreach (CamKey ck in mCamKeys)
            {
                DopesheetData tmp = new DopesheetData();
                tmp.camid = ck.Camid.ToString();
                tmp.camname = mWindow.mUnitMan.GetUnit(ck.Camid).mName;
                tmp.time = ck.T.ToString();
                dopesheetList.Add(tmp);
            }
            mWindow.DopesheetGrid.DataContext = dopesheetList;
        }
    }
}
