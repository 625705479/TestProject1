using FTOptix.HMIProject;
using FTOptix.Store;
using NPOI.HSSF.UserModel;
using NPOI.Util.Collections;
using RabbitMQ.Client;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UAManagedCore;

namespace TestProject1.Helper
{

    public static class PubilcMethodHelper
    {
        /// <summary>
        /// 通用方法插入DataTable数据到Store
        /// </summary>
        /// <param name="tablename">表名</param>
        /// <param name="data">数据源</param>
        /// <param name="columnName">列名</param>
        /// <param name="fieldConverters">需要格式化的列 三元表达式</param>
        /// <returns></returns>
        public static bool InsertStore(string tablename, DataTable data,  Dictionary<string, Func<object, object>> fieldConverters = null)
        {
            Func<object, object> handleDBNull = value =>
                  value == DBNull.Value ? null : value;
            try
            {
                var store = Project.Current.Get<Store>("DataStores/EmbeddedDatabase1");
                string deleteSql = $"DELETE FROM {tablename}";
                string[] columnNames;
                object[,] results;
                store.Query(deleteSql, out columnNames, out results);
             var   targetTable = store.Tables.FirstOrDefault(t =>
             string.Equals(t.BrowseName, tablename, StringComparison.OrdinalIgnoreCase));
                var columnName = new string[] { };
                var str = targetTable.Columns;
                str.ToList().ForEach(col =>
                {
                    columnName = columnName.Append(col.BrowseName).ToArray();
                });

                for (int i = 0; i < data.Rows.Count; i++)
      {
                    var row = data.Rows[i];


                    object[,] values = new object[1, columnName.Length];
                    // 4. 循环赋值（基于字段列表索引，消除硬编码0-19）
                    for (int j = 0; j < columnName.Length; j++)
                    {
                        string fieldName = columnName[j];
                        // 对特殊字段使用转换后的值，其他字段直接取原始值
                        object value = row[fieldName];
                        if (fieldConverters != null && fieldConverters.ContainsKey(fieldName))
                        {
                            value = fieldConverters[fieldName](value);
                        }
                        else
                        {
                            value = handleDBNull(value);
                        }
                        // 统一处理DBNull
                        values[0, j] = handleDBNull(value);
                    }

                    // 执行插入（字段与值数组长度严格一致）
                    store.Insert(tablename, columnName, values);
                }
                store.Query($"SELECT * from {tablename}",out columnName,out results);
                Logger.Info($"插入表 {tablename} 数据成功，共 {data.Rows.Count} 条记录");
                return true;
            }
            catch (Exception)
            {

                return false;
            }

        }
        public  static void RabbitMQTest()
        {
            var messageQueue = new InMemoryMessageQueue("demo_queue", 100);
            messageQueue.Enqueue("订单消息");
            var msgresult = messageQueue.Dequeue();
            //messageQueue.Ack(msg.MessageId);
            //var messageQueue = messageQueue.Dequeue();

     
            if (msgresult != null)
            {
                // 取值成功：获取消息ID、内容、创建时间等属性
                Console.WriteLine($"消息ID：{msgresult.MessageId}");
                Console.WriteLine($"消息内容：{msgresult.Content}");
                Console.WriteLine($"创建时间：{msgresult.CreateTime}");
                Console.WriteLine($"消息状态：{msgresult.Status}"); // 1=消费中

                // 处理完成后必须手动ACK（确认取值完成，否则消息会留在“消费中”队列）
                bool isAckSuccess = messageQueue.Ack(msgresult.MessageId);
                if (isAckSuccess)
                {
                    Console.WriteLine("消息确认成功，已从队列移除");
                }
            }
        }

    }
    public class QueueMessage() {
        /// <summary>
        /// 消息唯一ID
        /// </summary>
        public string MessageId { get; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; } = DateTime.Now;

        /// <summary>
        /// 消费状态（0-待消费 1-消费中 2-已确认 3-消费失败）
        /// </summary>
        public int Status { get; set; } = 0;

    }


    public interface IMessageQueue {
        /// <summary>
        /// 队列名称
        /// </summary>
        string QueueName { get; }

        /// <summary>
        /// 队列当前消息数
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 队列最大容量（0表示无限制）
        /// </summary>
        int MaxCapacity { get; }

        /// <summary>
        /// 生产者发送消息
        /// </summary>
        /// <param name="content">消息内容</param>
        /// <returns>消息ID</returns>
        string Enqueue(string content);

        /// <summary>
        /// 消费者获取消息（非阻塞）
        /// </summary>
        /// <returns>消息实体（无消息返回null）</returns>
        QueueMessage Dequeue();

        /// <summary>
        /// 手动确认消息（处理完成）
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <returns>是否确认成功</returns>
        bool Ack(string messageId);

        /// <summary>
        /// 消息消费失败，重新入队
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <returns>是否重新入队成功</returns>
        bool Requeue(string messageId);

    }

    public class InMemoryMessageQueue : IMessageQueue
    {
        /// <summary>
        /// 待消费消息队列（线程安全）
        /// </summary>
        private readonly ConcurrentQueue<QueueMessage> _pendingQueue = new();

        /// <summary>
        /// 消费中消息（未确认）
        /// </summary>
        private readonly ConcurrentDictionary<string, QueueMessage> _processingQueue = new();

        /// <summary>
        /// 锁对象（控制并发操作）
        /// </summary>
        private readonly object _lockObj = new();

        public string QueueName { get; }
        public int Count => _pendingQueue.Count + _processingQueue.Count;
        public int MaxCapacity { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="queueName">队列名称</param>
        /// <param name="maxCapacity">最大容量（0无限制）</param>
        public InMemoryMessageQueue(string queueName, int maxCapacity = 0)
        {
            QueueName = queueName;
            MaxCapacity = maxCapacity;
        }

        /// <summary>
        /// 生产者入队
        /// </summary>
        public string Enqueue(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentNullException(nameof(content));

            // 队列容量限制检查
            if (MaxCapacity > 0 && Count >= MaxCapacity)
                throw new InvalidOperationException($"队列 {QueueName} 已达最大容量：{MaxCapacity}");

            var message = new QueueMessage { Content = content };

            // 线程安全入队
            _pendingQueue.Enqueue(message);
            Console.WriteLine($"[生产者] 消息入队：ID={message.MessageId}，内容={content}");

            return message.MessageId;
        }

        /// <summary>
        /// 消费者出队（非阻塞）
        /// </summary>
        public QueueMessage Dequeue()
        {
            if (_pendingQueue.TryDequeue(out var message))
            {
                // 标记为消费中，移入处理队列
                message.Status = 1;
                _processingQueue.TryAdd(message.MessageId, message);
                Console.WriteLine($"[消费者] 获取消息：ID={message.MessageId}，内容={message.Content}");
                return message;
            }

            // 无消息返回null
            return null;
        }

        /// <summary>
        /// 手动确认消息（处理完成）
        /// </summary>
        public bool Ack(string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return false;

            lock (_lockObj)
            {
                if (_processingQueue.TryRemove(messageId, out var message))
                {
                    message.Status = 2;
                    Console.WriteLine($"[确认] 消息处理完成：ID={messageId}");
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 消息消费失败，重新入队
        /// </summary>
        public bool Requeue(string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return false;

            lock (_lockObj)
            {
                if (_processingQueue.TryRemove(messageId, out var message))
                {
                    // 重置状态，重新入队
                    message.Status = 0;
                    _pendingQueue.Enqueue(message);
                    Console.WriteLine($"[重入队] 消息重新入队：ID={messageId}");
                    return true;
                }
                return false;
            }
        }
    }
}