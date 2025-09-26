using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static SQLite.SQLite3;

namespace TestProject1
{
    public class ExcelHelper
    {
        /// <summary>
        /// 导出数据到Excel
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="dataList">数据列表</param>
        /// <param name="columnMappings">列名映射（属性名 -> 中文列描述）</param>
        /// <param name="isXlsx">是否生成.xlsx格式（true）或.xls格式（false）</param>
        /// <param name="sheetName">工作表名称</param>
        /// <returns>Excel文件字节数组</returns>
        public static byte[] ExportToExcel<T>(List<T> dataList, Dictionary<string, string> columnMappings,
                                             bool isXlsx = true, string sheetName = "数据报表")
        {
            // 创建工作簿（.xlsx或.xls）
            IWorkbook workbook = isXlsx ? new XSSFWorkbook() : new HSSFWorkbook();
            ISheet sheet = workbook.CreateSheet(sheetName);

            // 获取需要导出的属性
            var properties = typeof(T).GetProperties()
                .Where(p => columnMappings.ContainsKey(p.Name))
                .ToList();

            // 1. 创建表头样式（加粗、灰色背景、边框）
            ICellStyle headerStyle = CreateHeaderStyle(workbook);

            // 2. 构建表头（第一行）
            IRow headerRow = sheet.CreateRow(0); // NPOI行索引从0开始
            for (int i = 0; i < properties.Count; i++)
            {
                ICell cell = headerRow.CreateCell(i);
                cell.SetCellValue(columnMappings[properties[i].Name]);
                cell.CellStyle = headerStyle;
            }

            // 3. 填充数据行
            ICellStyle dataCellStyle = CreateDataCellStyle(workbook);
            for (int rowIdx = 0; rowIdx < dataList.Count; rowIdx++)
            {
                IRow dataRow = sheet.CreateRow(rowIdx + 1); // 数据从第1行开始（0是表头）
                T item = dataList[rowIdx];

                for (int colIdx = 0; colIdx < properties.Count; colIdx++)
                {
                    PropertyInfo prop = properties[colIdx];
                    ICell cell = dataRow.CreateCell(colIdx);

                    // 设置单元格值（处理常见数据类型）
                    object value = prop.GetValue(item);
                    SetCellValue(cell, value);

                    cell.CellStyle = dataCellStyle;
                }
            }

            // 4. 自动调整列宽
            for (int i = 0; i < properties.Count; i++)
            {
                sheet.AutoSizeColumn(i);
                // 调整列宽（避免中文显示不全）
                int width = (int)(sheet.GetColumnWidth(i) + 256); // 增加一点余量
                sheet.SetColumnWidth(i, width > 65535 ? 65535 : width); // 最大宽度限制
            }
            // 设置自动列宽（核心修改）
            AutoFitColumns(sheet, properties.Count);
            // 5. 转换为字节数组
            using (MemoryStream ms = new MemoryStream())
            {
                workbook.Write(ms);
                return ms.ToArray();
            }
        }
        /// <summary>
        /// 导出数据到Excel文件
        /// </summary>
        public static void ExportToExcelFile<T>(List<T> dataList, Dictionary<string, string> columnMappings,
                                              string filePath, string sheetName = "数据报表")
        {
            bool isXlsx = filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);
            byte[] excelBytes = ExportToExcel(dataList, columnMappings, isXlsx, sheetName);
            File.WriteAllBytes(filePath, excelBytes);
        }
        /// <summary>
        /// 创建表头样式（加粗、灰色背景、边框）
        /// </summary>
        private static ICellStyle CreateHeaderStyle(IWorkbook workbook)
        {
            ICellStyle style = workbook.CreateCellStyle();

            // 灰色背景
            style.FillForegroundColor = IndexedColors.Grey25Percent.Index;
            style.FillPattern = FillPattern.SolidForeground;

            // 加粗字体
            IFont font = workbook.CreateFont();
            font.Boldweight = (short)FontBoldWeight.Bold;
            style.SetFont(font);

            // 边框
            SetCellBorder(style);

            return style;
        }

        /// <summary>
        /// 创建数据单元格样式（边框）
        /// </summary>
        private static ICellStyle CreateDataCellStyle(IWorkbook workbook)
        {
            ICellStyle style = workbook.CreateCellStyle();
            // 边框
            SetCellBorder(style);
            return style;
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
        /// 设置单元格边框（上下左右）
        /// </summary>
        private static void SetCellBorder(ICellStyle style)
        {
            style.BorderTop = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
        }

        /// <summary>
        /// 根据值类型设置单元格内容（处理常见类型）
        /// </summary>
        private static void SetCellValue(ICell cell, object value)
        {
            if (value == null)
            {
                cell.SetCellValue("");
                return;
            }

            switch (value)
            {
                case string str:
                    cell.SetCellValue(str);
                    break;
                case int num:
                    cell.SetCellValue(num);
                    break;
                case long num:
                    cell.SetCellValue(num);
                    break;
                case decimal num:
                    cell.SetCellValue(Convert.ToDouble(num));
                    break;
                case double num:
                    cell.SetCellValue(num);
                    break;
                case float num:
                    cell.SetCellValue(num);
                    break;
                case DateTime date:
                    cell.SetCellValue(date.ToString("yyyy-MM-dd HH:mm:ss"));
                    break;
                case bool b:
                    cell.SetCellValue(b ? "是" : "否");
                    break;
                default:
                    // 其他类型默认转为字符串
                    cell.SetCellValue(value.ToString());
                    break;
            }
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
                var attribute = prop.GetCustomAttribute<ColumnDescriptionAttribute>();

                // 若有特性，使用特性中的描述；否则使用属性名
                string description = attribute?.Description ?? prop.Name;
                columnMappings[prop.Name] = description;
            }

            return columnMappings;
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <returns>文件结果</returns>
        public static bool FileDownExcel()
        {
            try
            {

                string remoteIp = "127.0.0.1"; // 远程服务器IP
                string driveShare = "E$";
                string folderPath = "generate"; // 远程服务器上共享的文件夹名
                string fileName1 = "新增点位.xlsx";
                //string filePath = "E:\\generate\\新增点位.xlsx";
                string filePath = $@"\\{remoteIp}\{driveShare}\{folderPath}\{fileName1}";
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                string fileName = Path.GetFileName(filePath);
                string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string saveFilePath = userProfilePath + "\\Downloads\\" + fileName;
                //通过io文件流保存到本地
                using (var fileStream = new FileStream(
                  path: saveFilePath,
                  mode: FileMode.Create, // 不存在则创建，存在则覆盖
                  access: FileAccess.Write,
                  share: FileShare.None)) // 写入时禁止其他程序占用
                {
                    // 将FileContent中的字节流写入本地文件
                    fileStream.Write(fileBytes, 0, fileBytes.Length);
                    // 强制刷新缓冲区，确保数据完全写入（大文件建议加）
                    fileStream.Flush();
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.Write($"下载失败：{ex.Message}");
                return false;
         
            }
        }
  
    }


    /// <summary>
    /// 用于标记属性中文描述的特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)] // 仅允许应用在属性上
    public class ColumnDescriptionAttribute : Attribute
    {
        public string Description { get; }

        public ColumnDescriptionAttribute(string description)
        {
            Description = description;
        }
    }
    /// <summary>
    /// 测试类
    /// </summary>
    public class MyClass
    {
        [ColumnDescription("产品ID")]
        public int Id { get; set; }

        [ColumnDescription("产品名称")]
        public string Name { get; set; }

        [ColumnDescription("产品价格")]
        public decimal Price { get; set; }

        [ColumnDescription("创建时间")]
        public DateTime CreateTime { get; set; }

        [ColumnDescription("是否启用")]
        public bool IsActive { get; set; }
    }

    }
