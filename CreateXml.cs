using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace TestProject1
{
    /// <summary>
    /// 批量生成Thing.xml和RemoteThing.xml
    /// </summary>
    public class CreateXml
    {
        /// <summary>
        /// 批量生成Thing.xml
        /// </summary>
        public static void CreateThingXml()
        {
            // 配置参数（根据RemoteThing实际情况调整路径）
            string originalXmlPath = @"E:\generate\generatexml\Things_TS.Module.TDTLAMINATEDREFLUXLINEM1001.Alarm.Thing.xml"; // 原始RemoteThing.xml路径
            string outputDirectory = @"E:\generate\generatexml\create_thing"; // 输出目录
            string originalNumber = "1001"; // 原始编号
            string[] targetNumbers = { "1002", "1003", "1004", "1005", "1006", "1007", "1008", "1009", "1010", "1011", "1012", "1013", "1014", "1015" }; // 目标编号
            string moduleName = originalXmlPath.Split('.')[2];
            // 使用正则表达式替换所有数字字符为空
            string devcidata = Regex.Replace(moduleName, @"\d", string.Empty);
            try
            {


                // 加载原始XML
                XDocument originalDoc = XDocument.Load(originalXmlPath);


                foreach (string targetNumber in targetNumbers)
                {
                    // 创建文档副本避免影响原始文档
                    XDocument newDoc = new XDocument(originalDoc);

                    // 1. 替换Thing节点的name属性
                    var thingNode = newDoc.Descendants("Thing").FirstOrDefault();
                    if (thingNode != null && thingNode.Attribute("name") != null)
                    {
                        thingNode.Attribute("name").Value =
                            thingNode.Attribute("name").Value.Replace(originalNumber, targetNumber);
                    }

                    // 2. 替换Thing节点的description属性
                    if (thingNode != null && thingNode.Attribute("description") != null)
                    {
                        thingNode.Attribute("description").Value =
                            thingNode.Attribute("description").Value.Replace(originalNumber, targetNumber);
                    }

                    // 3. 替换所有PropertyBinding的sourceName和sourceThingName
                    foreach (var propBinding in newDoc.Descendants("PropertyBinding"))
                    {
                        // 替换sourceName
                        if (propBinding.Attribute("sourceName") != null)
                        {
                            propBinding.Attribute("sourceName").Value =
                                propBinding.Attribute("sourceName").Value.Replace(originalNumber, targetNumber);
                        }

                        // 替换sourceThingName
                        if (propBinding.Attribute("sourceThingName") != null)
                        {
                            propBinding.Attribute("sourceThingName").Value =
                                propBinding.Attribute("sourceThingName").Value.Replace(originalNumber, targetNumber);
                        }
                    }

                    // 4. 替换equipmentNo的值
                    var equipmentNoValue = newDoc.Descendants("ThingProperties")
                                                .Elements("equipmentNo")
                                                .Elements("Value")
                                                .FirstOrDefault();
                    if (equipmentNoValue != null)
                    {
                        equipmentNoValue.Value = equipmentNoValue.Value.Replace(originalNumber, targetNumber);
                    }

                    // 5. 替换ConfigurationChange中的相关内容
                    foreach (var configChange in newDoc.Descendants("ConfigurationChange"))
                    {
                        if (configChange.Attribute("changeReason") != null)
                        {
                            configChange.Attribute("changeReason").Value =
                                configChange.Attribute("changeReason").Value.Replace(originalNumber, targetNumber);
                        }
                    }

                    string outputFileName = $"Things_TS.Module.{devcidata}{targetNumber}.Alarm.Thing.xml";
                    string outputPath = Path.Combine(outputDirectory, outputFileName);

                    // 配置XML保存格式（保持与原始格式一致）
                    var settings = new XmlWriterSettings
                    {
                        Encoding = Encoding.UTF8,
                        Indent = true,
                        IndentChars = "  ",
                        OmitXmlDeclaration = false,
                        NewLineHandling = NewLineHandling.None
                    };

                    // 保存生成的XML文件
                    using (var writer = XmlWriter.Create(outputPath, settings))
                    {
                        newDoc.Save(writer);
                    }

                    Console.WriteLine($"已生成RemoteThing文件: {outputPath}");
                }

                Console.WriteLine("批量替换完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理过程中发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量生成RemoteThing.xml
        /// </summary>
        public static void CreateRemoteThingXml()
        {
            // 配置参数（根据RemoteThing实际情况调整路径）
            string originalXmlPath = @"E:\generate\generatexml\Things_TS.Module.TDTLAMINATEDREFLUXLINEM1001.Alarm.RemoteThing.xml"; // 原始RemoteThing.xml路径
            string outputDirectory = @"E:\generate\generatexml\create_remote_thing"; // 输出目录
            string originalNumber = "1001"; // 原始编号
            string[] targetNumbers = { "1002", "1003", "1004", "1005", "1006", "1007", "1008", "1009", "1010", "1011", "1012", "1013", "1014", "1015", "2001" }; // 目标编号
                                                                                                                                                                 //提取出TDTLABELLERM这部分去掉1001

            string moduleName = originalXmlPath.Split('.')[2];
            // 使用正则表达式替换所有数字字符为空
            string devcidata = Regex.Replace(moduleName, @"\d", string.Empty);
            try
            {
                // 确保输出目录存在
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                    Console.WriteLine($"已创建输出目录: {outputDirectory}");
                }

                // 检查原始文件是否存在
                if (!File.Exists(originalXmlPath))
                {
                    Console.WriteLine($"错误: 找不到原始RemoteThing XML文件 - {originalXmlPath}");
                    return;
                }

                // 加载原始XML文档
                XDocument originalDoc = XDocument.Load(originalXmlPath);
                Console.WriteLine("成功加载原始RemoteThing XML文件");

                // 遍历目标编号生成对应文件
                foreach (string targetNumber in targetNumbers)
                {
                    // 复制原始文档避免修改源文件
                    XDocument newDoc = new XDocument(originalDoc);

                    // 替换XML中的编号相关内容（根据实际需要调整替换规则）
                    // 例如：替换1001、1001U、1001_U等为目标编号对应形式
                    ReplaceXmlValues(
                        newDoc,
                        originalNumber,          // 原始基础编号（如1001）
                        targetNumber,            // 目标基础编号（如1002）
                        $"{originalNumber}",    // 原始带U编号（如1001U）
                        $"{targetNumber}",      // 目标带U编号（如1002U）
                        $"{originalNumber}_",   // 原始下划线U编号（如1001_U）
                        $"{targetNumber}_"      // 目标下划线U编号（如1002_U）
                    );

                    // 生成输出文件名（根据实际命名规则调整）

                    string outputFileName = $"Things_TS.Module.{devcidata}{targetNumber}.Alarm.RemoteThing.xml";
                    string outputPath = Path.Combine(outputDirectory, outputFileName);

                    // 配置XML保存格式（保持与原始格式一致）
                    var settings = new XmlWriterSettings
                    {
                        Encoding = Encoding.UTF8,
                        Indent = true,
                        IndentChars = "  ",
                        OmitXmlDeclaration = false,
                        NewLineHandling = NewLineHandling.None
                    };

                    // 保存生成的XML文件
                    using (var writer = XmlWriter.Create(outputPath, settings))
                    {
                        newDoc.Save(writer);
                    }

                    Console.WriteLine($"已生成RemoteThing文件: {outputPath}");
                }

                Console.WriteLine("所有RemoteThing文件生成完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理RemoteThing时发生错误: {ex.Message}");
            }


        }

        /// <summary>
        /// 增强版替换XML中所有指定的值（支持更多替换对）
        /// </summary>
        static void ReplaceXmlValues(XDocument doc, string oldVal1, string newVal1, string oldVal2, string newVal2, string oldVal3, string newVal3)
        {
            // 替换所有元素的文本内容
            foreach (var element in doc.Descendants())
            {
                if (element.Value.Contains(oldVal1))
                    element.Value = element.Value.Replace(oldVal1, newVal1);
                if (element.Value.Contains(oldVal2))
                    element.Value = element.Value.Replace(oldVal2, newVal2);
                if (element.Value.Contains(oldVal3))
                    element.Value = element.Value.Replace(oldVal3, newVal3);

                // 替换所有属性值
                foreach (var attribute in element.Attributes())
                {
                    if (attribute.Value.Contains(oldVal1))
                        attribute.Value = attribute.Value.Replace(oldVal1, newVal1);
                    if (attribute.Value.Contains(oldVal2))
                        attribute.Value = attribute.Value.Replace(oldVal2, newVal2);
                    if (attribute.Value.Contains(oldVal3))
                        attribute.Value = attribute.Value.Replace(oldVal3, newVal3);
                }
            }
        }
        /// <summary>
        /// 绑定点位sourceName每次都要更改（）
        /// </summary>

        public static void PropertyBindServices()
        {
            try
            {
                // 1. 配置文件路径
                string xmlFilePath = @"E:\generate\generatexml\BindServices\Things_TS.Module.TDTLAMINATEDREFLUXLINEM2001.Alarm.Thing.xml";
                string excelFilePath = @"E:\generate\新增点位.xlsx"; // 支持.xls和.xlsx
                int nameColumnIndex = 0; // NPOI列索引从0开始（A列=0，B列=1...）0表示第一列（首列）
                int startRow = 1; // 数据开始行（0-based，跳过表头行）（表示第二行跳过表头）
                string targetPart = ExtractTargetPart(xmlFilePath);

                // 2. 读取Excel中的name列表
                List<string> names = ReadNamesFromExcel(excelFilePath, nameColumnIndex, startRow);
                if (names.Count == 0)
                {
                    Console.WriteLine("未从Excel中读取到任何name值");
                    return;
                }

                // 3. 加载XML文件（与之前逻辑一致）
                XDocument doc = XDocument.Load(xmlFilePath);

                // 4. 定位RemotePropertyBindings节点（根据提供的XML，该节点已存在）
                var remotePropertyBindings = doc.Descendants("PropertyBindings").FirstOrDefault();
                if (remotePropertyBindings == null)
                {
                    Console.WriteLine("未找到RemotePropertyBindings节点");
                    return;
                }

                // 5. 批量添加PropertyBinding元素（与之前逻辑一致）
                foreach (string name in names)
                {
                    // 去重检查
                    if (remotePropertyBindings.Elements("PropertyBinding")
                        .Any(b => b.Attribute("name")?.Value == name))
                    {
                        Console.WriteLine($"已存在name为[{name}]的绑定，跳过");
                        continue;
                    }

                    // 构造sourceName和sourceThingName（根据XML中Thing的name调整）
                    string sourceName = $"TDT_LAMINATED_REFLUX_LINE_M2001_M2001_PLC_{name}";//每次都要更改
                    string sourceThingName = targetPart + "RemoteThing";

                    // 创建并添加元素绑定点位
                    var binding = new XElement("PropertyBinding",
                        new XAttribute("name", name),
                        new XAttribute("sourceName", sourceName),
                        new XAttribute("sourceThingName", sourceThingName)
                    );
                    remotePropertyBindings.Add(binding);
                    Console.WriteLine($"已添加name为[{name}]的绑定");
                }

                // 6. 保存XML文件
                doc.Save(xmlFilePath);
                Console.WriteLine("所有绑定添加完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"操作失败：{ex.Message}");
            }

        }
        /// <summary>
        /// 从Excel读取指定列的name值
        /// </summary>
        static List<string> ReadNamesFromExcel(string filePath, int columnIndex, int startRow)
        {
            var names = new List<string>();
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Excel文件不存在", filePath);

            // 根据文件扩展名创建对应的Workbook
            IWorkbook workbook = null;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (filePath.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                    workbook = new HSSFWorkbook(stream); // .xls格式
                else if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    workbook = new XSSFWorkbook(stream); // .xlsx格式
                else
                    throw new Exception("不支持的Excel格式（仅支持.xls和.xlsx）");
            }

            // 获取第一个工作表
            ISheet sheet = workbook.GetSheetAt(0);
            if (sheet == null)
                throw new Exception("Excel中未找到工作表");

            // 遍历行（从startRow到最后一行）
            int lastRowNum = sheet.LastRowNum;
            for (int rowNum = startRow; rowNum <= lastRowNum; rowNum++)
            {
                IRow row = sheet.GetRow(rowNum);
                if (row == null) continue; // 跳过空行

                // 获取指定列的单元格
                ICell cell = row.GetCell(columnIndex);
                if (cell == null) continue; // 跳过空单元格

                // 统一转换为字符串（处理不同单元格类型）
                string name = GetCellValue(cell).Trim();
                if (!string.IsNullOrEmpty(name))
                    names.Add(name);
            }

            workbook.Close(); // 释放资源
            return names;
        }
        /// <summary>
        /// 获取单元格的文本值（兼容不同数据类型）
        /// </summary>
        static string GetCellValue(ICell cell)
        {
            if (cell == null) return string.Empty;

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue,
                CellType.Numeric =>
                    DateUtil.IsCellDateFormatted(cell) ? cell.DateCellValue.ToString() : cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.CellFormula, // 公式单元格直接取公式（或可计算结果）
                _ => string.Empty
            };
        }
        static string ExtractTargetPart(string filePath)
        {
            // 1. 获取文件名（含扩展名）
            string fileName = Path.GetFileName(filePath);
            // 2. 移除扩展名（.xml）
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            // 此时 fileNameWithoutExt 为 "Things_TS.Module.TDTLAMINATEDREFLUXLINEM2001.Alarm.Thing"

            // 3. 移除开头的 "Things_"
            string afterThings = fileNameWithoutExt.Replace("Things_", string.Empty);
            // 此时 afterThings 为 "TS.Module.TDTLAMINATEDREFLUXLINEM2001.Alarm.Thing"

            // 4. 移除结尾的 ".Thing"
            string beforeThing = afterThings.Replace(".Thing", string.Empty);
            // 此时 beforeThing 为 "TS.Module.TDTLAMINATEDREFLUXLINEM2001.Alarm"

            // 5. 拼接末尾的点
            return beforeThing + ".";
        }
    }
}
