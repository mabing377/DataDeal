using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySQL2MongoDB
{
    public class MongoServer
    {
        private IMongoDatabase database = null;
        private string server = System.Configuration.ConfigurationManager.AppSettings["MongoServer"];
        private string port = System.Configuration.ConfigurationManager.AppSettings["MongoPort"];
        private string db = System.Configuration.ConfigurationManager.AppSettings["DBName"];
        public MongoServer()
        {
            Init();
        }
        public void Init()
        {
            var client = new MongoClient($"mongodb://{server}:{port}");
            database = client.GetDatabase(db);
        }
        /// <summary>
        /// 查询一条
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public string FindOne(string tablename, BsonDocument filter)
        {
            var collection = database.GetCollection<BsonDocument>(tablename);
            var documents = collection.Find(filter).FirstOrDefault();
            return documents == null ? "" : documents.ToJson();
        }
        /// <summary>
        /// update
        /// </summary>
        /// <param name="collectionName"></param>
        /// <param name="filter"></param>
        /// <param name="update"></param>
        public void Update(string collectionName, BsonDocument filter, BsonDocument update)
        {
            var collection = database.GetCollection<BsonDocument>(collectionName);
            collection.FindOneAndUpdate(filter, update);
        }
        /// <summary>
        /// insert
        /// </summary>
        /// <param name="collectionName"></param>
        /// <param name="json"></param>
        public void Insert(string collectionName, string json)
        {
            var collection = database.GetCollection<BsonDocument>(collectionName);
            BsonDocument document = BsonDocument.Parse(json);
            collection.InsertOne(document);
        }
        /// <summary>
        /// delete
        /// </summary>
        /// <param name="collectionName"></param>
        /// <param name="json"></param>
        public void Delete(string collectionName, BsonDocument query)
        {
            var collection = database.GetCollection<BsonDocument>(collectionName);
            collection.FindOneAndDelete(query);
        }
        /// <summary>
        /// 批量导入
        /// </summary>
        /// <param name="collectionName"></param>
        /// <param name="docs"></param>
        public void ImportBatch(string collectionName, List<BsonDocument> docs)
        {
            var collection = database.GetCollection<BsonDocument>(collectionName);
            collection.InsertMany(docs);
        }
        /// <summary>
        /// 添加一条数组
        /// </summary>
        /// <param name="collectionName">集合名</param>
        /// <param name="filter">条件</param>
        /// <param name="json">更新内容</param>
        /// <param name="arrayName">数组名</param>
        public void UpdatePushArray(string collectionName, BsonDocument filter, string json, string arrayName)
        {
            //更新mongo简历
            var update = new BsonDocument();
            BsonDocument document = BsonDocument.Parse(json);
            //添加doucment数组对象
            update.Add("$push", new BsonDocument() { new BsonElement(arrayName, document) });
            //更新修改时间
            //update.Add("$set", new BsonDocument() {
            //                new BsonElement("",""),
            //            });
            Update(collectionName, filter, update);
        }
        /// <summary>
        /// 删除一条数组
        /// </summary>
        /// <param name="collectionName"></param>
        /// <param name="filter"></param>
        /// <param name="arrDoc"></param>
        public void UpdatePullArray(string collectionName, BsonDocument filter, BsonDocument arrDoc)
        {
            //更新mongo简历
            var update = new BsonDocument();
            //$pull删除对象数组idName=id的对象文档
            update.Add("$pull", arrDoc);
            Update(collectionName, filter, update);
        }
        /// <summary>
        /// 更新数组
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="json"></param>
        /// <param name="arrayName"></param>
        public void UpdateArray(string collectionName, BsonDocument filter, string json, string arrayName)
        {
            //更新mongo简历
            var update = new BsonDocument();
            var update1 = new BsonDocument();
            //删除原有数组文档
            update.Set("$unset", new BsonDocument() { new BsonElement(arrayName, "") });
            Update(collectionName, filter, update);
            //set新的数组
            json = "{ \"" + arrayName + "\" :" + json + "}";
            BsonDocument document = BsonDocument.Parse(json);
            update1.Set("$set", document);
            Update(collectionName, filter, update1);
        }
    }
}
