using NPOI.SS.Formula.Functions;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SQLite.SQLite3;

namespace TestProject1
{

    public class RedisExample()
    {    // 推荐使用单例模式管理ConnectionMultiplexer
        public static ConnectionMultiplexer _redis;
        // 初始化Redis连接
        public static void InitializeRedis(string connectionString = "localhost:6379")
        {
            try
            {
                // 连接Redis服务器，connectionString格式："ip:port,password=xxx"
                _redis = ConnectionMultiplexer.Connect(connectionString);

                // 注册连接事件（可选）
                _redis.ConnectionFailed += (sender, args) =>
                    Console.WriteLine($"Redis连接失败: {args.Exception.Message}");
                _redis.ConnectionRestored += (sender, args) =>
                    Console.WriteLine("Redis连接已恢复");

                Console.WriteLine("Redis连接成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Redis初始化失败: {ex.Message}");
                throw;
            }
        }

        // 获取数据库（Redis默认有16个数据库，索引从0开始）
        public static IDatabase GetDatabase(int dbIndex = 0)
        {
            InitializeRedis();
            if (_redis == null)
                throw new InvalidOperationException("请先初始化Redis连接");

            return _redis.GetDatabase(dbIndex);
        }
        /// <summary>
        /// 字符串操作示例
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="dbIndex"></param>
        /// <param name="istrue"></param>
        /// <param name="TimeSpanTimeout"></param>
        public static void StringOperations(string key, string value, int dbIndex = 0, bool istrue = false, int TimeSpanTimeout = 10)
        {

            var db = GetDatabase(dbIndex);
            if (istrue == false) {
                db.StringSet(key, value);
            }
            else
            {
                // 设置键值对（过期时间：10分钟）
                bool setSuccess = db.StringSet(key, value, TimeSpan.FromMinutes(TimeSpanTimeout));
            }
            //string username = db.StringGet("username");
        }
        /// <summary>
        /// 列表操作示例
        /// </summary>
        /// <param name="listKey"></param>
        /// <param name="listValues"></param>
        /// <param name="dbIndex"></param>
        /// <param name="istrue"></param>
        /// <param name="TimeSpanTimeout"></param>
        public static void ListOperations(string listKey,string[] listValues, int dbIndex = 0, bool istrue = false, int TimeSpanTimeout = 10)
        {
   
            var db = GetDatabase();
            if (istrue == false)
            {
                foreach (var value in listValues)
                {
                    db.ListRightPush(listKey, value);
                }
            }
            else
            {
                // 设置列表（过期时间：10分钟）
                foreach (var value in listValues)
                {
                    db.ListRightPush(listKey, value);
                    
                }
                bool setSuccess = db.KeyExpire(listKey, TimeSpan.FromMinutes(TimeSpanTimeout));
            }
            // 向列表添加元素


            // 获取列表长度
            //long length = db.ListLength(listKey);
            //Console.WriteLine($"消息列表长度: {length}");

            // 获取所有元素
            //RedisValue[] messages = db.ListRange(listKey);
            //Console.WriteLine("消息列表内容:");
            //foreach (var msg in messages)
            //{
            //    Console.WriteLine(msg);
            //}

            // 弹出元素（从左侧）
            //RedisValue firstMsg = db.ListLeftPop(listKey);
            //Console.WriteLine($"弹出的消息: {firstMsg}");
        }
        /// <summary>
        /// 哈希表操作示例
        /// </summary>
        /// <param name="hashKey"></param>
        /// <param name="HashEntry"></param>
        /// <param name="dbIndex"></param>
        /// <param name="istrue"></param>
        /// <param name="TimeSpanTimeout"></param>
        public static void HashOperations(string hashKey, HashEntry[] HashEntry, int dbIndex = 0, bool istrue = false, int TimeSpanTimeout = 10)
        {
           
            var db = GetDatabase();
            if (istrue == false)
            {
                // 存储哈希表
                db.HashSet(hashKey, HashEntry);
            }
            else
            {
                // 设置哈希表（过期时间：10分钟）
                db.HashSet(hashKey, HashEntry);
                bool setSuccess = db.KeyExpire(hashKey, TimeSpan.FromMinutes(TimeSpanTimeout));
            }

            //// 获取哈希表字段
            //string name = db.HashGet("user1001", "name");
            //Console.WriteLine($"用户姓名: {name}");

            //// 获取所有字段
            //HashEntry[] allFields = db.HashGetAll("user1001");
            //Console.WriteLine("用户所有信息:");
            //foreach (var field in allFields)
            //{
            //    Console.WriteLine($"{field.Name}: {field.Value}");
            //}
        }
        /// <summary>
        /// 集合操作示例
        /// </summary>
        /// <param name="setKey"></param>
        /// <param name="setValues"></param>
        /// <param name="dbIndex"></param>
        /// <param name="istrue"></param>
        /// <param name="TimeSpanTimeout"></param>
        public static void SetOperations(string setKey, string setValues, int dbIndex = 0, bool istrue = false, int TimeSpanTimeout = 10) {
            var db = GetDatabase();
            if (istrue == false) { 
            db.SetAdd(setKey, setValues);
            }
            else {
                // 设置集合（过期时间：10分钟）
                db.SetAdd(setKey, setValues);
                bool setSuccess = db.KeyExpire(setKey, TimeSpan.FromMinutes(TimeSpanTimeout));
            }

        }
        /// <summary>
        /// 有序集合操作示例
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="setKey"></param>
        /// <param name="setValues"></param>
        /// <param name="dbIndex"></param>
        /// <param name="istrue"></param>
        /// <param name="TimeSpanTimeout"></param>
        public static void ZSetOperations(string groupName,string setKey, double setValues, int dbIndex = 0, bool istrue = false, int TimeSpanTimeout = 10)
        {
            var db = GetDatabase();
            if (istrue == false)
            {
                db.SortedSetAdd(groupName, setKey, setValues);
            }
            else
            {
                // 设置有序集合（过期时间：10分钟）
                db.SortedSetAdd(groupName, setKey, setValues);
                bool setSuccess = db.KeyExpire(setKey, TimeSpan.FromMinutes(TimeSpanTimeout));
            }

        }

    }
}