-- Import Data from JSON file
DECLARE @FileStream VARBINARY(MAX)
DECLARE @Command NVARCHAR(1000)
DECLARE @FilePath NVARCHAR(128)
SET @FilePath = '/data/out/' + CONVERT(VARCHAR, GetDate(), 105) + '.json'

SET @Command = N'
    SELECT @FileStream1 = CAST(BulkColumn AS VARBINARY(MAX))
    FROM OPENROWSET(BULK ''' + @FilePath + ''', SINGLE_BLOB) ROW_SET
'  
EXEC sp_executesql @Command, N'@FileStream1 VARBINARY(MAX) OUTPUT', @FileStream1 = @FileStream OUTPUT

SELECT Area, Forecast, CONVERT(DATETIME, SqlStartTime) AS SqlStartTime, CONVERT(DATETIME, SqlEndTime) AS SqlEndTime
FROM OPENJSON(@FileStream)
WITH
    (
        Area VARCHAR(255),
        Forecast VARCHAR(255),
        SqlStartTime DATETIME,
        SqlEndTime DATETIME
    );