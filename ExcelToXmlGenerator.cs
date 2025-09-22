using Microsoft.VisualBasic;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TestProject1
{
    /// <summary>
    /// 从Excel生成XML配置文件的工具类
    /// </summary>
    public class ExcelToXmlGenerator
    {
        // 常量定义 - 集中管理固定值，便于维护
        private const int InitialOrdinal = 2; // 起始序号（参考示例XML的ordinal规则）
        private const string MonitorSuffix = "Monitor"; // 监控名称后缀

        /// <summary>
        /// 从Excel生成XML的主方法
        /// </summary>
        /// <param name="excelPath">Excel文件路径</param>
        /// <param name="xmlPath">源XML文件路径</param>
        /// <param name="outputXmlPath">输出XML文件路径，默认覆盖源文件</param>
        public static void GenerateXmlFromExcel(string excelPath, string xmlPath, string outputXmlPath = null)
        {
            try
            {
                // 处理输出路径，默认覆盖原文件
                outputXmlPath ??= xmlPath;

                Console.WriteLine("开始处理...");
                Console.WriteLine($"Excel路径: {excelPath}");
                Console.WriteLine($"XML源路径: {xmlPath}");
                Console.WriteLine($"XML输出路径: {outputXmlPath}");

                // 1. 读取Excel数据并验证
                var alarmList = ReadExcelData(excelPath);
                ValidateAlarmList(alarmList);
                Console.WriteLine($"成功读取 {alarmList.Count} 条告警数据");

                // 2. 加载并验证XML文档
                var xmlDoc = LoadAndValidateXmlDocument(xmlPath);
                Console.WriteLine("成功加载XML文档");

                // 3. 定位目标节点
                var (alertConfigurationsNode, propertyDefinitionsNode) = FindTargetNodes(xmlDoc);
                Console.WriteLine("成功定位目标节点");

                // 4. 清空原有节点（如需保留历史数据，可修改此逻辑）
                ClearExistingNodes(alertConfigurationsNode, propertyDefinitionsNode);
                Console.WriteLine("已清空原有节点数据");

                // 5. 生成并添加新节点
                GenerateAndAddNewNodes(alarmList, alertConfigurationsNode, propertyDefinitionsNode);
                Console.WriteLine("已生成并添加新节点");

                // 6. 保存修改后的XML
                SaveXmlDocument(xmlDoc, outputXmlPath);
                Console.WriteLine($"XML更新成功，路径：{outputXmlPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"操作失败：{ex.Message}");
                // 如需调试可添加堆栈信息
                // Console.WriteLine($"详细错误：{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 读取Excel数据
        /// </summary>
        private static List<AlarmInfo> ReadExcelData(string excelPath)
        {
            var result = new List<AlarmInfo>();

            if (!File.Exists(excelPath))
                throw new FileNotFoundException("Excel文件不存在", excelPath);

            // 根据文件后缀创建对应的Workbook
            IWorkbook workbook;
            using (var stream = new FileStream(excelPath, FileMode.Open, FileAccess.Read))
            {
                if (excelPath.EndsWith(".xlsx"))
                    workbook = new XSSFWorkbook(stream); // .xlsx格式
                else if (excelPath.EndsWith(".xls"))
                    workbook = new HSSFWorkbook(stream); // .xls格式
                else
                    throw new NotSupportedException("不支持的Excel格式（仅支持.xls和.xlsx）");
            }

            // 读取第一个工作表
            ISheet sheet = workbook.GetSheetAt(0);
            if (sheet == null)
                throw new Exception("Excel中无工作表");

            // 查找表头列索引（基础列：告警定义、告警描述、字段类型）
            int nameColIndex = -1;
            int descColIndex = -1;
            int typeColIndex = -1;

            IRow headerRow = sheet.GetRow(0); // 表头行（第1行，索引0）
            if (headerRow == null)
                throw new Exception("Excel中未找到表头行");

            for (int col = 0; col < headerRow.LastCellNum; col++)
            {
                ICell cell = headerRow.GetCell(col);
                if (cell == null) continue;

                string cellValue = cell.StringCellValue.Trim();
                if (cellValue == "告警定义")
                    nameColIndex = col;
                else if (cellValue == "告警描述")
                    descColIndex = col;
                else if (cellValue == "字段类型")
                    typeColIndex = col;
            }

            // 验证表头是否完整
            if (nameColIndex == -1 || descColIndex == -1 || typeColIndex == -1)
                throw new Exception("Excel表头必须包含：告警定义、告警描述、字段类型");

            // 读取数据行（从第2行开始，索引1）
            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                IRow dataRow = sheet.GetRow(rowIndex);
                if (dataRow == null) continue;

                // 读取“告警定义”（必填）
                string name = GetCellValue(dataRow.GetCell(nameColIndex));
                if (string.IsNullOrWhiteSpace(name))
                    continue; // 跳过空行

                // 读取“告警描述”和“字段类型”
                string description = GetCellValue(dataRow.GetCell(descColIndex));
                string baseType = GetCellValue(dataRow.GetCell(typeColIndex));

                // 字段类型为空时默认STRING
                if (string.IsNullOrWhiteSpace(baseType))
                    baseType = "STRING";

                result.Add(new AlarmInfo
                {
                    Name = name,
                    Description = description,
                    BaseType = baseType
                    // 如需从Excel读取AlertType和Priority，可在此扩展
                });
            }

            return result;
        }
        /// <summary>
        /// 获取单元格的值（兼容不同数据类型）
        /// </summary>
        private static string GetCellValue(ICell cell)
        {
            if (cell == null)
                return string.Empty;

            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue.Trim();
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                        return cell.DateCellValue.ToString();
                    else
                        return cell.NumericCellValue.ToString();
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                default:
                    return string.Empty;
            }
        }


        /// <summary>
        /// 验证告警列表数据
        /// </summary>
        private static void ValidateAlarmList(List<AlarmInfo> alarmList)
        {
            if (alarmList == null || alarmList.Count == 0)
                throw new Exception("Excel中未读取到有效数据");

            // 检查必填字段
            var invalidItems = alarmList
                .Where(a => string.IsNullOrWhiteSpace(a.Name) ||
                           string.IsNullOrWhiteSpace(a.BaseType) ||
                           string.IsNullOrWhiteSpace(a.AlertType))
                .ToList();

            if (invalidItems.Any())
                throw new Exception($"发现{invalidItems.Count}条无效数据：告警名称、基础类型和告警类型不能为空");
        }

        /// <summary>
        /// 加载并验证XML文档
        /// </summary>
        private static XDocument LoadAndValidateXmlDocument(string xmlPath)
        {
            try
            {
                if (!File.Exists(xmlPath))
                    throw new FileNotFoundException("XML文件不存在", xmlPath);

                return XDocument.Load(xmlPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"加载XML文档失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 查找目标节点并验证
        /// </summary>
        private static (XElement alertConfigs, XElement propertyDefinitions) FindTargetNodes(XDocument xmlDoc)
        {
            var alertConfigs = xmlDoc.Descendants("AlertConfigurations").FirstOrDefault();
            var propertyDefinitions = xmlDoc.Descendants("ThingShape")
                                            .Elements("PropertyDefinitions")
                                            .FirstOrDefault();

            if (alertConfigs == null)
                throw new Exception("XML中未找到AlertConfigurations节点");

            if (propertyDefinitions == null)
                throw new Exception("XML中未找到PropertyDefinitions节点");

            return (alertConfigs, propertyDefinitions);
        }

        /// <summary>
        /// 清空现有节点
        /// </summary>
        private static void ClearExistingNodes(XElement alertConfigurations, XElement propertyDefinitions)
        {
            alertConfigurations.RemoveAll();
            propertyDefinitions.RemoveAll();
        }

        /// <summary>
        /// 生成并添加新节点
        /// </summary>
        private static void GenerateAndAddNewNodes(List<AlarmInfo> alarmList,
                                          XElement alertConfigurationsNode,
                                          XElement propertyDefinitionsNode)
        {
            int ordinal = InitialOrdinal;

            foreach (var alarm in alarmList)
            {
                // 添加AlertDefinitions节点
                var alertDefinitions = CreateAlertDefinitions(alarm);
                alertConfigurationsNode.Add(alertDefinitions);

                // 添加PropertyDefinition节点
                var propertyDefinition = CreatePropertyDefinition(alarm, ordinal);
                propertyDefinitionsNode.Add(propertyDefinition);

                ordinal++;
            }
        }

        /// <summary>
        /// 创建AlertDefinitions节点
        /// </summary>
        private static XElement CreateAlertDefinitions(AlarmInfo alarm)
        {
            var alertDefinitions = new XElement("AlertDefinitions",
                new XAttribute("name", alarm.Name)
            );

            // 根据基础类型创建不同的AlertDefinition
            if (IsBooleanType(alarm.BaseType))
            {
                alertDefinitions.Add(CreateBooleanAlertDefinition(alarm));
            }
            else
            {
                alertDefinitions.Add(CreateNonBooleanAlertDefinition(alarm));
            }

            return alertDefinitions;
        }

        /// <summary>
        /// 创建布尔类型的AlertDefinition
        /// </summary>
        private static XElement CreateBooleanAlertDefinition(AlarmInfo alarm)
        {
            return new XElement("AlertDefinition",
                new XAttribute("alertType", alarm.AlertType),
                new XAttribute("description", alarm.Description),
                new XAttribute("enabled", "true"),
                new XAttribute("name", $"{alarm.Name}{MonitorSuffix}"),
                new XAttribute("priority", alarm.Priority),
                new XAttribute("propertyBaseType", alarm.BaseType),
                // AlertAttributes子节点
                CreateAlertAttributes(alarm.BaseType, "true", false)
            );
        }

        /// <summary>
        /// 创建非布尔类型的AlertDefinition
        /// </summary>
        private static XElement CreateNonBooleanAlertDefinition(AlarmInfo alarm)
        {
            (string valueContent, bool isCData) = GetValueContentByBaseType(alarm.BaseType);

            return new XElement("AlertDefinition",
                new XAttribute("alertType", alarm.AlertType),
                new XAttribute("description", alarm.Description),
                new XAttribute("enabled", "true"),
                new XAttribute("name", $"{alarm.Name}{MonitorSuffix}"),
                new XAttribute("priority", alarm.Priority),
                new XAttribute("propertyBaseType", alarm.BaseType),
                CreateAlertAttributes(alarm.BaseType, valueContent, isCData)
            );
        }

        /// <summary>
        /// 创建AlertAttributes节点
        /// </summary>
        private static XElement CreateAlertAttributes(string baseType, string valueContent, bool isCData)
        {
            return new XElement("AlertAttributes",
                new XElement("DataShape",
                    new XElement("FieldDefinitions",
                        new XElement("FieldDefinition",
                            GetFieldDefinitionAttributes(baseType),
                            new XAttribute("description", "value"),
                            new XAttribute("name", "value"),
                            new XAttribute("ordinal", "0")
                        )
                    )
                ),
                new XElement("Rows",
                    new XElement("Row",
                        CreateValueElement(valueContent, isCData)
                    )
                )
            );
        }

        /// <summary>
        /// 创建PropertyDefinition节点
        /// </summary>
        private static XElement CreatePropertyDefinition(AlarmInfo alarm, int ordinal)
        {
            return new XElement("PropertyDefinition",
                new XAttribute("aspect.cacheTime", "0.0"),
                new XAttribute("aspect.dataChangeType", "VALUE"),
                new XAttribute("aspect.isPersistent", "true"),
                new XAttribute("baseType", alarm.BaseType),
                new XAttribute("category", ""),
                new XAttribute("description", alarm.Description),
                new XAttribute("isLocalOnly", "false"),
                new XAttribute("name", alarm.Name),
                new XAttribute("ordinal", ordinal.ToString())
            );
        }

        /// <summary>
        /// 保存XML文档
        /// </summary>
        private static void SaveXmlDocument(XDocument xmlDoc, string outputPath)
        {
            try
            {
                // 确保输出目录存在
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                    Console.WriteLine($"已创建输出目录: {outputDir}");
                }

                xmlDoc.Save(outputPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存XML文件失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 判断是否为布尔类型
        /// </summary>
        private static bool IsBooleanType(string baseType)
        {
            return string.Equals(baseType, "BOOLEAN", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 根据基础类型获取值内容
        /// </summary>
        private static (string value, bool isCData) GetValueContentByBaseType(string baseType)
        {
            return baseType.ToUpperInvariant() switch
            {
                "STRING" => (" 1 ", true),
                "NUMBER" => ("1.0", false),
                "DATETIME" => ("2025-09-17T00:00:00.000+08:00", false),
                "LONG" => ("1", false),
                _ => throw new NotSupportedException($"不支持的基础类型: {baseType}")
            };
        }

        /// <summary>
        /// 获取字段定义的属性集合
        /// </summary>
        private static IEnumerable<XAttribute> GetFieldDefinitionAttributes(string baseType)
        {
            var attributes = new List<XAttribute>
        {
            new XAttribute("aspect.friendlyName", "Value"),
            new XAttribute("aspect.isRequired", "true"),
            new XAttribute("baseType", baseType)
        };

            // 布尔类型需要默认值属性
            if (IsBooleanType(baseType))
            {
                attributes.Add(new XAttribute("aspect.defaultValue", "false"));
            }

            return attributes;
        }

        /// <summary>
        /// 创建值元素（根据需要使用CDATA）
        /// </summary>
        private static XElement CreateValueElement(string content, bool isCData)
        {
            return isCData
                ? new XElement("value", new XCData(content))
                : new XElement("value", content);
        }
    }
}
