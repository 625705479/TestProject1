using System;
using System.Text;

 using SQLite;

namespace TestProject1.Model
{
    [Table("grading_detail")] 
    public class GradingDetail
    {
        /// <summary>
        /// id
        /// </summary>
        public long id { get; set; }

[ExcelColumnDescription("线别")]     
        /// <summary>
        /// line_no线别
        /// </summary>
        public string line_no { get; set; }

[ExcelColumnDescription("档位")]     
        /// <summary>
        /// grading_position档位
        /// </summary>
        public string grading_position { get; set; }

[ExcelColumnDescription("可存放最大数量")]     
        /// <summary>
        /// max_num可存放最大数量
        /// </summary>
        public long max_num { get; set; }

[ExcelColumnDescription("放置方式 1平放 2竖放")]     
        /// <summary>
        /// place_type放置方式 1平放 2竖放
        /// </summary>
        public long place_type { get; set; }

[ExcelColumnDescription("功率")]     
        /// <summary>
        /// power功率
        /// </summary>
        public object power { get; set; }

[ExcelColumnDescription("电流")]     
        /// <summary>
        /// electric电流
        /// </summary>
        public object electric { get; set; }

[ExcelColumnDescription("等级")]     
        /// <summary>
        /// grade等级
        /// </summary>
        public object grade { get; set; }

[ExcelColumnDescription("色系")]     
        /// <summary>
        /// color色系
        /// </summary>
        public object color { get; set; }

[ExcelColumnDescription("料号")]     
        /// <summary>
        /// item料号
        /// </summary>
        public string item { get; set; }

[ExcelColumnDescription("工单")]     
        /// <summary>
        /// order工单
        /// </summary>
        public object order { get; set; }

[ExcelColumnDescription("区域A区 B区")]     
        /// <summary>
        /// region区域A区 B区
        /// </summary>
        public object region { get; set; }

[ExcelColumnDescription("档位类型 0-NG档 1-非NG档")]     
        /// <summary>
        /// grading_type档位类型 0-NG档 1-非NG档
        /// </summary>
        public long grading_type { get; set; }

[ExcelColumnDescription("当前档位使用状态")]     
        /// <summary>
        /// is_use当前档位使用状态
        /// </summary>
        public string is_use { get; set; }

[ExcelColumnDescription("修改时间")]     
        /// <summary>
        /// update_time修改时间
        /// </summary>
        public object update_time { get; set; }

    }
}
