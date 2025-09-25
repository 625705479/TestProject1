using System;
using System.Text;
 using SQLite;

namespace TestProject1.Model
{
    [Table("grading_type")] 
    public class GradingType
    {
        /// <summary>
        /// id
        /// </summary>
        public long id { get; set; }

[ExcelColumnDescription("编码")]     
        /// <summary>
        /// code编码
        /// </summary>
        public string code { get; set; }

[ExcelColumnDescription("名称")]     
        /// <summary>
        /// name名称
        /// </summary>
        public string name { get; set; }

    }
}
