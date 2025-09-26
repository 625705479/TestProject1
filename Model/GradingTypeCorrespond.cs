using System;
using System.Text;

 using SQLite;

namespace TestProject1.Model
{
    /// <summary>
    /// 挡位分类关联表
    /// </summary>
    [Table("grading_type_correspond")] 
    public class GradingTypeCorrespond
    {
[ExcelColumnDescription("类型编码")]     
        /// <summary>
        /// code类型编码
        /// </summary>
        public string code { get; set; }

[ExcelColumnDescription("分档档位主键")]     
        /// <summary>
        /// grading_position分档档位主键
        /// </summary>
        public string grading_position { get; set; }

        /// <summary>
        /// id
        /// </summary>
        public long id { get; set; }

[ExcelColumnDescription("创建时间")]     
        /// <summary>
        /// created_time创建时间
        /// </summary>
        public string created_time { get; set; }

    }
}
