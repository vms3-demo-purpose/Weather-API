# Weather-API vs weather-program:
Weather-API passes JSON to DB, DB performs queries for data insertion
weather-program deserialises JSON and uses EF Core to insert data into DB

# Weather-API

This program pulls weather data from https://api.data.gov.sg

A portion of the data gathered is extracted and transformed prior to insertion into a database.

The data is to be used to present weather data on a web application.

# Running the container
Clone the repository. Open PowerShell (preferably with administrator rights), navigate to the directory and run the following command:

`docker compose up --build --force-recreate --detach`

Once the containers are running, run the following command:

`docker logs --follow weather_api`

The records within the DB should be displayed.

# Versions of Framework / Libraries used

1. docker-compose: 3
2. SQL Server: 2019-CU18-ubuntu-20.04
3. .NET: 6.0.402

# JSON Format
[18-10-2022.txt](https://github.com/vms3-demo-purpose/Weather-API/files/9816764/18-10-2022.txt)

# DB Schema
[CREATE_TABLE.txt](https://github.com/vms3-demo-purpose/Weather-API/files/9816766/CREATE_TABLE.txt)

