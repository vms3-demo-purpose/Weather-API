# Weather-API

This program pulls weather data from https://api.data.gov.sg

A portion of the data gathered is extracted and transformed prior to insertion into a database.

The data is to be used to present weather data on a web application.

# Versions of Framework / Libraries used

1. docker-compose: 3
2. SQL Server: 2019-CU18-ubuntu-20.04
3. .NET: 6.0.402

# JSON Format
[
	{
		"Area": "Ang Mo Kio",
		"Forecast": "Partly Cloudy (Night)",
		"SqlStartTime": "2022-10-18 00:30:00",
		"SqlEndTime": "2022-10-18 02:30:00"
	},
	{
		"Area": "Bedok",
		"Forecast": "Partly Cloudy (Night)",
		"SqlStartTime": "2022-10-18 00:30:00",
		"SqlEndTime": "2022-10-18 02:30:00"
	}
]

# DB Schema
CREATE TABLE weather_records (
	RecordID        INT				IDENTITY(1, 1)	PRIMARY KEY,
	Area			VARCHAR(255)	NOT NULL,
 	Forecast		VARCHAR(255)	NOT NULL,
	SqlStartTime	DATETIME		NOT NULL,
	SqlEndTime		DATETIME		NOT NULL
);
