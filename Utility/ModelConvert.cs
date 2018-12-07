using StackExchange.Redis;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public class ModelConvert
    {
        public static object Enum { get; set; }

        public static HashEntry[] GetEntrys<T>(T t)
        {
            Type type = t.GetType();
            PropertyInfo[] pInfos = type.GetProperties();
            var entryArr = new HashEntry[pInfos.Length];
            for (int i = 0; i < pInfos.Length; i++)
            {
                var val = pInfos[i].GetValue(t);
                //枚举
                if (pInfos[i].PropertyType.IsEnum)
                    entryArr[i] = new HashEntry(pInfos[i].Name, (int)val);
                //int
                else if (pInfos[i].PropertyType.FullName == typeof(int).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? (int)val : 0);
                //int||null
                else if (pInfos[i].PropertyType.FullName == typeof(Nullable<int>).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? ((int?)val) : 0);
                //byte
                else if (pInfos[i].PropertyType.FullName == typeof(byte).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? (byte)val : 0);
                //byte||null
                else if (pInfos[i].PropertyType.FullName == typeof(Nullable<byte>).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? ((byte?)val) : 0);
                //string
                else if (pInfos[i].PropertyType.FullName == typeof(string).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? (string)val : string.Empty);
                //date
                else if (pInfos[i].PropertyType.FullName == typeof(DateTime).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? val.ToString() : string.Empty);
                //date||null
                else if (pInfos[i].PropertyType.FullName == typeof(Nullable<DateTime>).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? ((Nullable<DateTime>)val).ToString() : string.Empty);
                //bool
                else if (pInfos[i].PropertyType.FullName == typeof(bool).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? (bool)val : false);
                //bool||null
                else if (pInfos[i].PropertyType.FullName == typeof(Nullable<bool>).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? (bool?)val : false);
                //long
                else if (pInfos[i].PropertyType.FullName == typeof(long).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? Convert.ToInt32(val) : 0);
                //long||null
                else if (pInfos[i].PropertyType.FullName == typeof(Nullable<long>).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? (long?)val : 0);
                //decimal
                else if (pInfos[i].PropertyType.FullName == typeof(decimal).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? Convert.ToDouble(val) : 0);
                //decimal||null
                else if (pInfos[i].PropertyType.FullName == typeof(Nullable<decimal>).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? Convert.ToDouble((decimal?)val) : 0);
                //guid
                else if (pInfos[i].PropertyType.FullName == typeof(Guid).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? ((Guid)val).ToString() : string.Empty);
                //guid||null
                else if (pInfos[i].PropertyType.FullName == typeof(Nullable<Guid>).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? ((Guid?)val).ToString() : string.Empty);
                //float
                else if (pInfos[i].PropertyType.FullName == typeof(float).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? Convert.ToSingle(val) : 0);
                //float||null
                else if (pInfos[i].PropertyType.FullName == typeof(Nullable<float>).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? Convert.ToSingle(val) : 0);
                //double
                else if (pInfos[i].PropertyType.FullName == typeof(double).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? Convert.ToDouble(val) : 0);
                //double||null
                else if (pInfos[i].PropertyType.FullName == typeof(Nullable<double>).FullName)
                    entryArr[i] = new HashEntry(pInfos[i].Name, val != null ? Convert.ToDouble(val) : 0);
            }
            return entryArr;
        }
        public static T GetModel<T>(HashEntry[] entrys) where T : new()
        {
            var t = new T();
            Type type = t.GetType();
            for (int i = 0; i < entrys.Length; i++)
            {
                var pInfo = type.GetProperty(entrys[i].Name);
                if (pInfo == null)
                {
                    continue;
                }
                if (pInfo.PropertyType.IsEnum)
                    pInfo.SetValue(t, (int)entrys[i].Value);
                else if (pInfo.PropertyType.FullName == typeof(string).FullName)
                    pInfo.SetValue(t, (string)entrys[i].Value);

                else if (pInfo.PropertyType.FullName == typeof(int).FullName || pInfo.PropertyType.FullName == typeof(int?).FullName)
                {
                    if (!entrys[i].Value.IsNullOrEmpty)
                        pInfo.SetValue(t, (int)entrys[i].Value);
                }
                else if (pInfo.PropertyType.FullName == typeof(byte).FullName || pInfo.PropertyType.FullName == typeof(byte?).FullName)
                {
                    if (!entrys[i].Value.IsNullOrEmpty)
                        pInfo.SetValue(t, (byte)entrys[i].Value);
                }
                else if (pInfo.PropertyType.FullName == typeof(bool).FullName || pInfo.PropertyType.FullName == typeof(bool?).FullName)
                {
                    if (!entrys[i].Value.IsNullOrEmpty)
                        pInfo.SetValue(t, (bool)entrys[i].Value);
                }
                else if (pInfo.PropertyType.FullName == typeof(double).FullName || pInfo.PropertyType.FullName == typeof(double?).FullName)
                {
                    if (!entrys[i].Value.IsNullOrEmpty)
                        pInfo.SetValue(t, (double)entrys[i].Value);
                }
                else if (pInfo.PropertyType.FullName == typeof(decimal).FullName || pInfo.PropertyType.FullName == typeof(decimal?).FullName)
                {
                    if (!entrys[i].Value.IsNullOrEmpty)
                        pInfo.SetValue(t, (decimal)entrys[i].Value);
                }
                else if (pInfo.PropertyType.FullName == typeof(DateTime).FullName || pInfo.PropertyType.FullName == typeof(DateTime?).FullName)
                {
                    if (!entrys[i].Value.IsNullOrEmpty)
                        pInfo.SetValue(t, Convert.ToDateTime(entrys[i].Value));
                }
                else if (pInfo.PropertyType.FullName == typeof(long).FullName || pInfo.PropertyType.FullName == typeof(long?).FullName)
                {
                    if (!entrys[i].Value.IsNullOrEmpty)
                        pInfo.SetValue(t, (long)entrys[i].Value);
                }
                else if (pInfo.PropertyType.FullName == typeof(float).FullName || pInfo.PropertyType.FullName == typeof(float?).FullName)
                {
                    if (!entrys[i].Value.IsNullOrEmpty)
                        pInfo.SetValue(t, Convert.ToSingle(entrys[i].Value));
                }
                else if (pInfo.PropertyType.FullName == typeof(Guid).FullName || pInfo.PropertyType.FullName == typeof(Guid?).FullName)
                {
                    if ((!entrys[i].Value.IsNullOrEmpty) && (string)entrys[i].Value != Guid.Empty.ToString())
                        pInfo.SetValue(t, Guid.Parse((string)entrys[i].Value));
                }
            }
            return t;
        }
    }
}
