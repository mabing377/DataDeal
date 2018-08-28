using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP
{
    public class ListPageData<T>
    {
        /// <summary>
        /// 数据列表
        /// </summary>
        public List<T> DataList { get; set; }

        /// <summary>
        /// 对象数组
        /// </summary>
        public T[] DataArray { get; set; }

        /// <summary>
        /// 数据总数
        /// </summary>
        public int DataCount { get; set; }

        /// <summary>
        /// 每页数据数量
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 创建一个分页数据集
        /// </summary>
        /// <param name="datas">数据</param>
        /// <param name="dataCount">数据统计</param>
        /// <param name="pageSize">页大小</param>
        /// <returns></returns>
        public static ListPageData<T> CreateData(List<T> datas, int dataCount, int pageSize)
        {
            ListPageData<T> theLPD = new ListPageData<T>();
            theLPD.DataList = datas;
            theLPD.DataCount = dataCount;
            theLPD.PageSize = pageSize;
            return theLPD;
        }

    }
}
