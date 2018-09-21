using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Utility
{
    public static class SqlHelper
    {
        /// <summary>
        /// 创建连接的字符串
        /// </summary>
        static readonly string connStr = ConfigurationManager.ConnectionStrings["SqlConnectionString"].ConnectionString;

        #region 1.0 执行查询语句，返回一个表 + static DataTable ExcuteTable(string sql, params SqlParameter[] ps)
        /// <summary>
        /// 1.0 执行查询语句，返回一个表
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ps">参数数组</param>
        /// <returns>返回一张表</returns>
        public static DataTable ExcuteTable(string sql, params SqlParameter[] ps)
        {
            SqlDataAdapter da = new SqlDataAdapter(sql, connStr);
            da.SelectCommand.Parameters.AddRange(ps);
            DataTable dt = new DataTable();
            da.Fill(dt);
            return dt;
        }
        #endregion

        #region 2.0 执行增删改的方法 + static int ExcuteNoQuery(string sql, params SqlParameter[] ps)
        /// <summary>
        /// 2.0 执行增删改的方法
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ps">参数数组</param>
        /// <returns>返回一条记录</returns>
        public static int ExcuteNoQuery(string sql, params SqlParameter[] ps)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand command = new SqlCommand(sql, conn);
                command.Parameters.AddRange(ps);
                return command.ExecuteNonQuery();
            }
        }
        #endregion

        #region 3.0 执行存储过程的方法 + static int ExcuteProc(string procName, params SqlParameter[] ps)
        /// <summary>
        /// 3.0 执行存储过程的方法
        /// </summary>
        /// <param name="procName">存储过程名</param>
        /// <param name="ps">参数数组</param>
        /// <returns></returns>
        public static int ExcuteProc(string procName, params SqlParameter[] ps)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand command = new SqlCommand(procName, conn);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddRange(ps);
                return command.ExecuteNonQuery();
            }
        }
        #endregion

        #region 4.0 查询结果集，返回的是首行首列 + static int ExecScalar(string sql, params SqlParameter[] ps)
        /// <summary>
        /// 4.0 查询结果集，返回的是首行首列
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="ps">参数数组</param>
        /// <returns></returns>
        public static object ExecScalar(string sql, params SqlParameter[] ps)  //调用的时候才判断是什么类型
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand command = new SqlCommand(sql, conn);
                command.Parameters.AddRange(ps);
                return command.ExecuteScalar();
            }
        }
        #endregion
    }
}
