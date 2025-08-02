using System;
using System.Data.SQLite;
using System.IO;

namespace ToastFish
{
    /// <summary>
    /// 数据库检查工具
    /// </summary>
    public class DatabaseChecker
    {
        public static void CheckDatabaseTables()
        {
            try
            {
                string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string exeDir = System.IO.Path.GetDirectoryName(strExeFilePath);
                string dbFilePath = System.IO.Path.Combine(exeDir, "Resources", "inami.db");
                string databasePath = @"Data Source=" + dbFilePath + ";Version=3";
                
                System.Diagnostics.Debug.WriteLine($"数据库文件路径: {dbFilePath}");
                System.Diagnostics.Debug.WriteLine($"文件是否存在: {File.Exists(dbFilePath)}");

                if (!File.Exists(dbFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("数据库文件不存在！");
                    return;
                }

                using (SQLiteConnection connection = new SQLiteConnection(databasePath))
                {
                    connection.Open();
                    System.Diagnostics.Debug.WriteLine("数据库连接成功");
                    
                    // 查询所有表名
                    string sql = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
                    using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                    {
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            System.Diagnostics.Debug.WriteLine("\n数据库中的所有表:");
                            System.Diagnostics.Debug.WriteLine("==================");
                            while (reader.Read())
                            {
                                string tableName = reader["name"].ToString();
                                System.Diagnostics.Debug.WriteLine($"- {tableName}");

                                // 检查是否是日语相关的表
                                if (tableName.ToLower().Contains("jp") ||
                                    tableName.ToLower().Contains("japan") ||
                                    tableName.ToLower().Contains("std"))
                                {
                                    System.Diagnostics.Debug.WriteLine($"  *** 可能的日语表: {tableName} ***");
                                }
                            }
                        }
                    }
                    
                    // 检查特定表的结构
                    CheckTableStructure(connection, "StdJp_Mid");
                    
                    // 尝试查找其他可能的日语表
                    string[] possibleJpTables = { "StdJp", "JpWord", "Japanese", "Jp_Mid", "StdJapanese" };
                    foreach (string tableName in possibleJpTables)
                    {
                        CheckTableStructure(connection, tableName);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检查数据库时发生错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        private static void CheckTableStructure(SQLiteConnection connection, string tableName)
        {
            try
            {
                string sql = $"PRAGMA table_info({tableName});";
                using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            System.Diagnostics.Debug.WriteLine($"\n表 '{tableName}' 的结构:");
                            System.Diagnostics.Debug.WriteLine("列名\t\t类型\t\t是否为空\t默认值");
                            System.Diagnostics.Debug.WriteLine("------------------------------------------------");
                            while (reader.Read())
                            {
                                string colName = reader["name"].ToString();
                                string colType = reader["type"].ToString();
                                string notNull = reader["notnull"].ToString();
                                string defaultValue = reader["dflt_value"]?.ToString() ?? "NULL";
                                System.Diagnostics.Debug.WriteLine($"{colName}\t\t{colType}\t\t{notNull}\t\t{defaultValue}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 表不存在或其他错误，忽略
                System.Diagnostics.Debug.WriteLine($"表 '{tableName}' 不存在或无法访问");
            }
        }
    }
}
