IF COL_LENGTH('dbo.RentalRequests', 'CustomerImageUrl') IS NULL
BEGIN
    ALTER TABLE dbo.RentalRequests ADD CustomerImageUrl VARCHAR(500) NULL;
END
GO

IF COL_LENGTH('dbo.RentalRequests', 'IdProofNumber') IS NULL
BEGIN
    ALTER TABLE dbo.RentalRequests ADD IdProofNumber VARCHAR(100) NULL;
END
GO

IF OBJECT_ID('dbo.RentalRequestDocuments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RentalRequestDocuments
    (
        DocumentId INT IDENTITY(1,1) PRIMARY KEY,
        RequestId INT NOT NULL,
        DocumentUrl VARCHAR(500) NOT NULL,
        DocumentName VARCHAR(255) NULL,
        UploadedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_RentalRequestDocuments_RentalRequests
            FOREIGN KEY (RequestId) REFERENCES dbo.RentalRequests(RequestId) ON DELETE CASCADE
    );
END
GO
