﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace WPF场景仿真推演系统
{
    public enum UnitType
    {
        驱逐舰,摄像机,战列舰,航空母舰,炮弹,鱼雷
    }
    public class UnitProfile
    {
        public UnitType mType;
        public int mID;
        public int mTeam;
        public string mName;
        public List<Position> mTargets;
        protected Dictionary<int, int> mTimeDict;
        public Dictionary<int, string> mTimeMemo;
        public bool canRotate;
        public bool canFire;
        public MainWindow mWindow;
        public float mSpeed = 10;
        public UnitProfile(int id, int type,int team, MainWindow mw)
        {
            
            mWindow = mw;
            mType = (UnitType)type;
            mID = id;
            mTeam = team;
            mName = $"{mType}_{mID}";
            mTimeMemo = new Dictionary<int, string>();
            canRotate = false;
            canFire = true;
        }

        public void AddTarget(string x,string y,string z,int t,string rx,string ry,string rz)
        {
            Position target = new Position();
            target.X = x;
            target.Y = y;
            target.Z = z;
            target.T = t;
            target.RotX = rx;
            target.RotY = ry;
            target.RotZ = rz;
            Console.WriteLine("Add Target at" + target.X + ' ' + target.Y + ' ' + target.Z + ' ' + target.T+' '+target.RotX+' '+target.RotY+' '+target.RotZ);
            if (mTargets == null)
                mTargets = new List<Position>();

            mTargets.Add(target);
            mTargets.Sort((left, right) =>
            {
                if (left.T < right.T)
                    return -1;
                else if (left.T == right.T)
                    return 0;
                else
                    return 1;
            });
            mTimeMemo[t] = "请添加流程说明……";
            RebuildTimeDict();
        }
        public void AddTarget(string x, string y, string z, int t)
        {
            Position target = new Position();
            target.X = x;
            target.Y = y;
            target.Z = z;
            target.T = t;
            if(canRotate)
            {
                target.RotX = "0";
                target.RotY = "0";
                target.RotZ = "0";
            }
            Console.WriteLine("Add Target at" + target.X + ' ' + target.Y + ' ' + target.Z + ' ' + target.T);
            if (mTargets == null)
                mTargets = new List<Position>();

            mTargets.Add(target);
            mTargets.Sort((left, right) =>
            {
                if (left.T < right.T)
                    return -1;
                else if (left.T == right.T)
                    return 0;
                else
                    return 1;
            });
            mTimeMemo[t] = "请添加流程说明……";
            RebuildTimeDict();
        }
        public void ClearTargets()
        {
            mTargets.Clear();
            mTimeDict.Clear();
        }
        public void ModifyTarget(Position nt)
        {
            mTargets[mTimeDict[nt.T]]=nt;
            mTargets.Sort((left, right) =>
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
        
        public void TimeshiftTarget(int old,int now)
        {
            mTimeMemo[now] = mTimeMemo[old];
            mTimeMemo.Remove(old);
            mTargets[mTimeDict[old]].T = now;
            mTargets.Sort((left, right) =>
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
        public void SwapTime(int a,int b)
        {
            string tmp = mTimeMemo[b];
            mTimeMemo[b] = mTimeMemo[a];
            mTimeMemo[a] = tmp;
            mTargets[mTimeDict[a]].T = b;
            mTargets[mTimeDict[b]].T = a;
            mTargets.Sort((left, right) =>
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
            for (int i = 0; i < mTargets.Count; i++)
            {
                mTimeDict.Add(mTargets[i].T, i);
            }
        }
        public void SetKeyframeMemo(string s,int time)
        {
            mTimeMemo[time] = s;
        }
        public UnitData GetUnitData()
        {
            string typestr = mType.ToString();
            return new UnitData(mID.ToString(), mName,typestr);
        }
        public virtual ObservableCollection<ParamsData> GetParamsAtTime(int time)
        {
            if (mTimeDict.ContainsKey(time))
            {
                ObservableCollection<ParamsData> ans = new ObservableCollection<ParamsData>();
                ans.Add(new ParamsData("名称", mName.ToString()));
                ans.Add(new ParamsData("类型", mType.ToString()));
                ans.Add(new ParamsData("队伍", mTeam.ToString()));
                ans.Add(new ParamsData("X坐标", mTargets[mTimeDict[time]].X));
                ans.Add(new ParamsData("Y坐标", mTargets[mTimeDict[time]].Y));
                ans.Add(new ParamsData("Z坐标", mTargets[mTimeDict[time]].Z));
                return ans;
            }
            else
            {
                Position tp=Move(time);
                
                ObservableCollection<ParamsData> ansOb = new ObservableCollection<ParamsData>();
                ansOb.Add(new ParamsData("名称", mName.ToString()));
                ansOb.Add(new ParamsData("类型", mType.ToString()));
                ansOb.Add(new ParamsData("队伍", mTeam.ToString()));
                ansOb.Add(new ParamsData("X坐标", tp.X));
                ansOb.Add(new ParamsData("Y坐标", tp.Y));
                ansOb.Add(new ParamsData("Z坐标", tp.Z));
                return ansOb;
            }
        }

        public Position Move(int time)
        {
            Position ans = new Position();
            int currentTargetPtr = 0;
            if (mTargets.Count == 1)
            {
                ans.X = mTargets[0].X;
                ans.Y = mTargets[0].Y;
                ans.Z = mTargets[0].Z;
                if(canRotate)
                {
                    ans.RotX = mTargets[0].RotX;
                    ans.RotY = mTargets[0].RotY;
                    ans.RotZ = mTargets[0].RotZ;
                }
            }
            else if (mTargets.Count > 1)
            {
                for (currentTargetPtr = 0; currentTargetPtr < mTargets.Count - 1 && time - mTargets[currentTargetPtr + 1].T >= 0; currentTargetPtr++)
                {
                }
                if (currentTargetPtr < mTargets.Count - 1)
                {
                    float div = (time - mTargets[currentTargetPtr].T) / (float)(mTargets[currentTargetPtr + 1].T - mTargets[currentTargetPtr].T);
                    ans.X = (float.Parse(mTargets[currentTargetPtr].X) * (1 - div) + float.Parse(mTargets[currentTargetPtr + 1].X) * div).ToString();
                    ans.Y = (float.Parse(mTargets[currentTargetPtr].Y) * (1 - div) + float.Parse(mTargets[currentTargetPtr + 1].Y) * div).ToString();
                    ans.Z = (float.Parse(mTargets[currentTargetPtr].Z) * (1 - div) + float.Parse(mTargets[currentTargetPtr + 1].Z) * div).ToString();
                    if (canRotate)
                    {
                        ans.RotX = mTargets[currentTargetPtr].RotX;
                        ans.RotY = mTargets[currentTargetPtr].RotY;
                        ans.RotZ = mTargets[currentTargetPtr].RotZ;
                    }
                }
                else
                {
                    ans.X = mTargets[currentTargetPtr].X;
                    ans.Y = mTargets[currentTargetPtr].Y;
                    ans.Z = mTargets[currentTargetPtr].Z;
                    if (canRotate)
                    {
                        ans.RotX = mTargets[currentTargetPtr].RotX;
                        ans.RotY = mTargets[currentTargetPtr].RotY;
                        ans.RotZ = mTargets[currentTargetPtr].RotZ;
                    }
                }
            }
            Console.WriteLine($"{currentTargetPtr} {time}");
            return ans;
        }


        public ObservableCollection<KeyframeData> GetKeyframes()
        {
            ObservableCollection<KeyframeData> ans = new ObservableCollection<KeyframeData>();
            {
                for (int i = 0; i < mTargets.Count; i++)
                {
                    ans.Add(new KeyframeData(mTargets[i].T.ToString(), mTimeMemo[mTargets[i].T]));
                }
            }
            return ans;
        }

        
        //private void RelocateCurrentTargetPtr()
        //{
        //    int i = 0;
        //    for (i = 0; i < mTargets.Count - 1; i++)
        //    {
        //        if (mTargets[i].T <= mClock.CurrentTime && mTargets[i + 1].T > mClock.CurrentTime)
        //        {
        //            break;
        //        }
        //    }
        //    currentTargetPtr = i;
        //}
        public bool ContainsTargetAtSameTime(Position b)
        {
            if (!mTimeDict.ContainsKey(b.T))
            {
                return false;
            }
            return true;
        }
        public bool ContainsTargetAtSameTime(int b)
        {
            if (!mTimeDict.ContainsKey(b))
            {
                return false;
            }
            return true;
        }
        public List<string> Serialize()
        {
            List<string> ans = new List<string>();
            foreach (Position p in mTargets)
            {
                ans.Add($"{p.T} {p.X} {p.Y} {p.Z} {p.RotX} {p.RotY} {p.RotZ}");
                ans.Add(mTimeMemo[p.T]);
            }
            return ans;
        }
    }
}