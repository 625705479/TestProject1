using System;

namespace TestProject1.Model
{
    /// <summary>
    /// 用于标记属性中文描述的特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)] // 仅允许应用在属性上
    public class ExcelColumnDescription : Attribute
    {
        public string Description { get; }

        public ExcelColumnDescription(string description)
        {
            Description = description;
        }
    }
}