IF NOT EXISTS (SELECT * FROM sysobjects WHERE Name = 'weather_records')
    CREATE TABLE weather_records (
        RecordID        INT             IDENTITY(1, 1)  PRIMARY KEY,
        Area            VARCHAR(255)    NOT NULL,
        Forecast        VARCHAR(255)    NOT NULL,
        SqlStartTime   DATETIME         NOT NULL,
        SqlEndTime     DATETIME         NOT NULL
    );