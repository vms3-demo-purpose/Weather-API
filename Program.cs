using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;

namespace WebApiClient
{
    class Program
    {
        const string api_url = "https://api.data.gov.sg/v1/environment/2-hour-weather-forecast?date=";
        static async Task Main(string[] args)
        {
            await callWeatherAPI();
        }

        static async Task callWeatherAPI()
        {
            // Get current date in yyyy-MM-dd to be passed in as query to the API
            DateTime dateTime = DateTime.Today;
            var singaporeTime = TimeZoneInfo.ConvertTime(DateTime.Today, TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time"));
            String queryDate = singaporeTime.ToString("yyyy-MM-dd");

            // SQL's DATETIME uses a different format
            String sqlDate = singaporeTime.ToString("dd-MM-yyyy");

            // Pull data from API, extract relevant bits and write to new json file to be pushed into Sql Server
            using (var client = new HttpClient())
            {
                // Setup HttpClient
                client.BaseAddress = new Uri(api_url + queryDate);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync(api_url + queryDate);
                if (response.IsSuccessStatusCode)
                {
                    // This contains all data, from which we only want a subset of
                    WeatherData? weatherData = JsonConvert.DeserializeObject<WeatherData>(await response.Content.ReadAsStringAsync());
                    List<Data> data = new List<Data>();

                    // Extract the parts we want and write it to a new json file
                    if (weatherData is not null)
                    {
                        foreach (Item i in weatherData.items!)
                        {
                            DateTime start = i.valid_period!.start;
                            DateTime end = i.valid_period.end;
                            foreach (Forecast f in i.forecasts!)
                            {
                                string? area = f.area;
                                string? forecast = f.forecast;
                                data.Add(new Data()
                                {
                                    StartTime = start,
                                    EndTime = end,
                                    Area = area, 
                                    Forecast = forecast,
                                    SqlStartTime = start.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                    SqlEndTime = end.ToString("yyyy-MM-dd HH:mm:ss.fff")
                                });

                                string json = JsonConvert.SerializeObject(data.ToArray(), Formatting.Indented);
                                
                                System.IO.File.WriteAllText("./" + sqlDate + ".json", json);
                            }
                        }

                        Console.WriteLine("Pulled {0} weather records for date: {1}.", data.Count, sqlDate);
                    } 
                } 
                else
                {
                    Console.WriteLine("Failed Response Code: {0}", response.StatusCode);
                }
            }

            // TODO: Find ways to retry connection periodically instead of flat out waiting a full minute
            Console.WriteLine("Waiting 60 secs for DB to finish initialising before connecting to DB...");
            String connectionString = @"
            Server=weather_db,1433;
            Database=Master;
            User Id=SA;
            Password=Passw0rd#;
            Encrypt=False;
            TrustServerCertificate=True;
            Trusted_Connection=True;
            Integrated Security=False;
            ";
            SqlConnection connection = new SqlConnection(connectionString);
            System.Threading.Thread.Sleep(60000);

            Console.WriteLine("Creating Table...");
            String createTableQuery = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE Name = 'weather_records')
                CREATE TABLE weather_records (
                    RecordID    INT             IDENTITY(1, 1)  PRIMARY KEY,
                    Area        VARCHAR(255)                    NOT NULL,
                    Forecast    VARCHAR(255)                    NOT NULL,
                    SqlStartTime   DATETIME                     NOT NULL,
                    SqlEndTime     DATETIME                     NOT NULL
                );
            ";
            ExecuteQuery(connection, createTableQuery);
            
            Console.WriteLine("Inserting into Table...");
            String insertIntoTableQuery = @"
                -- Constructing directory path of json
                DECLARE @FilePath NVARCHAR(128)
                SET @FilePath = '/data/out/' + CONVERT(VARCHAR, GetDate(), 105) + '.json'
                -- End of Constructing directory path of json

                -- Inserting JSON into DB
                DECLARE @SQL NVARCHAR(MAX)
                SET @SQL = N'
                    DECLARE @JSON VARCHAR(MAX)
                    SELECT @JSON = BULKCOLUMN
                    FROM OPENROWSET (BULK ''' + @FilePath + ''', SINGLE_CLOB) import

                    INSERT INTO weather_records (Area, Forecast, SqlStartTime, SqlEndTime)
                    SELECT *
                    FROM OPENJSON (@JSON)
                    WITH (
                        [Area] VARCHAR(255),
                        [Forecast] VARCHAR(255),
                        [SqlStartTime] DATETIME,
                        [SqlEndTime] DATETIME                        
                    );
                '
                EXEC(@SQL)
                -- End of Inserting JSON into DB
            ";
            ExecuteQuery(connection, insertIntoTableQuery);

            Console.WriteLine("Reading from Table...");
            String readFromQuery = @"
                SELECT 
                    RecordID AS RID, 
                    Area AS AREA, 
                    Forecast AS FORECAST, 
                    SqlStartTime AS STARTTIME,
                    SqlEndTime AS ENDTIME
                FROM weather_records;
            ";
            ExecuteQuery(connection, readFromQuery);

            Console.Read();
        }
        
        static void ExecuteQuery(SqlConnection connection, String query)
        {
            SqlCommand sqlCommand = new SqlCommand(query, connection);
            try
            {
                connection.Open();
                SqlDataReader reader = sqlCommand.ExecuteReader();
                while (reader.Read())
                {
                    // Format output so that RID is left aligned with 5 width, Area is left aligned with 40 width, etc.
                    Console.WriteLine("{0, -5}{1, -40}{2, -45}{3, -25}{4, -25}", 
                        reader["RID"].ToString(),
                        reader["AREA"].ToString(),
                        reader["FORECAST"].ToString(),
                        reader["STARTTIME"].ToString(),
                        reader["ENDTIME"].ToString()
                    );
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
    }
}