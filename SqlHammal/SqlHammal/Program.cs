using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using System.ComponentModel.Design;
using System.Data.SqlClient;
using System.Data;
using System.Runtime.CompilerServices;
using SqlHammal;



string? sourceConnectionString;
string? targetConnectionString;
string selectTop = "";

// Adjust Configuration
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.Sources.Clear();
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .Build();
var configuration = host.Services.GetService(typeof(IConfiguration)) as IConfiguration;

sourceConnectionString = configuration["ConnectionStrings:Source"];
targetConnectionString = configuration["ConnectionStrings:Target"];

if (string.IsNullOrWhiteSpace(sourceConnectionString) || string.IsNullOrWhiteSpace(targetConnectionString))
    AnsiConsole.WriteLine("[red] Please insert connection string in appsetting.js and re-run the appliation");
else
{

    Menu();
    Console.ReadKey();
}





void Menu()
{

    AnsiConsole.Write(
             new FigletText("SQL - Hammal")
            .LeftJustified()
            .Color(Color.Red));



    var copyType = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select amount of data to copy ")
                        .PageSize(10)
                        .AddChoices(new[] {
                            "Copy Top Number ", "Copy All Data "
                        }));

    if (copyType == "Copy Top Number ")
    {
        var topCount = getTopCountNumber();
        selectTop = $" SELECT {topCount} ";
    }

    Run();

}
void Run()
{

    try
    {

        using (SqlConnection sourceConnection = new SqlConnection(sourceConnectionString))
        using (SqlConnection targetConnection = new SqlConnection(targetConnectionString))
        {
            sourceConnection.Open();
            targetConnection.Open();

            var schemaNames = Sqlhandler.GetSchemaNames(sourceConnection);
            var tableOrder = Sqlhandler.GetTableOrder(sourceConnection, schemaNames);

            int tableCounts = 0;
            int totalTableCounts = tableOrder.Count;

            foreach (var fullTableName in tableOrder)
            {
                string[] tableParts = fullTableName.Split('.');
                string schemaName = tableParts[0];
                string tableName = tableParts[1];
                Console.WriteLine($"Proccessing Table => {schemaName}.{tableName} ");


                DataTable schemaTable = new DataTable();
                string schemaQuery = $@"
                    SELECT COLUMN_NAME,DATA_TYPE, COLUMNPROPERTY(object_id(TABLE_SCHEMA + '.' + TABLE_NAME), COLUMN_NAME, 'IsIdentity') AS IsIdentity
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = '{tableName}' AND TABLE_SCHEMA = '{schemaName}'";
                using (SqlCommand schemaCommand = new SqlCommand(schemaQuery, sourceConnection))
                using (SqlDataAdapter schemaAdapter = new SqlDataAdapter(schemaCommand))
                {
                    schemaAdapter.Fill(schemaTable);
                }

                bool hasIdentityColumn = schemaTable.AsEnumerable().Any(row => row.Field<int>("IsIdentity") == 1);

                string[] columnNames = schemaTable.AsEnumerable().Select(r => r.Field<string>("COLUMN_NAME")).ToArray();
                string columnList = string.Join(", ", columnNames);
                string parameterList = string.Join(", ", columnNames.Select((name, index) => $"@p{index}"));



                string identityInsertOn = hasIdentityColumn ? $"SET IDENTITY_INSERT {schemaName}.{tableName} ON; " : "";
                string identityInsertOff = hasIdentityColumn ? $" SET IDENTITY_INSERT {schemaName}.{tableName} OFF;" : "";
                string insertQuery = $"{identityInsertOn}INSERT INTO {schemaName}.{tableName} ({columnList}) VALUES ({parameterList});{identityInsertOff}";

                using (SqlCommand insertCommand = new SqlCommand(insertQuery, targetConnection))
                {
                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        string columnName = columnNames[i];
                        string dataType = schemaTable.AsEnumerable().First(row => row.Field<string>("COLUMN_NAME") == columnName).Field<string>("DATA_TYPE");

                        SqlDbType sqlDbType = Sqlhandler.GetSqlDbType(dataType);

                        insertCommand.Parameters.Add(new SqlParameter($"@p{i}", sqlDbType));
                    }


                    string selectQuery = $"SELECT {selectTop} * FROM {schemaName}.{tableName} ORDER BY 1 DESC";
                    using (SqlCommand selectCommand = new SqlCommand(selectQuery, sourceConnection))
                    using (SqlDataReader reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < columnNames.Length; i++)
                            {
                                insertCommand.Parameters[$"@p{i}"].Value = reader[columnNames[i]];
                            }
                            try
                            {
                                insertCommand.ExecuteNonQuery();
                            }
                            catch
                            {
                                continue;
                            }

                        }
                    }
                }
                Console.WriteLine($"Table Has been Proccessd =>  {schemaName}.{tableName} ");
                tableCounts++;
            }


            Console.WriteLine($" --- {tableCounts} Proccessed from {totalTableCounts}");
            Console.WriteLine("All Done !");
        }

    }
    catch (Exception exp)
    {
        Console.WriteLine(exp.Message);
        throw;
    }
}



int getTopCountNumber()
{
    AnsiConsole.WriteLine("Enter the number of records to fetch from each table : ");
    var topCount = Console.ReadLine();

    if (int.TryParse(topCount, out int number))
        return number;
    else
        getTopCountNumber();


    return 0;
}






