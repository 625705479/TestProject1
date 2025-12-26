using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TestProject1.Helper
{

        /// <summary>
        /// CSV文件读取助手类
        /// 支持：自动编码检测(GBK/UTF8/UTF8-BOM)、大文件低内存读取、CSV规范解析(引号/逗号/换行)
        /// </summary>
        public static class CsvReaderHelper
        {
            #region 静态初始化：注册GBK编码（.NET Core/.NET 5+ 必须）
            static CsvReaderHelper()
            {
                // 注册GBK编码提供者（解决.NET Core中找不到GBK的问题）
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }
            #endregion

            #region 核心工具方法：自动检测文件编码（解决乱码关键）
            /// <summary>
            /// 自动检测CSV文件的编码（优先识别UTF8-BOM > UTF8 > GBK）
            /// </summary>
            /// <param name="filePath">CSV文件路径</param>
            /// <returns>匹配的编码</returns>
            /// <exception cref="FileNotFoundException">文件不存在时抛出</exception>
            public static Encoding DetectFileEncoding(string filePath)
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("CSV文件不存在", filePath);

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    // 1. 检测UTF8-BOM（EF BB BF）
                    var bomBuffer = new byte[3];
                    int bomReadLen = fs.Read(bomBuffer, 0, 3);
                    if (bomReadLen >= 3 && bomBuffer[0] == 0xEF && bomBuffer[1] == 0xBB && bomBuffer[2] == 0xBF)
                    {
                        return new UTF8Encoding(true); // 返回带BOM的UTF8
                    }
                    fs.Seek(0, SeekOrigin.Begin); // 重置流位置

                    // 2. 简易检测UTF8(无BOM) / GBK
                    var sampleBuffer = new byte[1024];
                    int sampleReadLen = fs.Read(sampleBuffer, 0, sampleBuffer.Length);
                    int utf8Score = 0, gbkScore = 0;

                    for (int i = 0; i < sampleReadLen - 1; i++)
                    {
                        // UTF8多字节特征：2字节(0xC0-0xDF)、3字节(0xE0-0xEF)
                        if ((sampleBuffer[i] & 0xF0) == 0xE0) // 3字节UTF8
                        {
                            utf8Score++;
                            i += 2; // 跳过后续2个字节
                        }
                        else if ((sampleBuffer[i] & 0xE0) == 0xC0) // 2字节UTF8
                        {
                            utf8Score++;
                            i += 1; // 跳过后续1个字节
                        }
                        else if (sampleBuffer[i] > 0x7F) // 非ASCII字符，GBK特征
                        {
                            gbkScore++;
                            i += 1; // GBK多为2字节
                        }
                    }

                    // 评分高的编码优先
                    return utf8Score > gbkScore
                        ? new UTF8Encoding(false) // 无BOM的UTF8
                        : Encoding.GetEncoding("GBK"); // GBK/GB2312
                }
            }
            #endregion

            #region 场景1：小文件读取（<100MB）→ 转为DataTable（最常用）
            /// <summary>
            /// 读取CSV文件并转换为DataTable（自动适配编码，支持自定义分隔符）
            /// </summary>
            /// <param name="filePath">CSV文件路径</param>
            /// <param name="delimiter">CSV分隔符（默认逗号）</param>
            /// <param name="skipHeader">是否跳过表头（false=第一行作为列名）</param>
            /// <param name="encoding">编码（null=自动检测）</param>
            /// <returns>解析后的DataTable</returns>
            /// <exception cref="IOException">文件读取异常时抛出</exception>
            public static DataTable ReadToDataTable(string filePath, char delimiter = ',', bool skipHeader = false, Encoding encoding = null)
            {
                try
                {
                    // 参数校验
                    if (!File.Exists(filePath))
                        throw new FileNotFoundException("CSV文件不存在", filePath);

                    // 自动检测编码（未指定时）
                    encoding = DetectFileEncoding(filePath);

                    // 读取所有行（处理字段内换行）
                    var allCsvLines = ReadAllCsvLines(filePath, encoding);
                    var dataTable = new DataTable();

                    // 解析每一行数据
                    for (int lineIndex = 0; lineIndex < allCsvLines.Count; lineIndex++)
                    {
                        string line = allCsvLines[lineIndex];
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // 解析当前行的字段
                        List<string> fields = ParseCsvLine(line, delimiter);

                        // 初始化DataTable列（第一行作为列名）
                        if (dataTable.Columns.Count == 0)
                        {
                            if (skipHeader)
                            {
                                continue; // 跳过表头，自动生成列名
                            }
                            // 用解析后的字段名创建列
                            foreach (string field in fields)
                            {
                                string columnName = field.Trim().Length == 0 ? $"列{dataTable.Columns.Count + 1}" : field.Trim();
                                // 避免重复列名
                                if (dataTable.Columns.Contains(columnName))
                                    columnName += $"_{dataTable.Columns.Count + 1}";
                                dataTable.Columns.Add(columnName, typeof(string));
                            }
                        }
                        else
                        {
                            // 补充空字段（避免行字段数与列数不一致）
                            while (fields.Count < dataTable.Columns.Count)
                            {
                                fields.Add(string.Empty);
                            }
                            // 添加数据行
                            dataTable.Rows.Add(fields.ToArray());
                        }
                    }

                    // 若跳过表头且无列名，自动生成列名
                    if (dataTable.Columns.Count == 0 && allCsvLines.Count > 0)
                    {
                        int defaultColumnCount = ParseCsvLine(allCsvLines[skipHeader ? 1 : 0], delimiter).Count;
                        for (int i = 0; i < defaultColumnCount; i++)
                        {
                            dataTable.Columns.Add($"列{i + 1}", typeof(string));
                        }
                    }

                    return dataTable;
                }
                catch (Exception ex)
                {
                    throw new IOException($"读取CSV文件失败：{ex.Message}", ex);
                }
            }
            #endregion

            #region 场景2：大文件读取（>100MB）→ 按行迭代（低内存）
            /// <summary>
            /// 按行读取CSV文件（返回迭代器，避免内存溢出）
            /// </summary>
            /// <param name="filePath">CSV文件路径</param>
            /// <param name="delimiter">CSV分隔符（默认逗号）</param>
            /// <param name="skipHeader">是否跳过表头</param>
            /// <param name="encoding">编码（null=自动检测）</param>
            /// <returns>每行的字段列表迭代器</returns>
            /// <exception cref="IOException">文件读取异常时抛出</exception>
            public static IEnumerable<List<string>> ReadLineByLine(string filePath, char delimiter = ',', bool skipHeader = false, Encoding encoding = null)
            {
             
                    if (!File.Exists(filePath))
                        throw new FileNotFoundException("CSV文件不存在", filePath);

                    encoding = DetectFileEncoding(filePath);
                    bool isHeaderSkipped = !skipHeader; // 标记是否已跳过表头

                    using (var streamReader = new StreamReader(filePath, encoding))
                    {
                        string currentLine;
                        StringBuilder multiLineField = new StringBuilder();
                        bool inQuotes = false; // 是否在双引号包裹的字段内（处理字段内换行）

                        while ((currentLine = streamReader.ReadLine()) != null)
                        {
                            // 拼接跨行使的字段
                            multiLineField.Append(currentLine);

                            // 统计双引号数量（奇数=在引号内，字段未结束）
                            int quoteCount = Regex.Matches(currentLine, "\"").Count;
                            inQuotes = quoteCount % 2 != 0;

                            // 字段未结束（跨行），继续拼接
                            if (inQuotes)
                            {
                                multiLineField.Append(Environment.NewLine);
                                continue;
                            }

                            // 字段结束，解析当前完整行
                            string fullLine = multiLineField.ToString();
                            multiLineField.Clear();

                            // 跳过表头（仅第一次）
                            if (skipHeader && !isHeaderSkipped)
                            {
                                isHeaderSkipped = true;
                                continue;
                            }

                            if (!string.IsNullOrWhiteSpace(fullLine))
                            {
                                yield return ParseCsvLine(fullLine, delimiter);
                            }
                        }

                        // 处理最后一行未闭合的字段
                        if (multiLineField.Length > 0 && !string.IsNullOrWhiteSpace(multiLineField.ToString()))
                        {
                            yield return ParseCsvLine(multiLineField.ToString(), delimiter);
                        }
                    }
                
            
            }
            #endregion

            #region 底层私有方法：解析单行CSV（处理引号/分隔符）
            /// <summary>
            /// 解析单行CSV字符串为字段列表（兼容RFC4180规范）
            /// </summary>
            /// <param name="line">CSV单行字符串</param>
            /// <param name="delimiter">分隔符</param>
            /// <returns>解析后的字段列表</returns>
            private static List<string> ParseCsvLine(string line, char delimiter)
            {
                var fields = new List<string>();
                var currentField = new StringBuilder();
                bool inQuotes = false; // 是否在双引号包裹中

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];

                    // 处理双引号转义（"" → "）
                    if (c == '"' && i < line.Length - 1 && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++; // 跳过下一个双引号
                        continue;
                    }

                    // 进入/退出双引号包裹状态
                    if (c == '"')
                    {
                        inQuotes = !inQuotes;
                        continue;
                    }

                    // 分隔符（仅不在引号内时生效）
                    if (c == delimiter && !inQuotes)
                    {
                        fields.Add(currentField.ToString().Trim());
                        currentField.Clear();
                        continue;
                    }

                    // 普通字符，追加到当前字段
                    currentField.Append(c);
                }

                // 添加最后一个字段
                fields.Add(currentField.ToString().Trim());
                return fields;
            }
            #endregion

            #region 底层私有方法：读取所有CSV行（处理字段内换行）
            /// <summary>
            /// 读取CSV所有行（解决字段内换行的场景）
            /// </summary>
            /// <param name="filePath">文件路径</param>
            /// <param name="encoding">编码</param>
            /// <returns>完整的CSV行列表</returns>
            private static List<string> ReadAllCsvLines(string filePath, Encoding encoding)
            {
                var lines = new List<string>();
                var currentLine = new StringBuilder();
                bool inQuotes = false;

                using (var reader = new StreamReader(filePath, encoding))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        currentLine.Append(line);
                        // 统计双引号数量，判断是否在引号内（字段内换行）
                        int quoteCount = Regex.Matches(line, "\"").Count;
                        inQuotes = quoteCount % 2 != 0;

                        // 不在引号内 = 当前行完整，加入列表
                        if (!inQuotes)
                        {
                            lines.Add(currentLine.ToString());
                            currentLine.Clear();
                        }
                        else
                        {
                            // 在引号内 = 字段跨行，保留换行符
                            currentLine.Append(Environment.NewLine);
                        }
                    }

                    // 处理最后一行未闭合的字段
                    if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine.ToString());
                    }
                }

                return lines;
            }
            #endregion
        }
    }


