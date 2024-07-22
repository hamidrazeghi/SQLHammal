using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlHammal
{
    public static class Sqlhandler
    {
        public static List<string> GetTableOrder(SqlConnection connection, Dictionary<int, string> schemaNames)
        {
            var tableOrder = new List<string>();
            var dependencies = new Dictionary<string, List<string>>();

            // Get all tables and their foreign key dependencies
            string query = @"
            SELECT 
                fk.name AS FK_Name, 
                tp.name AS ParentTable, 
                tr.name AS ReferencedTable,
                tp.schema_id AS ParentSchemaId,
                tr.schema_id AS ReferencedSchemaId
            FROM 
                sys.foreign_keys AS fk
                INNER JOIN sys.tables AS tp ON fk.parent_object_id = tp.object_id
                INNER JOIN sys.tables AS tr ON fk.referenced_object_id = tr.object_id
                INNER JOIN sys.schemas AS sp ON tp.schema_id = sp.schema_id
                INNER JOIN sys.schemas AS sr ON tr.schema_id = sr.schema_id";

            using (SqlCommand command = new SqlCommand(query, connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string parentTable = reader["ParentTable"].ToString();
                    string referencedTable = reader["ReferencedTable"].ToString();
                    string parentSchema = schemaNames[(int)reader["ParentSchemaId"]];
                    string referencedSchema = schemaNames[(int)reader["ReferencedSchemaId"]];

                    string parentFullTableName = $"{parentSchema}.{parentTable}";
                    string referencedFullTableName = $"{referencedSchema}.{referencedTable}";

                    if (!dependencies.ContainsKey(parentFullTableName))
                    {
                        dependencies[parentFullTableName] = new List<string>();
                    }

                    dependencies[parentFullTableName].Add(referencedFullTableName);
                }
            }


            var visited = new HashSet<string>();
            foreach (var table in dependencies.Keys)
            {
                Visit(table, dependencies, visited, tableOrder);
            }



            var allTables = new HashSet<string>();
            string allTablesQuery = @"
            SELECT 
                TABLE_SCHEMA, 
                TABLE_NAME 
            FROM 
                INFORMATION_SCHEMA.TABLES 
            WHERE 
                TABLE_TYPE = 'BASE TABLE'";
            using (SqlCommand command = new SqlCommand(allTablesQuery, connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string schemaName = reader["TABLE_SCHEMA"].ToString();
                    string tableName = reader["TABLE_NAME"].ToString();
                    allTables.Add($"{schemaName}.{tableName}");
                }
            }


            foreach (var table in allTables.Except(tableOrder))
            {
                tableOrder.Add(table);
            }



            return tableOrder;
        }

        public static SqlDbType GetSqlDbType(string dataType)
        {
            switch (dataType.ToLower())
            {
                case "bigint": return SqlDbType.BigInt;
                case "binary": return SqlDbType.Binary;
                case "bit": return SqlDbType.Bit;
                case "char": return SqlDbType.Char;
                case "date": return SqlDbType.Date;
                case "datetime": return SqlDbType.DateTime;
                case "datetime2": return SqlDbType.DateTime2;
                case "datetimeoffset": return SqlDbType.DateTimeOffset;
                case "decimal": return SqlDbType.Decimal;
                case "float": return SqlDbType.Float;
                case "image": return SqlDbType.Image;
                case "int": return SqlDbType.Int;
                case "money": return SqlDbType.Money;
                case "nchar": return SqlDbType.NChar;
                case "ntext": return SqlDbType.NText;
                case "nvarchar": return SqlDbType.NVarChar;
                case "real": return SqlDbType.Real;
                case "smalldatetime": return SqlDbType.SmallDateTime;
                case "smallint": return SqlDbType.SmallInt;
                case "smallmoney": return SqlDbType.SmallMoney;
                case "text": return SqlDbType.Text;
                case "time": return SqlDbType.Time;
                case "timestamp": return SqlDbType.Timestamp;
                case "tinyint": return SqlDbType.TinyInt;
                case "uniqueidentifier": return SqlDbType.UniqueIdentifier;
                case "varbinary": return SqlDbType.VarBinary;
                case "varchar": return SqlDbType.VarChar;
                case "xml": return SqlDbType.Xml;
                default: throw new ArgumentOutOfRangeException(nameof(dataType), $"Unsupported SQL data type: {dataType}");
            }
        }

        public static Dictionary<int, string> GetSchemaNames(SqlConnection connection)
        {
            var schemaNames = new Dictionary<int, string>();
            string query = "SELECT schema_id, name FROM sys.schemas";
            using (SqlCommand command = new SqlCommand(query, connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    schemaNames.Add(reader.GetInt32(0), reader.GetString(1));
                }
            }
            return schemaNames;
        }

        public static void Visit(string table, Dictionary<string, List<string>> dependencies, HashSet<string> visited, List<string> tableOrder)
        {
            if (!visited.Contains(table))
            {
                visited.Add(table);

                if (dependencies.ContainsKey(table))
                {
                    foreach (var dependency in dependencies[table])
                    {
                        Visit(dependency, dependencies, visited, tableOrder);
                    }
                }

                tableOrder.Add(table);
            }
        }
    }
}
