using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
            string originalXmlPath = @"E:\generate\generatexml\Things_TS.Module.TDTWireBoxWeldingMachineM1001.Alarm.Thing.xml"; // 原始RemoteThing.xml路径
            string outputDirectory = @"E:\generate\generatexml\create_thing"; // 输出目录
            string originalNumber = "1001"; // 原始编号
            string[] targetNumbers = { "1002", "1003","1004","1005", "1006", "1007", "1008", "1009", "1010" }; // 目标编号
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
            string originalXmlPath = @"E:\generate\generatexml\Things_TS.Module.TDTWireBoxWeldingMachineM1001.Alarm.RemoteThing.xml"; // 原始RemoteThing.xml路径
            string outputDirectory = @"E:\generate\generatexml\create_remote_thing"; // 输出目录
            string originalNumber = "1001"; // 原始编号
            string[] targetNumbers = { "1002", "1003","1004", "1005","1006", "1007", "1008", "1009", "1010" }; // 目标编号
            //提取出TDTLABELLERM这部分去掉1001
    
            string moduleName = originalXmlPath.Split('.')[2];
            // 使用正则表达式替换所有数字字符为空
           string devcidata=    Regex.Replace(moduleName, @"\d", string.Empty);
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
    }
}
