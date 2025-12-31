using FTOptix.HMIProject;
using FTOptix.UI;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UAManagedCore;

namespace TestProject1.Helper
{
    public static class Utilities
    {
        /// <summary>
        /// 将DataTable转换为指定类型的实体列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="dataTable">要转换的DataTable</param>
        /// <returns>实体列表</returns>
        public static List<T> DataTableToList<T>(this DataTable dataTable) where T : class, new()
        {
            // 验证输入
            if (dataTable == null || dataTable.Rows.Count == 0)
                return new List<T>();

            var result = new List<T>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var columnNames = new HashSet<string>(dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));

            foreach (DataRow row in dataTable.Rows)
            {
                // 创建实体实例
                var item = new T();

                foreach (var property in properties)
                {
                    // 检查DataTable是否包含与属性名匹配的列
                    if (!columnNames.Contains(property.Name))
                        continue;

                    // 获取单元格值
                    var value = row[property.Name];

                    // 处理DBNull值
                    if (value == DBNull.Value)
                        continue;

                    try
                    {
                        // 转换值类型并设置属性
                        var convertedValue = Convert.ChangeType(value, property.PropertyType);
                        property.SetValue(item, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        // 可以记录转换错误日志
                        // 例如: Logger.LogError($"转换属性 {property.Name} 时出错: {ex.Message}");
                        throw new InvalidCastException($"无法将列 {property.Name} 的值 {value} 转换为类型 {property.PropertyType}", ex);
                    }
                }

                result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// 将实体转换为字典
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体对象</param>
        /// <returns>包含实体属性和值的字典</returns>
        public static Dictionary<string, object> ToDictionary<T>(this T entity) where T : class
        {
            if (entity == null) return null;

            var dictionary = new Dictionary<string, object>();
            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (property.CanRead)
                {
                    dictionary[property.Name] = property.GetValue(entity);
                }
            }

            return dictionary;
        }
        /// <summary>
        /// 忽略指定属性，复制其他属性到目标实体
        /// </summary>
        public static void CopyPropertiesTo<T>(this T source, T target, params string[] ignoreProperties) where T : class
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (ignoreProperties == null) ignoreProperties = Array.Empty<string>();

            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (property.CanRead && property.CanWrite &&
                    !ignoreProperties.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                {
                    object value = property.GetValue(source);
                    property.SetValue(target, value);
                }
            }
        }
        /// <summary>
        /// 创建实体的深层副本
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="source">源实体</param>
        /// <returns>新的实体副本</returns>
        public static T Clone<T>(this T source) where T : class, new()
        {
            if (source == null) return null;

            T clone = new T();
            source.CopyPropertiesTo(clone);
            return clone;
        }
        /// <summary>
        /// 分页查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static PagedResult<T> PageEach<T>(this IEnumerable<T> source, int pageIndex, int pageSize)
        {
            // 参数验证
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (pageIndex < 1)
                throw new ArgumentOutOfRangeException(nameof(pageIndex), "页码必须大于等于1");
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "每页条数必须大于等于1");

            // 转换为列表（避免多次枚举）
            var list = source.ToList();
            int totalCount = list.Count;

            // 计算总页数（处理0条数据的情况）
            int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

            // 处理页码超出范围的情况（总页数为0时保持pageIndex=1）
            if (totalPages > 0 && pageIndex > totalPages)
                pageIndex = pageIndex;

            // 获取当前页数据（总页数为0时返回空列表）
            var data = totalPages == 0
                ? new List<T>()
                : list.Skip((pageIndex - 1) * pageSize)
                     .Take(pageSize)
                     .ToList();

            return new PagedResult<T>
            {
                Data = data,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }
        /// <summary>
        /// 获取分页数据结果
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class PagedResult<T>
        {
            /// <summary>
            /// 当前页数据
            /// </summary>
            public List<T> Data { get; set; } = new List<T>();

            /// <summary>
            /// 当前页码（从1开始）
            /// </summary>
            public int PageIndex { get; set; }

            /// <summary>
            /// 每页条数
            /// </summary>
            public int PageSize { get; set; }

            /// <summary>
            /// 总记录数
            /// </summary>
            public int TotalCount { get; set; }

            /// <summary>
            /// 总页数
            /// </summary>
            public int TotalPages { get; set; }

            /// <summary>
            /// 是否有上一页
            /// </summary>
            public bool HasPreviousPage => PageIndex > 1;

            /// <summary>
            /// 是否有下一页
            /// </summary>
            public bool HasNextPage => PageIndex < TotalPages;
        
        }
        /// <summary>
        /// 根据条件筛选DataTable中的行
        /// </summary>
        /// <param name="dt">源DataTable</param>
        /// <param name="condition">筛选条件</param>
        /// <returns>筛选后的新DataTable</returns>
        public static DataTable Filter(this DataTable dt, Func<DataRow, bool> condition)
        {
            if (dt == null || dt.Rows.Count == 0)
                return dt.Clone();

            var filteredRows = dt.Rows.Cast<DataRow>().Where(condition).ToArray();
            var result = dt.Clone();

            foreach (var row in filteredRows)
            {
                result.ImportRow(row);
            }
            return result;
        }
        public static string RemoveBracketContentByReplace(string input)
        {
            // 空值校验
            if (string.IsNullOrEmpty(input)) return input;

            // 找到左括号位置
            int bracketIndex = input.IndexOf('(');
            if (bracketIndex > -1)
            {
                // 构造要替换的子串（括号及后面所有内容）
                string toReplace = input.Substring(bracketIndex);
                // 替换并清理空格
                input = input.Replace(toReplace, "").Trim();
            }

            return input;
        }
        // <summary>
        /// 保存数据到文件缓存中
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expiration">缓存有效期</param>
        public static void SaveToFileCache(string key, string value,TimeSpan timeSpan)
        {
            var cacheDir = Path.Combine(AppContext.BaseDirectory, "Cache");
            if (string.IsNullOrEmpty(AppContext.BaseDirectory))
            {
                 cacheDir = "E:\\aa\\TestProject1\\ProjectFiles\\NetSolution\\Cache";
            }
            Directory.CreateDirectory(cacheDir);

            var filePath = Path.Combine(cacheDir, key);
            var content = $"{value}|{DateTime.Now.Add(timeSpan):o}"; // 包含过期时间

            File.WriteAllText(filePath, content);
        }
        /// <summary>
        /// 从文件缓存中获取数据
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string? GetFromFileCache(string key)
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Cache", key);
            if (!File.Exists(filePath)) return null;

            var content = File.ReadAllText(filePath);
            var parts = content.Split('|');
            if (parts.Length != 2) return null;

            if (DateTime.TryParse(parts[1], out var expiration) && expiration > DateTime.Now)
            {
                return parts[0]; // 未过期，返回值
            }

            File.Delete(filePath); // 已过期，删除缓存
            return null;
        }
        /// <summary>
        /// 树节点类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class TreeNode<T>
        {
            // 节点ID
            public int Id { get; set; }

            // 父节点ID
            public int ParentId { get; set; }

            // 节点数据
            public T Data { get; set; }

            // 子节点集合
            public List<TreeNode<T>> Children { get; set; } = new List<TreeNode<T>>();

            // 构造函数
            public TreeNode(int id, int parentId, T data)
            {
                Id = id;
                ParentId = parentId;
                Data = data;
            }
        }
        /// <summary>
        /// 将扁平列表转换为树形结构
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public static List<TreeNode<T>> BuildTree<T>(List<TreeNode<T>> nodes)
        {
            // 查找所有根节点（ParentId为0或不存在父节点）
            var rootNodes = nodes.Where(n => n.ParentId == 0 || !nodes.Any(p => p.Id == n.ParentId))
                                .ToList();

            // 为每个根节点递归添加子节点
            foreach (var root in rootNodes)
            {
                AddChildren(root, nodes);
            }

            return rootNodes;
        }
        /// <summary>
        /// 递归添加子节点
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parentNode"></param>
        /// <param name="allNodes"></param>
        private static void AddChildren<T>(TreeNode<T> parentNode, List<TreeNode<T>> allNodes)
        {
            // 查找当前节点的直接子节点
            var children = allNodes.Where(n => n.ParentId == parentNode.Id).ToList();

            // 添加子节点
            parentNode.Children.AddRange(children);

            // 递归为每个子节点添加它们的子节点
            foreach (var child in children)
            {
                AddChildren(child, allNodes);
            }
        }
        /// <summary>
        /// 查找满足条件的节点 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static List<TreeNode<T>> FindNodes<T>(TreeNode<T> node, Func<TreeNode<T>, bool> predicate)
        {
            var result = new List<TreeNode<T>>();

            if (node == null) return result;

            // 如果满足条件，添加到结果集
            if (predicate(node))
                result.Add(node);

            // 递归查找子节点
            foreach (var child in node.Children)
            {
                result.AddRange(FindNodes(child, predicate));
            }

            return result;
        }

        /// <summary>
        /// 安全获取值，如果键不存在则返回默认值
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>获取到的值或默认值</returns>
        public static TValue SafeGetValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            if (dictionary == null)
                return defaultValue;

            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
        /// <summary>
        /// 添加或更新键值对，如果键已存在则更新值
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">字典</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
            else
            {
                dictionary.Add(key, value);
            }
        }
        /// <summary>
        /// 过滤字典，保留符合条件的键值对
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="dictionary">原字典</param>
        /// <param name="predicate">过滤条件</param>
        /// <returns>过滤后的新字典</returns>
        public static Dictionary<TKey, TValue> Filter<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary,
            Func<KeyValuePair<TKey, TValue>, bool> predicate)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return dictionary.Where(predicate).ToDictionary(item => item.Key, item => item.Value);
        }
        /// <summary>
        /// Excel文件格式枚举
        /// </summary>
        public enum ExcelFormat
        {
            Xls,   // .xls格式（Excel 97-2003）
            Xlsx   // .xlsx格式（Excel 2007+）
        }
        /// <summary>
        /// 通过特性获取类属性的中文描述（无需文件路径）
        /// </summary>
        /// <param name="type">目标类的Type</param>
        /// <returns>属性名→中文描述的字典</returns>
        public static Dictionary<string, string> GetColumnMappings(Type type)
        {
            var columnMappings = new Dictionary<string, string>();

            // 获取类的所有公共属性
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                // 读取属性上的ColumnDescription特性
                var attribute = prop.GetCustomAttribute < ExcelColumnDescription> ();

                // 若有特性，使用特性中的描述；否则使用属性名
                string description = attribute?.Description ?? prop.Name;
                columnMappings[prop.Name] = description;
            }

            return columnMappings;
        }
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
        /// <summary>
        /// 导出列表数据到Excel文件
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="dataList">数据列表</param>
        /// <param name="filePath">保存路径</param>
        /// <param name="sheetName">工作表名称</param>
        /// <param name="headerMappings">表头映射（属性名 -> 显示名称）</param>
        /// <param name="excludeProperties">需要排除的属性名</param>
        /// <param name="dateTimeFormat">日期时间格式</param>
        /// <param name="format">Excel文件格式</param>
        public static void ExportToExcel<T>(List<T> dataList, string filePath, string sheetName = "数据列表",
            Dictionary<string, string> headerMappings = null, List<string> excludeProperties = null,
            string dateTimeFormat = "yyyy-MM-dd HH:mm:ss", ExcelFormat format = ExcelFormat.Xlsx) where T : class
        {
            if (dataList == null || !dataList.Any())
                throw new ArgumentException("数据列表不能为空", nameof(dataList));

            // 创建工作簿
            IWorkbook workbook = CreateWorkbook(format);

            // 创建工作表
            ISheet sheet = workbook.CreateSheet(sheetName);

            // 获取可导出的属性
            var properties = GetExportableProperties<T>(excludeProperties);

            // 创建表头样式
            ICellStyle headerStyle = CreateHeaderStyle(workbook);

            // 创建数据单元格样式
            ICellStyle dataStyle = CreateDataStyle(workbook);

            // 设置表头
            SetHeaders(sheet, properties, headerMappings, headerStyle);

            // 填充数据
            FillData(sheet, dataList, properties, dateTimeFormat, dataStyle);

            // 自动调整列宽
            AutoFitColumns(sheet, properties.Count);

            // 保存到文件
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }
        }
        /// <summary>
        /// 创建工作簿（根据格式）
        /// </summary>
        private static IWorkbook CreateWorkbook(ExcelFormat format)
        {
            return format == ExcelFormat.Xls
                ? new HSSFWorkbook()
                : (IWorkbook)new XSSFWorkbook();
        }
        /// <summary>
        /// 获取可导出的属性
        /// </summary>
        private static List<PropertyInfo> GetExportableProperties<T>(List<string> excludeProperties) where T : class
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToList();

            if (excludeProperties != null && excludeProperties.Any())
            {
                properties = properties
                    .Where(p => !excludeProperties.Contains(p.Name))
                    .ToList();
            }

            return properties;
        }

        /// <summary>
        /// 创建表头样式
        /// </summary>
        private static ICellStyle CreateHeaderStyle(IWorkbook workbook)
        {
            ICellStyle style = workbook.CreateCellStyle();

            // 设置背景色
            style.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;
            style.FillPattern = FillPattern.SolidForeground;

            // 设置边框
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;

            // 设置字体
            IFont font = workbook.CreateFont();
            font.Boldweight = (short)FontBoldWeight.Bold;
            style.SetFont(font);

            return style;
        }
        /// <summary>
        /// 创建数据单元格样式
        /// </summary>
        private static ICellStyle CreateDataStyle(IWorkbook workbook)
        {
            ICellStyle style = workbook.CreateCellStyle();

            // 设置边框
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;

            return style;
        }
        /// <summary>
        /// 设置表头
        /// </summary>
        private static void SetHeaders(ISheet sheet, List<PropertyInfo> properties,
            Dictionary<string, string> headerMappings, ICellStyle headerStyle)
        {
            IRow headerRow = sheet.CreateRow(0); // 表头行（第一行）

            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                ICell cell = headerRow.CreateCell(i);

                // 设置表头文本
                cell.SetCellValue(headerMappings != null && headerMappings.ContainsKey(property.Name)
                    ? headerMappings[property.Name]
                    : property.Name);

                // 应用表头样式
                cell.CellStyle = headerStyle;
            }
        }
        /// <summary>
        /// 填充数据
        /// </summary>
        private static void FillData<T>(ISheet sheet, List<T> dataList, List<PropertyInfo> properties,
            string dateTimeFormat, ICellStyle dataStyle) where T : class
        {
            // 获取工作簿对象，用于创建新样式
            IWorkbook workbook = sheet.Workbook;

            for (int rowIndex = 0; rowIndex < dataList.Count; rowIndex++)
            {
                int excelRowNum = rowIndex + 1; // 数据从第二行开始（0是表头）
                IRow dataRow = sheet.CreateRow(excelRowNum);
                var item = dataList[rowIndex];

                for (int colIndex = 0; colIndex < properties.Count; colIndex++)
                {
                    ICell cell = dataRow.CreateCell(colIndex);
                    var property = properties[colIndex];
                    var value = property.GetValue(item);

                    // 处理不同数据类型
                    if (value is DateTime dateValue)
                    {
                        // 1. 设置日期值
                        cell.SetCellValue(dateValue);

                        // 2. 创建全新的日期样式（关键修复）
                        // 克隆dataStyle保留边框等基础样式，再叠加日期格式
                        ICellStyle dateStyle = workbook.CreateCellStyle();
                        dateStyle.CloneStyleFrom(dataStyle); // 继承原有数据样式（如边框）

                        // 3. 应用自定义日期格式
                        IDataFormat dataFormat = workbook.CreateDataFormat();
                        dateStyle.DataFormat = dataFormat.GetFormat(dateTimeFormat);

                        // 4. 为单元格指定样式
                        cell.CellStyle = dateStyle;
                    }
                    else if (value is bool boolValue)
                    {
                        cell.SetCellValue(boolValue ? "是" : "否");
                        cell.CellStyle = dataStyle; // 应用基础样式
                    }
                    else if (value is int || value is long || value is decimal || value is double)
                    {
                        // 数字类型
                        if (value != null)
                        {
                            cell.SetCellValue(Convert.ToDouble(value));
                        }
                        cell.CellStyle = dataStyle; // 应用基础样式
                    }
                    else
                    {
                        // 字符串类型
                        cell.SetCellValue(value?.ToString() ?? string.Empty);
                        cell.CellStyle = dataStyle; // 应用基础样式
                    }
                }
            }
        }

        /// <summary>
        /// 自动调整列宽
        /// </summary>
        private static void AutoFitColumns(ISheet sheet, int columnCount)
        {
            for (int i = 0; i < columnCount; i++)
            {
                sheet.AutoSizeColumn(i);
                // 适当调整宽度，避免过窄
                if (sheet.GetColumnWidth(i) < 256 * 10) // 最小宽度10个字符
                {
                    sheet.SetColumnWidth(i, 256 * 10);
                }
            }
        }
        /// <summary>
        /// 隐藏窗体LoginWindow中的指定控件
        /// </summary>
        public static void HideLoginWindowControls() {
            try
            {
                // 1. 统一控件路径前缀（避免重复拼接）
                string controlPrefix = "UI/LoginWindow/";
                // 2. 循环遍历1/2（对应控件后缀）
                for (int i = 1; i <= 2; i++)
                {
                    // -------------------- 处理Button --------------------
                    string buttonPath = string.Format("{0}Button{1}", controlPrefix, i); // 替换$插值，兼容FT Optix
                    var targetButton = Project.Current.Get<Button>(buttonPath);
                    if (targetButton != null)
                    {
                        targetButton.Visible = false;
                        //Log.Info($"隐藏按钮：{buttonPath}");
                    }
                    else
                    {
                        //Log.Error($"未找到按钮：{buttonPath}");
                    }

                    // -------------------- 处理TextBox（修正为InputField） --------------------
                    string textPath = string.Format("{0}TextBox{1}", controlPrefix, i);
                    var targetText = Project.Current.Get<FTOptix.UI.TextBox>(textPath); // FT Optix文本框是InputField
                    if (targetText != null)
                    {
                        targetText.Visible = false;
                      
                    }
                    else
                    {
                      
                    }

                    // -------------------- 处理Label --------------------
                    string labelPath = string.Format("{0}Label{1}", controlPrefix, i);
                    var targetLabel = Project.Current.Get<FTOptix.UI.Label>(labelPath);
                    if (targetLabel != null)
                    {
                        targetLabel.Visible = false;
                       
                    }
                    else
                    {
                       
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"批量隐藏LoginWindow控件失败：{ex.Message}");
            }
        }
        /// <summary>
        /// 弹出框显示消息
        /// </summary>
        /// <param name="message"></param>
        public static void MessageboxShow(string message) {
            Project.Current.GetVariable("Model/Component/message").Value =string.Empty; Project.Current.GetVariable("Model/Component/message").Value = "";
            Project.Current.GetVariable("Model/Component/message").Value =message;
  

        }
    }
}


