-- Import Data from JSON file
DECLARE @JSON varchar(max)
SELECT @JSON=BulkColumn
FROM OPENROWSET (BULK '/var/lib/mysql/2022-09-28.json', SINGLE_CLOB) AS import

-- Insert into database
INSERT INTO weather_records (Area, Forecast, SqlStartTime, SqlEndTime)
 SELECT Area, Forecast, CONVERT(DATETIME, SqlStartTime) AS SqlStartTime, CONVERT(DATETIME, SqlEndTime) AS SqlEndTime 
 FROM OPENJSON(@JSON)
 WITH 
    (
        Area VARCHAR(255),
        Forecast VARCHAR(255), 
        SqlStartTime DATETIME,
        SqlEndTime DATETIME
    );