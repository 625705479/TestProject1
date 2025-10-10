namespace TestProject1.Model
{
    /// <summary>
    /// 告警信息实体类
    /// </summary>
    public class AlarmInfo
    {
        public string Name { get; set; }           // 对应Excel“告警定义”列（XML的name属性）
        public string Description { get; set; }    // 对应Excel“告警描述”列（XML的description属性）
        public string BaseType { get; set; }       // 对应Excel“字段类型”列（XML的baseType属性）
        public string AlertType { get; set; } = "EqualTo"; // 告警类型（默认EqualTo，可从Excel扩展）
        public int Priority { get; set; } = 1;     // 告警优先级（默认1，可从Excel扩展）

    }
    public enum AlertType
    {
        EqualTo,//等于
        NotEqualTo,//不等于

    }
}
