using StackExchange.Redis;
using System;


namespace Utility
{
    /// <summary>
    /// Redis 连接单例(依赖 StackExchange.Redis)
    /// </summary>
    public static class Redis
    {
        /// <summary>
        /// redis 连接池
        /// </summary>
        private static ConnectionMultiplexer _redis;
        /// <summary>
        /// redis单例 是否初始化
        /// </summary>
        private static bool isInit = false;
        /// <summary>
        /// 初始化 redis 单例
        /// </summary>
        /// <param name="connStr">redis 连接字符串</param>
        public static void Init(string connStr)
        {
            if (!isInit)
            {
                //配置
                ConfigurationOptions opt = new ConfigurationOptions();
                opt.SslHost = connStr;
                opt.EndPoints.Add(connStr);
                opt.AbortOnConnectFail = false;
                //初始化 连接池
                _redis = ConnectionMultiplexer.Connect(opt);
                isInit = true;
            }
        }
        /// <summary>
        /// 根据索引获取 redis database (0-15)
        /// </summary>
        /// <param name="dbIndex">db 索引</param>
        /// <returns>db 对象</returns>
        public static RedisDB DB(int dbIndex)
        {
            return new RedisDB(_redis.GetDatabase(dbIndex));
        }
        /// <summary>
        /// 获取redis 的订阅者 用来 发布 订阅事件
        /// </summary>
        /// <returns>订阅者</returns>
        public static ISubscriber Subscriber()
        {
            return _redis.GetSubscriber();
        }
    }
    /// <summary>
    /// redis db 对象
    /// </summary>
    public class RedisDB
    {
        /// <summary>
        /// StackExchange.Redis 中的原始 DB 对象
        /// </summary>
        private IDatabase _base;
        public RedisDB(IDatabase baseDB)
        {
            _base = baseDB;
        }

        /// <summary>
        /// 指定某个键的过期时间
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="expiry">时间</param>
        /// <returns></returns>
        public bool KeyExpire(string key, TimeSpan? expiry)
        {
            return _base.KeyExpire(key, expiry);
        }

        /// <summary>
        /// 设置或更新 HashSet 数据
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="hashFields">键值对</param>
        public void HashSet(string key, HashEntry[] hashFields)
        {
            _base.HashSet(key, hashFields);
        }

        /// <summary>
        /// 设置或更新HashSet数据的某个字段
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="field">字段</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public bool HashSet(string key, RedisValue field, RedisValue value)
        {
            _base.HashDelete(key, field);
            return _base.HashSet(key, field, value);
        }
        /// <summary>
        /// 删除多个field
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public long HashDeleteField(string key, RedisValue[] field)
        {
            return _base.HashDelete(key, field);
        }
        /// <summary>
        /// 获取 HashSet 数据
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public HashEntry[] HashGetAll(string key)
        {
            try
            {
                return _base.HashGetAll(key);
            }
            catch
            {
                return new HashEntry[] { };
            }
        }

        /// <summary>
        /// 获取 HashSet 中的某个字段
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="hashField">字段名</param>
        /// <returns>字段值</returns>
        public RedisValue HashGet(string key, string hashField)
        {
            return _base.HashGet(key, hashField);
        }

        /// <summary>
        /// 删除某个键
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public bool KeyDelete(string key)
        {
            return _base.KeyDelete(key);
        }

        /// <summary>
        /// 设置或更新一个简单的字符串键值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public bool StringSet(string key, RedisValue value)
        {
            return _base.StringSet(key, value);
        }

        /// <summary>
        /// 获取 一个简单的字符串键值
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>值</returns>
        public RedisValue StringGet(string key)
        {
            return _base.StringGet(key);
        }

        /// <summary>
        /// 添加一个 地理信息
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="geo">地理信息</param>
        /// <returns></returns>
        public bool GeoAdd(string key, GeoEntry geo)
        {
            return _base.GeoAdd(key, geo);
        }

        /// <summary>
        /// 删除一个地理信息
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="member">地理信息添加时指定的 member(系统中主要是跑男的 UserID)</param>
        /// <returns></returns>
        public bool GeoRemove(string key, string member)
        {
            return _base.GeoRemove(key, member);
        }

        /// <summary>
        /// 获取数据库中地理信息 解析后的位置信息
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="member">地理信息添加时指定的 member(系统中主要是跑男的 UserID)</param>
        /// <returns></returns>
        public GeoPosition? GeoPosition(string key, RedisValue member)

        {
            return _base.GeoPosition(key, member);
        }

        /// <summary>
        /// 通过给定距离 获取附近的地理信息
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="lng">中心经度</param>
        /// <param name="lat">中心纬度</param>
        /// <param name="radius">地理信息数组</param>
        /// <returns></returns>
        public GeoRadiusResult[] GeoRadius(string key, double lng, double lat, double radius)
        {
            return _base.GeoRadius(key, lng, lat, radius);
        }

        /// <summary>
        /// 从结尾处向List中插入 一组值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="values">值数组</param>
        /// <returns></returns>
        public long ListRightPush(string key, RedisValue[] values)
        {
            return _base.ListRightPush(key, values);
        }

        /// <summary>
        /// 从 List 中获取指定开始与结束索引之间的 值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="start">开始索引</param>
        /// <param name="stop">结束索引</param>
        /// <returns>值 数组</returns>
        public RedisValue[] ListRange(string key, long start = 0, long stop = -1)
        {
            return _base.ListRange(key, start, stop);
        }

        /// <summary>
        /// List 的长度
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>长度</returns>
        public long ListLength(string key)
        {
            return _base.ListLength(key);
        }

        /// <summary>
        /// 向 Set 中添加值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public bool SetAdd(string key, RedisValue value)
        {
            return _base.SetAdd(key, value);
        }

        /// <summary>
        /// 获取 Set 中的 所有值
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>值数组</returns>
        public RedisValue[] SetMembers(string key)
        {
            return _base.SetMembers(key);
        }

        /// <summary>
        /// 从结尾处向List中插入单个值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="values">值</param>
        /// <returns></returns>
        public long ListRightPush(string key, string value)
        {
            return _base.ListRightPush(key, value);
        }

        /// <summary>
        /// 从List 开始出 取出一个值(获取并删除、与 ListRightPush 形成队列的作用)
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>值</returns>
        public RedisValue ListLeftPop(string key)
        {
            return _base.ListLeftPop(key);
        }

        /// <summary>
        /// 向 Set 中插入一组值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="values">值</param>
        /// <returns></returns>
        public long SetAdd(string key, RedisValue[] values)
        {
            return _base.SetAdd(key, values);
        }
        /// <summary>
        /// 判断values是否在 Set 中
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="values">值</param>
        /// <returns></returns>
        public bool SetContains(RedisKey key, RedisValue values)
        {
            return _base.SetContains(key, values);
        }
        /// <summary>
        /// 从Set集合中移出values值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool SetRemove(RedisKey key, RedisValue values)
        {
            return _base.SetRemove(key, values);
        }
    }
}
