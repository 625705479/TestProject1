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

    public  class  RedisExample
    {
        public RedisExample() { }
     // 推荐使用单例模式管理ConnectionMultiplexer
            public static ConnectionMultiplexer _redis;     
        // 线程安全锁
        private static readonly object _lockObj = new object();
        // Redis数据结构类型枚举
        public enum RedisType
            {
                String,    // 字符串
                List,      // 列表
                Hash,      // 哈希表
                Set,       // 集合
                ZSet,      // 有序集合
                  Stream     // 流（新增）
        }

        // 初始化/获取连接（确保单例+长连接）
        public static ConnectionMultiplexer GetConnection()
        {
            // 第一次检查（无锁，提高性能）
            if (_redis == null || !_redis.IsConnected)
            {
                // 加锁确保线程安全
                lock (_lockObj)
                {
                    // 第二次检查（防止多线程同时通过第一次检查）
                    if (_redis == null || !_redis.IsConnected)
                    {
                        // 连接字符串建议配置在外部（如appsettings.json）
                        var connectionString = "localhost:6379,password=xxx,abortConnect=false";
                        _redis = ConnectionMultiplexer.Connect(connectionString);

                        // 可选：注册连接断开事件，便于监控
                        _redis.ConnectionFailed += (sender, e) =>
                        {
                            Console.WriteLine($"Redis连接失败: {e.Exception}");
                        };
                    }
                }
            }
            return _redis;
        }

        // 获取数据库（Redis默认有16个数据库，索引从0开始）
        public static IDatabase GetDatabase(int dbIndex = 0)
            {
               
                if (_redis == null)
                    throw new InvalidOperationException("请先初始化Redis连接");

          return  GetConnection().GetDatabase(dbIndex);
        }
            /// <summary>
            /// 统一Redis操作方法
            /// </summary>
            /// <param name="type">Redis数据结构类型</param>
            /// <param name="key">键名</param>
            /// <param name="value">值（根据类型传入不同结构）</param>
            /// <param name="score">有序集合专用分数（其他类型忽略）</param>
            /// <param name="hashEntries">哈希表专用字段集合（其他类型忽略）</param>
            /// <param name="dbIndex">数据库索引</param>
            /// <param name="useExpire">是否设置过期时间</param>
            /// <param name="expireMinutes">过期时间（分钟）</param>
            /// <returns>操作是否成功</returns>
            public static bool Operate(
                RedisType type,
                string key,
                object value = null,
                double score = 0,
                HashEntry[] hashEntries = null,
                  NameValueEntry[] streamEntries = null,  // Stream专用字段（新增）
                int dbIndex = 0,
                bool useExpire = false,
                int expireMinutes = 10)
            {
                var db = GetDatabase(dbIndex);
                bool success = false;

                try
                {
                    switch (type)
                    {
                        case RedisType.String:
                            // 值需为字符串类型
                            if (value is string strValue)
                            {
                                success = useExpire
                                    ? db.StringSet(key, strValue, TimeSpan.FromMinutes(expireMinutes))
                                    : db.StringSet(key, strValue);
                            }
                            break;

                        case RedisType.List:
                            // 值需为字符串数组
                            if (value is string[] listValues)
                            {
                                foreach (var item in listValues)
                                {
                                    db.ListRightPush(key, item);
                                }
                                success = true;
                                // 设置过期时间
                                if (useExpire)
                                {
                                    db.KeyExpire(key, TimeSpan.FromMinutes(expireMinutes));
                                }
                            }
                            break;

                        case RedisType.Hash:
                            // 使用传入的哈希字段集合
                            if (hashEntries != null && hashEntries.Length > 0)
                            {
                                db.HashSet(key, hashEntries);
                                success = true;
                                if (useExpire)
                                {
                                    db.KeyExpire(key, TimeSpan.FromMinutes(expireMinutes));
                                }
                            }
                            break;

                        case RedisType.Set:
                            // 值需为字符串类型
                            if (value is string setValue)
                            {
                                success = db.SetAdd(key, setValue);
                                if (useExpire)
                                {
                                    db.KeyExpire(key, TimeSpan.FromMinutes(expireMinutes));
                                }
                            }
                            break;

                        case RedisType.ZSet:
                            // 值需为字符串类型，同时需要分数
                            if (value is string zsetValue)
                            {
                                success = db.SortedSetAdd(key, zsetValue, score);
                                if (useExpire)
                                {
                                    db.KeyExpire(key, TimeSpan.FromMinutes(expireMinutes));
                                }
                            }
                            break;
                    // 新增Stream操作逻辑
                    case RedisType.Stream:
                        if (streamEntries != null && streamEntries.Length > 0)
                        {
                            // 添加流消息（自动生成消息ID）
                            var messageId = db.StreamAdd(key, streamEntries);
                            success = !string.IsNullOrEmpty(messageId); // 消息ID不为空则成功

                            // 设置过期时间
                            if (useExpire)
                            {
                                db.KeyExpire(key, TimeSpan.FromMinutes(expireMinutes));
                            }
                        }
                        break;
                }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Redis操作失败: {ex.Message}");
                    success = false;
                }

                return success;
            }
        }
    }
