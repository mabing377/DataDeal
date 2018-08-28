using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Linq.Expressions;
using System.Configuration;

namespace MySQL2MongoDB
{
    /// <summary>
    /// MongoDb 数据库操作类
    /// </summary>
    public class MongoHelper<T> where T : BaseEntity
    {
        //数据库连接字符串(web.config来配置)，可以动态更改connectionString支持多数据库.        
        public static string connectionString = ConfigurationManager.AppSettings["MongoConnectionString"];
        /// <summary>
        /// 数据库对象
        /// </summary>
        private IMongoDatabase database;
        /// <summary>
        /// 构造函数
        /// </summary>
        public MongoHelper()
        {
            var url = new MongoUrl(connectionString);
            MongoClientSettings mcs = MongoClientSettings.FromUrl(url);
            mcs.MaxConnectionLifeTime = TimeSpan.FromMilliseconds(1000);
            var client = new MongoClient(mcs);
            this.database = client.GetDatabase(url.DatabaseName);
        }
        /// <summary>
        /// 创建集合对象
        /// </summary>
        /// <param name="collName">集合名称</param>
        ///<returns>集合对象</returns>
        private IMongoCollection<T> GetColletion(String collName)
        {
            return database.GetCollection<T>(collName);
        }

        #region 增加
        /// <summary>
        /// 插入对象
        /// </summary>
        /// <param name="collName">集合名称</param>
        /// <param name="t">插入的对象</param>
        public void Insert(string collName, T t)
        {
            var coll = GetColletion(collName);
            coll.InsertOne(t);
        }
        /// <summary>
        /// 批量插入
        /// </summary>
        /// <param name="collName">集合名称</param>
        /// <param name="ts">要插入的对象集合</param>
        public void InsertBath(string collName, IEnumerable<T> ts)
        {
            var coll = GetColletion(collName);
            coll.InsertMany(ts);           
        }
        #endregion

        #region 删除
        /// <summary>
        /// 按BsonDocument条件删除
        /// </summary>
        /// <param name="collName">集合名称</param>
        /// <param name="document">文档</param>
        /// <returns></returns>
        public Int64 Delete(string collName, BsonDocument document)
        {
            var coll = GetColletion(collName);
            var result = coll.DeleteOne(document);
            return result.DeletedCount;
        }
        /// <summary>
        /// 按json字符串删除
        /// </summary>
        /// <param name="collName">集合名称</param>
        /// <param name="json">json字符串</param>
        /// <returns></returns>
        public Int64 Delete(string collName, String json)
        {
            var coll = GetColletion(collName);
            var result = coll.DeleteOne(json);
            return result.DeletedCount;
        }
        /// <summary>
        /// 按条件表达式删除
        /// </summary>
        /// <param name="collName">集合名称</param>
        /// <param name="predicate">条件表达式</param>
        /// <returns></returns>
        public Int64 Delete(string collName, Expression<Func<T, Boolean>> predicate)
        {
            var coll = GetColletion(collName);
            var result = coll.DeleteOne(predicate);
            return result.DeletedCount;
        }
        /// <summary>
        /// 按检索条件删除
        /// 建议用Builders<T>构建复杂的查询条件
        /// </summary>
        /// <param name="collName">集合名称</param>
        /// <param name="filter">条件</param>
        /// <returns></returns>
        public Int64 Delete(string collName, FilterDefinition<T> filter)
        {
            var coll = GetColletion(collName);
            var result = coll.DeleteOne(filter);
            return result.DeletedCount;
        }
        #endregion

        #region 修改
        /// <summary>
        /// 修改文档
        /// </summary>
        /// <param name="collName">集合名称</param>
        /// <param name="filter">修改条件</param>
        /// <param name="update">修改结果</param>
        /// <param name="upsert">是否插入新文档（filter条件满足就更新，否则插入新文档）</param>
        /// <returns></returns>
        public Int64 Update(string collName, Expression<Func<T, Boolean>> filter, UpdateDefinition<T> update, Boolean upsert = false)
        {
            var coll = GetColletion(collName);
            var result = coll.UpdateMany(filter, update, new UpdateOptions { IsUpsert = upsert });
            return result.ModifiedCount;
        }
        /// <summary>
        /// 用新对象替换新文档
        /// </summary>
        /// <param name="collName">集合名称</param>
        /// <param name="filter">修改条件</param>
        /// <param name="t">新对象</param>
        /// <param name="upsert">是否插入新文档（filter条件满足就更新，否则插入新文档）</param>
        /// <returns>修改影响文档数</returns>
        //public Int64 Update(String collName, Expression<Func<T, Boolean>> filter, T t, Boolean upsert = false)
        //{
        //    var coll = GetColletion(collName);
        //    BsonDocument document = t.ToBsonDocument<T>();
        //    document.Remove("_id");
        //    UpdateDocument update = new UpdateDocument("$set", document);
        //    var result = coll.UpdateMany(filter, update, new UpdateOptions { IsUpsert = upsert });
        //    return result.ModifiedCount;
        //}
        #endregion

        /// <summary>
        /// 查询，复杂查询直接用Linq处理
        /// </summary>
        /// <param name="collName">集合名称</param>
        /// <returns>要查询的对象</returns>
        public IQueryable<T> GetQueryable(string collName)
        {
            var coll = GetColletion(collName);
            return coll.AsQueryable<T>();
        }
        #region 查询 
        #endregion
    }

    /// <summary>
    /// 实体基类，方便生成_id
    /// </summary>
    public class BaseEntity
    {
        //[BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public String Id { get; set; }
        /// <summary>
        /// 给对象初值
        /// </summary>
        public BaseEntity()
        {
            this.Id = ObjectId.GenerateNewId().ToString();
        }
    }


}
