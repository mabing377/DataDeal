using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using MongoDB;

namespace Utility
{
    public class MongoHelper
    {
        public static readonly string connectionString = ConfigurationManager.AppSettings["MongoConnectionString"];
        public static readonly string database = ConfigurationManager.AppSettings["Database_mongoDB"];

        #region 新增
        /// <summary>
        /// 插入新数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="entiry"></param>
        public static void InsertOne<T>(string collectionName, T entity) where T : class
        {
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                MongoDB.IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                collection.Insert(entity, true);
                mongo.Disconnect();
            }
        }
        /// <summary>
        /// 插入多个数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="entiry"></param>
        public static void InsertAll<T>(string collectionName, IEnumerable<T> entity) where T : class
        {
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                collection.Insert(entity, true);
                mongo.Disconnect();

            }
        }
        #endregion

        #region 更新
        /// <summary>
        /// 更新操作
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="collectionName">表名</param>
        /// <param name="query">条件</param>
        /// <param name="entry">新实体</param>
        public static void Update<T>(string collectionName, Document entity, Document query) where T : class
        {
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                collection.Update(entity, query, true);
                mongo.Disconnect();
            }
        }
        /// <summary>
        /// 更新操作
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="collectionName">表名</param>
        /// <param name="query">条件</param>
        /// <param name="entry">新实体</param>
        public static void UpdateWithModel<T>(string collectionName, T model, Document query) where T : class
        {
            Document entity = Model2Document<T>(model);
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                collection.Update(entity, query, true);
                mongo.Disconnect();
            }
        }
        /// <summary>
        /// 更新操作
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="collectionName">表名</param>
        /// <param name="query">条件</param>
        /// <param name="entry">新实体</param>
        public static void UpdateWithColumn<T>(string collectionName, Document entity, Document query) where T : class
        {
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                collection.Update(entity, query, UpdateFlags.MultiUpdate, true);
                mongo.Disconnect();
            }
        }
        /// <summary>
        /// 更新操作
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="collectionName">表名</param>
        /// <param name="entry">新实体</param>
        public static void UpdateAll<T>(string collectionName, Document entity) where T : class
        {
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                collection.Update(entity, true);
                mongo.Disconnect();
            }
        }
        /// <summary>
        /// 更新操作
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="collectionName">表名</param>
        /// <param name="entry">新实体</param>
        public static void Save<T>(string collectionName, T entity) where T : class
        {
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                collection.Save(entity,true);
                mongo.Disconnect();
            }
        }
        #endregion

        #region 查询
        /// <summary>
        /// 获取一条数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static T GetOne<T>(string collectionName, Document query) where T : class
        {
            T result = default(T);
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                result = collection.FindOne(query);
                mongo.Disconnect();

            }
            return result;
        }
        /// <summary>
        /// 获取一条数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static T GetFirst<T>(string collectionName, Document query, Document fields) where T : class
        {
            T result = default(T);
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                result = collection.Find(query, fields).Skip(0).Limit(1).Documents.First();
                mongo.Disconnect();

            }
            return result;
        }
        /// <summary>
        /// 获取一个集合下所有数据
        /// </summary>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public static List<T> GetAll<T>(string collectionName, Document sort=null) where T : class
        {
            List<T> result = new List<T>();
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                if(sort == null)
                    foreach (T entity in collection.FindAll().Documents)
                    {
                        result.Add(entity);
                    }
                else
                    foreach (T entity in collection.FindAll().Sort(sort).Documents)
                    {
                        result.Add(entity);
                    }
                mongo.Disconnect();

            }
            return result;
        }
        /// <summary>
        /// 获取一个集合下所有数据
        /// </summary>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public static List<T> GetAllByField<T>(string collectionName, Document fields) where T : class
        {
            List<T> result = new List<T>();
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                if(fields == null)
                    foreach (T entity in collection.FindAll().Documents)
                    {
                        result.Add(entity);
                    }
                else
                    foreach (T entity in collection.Find(null,fields).Documents)
                    {
                        result.Add(entity);
                    }
                mongo.Disconnect();

            }
            return result;
        }
        /// <summary>
        /// 获取列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="query"></param>
        /// <param name="Sort"></param>
        /// <param name="pageindex"></param>
        /// <param name="pagesize"></param>
        /// <returns></returns>
        public static List<T> GetList<T>(string collectionName, object query, Document sort, int pageindex, int pagesize) where T : class
        {
            List<T> result = new List<T>();
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                foreach (T entity in collection.Find(query).Sort(sort).Skip((pageindex - 1) * pagesize).Limit(pagesize).Documents)
                {
                    result.Add(entity);
                }
                mongo.Disconnect();

            }
            return result;
        }
        /// <summary>
        /// 获取列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="query"></param>
        /// <param name="Sort"></param>
        /// <param name="pageindex"></param>
        /// <param name="pagesize"></param>
        /// <returns></returns>
        public static List<T> GetList<T>(string collectionName, object query, int pageindex, int pagesize) where T : class
        {
            List<T> result = new List<T>();
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                foreach (T entity in collection.Find(query).Skip((pageindex - 1) * pagesize).Limit(pagesize).Documents)
                {
                    result.Add(entity);
                }
                mongo.Disconnect();

            }
            return result;
        }
        /// <summary>
        /// 获取列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="query"></param>
        /// <param name="fields"></param>
        /// <param name="sort"></param>
        /// <param name="pageindex"></param>
        /// <param name="pagesize"></param>
        /// <returns></returns>
        public static List<T> GetList<T>(string collectionName, object query, object fields, Document sort, int pageindex, int pagesize) where T : class
        {
            List<T> result = new List<T>();
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                foreach (T entity in collection.Find(query, fields).Sort(sort).Skip((pageindex - 1) * pagesize).Limit(pagesize).Documents)
                {
                    result.Add(entity);
                }
                mongo.Disconnect();

            }
            return result;
        }
        /// <summary>
        /// 获取一个通过查询条件的集合下所有数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static List<T> GetList<T>(string collectionName, object query) where T : class
        {
            List<T> result = new List<T>();
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                foreach (T entity in collection.Find(query).Documents)
                {
                    result.Add(entity);
                }
                mongo.Disconnect();
            }
            return result;
        }

        /// <summary>  
        /// 获取数据表总行数  
        /// </summary>  
        /// <typeparam name="T"></typeparam>  
        /// <param name="query"></param>  
        /// <param name="collectionName"></param>  
        /// <returns></returns>  
        public static long GetCount<T>(string collectionName, object query) where T : class
        {
            long count = 0;
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                if (query == null)
                {
                    count= collection.Count();
                }
                else
                {
                    count= collection.Count(query);
                }
                mongo.Disconnect();
                return count;
            }
        }
        #endregion

        #region 删除
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionName"></param>
        /// <param name="entity"></param>
        public static void Delete<T>(string collectionName, Document query) where T : class
        {
            using (Mongo mongo = new Mongo(connectionString))
            {
                mongo.Connect();
                IMongoDatabase friends = mongo.GetDatabase(database);
                IMongoCollection<T> collection = friends.GetCollection<T>(collectionName);
                collection.Remove(query, true);
                mongo.Disconnect();
            }
        }
        #endregion

        /// <summary>
        /// C#反射遍历对象属性
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="model">对象</param>
        public static Document Model2Document<T>(T model)
        {
            Type t = model.GetType();
            PropertyInfo[] PropertyList = t.GetProperties();
            Document doc = new Document();
            foreach (PropertyInfo item in PropertyList)
            {
                doc.Add(item.Name, item.GetValue(model));
            }
            return doc;
        }
    }
}