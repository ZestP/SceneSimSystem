using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF场景仿真推演系统
{

        class Quaternion
        {
            public double t;
            public double x;
            public double y;
            public double z;
            public double module;//模 
                                 /// <summary> 
                                 /// 构造函数 
                                 /// </summary> 
                                 /// <param name="t"></param> 
                                 /// <param name="x"></param> 
                                 /// <param name="y"></param> 
                                 /// <param name="z"></param> 
            public Quaternion(double t, double x, double y, double z)
            {
                this.t = t;
                this.x = x;
                this.y = y;
                this.z = z;
                this.module = Math.Sqrt(t * t + x * x + y * y + z * z);
            }
            /// <summary> 
            /// 四元数单位化 
            /// </summary> 
            /// 

            public void Normalize()
            {
                this.t = this.t / this.module;
                this.x = this.x / this.module;
                this.y = this.y / this.module;
                this.z = this.z / this.module;
            }
            /// <summary> 
            /// 乘法 
            /// </summary> 
            /// <param name="left"></param> 
            /// <param name="right"></param> 
            /// <returns></returns> 
            public static Quaternion Multiply(Quaternion left, Quaternion right)
            {
                double d1, d2, d3, d4;

                d1 = left.t * right.t;
                d2 = -left.x * right.x;
                d3 = -left.y * right.y;
                d4 = -left.z * right.z;
                double t = d1 + d2 + d3 + d4;

                d1 = left.t * right.x;
                d2 = right.t * left.x;
                d3 = left.y * right.z;
                d4 = -left.z * right.y;
                double x = d1 + d2 + d3 + d4;

                d1 = left.t * right.y;
                d2 = right.t * left.y;
                d3 = left.z * right.x;
                d4 = -left.x * right.z;
                double y = d1 + d2 + d3 + d4;

                d1 = left.t * right.z;
                d2 = right.t * left.z;
                d3 = left.x * right.y;
                d4 = -left.y * right.x;
                double z = d1 + d2 + d3 + d4;

                return new Quaternion(t, x, y, z);
            }
            /// <summary> 
            /// 四元数求逆 
            /// </summary> 
            /// <param name="q"></param> 
            /// <returns></returns> 
            public static Quaternion Inverse(Quaternion q)
            {
                Quaternion q1 = new Quaternion(0, 0, 0, 0);
                double m = q.t * q.t + q.x * q.x + q.y * q.y + q.z * q.z;
                if (m == 0)
                    return q1;
                else
                {
                    q1.t = q.t / m;
                    q1.x = -q.x / m;
                    q1.y = -q.y / m;
                    q1.z = -q.z / m;
                    return q1;
                }
            }
            /// <summary> 
            /// 利用旋转角和旋转轴构造旋转四元数 
            /// </summary> 
            /// <param name="radian"></param> 
            /// <param name="AxisX"></param> 
            /// <param name="AxisY"></param> 
            /// <param name="AxisZ"></param> 
            /// <returns></returns> 
            Quaternion MakeRotationalQuaternion(double radian, double AxisX, double AxisY, double AxisZ)
            {
                Quaternion ans = new Quaternion(0, 0, 0, 0); ;
                double norm;
                double ccc, sss;

                norm = AxisX * AxisX + AxisY * AxisY + AxisZ * AxisZ;
                if (norm <= 0.0) return ans;

                norm = 1.0 / Math.Sqrt(norm);
                AxisX *= norm;
                AxisY *= norm;
                AxisZ *= norm;

                ccc = Math.Cos(0.5 * radian);
                sss = Math.Sin(0.5 * radian);

                ans.t = ccc;
                ans.x = sss * AxisX;
                ans.y = sss * AxisY;
                ans.z = sss * AxisZ;

                return ans;
            }
            /// <summary> 
            /// 利用四元数对向量（或点）旋转返回一个四元数（0，v），v为目标向量 
            /// </summary> 
            /// <param name="x"></param> 
            /// <param name="y"></param> 
            /// <param name="z"></param> 
            /// <param name="q"></param> 
            /// <returns></returns> 
            public static Quaternion RotateByQuaternion(double x, double y, double z, Quaternion q)
            {
                Quaternion result = new Quaternion(0, x, y, z);
                if (q.module == 0)
                    return result;
                else
                {
                    q.Normalize();
                    result = Multiply(q, result);
                    result = Multiply(result, Inverse(q));
                    return result;
                }
            }
            /// <summary> 
            /// 由单位四元数求旋转矩阵 
            /// </summary> 
            /// <param name="q"></param> 
            /// <returns></returns> 
            public static double[,] QuaternionToRotation(Quaternion q)
            {
                double[,] rotate = new double[3, 3] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };
                /* 
                 * if (q.module != 1) 
                    return rotate 
                 * 这段代码存在严重问题，因为计算机数值总是存在精度范围内的误差，因此给定的单位四元数模长不可能严格等于0！ 
                 */


                rotate[0, 0] = 1.0 - 2.0 * (q.y * q.y + q.z * q.z);
                rotate[0, 1] = 2.0 * (q.x * q.y + q.t * q.z);
                rotate[0, 2] = 2.0 * (q.x * q.z - q.t * q.y);

                rotate[1, 0] = 2.0 * (q.x * q.y - q.t * q.z);
                rotate[1, 1] = 1.0 - 2.0 * (q.x * q.x + q.z * q.z);
                rotate[1, 2] = 2.0 * (q.y * q.z + q.t * q.x);

                rotate[2, 0] = 2.0 * (q.x * q.z + q.t * q.y);
                rotate[2, 1] = 2.0 * (q.y * q.z - q.t * q.x);
                rotate[2, 2] = 1.0 - 2.0 * (q.x * q.x + q.y * q.y);

                return rotate;

            }
            /// <summary> 
            /// 由旋转矩阵计算四元数 
            /// </summary> 
            /// <param name="rotate"></param> 
            /// <returns></returns> 
            public static Quaternion RotateToQuaternion(double[,] rotate)
            {
                if (rotate.GetLength(0) != 3 || rotate.GetLength(1) != 3)
                    return null;
                double t = Math.Sqrt(rotate[0, 0] + rotate[1, 1] + rotate[2, 2] + 1) / 2.0;
                double x = (rotate[1, 2] - rotate[2, 1]) / 4.0 / t;
                double y = (rotate[2, 0] - rotate[0, 2]) / 4.0 / t;
                double z = (rotate[0, 1] - rotate[1, 0]) / 4.0 / t;
                return new Quaternion(t, x, y, z);
            }


        }
    
}

