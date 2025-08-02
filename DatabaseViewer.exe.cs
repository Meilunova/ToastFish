using System;
using System.Data.SQLite;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            string dbPath = @".\Resources\inami.db";
            if (!File.Exists(dbPath))
            {
                Console.WriteLine($"数据库文件不存在: {dbPath}");
                Console.WriteLine("请确保在ToastFish程序目录下运行此工具");
                Console.ReadKey();
                return;
            }

            string connectionString = $"Data Source={dbPath};Version=3;";
            
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("数据库连接成功！");
                Console.WriteLine("==================");
                
                // 查询所有表
                string sql = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
                using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine("数据库中的所有表:");
                        int tableCount = 0;
                        while (reader.Read())
                        {
                            string tableName = reader["name"].ToString();
                            tableCount++;
                            Console.WriteLine($"{tableCount}. {tableName}");
                            
                            // 特别标记可能的日语表
                            if (tableName.ToLower().Contains("jp") || 
                                tableName.ToLower().Contains("japan") || 
                                tableName.ToLower().Contains("std") ||
                                tableName.ToLower().Contains("mid"))
                            {
                                Console.WriteLine($"   *** 可能的日语表 ***");
                            }
                        }
                        
                        if (tableCount == 0)
                        {
                            Console.WriteLine("没有找到任何表！");
                        }
                    }
                }
                
                Console.WriteLine("\n==================");
                Console.WriteLine("查找包含日语相关字段的表...");
                
                // 查找包含日语相关字段的表
                string[] possibleTables = GetAllTables(connection);
                foreach (string table in possibleTables)
                {
                    if (HasJapaneseColumns(connection, table))
                    {
                        Console.WriteLine($"\n表 '{table}' 包含日语相关字段:");
                        ShowTableStructure(connection, table);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
    
    static string[] GetAllTables(SQLiteConnection connection)
    {
        var tables = new System.Collections.Generic.List<string>();
        string sql = "SELECT name FROM sqlite_master WHERE type='table';";
        using (SQLiteCommand command = new SQLiteCommand(sql, connection))
        {
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    tables.Add(reader["name"].ToString());
                }
            }
        }
        return tables.ToArray();
    }
    
    static bool HasJapaneseColumns(SQLiteConnection connection, string tableName)
    {
        try
        {
            string sql = $"PRAGMA table_info({tableName});";
            using (SQLiteCommand command = new SQLiteCommand(sql, connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string columnName = reader["name"].ToString().ToLower();
                        if (columnName.Contains("hiragana") || 
                            columnName.Contains("katakana") || 
                            columnName.Contains("romaji") ||
                            columnName.Contains("jp") ||
                            columnName.Contains("japanese"))
                        {
                            return true;
                        }
                    }
                }
            }
        }
        catch
        {
            // 忽略错误
        }
        return false;
    }
    
    static void ShowTableStructure(SQLiteConnection connection, string tableName)
    {
        try
        {
            string sql = $"PRAGMA table_info({tableName});";
            using (SQLiteCommand command = new SQLiteCommand(sql, connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    Console.WriteLine("  列名\t\t类型");
                    Console.WriteLine("  ----------------");
                    while (reader.Read())
                    {
                        string colName = reader["name"].ToString();
                        string colType = reader["type"].ToString();
                        Console.WriteLine($"  {colName}\t\t{colType}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  无法读取表结构: {ex.Message}");
        }
    }
}
