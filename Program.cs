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
            PushToDB();
            Console.Read();
        }

        static async Task callWeatherAPI()
        {
            // Get current date in yyyy-MM-dd to be passed in as query to the API
            DateTime dateTime = DateTime.Today;
            var singaporeTime = TimeZoneInfo.ConvertTime(DateTime.Today, TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time"));
            String queryDate = singaporeTime.ToString("yyyy-MM-dd");

            // SQL's DATETIME uses a different format
            String sqlDate = singaporeTime.ToString("dd-MM-yyyy");

            // Pull data from API, extract relevant bits and write to new json file to be pushed into DB
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
                                data.Add(new Data()
                                {
                                    Area = f.area, 
                                    Forecast = f.forecast,
                                    SqlStartTime = start.ToString("yyyy-MM-dd HH:mm:ss"),
                                    SqlEndTime = end.ToString("yyyy-MM-dd HH:mm:ss")
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
        }

        static void PushToDB()
        {
            bool tableCreated = false;
            bool dataInserted = false;
            bool tableReadFrom = false;
            bool succeeded = false;

            const int retryCount = 10;
            int retryIntervalSeconds = 15;
            String connectionString = File.ReadAllText("./DB_Files/connection_string.txt");
            SqlConnection connection = new SqlConnection(connectionString);

            for (int tries = 1; tries <= retryCount; tries++)
            {
                try
                {
                    if (!tableCreated)
                    {
                        String createTableQuery = File.ReadAllText("./DB_Files/CREATE_TABLE.sql");
                        ExecuteQuery(connection, createTableQuery);
                        tableCreated = true;
                    }
                    
                    if (!dataInserted)
                    {
                        String insertIntoTableQuery = File.ReadAllText("./DB_Files/INSERT_INTO.sql");
                        ExecuteQuery(connection, insertIntoTableQuery);
                        dataInserted = true;
                    }
                    
                    if (!tableReadFrom)
                    {
                        String readFromQuery = File.ReadAllText("./DB_Files/SELECT_TABLE.sql");
                        ExecuteQuery(connection, readFromQuery);
                        tableReadFrom = true;
                    }

                    if (tries > 1 && !succeeded)
                    {
                        Console.WriteLine("Attempting retry {0}/{1}. Retrying in {2} seconds. Current progress:", tries, retryCount, retryIntervalSeconds);
                        Console.WriteLine(tableCreated ? "Table Created: Yes" : "Table Created: No");
                        Console.WriteLine(dataInserted ? "Data Inserted: Yes" : "Data Inserted: No");
                        Console.WriteLine(tableReadFrom ? "Read from Table: Yes" : "Read from Table: No");
                        Thread.Sleep(1000 * retryIntervalSeconds);
                    }
                    succeeded = tableCreated && dataInserted && tableReadFrom;
                }
                catch (SqlException sqlException)
                {
                    Console.WriteLine("{0}: Error occurred.", sqlException.Number);
                    succeeded = false;
                }               
            }

            if (!succeeded)
            {
                Console.WriteLine("Failed to establish connection to database to perform the following:");
                Console.WriteLine(tableCreated ? "" : "Create Table");
                Console.WriteLine(dataInserted ? "" : "Insert Data");
                Console.WriteLine(tableReadFrom ? "" : "Read from Table");
            }
        }

        static void ExecuteQuery(SqlConnection connection, String query)
        {
            SqlCommand sqlCommand = new SqlCommand(query, connection);
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

            if (connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }
    }
}