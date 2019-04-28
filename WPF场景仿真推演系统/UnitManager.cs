using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF场景仿真推演系统
{
    class UnitManager
    {
        private List<UnitProfile> units;
        private List<bool> unitAvability;
        public ObservableCollection<UnitData> unitsDisplayList;
        public MainWindow mWindow;
        public UnitProfile selUnit;
        public UnitManager(MainWindow mw)
        {
            selUnit = null;
            mWindow = mw;
            units = new List<UnitProfile>();
            unitAvability = new List<bool>();
            unitsDisplayList = new ObservableCollection<UnitData>();
        }
        public int AddUnit(int type,string initPosX,string initPosY,string initPosZ)
        {
            
            int ptr = -1;
            for (int i = 0; i < unitAvability.Count; i++)
            {
                if (!unitAvability[i])
                {
                    ptr = i;
                    
                    break;
                }
            }
            Console.WriteLine($"Add unit at {initPosX} {initPosY} {initPosZ}");
            if(ptr==-1)
            {
                ptr = units.Count;
                UnitProfile tup = SpawnUnit(ptr,type);
                tup.AddTarget(initPosX, initPosY, initPosZ, 0);
                units.Add(tup);
                unitAvability.Add(true);
            }
            else
            {
                UnitProfile tup = SpawnUnit(ptr,type);
                tup.AddTarget(initPosX, initPosY, initPosZ, 0);
                units[ptr]=tup;
                unitAvability[ptr] = true;
            }
            
            
            return ptr;
        }
        private UnitProfile SpawnUnit(int ID,int type)
        {

            UnitProfile tup = new UnitProfile(ID,type,mWindow);
            return tup;
        }

        public UnitProfile GetUnit(int uid)
        {
            if (unitAvability[uid])
            {
                return units[uid];
            }
            return null;
        }
        public void RemoveUnit(int uid)
        {
            if (unitAvability[uid])
            {
                unitAvability[uid] = false;
            }
        }
        public void ParseMsg(List<string> msg)
        {
            if (msg != null)
            {
                switch (msg[0])
                {
                    case "Spawn":
                        switch (msg[1])
                        {
                            case "DD":
                                Console.WriteLine("PArsing");
                                int uid = AddUnit(0,msg[3],msg[4],msg[5]);
                                UpdateDisplayList();
                                break;
                            case "Camera":
                                Console.WriteLine("PArsing Cam");
                                uid = AddUnit(1, msg[3], msg[4], msg[5]);
                                UpdateDisplayList();
                                break;
                            default:
                                break;
                        }
                        break;
                    case "Select":
                        
                        selUnit=GetUnit(int.Parse(msg[1]));
                        UpdateKeyframeList();
                        UpdateParamList();
                        mWindow.LeftTabCtrl.SelectedIndex = 2;
                        break;
                    case "Modify":

                        selUnit = GetUnit(int.Parse(msg[1]));
                        Position tp = new Position();
                        tp.X = msg[2];
                        tp.Y = msg[3];
                        tp.Z = msg[4];
                        tp.T = int.Parse(msg[5]);
                        selUnit.ModifyTarget(tp);
                        UpdateKeyframeList();
                        UpdateParamList();
                        break;
                    case "Add":

                        selUnit = GetUnit(int.Parse(msg[1]));
                        Position tp2 = new Position();
                        tp2.X = msg[2];
                        tp2.Y = msg[3];
                        tp2.Z = msg[4];
                        tp2.T = int.Parse(msg[5]);
                        selUnit.AddTarget(tp2.X,tp2.Y,tp2.Z,tp2.T);
                        UpdateKeyframeList();
                        UpdateParamList();
                        break;
                    case "Disselect":

                        mWindow.ParamsDataGrid.DataContext = null;
                        mWindow.KeyDataGrid.DataContext = null;
                        break;
                }


            }
        }


        public void UpdateParamList()
        {
            if (selUnit != null)
            {
                mWindow.ParamsDataGrid.DataContext = selUnit.GetParamsAtTime((int)mWindow.mClock.CurrentTime);
            }
        }
        public void UpdateKeyframeList()
        {
            if (selUnit != null)
            {
                mWindow.KeyDataGrid.DataContext = selUnit.GetKeyframes();
            }
        }
        public void UpdateDisplayList()
        {
            unitsDisplayList.Clear();
            foreach(UnitProfile u in units)
            {
                unitsDisplayList.Add(u.GetUnitData());
            }
        }

        public List<string> Serialize()
        {
            List<string> ans = new List<string>();
            foreach(UnitProfile up in units)
            {
                ans.Add($"{up.mID} {up.mName} {(int)up.mType} {up.mTargets.Count}");
                ans.AddRange(up.Serialize());
            }
            return ans;
        }
        public void Deserialize(List<string> list)
        {
            if (list == null) return;
            units.Clear();
            for(int i=0;i<list.Count;i++)
            {
                string[] temp = list[i].Split(new char[] { ' ' });
                if(temp.Length>0)
                {
                    AddUnit(int.Parse(temp[2]), "0", "0", "0");
                    int tcount = int.Parse(temp[3]);
                    units[units.Count - 1].mName = temp[1];
                    units[units.Count - 1].ClearTargets();
                    for (int j=0;j<tcount;j++)
                    {
                        i++;
                        temp = list[i].Split(new char[] { ' ' });
                        units[units.Count - 1].AddTarget(temp[1],temp[2],temp[3],int.Parse(temp[0]));
                        i++;
                        units[units.Count - 1].SetKeyframeMemo(list[i],int.Parse(temp[0]));
                    }
                }
            }
        }
    }
}
