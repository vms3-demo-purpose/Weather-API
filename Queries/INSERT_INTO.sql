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