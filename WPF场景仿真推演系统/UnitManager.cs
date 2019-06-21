using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF场景仿真推演系统
{
    public class UnitManager
    {
        private List<UnitProfile> units;
        private List<bool> unitAvability;
        public ObservableCollection<UnitData> unitsDisplayList;
        
        public MainWindow mWindow;
        public UnitProfile selUnit;
        public CamManager mCamMan;
        public UnitManager(MainWindow mw)
        {
            selUnit = null;
            mWindow = mw;
            mCamMan = new CamManager(mw);
            units = new List<UnitProfile>();
            unitAvability = new List<bool>();
            unitsDisplayList = new ObservableCollection<UnitData>();
        }
        public int AddUnit(int type,string initPosX,string initPosY,string initPosZ,int team)
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
                UnitProfile tup = SpawnUnit(ptr,type,team);
                tup.AddTarget(initPosX, initPosY, initPosZ, 0);
                units.Add(tup);
                unitAvability.Add(true);
            }
            else
            {
                UnitProfile tup = SpawnUnit(ptr,type,team);
                tup.AddTarget(initPosX, initPosY, initPosZ, 0);
                units[ptr]=tup;
                unitAvability[ptr] = true;
            }
            
            
            return ptr;
        }
        private UnitProfile SpawnUnit(int ID,int type,int team)
        {
            UnitProfile tup = null;
            if (type == 0||type==2||type==3||type==4)
                tup = new UnitProfile(ID, type,team, mWindow);
            else if (type == 1)
                tup = new CameraProfile(ID, type,team, mWindow);
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
                                int uid = AddUnit(0, msg[3], msg[4], msg[5],int.Parse(msg[6]));
                                UpdateDisplayList();
                                UpdateCamList();
                                break;
                            case "Camera":
                                Console.WriteLine("PArsing Cam");
                                uid = AddUnit(1, msg[3], msg[4], msg[5], int.Parse(msg[6]));
                                UpdateDisplayList();
                                UpdateCamList();
                                break;
                            case "BB":
                                Console.WriteLine("PArsing");
                                uid = AddUnit(2, msg[3], msg[4], msg[5], int.Parse(msg[6]));
                                UpdateDisplayList();
                                UpdateCamList();
                                break;
                            case "CV":
                                Console.WriteLine("PArsing");
                                uid = AddUnit(3, msg[3], msg[4], msg[5], int.Parse(msg[6]));
                                UpdateDisplayList();
                                UpdateCamList();
                                break;
                            case "Shell":
                                Console.WriteLine("PArsing");
                                uid = AddUnit(4, msg[3], msg[4], msg[5], int.Parse(msg[6]));
                                UpdateDisplayList();
                                UpdateCamList();
                                break;
                            case "Torpedo":
                                Console.WriteLine("PArsing");
                                uid = AddUnit(5, msg[3], msg[4], msg[5], int.Parse(msg[6]));
                                UpdateDisplayList();
                                UpdateCamList();
                                break;
                            default:
                                break;
                        }
                        break;
                    case "Fire":
                        int parentid = int.Parse(msg[2]);
                        Position p = GetUnit(parentid).Move((int)mWindow.mClock.CurrentTime);
                        int nuid = AddUnit(int.Parse(msg[1]),p.X , p.Y, p.Z, GetUnit(parentid).mTeam);
                        mWindow.WpfServer.SendMessage($"Spawn Shell {GetUnit(parentid).mTeam} {p.X} {p.Y} {p.Z} {p.T}");
                        int endtime = (int)(mWindow.mClock.CurrentTime +
                            (float)Math.Sqrt((float.Parse(msg[3]) - float.Parse(p.X))* (float.Parse(msg[3]) - float.Parse(p.X))+
                            (float.Parse(msg[4]) - float.Parse(p.Y)) * (float.Parse(msg[4]) - float.Parse(p.Y))+
                            (float.Parse(msg[5]) - float.Parse(p.Z)) * (float.Parse(msg[5]) - float.Parse(p.Z)))/GetUnit(parentid).mSpeed);
                        GetUnit(nuid).AddTarget(msg[3], msg[4], msg[5],endtime);
                        mWindow.WpfServer.SendMessage($"Add {nuid} {p.X} {p.Y} {p.Z} {endtime} {(int)GetUnit(nuid).mType} {GetUnit(nuid).mName}");
                        UpdateDisplayList();
                        UpdateCamList();
                                
                        
                        break;
                    case "Select":

                        selUnit = GetUnit(int.Parse(msg[1]));
                        UpdateKeyframeList();
                        UpdateParamList();
                        mWindow.LeftTabCtrl.SelectedIndex = 2;
                        break;
                    case "Modify":

                        UnitProfile tselUnit = GetUnit(int.Parse(msg[1]));

                        Position tp = new Position();

                        tp.X = msg[2];
                        tp.Y = msg[3];
                        tp.Z = msg[4];
                        tp.T = int.Parse(msg[5]);
                        if (tselUnit.canRotate)
                        {
                            tp.RotX = msg[8];
                            tp.RotY = msg[9];
                            tp.RotZ = msg[10];
                        }
                        tselUnit.ModifyTarget(tp);
                        UpdateKeyframeList();
                        UpdateParamList();
                        break;
                    case "Add":

                        tselUnit = GetUnit(int.Parse(msg[1]));
                        Position tp2 = new Position();
                        tp2.X = msg[2];
                        tp2.Y = msg[3];
                        tp2.Z = msg[4];
                        tp2.T = int.Parse(msg[5]);
                        if (tselUnit.canRotate)
                        {
                            tp2.RotX = msg[8];
                            tp2.RotY = msg[9];
                            tp2.RotZ = msg[10];
                            tselUnit.AddTarget(tp2.X, tp2.Y, tp2.Z, tp2.T,tp2.RotX,tp2.RotY,tp2.RotZ);
                        }
                        else { tselUnit.AddTarget(tp2.X, tp2.Y, tp2.Z, tp2.T); }
                        
                        UpdateKeyframeList();
                        UpdateParamList();
                        break;
                    case "Disselect":

                        mWindow.ParamsDataGrid.DataContext = null;
                        mWindow.KeyDataGrid.DataContext = null;
                        break;
                    case "SyncTime":
                        mWindow.Timeshift((int)float.Parse(msg[1]));
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
            ToggleFunctionBtns();
        }
        public void UpdateKeyframeList()
        {
            if (selUnit != null)
            {
                mWindow.KeyDataGrid.DataContext = selUnit.GetKeyframes();
            }
            
        }
        public void ToggleFunctionBtns()
        {
            if (selUnit != null)
            {
                if(selUnit.canFire)
                    mWindow.LaunchShellBtn.Visibility = System.Windows.Visibility.Visible;
                else
                    mWindow.LaunchShellBtn.Visibility = System.Windows.Visibility.Collapsed;
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
        public void UpdateCamList()
        {
            camList.Clear();
            foreach (UnitProfile u in units)
            {
                if(u is CameraProfile)
                {
                    camList.Add(u.mID.ToString());
                }
            }
            
        }
        public static List<string> camList = new List<string>();
        

        public List<string> Serialize()
        {
            List<string> ans = new List<string>();
            foreach(UnitProfile up in units)
            {
                ans.Add($"{up.mID} {up.mName} {(int)up.mType} {up.mTeam} {up.mTargets.Count}");
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
                    AddUnit(int.Parse(temp[2]), "0", "0", "0",int.Parse(temp[3]));
                    mWindow.WpfServer.SendMessage($"Spawn {temp[2]} {temp[3]} {temp[0]}");
                    int tcount = int.Parse(temp[4]);
                    units[units.Count - 1].mName = temp[1];
                    units[units.Count - 1].ClearTargets();
                    mWindow.WpfServer.SendMessage($"Modify {units[units.Count - 1].mID} {0} {0} {0} {0} {temp[2]} {temp[1]}");
                    for (int j=0;j<tcount;j++)
                    {
                        i++;
                        temp = list[i].Split(new char[] { ' ' });
                        
                        units[units.Count - 1].AddTarget(temp[1], temp[2], temp[3], int.Parse(temp[0]), temp[4], temp[5], temp[6]);
                        if (units[units.Count - 1].canRotate)
                        {
                            mWindow.WpfServer.SendMessage($"AddKey {units[units.Count - 1].mID} {temp[0]} {temp[1]} {temp[2]} {temp[3]} {temp[4]} {temp[5]} {temp[6]}");
                        }
                        else
                        {
                            mWindow.WpfServer.SendMessage($"AddKey {units[units.Count - 1].mID} {temp[0]} {temp[1]} {temp[2]} {temp[3]}");
                        }
                        i++;
                        units[units.Count - 1].SetKeyframeMemo(list[i],int.Parse(temp[0]));
                    }
                }
            }
        }
    }
}
