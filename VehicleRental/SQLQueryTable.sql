IF OBJECT_ID('dbo.RentalTransactions', 'U') IS NOT NULL DROP TABLE dbo.RentalTransactions;
IF OBJECT_ID('dbo.RentalRequestDocuments', 'U') IS NOT NULL DROP TABLE dbo.RentalRequestDocuments;
IF OBJECT_ID('dbo.RentalRequests', 'U') IS NOT NULL DROP TABLE dbo.RentalRequests;
IF OBJECT_ID('dbo.VehicleDocuments', 'U') IS NOT NULL DROP TABLE dbo.VehicleDocuments;
IF OBJECT_ID('dbo.VehicleImages', 'U') IS NOT NULL DROP TABLE dbo.VehicleImages;
IF OBJECT_ID('dbo.AdminActions', 'U') IS NOT NULL DROP TABLE dbo.AdminActions;
IF OBJECT_ID('dbo.Vehicles', 'U') IS NOT NULL DROP TABLE dbo.Vehicles;
IF OBJECT_ID('dbo.UserMaster', 'U') IS NOT NULL DROP TABLE dbo.UserMaster;
IF OBJECT_ID('dbo.UserRole', 'U') IS NOT NULL DROP TABLE dbo.UserRole;
GO

CREATE TABLE dbo.UserRole
(
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName VARCHAR(30) NOT NULL UNIQUE,
    Description VARCHAR(200) NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1
);
GO

INSERT INTO dbo.UserRole (RoleName, Description)
VALUES ('Admin', 'System administrator'), ('Seller', 'Vehicle owner or rental partner'), ('User', 'Rental customer');
GO

CREATE TABLE dbo.UserMaster
(
    U_Id INT IDENTITY(1,1) PRIMARY KEY,
    Full_name VARCHAR(50) NOT NULL,
    Password VARCHAR(100) NOT NULL,
    Address VARCHAR(100) NOT NULL DEFAULT 'Not specified',
    Birthdate DATETIME NOT NULL,
    Contact_No VARCHAR(12) NOT NULL DEFAULT '0000000000',
    Email VARCHAR(50) NOT NULL UNIQUE,
    RoleId INT NOT NULL,
    Status VARCHAR(30) NOT NULL DEFAULT 'Approved',
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    LastLoginDate DATETIME NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_UserMaster_UserRole FOREIGN KEY (RoleId) REFERENCES dbo.UserRole(RoleId)
);
GO

CREATE TABLE dbo.Vehicles
(
    VehicleId INT IDENTITY(1,1) PRIMARY KEY,
    SellerId INT NOT NULL,
    Make VARCHAR(50) NOT NULL,
    Model VARCHAR(50) NOT NULL,
    [Year] INT NOT NULL,
    Description VARCHAR(1000) NULL,
    DailyRate DECIMAL(18,2) NOT NULL,
    PricePerDay AS DailyRate,
    MinimumRentalDays INT NOT NULL DEFAULT 1,
    IsAvailable BIT NOT NULL DEFAULT 1,
    Status VARCHAR(30) NOT NULL DEFAULT 'Pending',
    InsuranceDetails VARCHAR(500) NULL,
    RejectionReason VARCHAR(500) NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    ApprovedDate DATETIME NULL,
    ApprovedBy INT NULL,
    CONSTRAINT FK_Vehicles_Seller FOREIGN KEY (SellerId) REFERENCES dbo.UserMaster(U_Id),
    CONSTRAINT FK_Vehicles_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES dbo.UserMaster(U_Id)
);
GO

CREATE TABLE dbo.VehicleImages
(
    ImageId INT IDENTITY(1,1) PRIMARY KEY,
    VehicleId INT NOT NULL,
    ImageUrl VARCHAR(500) NOT NULL,
    CONSTRAINT FK_VehicleImages_Vehicles FOREIGN KEY (VehicleId) REFERENCES dbo.Vehicles(VehicleId) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.VehicleDocuments
(
    DocumentId INT IDENTITY(1,1) PRIMARY KEY,
    VehicleId INT NOT NULL,
    DocumentUrl VARCHAR(500) NOT NULL,
    CONSTRAINT FK_VehicleDocuments_Vehicles FOREIGN KEY (VehicleId) REFERENCES dbo.Vehicles(VehicleId) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.RentalRequests
(
    RequestId INT IDENTITY(1,1) PRIMARY KEY,
    VehicleId INT NOT NULL,
    SellerId INT NOT NULL,
    CustomerId INT NOT NULL,
    UserId INT NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status VARCHAR(30) NOT NULL DEFAULT 'Pending',
    Notes VARCHAR(1000) NULL,
    AdminNotes VARCHAR(1000) NULL,
    RejectionReason VARCHAR(500) NULL,
    CustomerImageUrl VARCHAR(500) NULL,
    IdProofNumber VARCHAR(100) NULL,
    PaymentStatus VARCHAR(30) NOT NULL DEFAULT 'Unpaid',
    PaymentMethod VARCHAR(50) NULL,
    RequestDate DATETIME NOT NULL DEFAULT GETDATE(),
    ApprovalDate DATETIME NULL,
    CONSTRAINT FK_RentalRequests_Vehicles FOREIGN KEY (VehicleId) REFERENCES dbo.Vehicles(VehicleId),
    CONSTRAINT FK_RentalRequests_Seller FOREIGN KEY (SellerId) REFERENCES dbo.UserMaster(U_Id),
    CONSTRAINT FK_RentalRequests_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.UserMaster(U_Id)
);
GO

CREATE TABLE dbo.RentalRequestDocuments
(
    DocumentId INT IDENTITY(1,1) PRIMARY KEY,
    RequestId INT NOT NULL,
    DocumentUrl VARCHAR(500) NOT NULL,
    DocumentName VARCHAR(255) NULL,
    UploadedDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_RentalRequestDocuments_RentalRequests FOREIGN KEY (RequestId) REFERENCES dbo.RentalRequests(RequestId) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.RentalTransactions
(
    TransactionId INT IDENTITY(1,1) PRIMARY KEY,
    RequestId INT NOT NULL,
    CustomerId INT NOT NULL,
    VehicleId INT NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
    Status VARCHAR(30) NOT NULL DEFAULT 'Completed',
    TransactionStatus VARCHAR(30) NOT NULL DEFAULT 'Completed',
    PaymentMethod VARCHAR(50) NOT NULL DEFAULT 'Dummy Payment',
    ReferenceNo VARCHAR(50) NULL,
    CONSTRAINT FK_RentalTransactions_Requests FOREIGN KEY (RequestId) REFERENCES dbo.RentalRequests(RequestId)
);
GO

CREATE TABLE dbo.AdminActions
(
    ActionId INT IDENTITY(1,1) PRIMARY KEY,
    AdminId INT NOT NULL,
    ActionType VARCHAR(60) NOT NULL,
    TargetId INT NOT NULL,
    TargetType VARCHAR(50) NOT NULL,
    ActionDate DATETIME NOT NULL DEFAULT GETDATE(),
    Notes VARCHAR(1000) NULL
);
GO

CREATE OR ALTER PROCEDURE usp_RegisterUser
    @Full_name VARCHAR(50),
    @Password VARCHAR(100),
    @Address VARCHAR(100),
    @Birthdate DATETIME,
    @Contact_No VARCHAR(12),
    @Email VARCHAR(50),
    @RoleName VARCHAR(30) = 'User'
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @NewUserID INT = -1;
    
    BEGIN TRY
        IF EXISTS (SELECT 1 FROM UserMaster WHERE Email = @Email)
            RETURN -1;
        
        DECLARE @RoleId INT;
        SELECT @RoleId = RoleId FROM UserRole WHERE RoleName = @RoleName AND IsActive = 1;
        
        IF @RoleId IS NULL
            RETURN -2;
        
        BEGIN TRANSACTION;
        
        INSERT INTO UserMaster
        (
            Full_name, Password, Address, Birthdate, Contact_No, Email, RoleId, Status, CreatedDate, IsActive
        )
        VALUES
        (
            @Full_name, @Password, ISNULL(NULLIF(@Address, ''), 'Not specified'), @Birthdate,
            ISNULL(NULLIF(@Contact_No, ''), '0000000000'), @Email, @RoleId,
            CASE WHEN @RoleName = 'Seller' THEN 'Pending' ELSE 'Approved' END,
            GETDATE(), 1
        );
        
        SET @NewUserID = SCOPE_IDENTITY();
        
        COMMIT TRANSACTION;
        RETURN @NewUserID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        RETURN -3;
    END CATCH
END
GO

DECLARE @AdminRoleId INT = (SELECT RoleId FROM dbo.UserRole WHERE RoleName = 'Admin');
DECLARE @SellerRoleId INT = (SELECT RoleId FROM dbo.UserRole WHERE RoleName = 'Seller');
DECLARE @UserRoleId INT = (SELECT RoleId FROM dbo.UserRole WHERE RoleName = 'User');

INSERT INTO dbo.UserMaster
    (Full_name, Password, Address, Birthdate, Contact_No, Email, RoleId, Status, CreatedDate, IsActive)
VALUES
    ('System Admin', 'e86f78a8a3caf0b60d8e74e5942aa6d86dc150cd3c03338aef25b7d2d7e3acc7', 'Head Office', '1990-01-01', '9999999999', 'admin@vehiclerental.com', @AdminRoleId, 'Approved', GETDATE(), 1),
    ('Demo Seller', 'bd28c94800c2be055b3329f8dd63a3d5a4137c0def2517bf4fce85eb11e62853', 'Demo Motors', '1992-01-01', '8888888888', 'seller@vehiclerental.com', @SellerRoleId, 'Approved', GETDATE(), 1),
    ('Demo User', '3e7c19576488862816f13b512cacf3e4ba97dd97243ea0bd6a2ad1642d86ba72', 'Customer Address', '1998-01-01', '7777777777', 'user@vehiclerental.com', @UserRoleId, 'Approved', GETDATE(), 1);

DECLARE @SellerId INT = (SELECT U_Id FROM dbo.UserMaster WHERE Email = 'seller@vehiclerental.com');
DECLARE @AdminId INT = (SELECT U_Id FROM dbo.UserMaster WHERE Email = 'admin@vehiclerental.com');

INSERT INTO dbo.Vehicles
    (SellerId, Make, Model, [Year], Description, DailyRate, MinimumRentalDays, IsAvailable, Status, InsuranceDetails, CreatedDate, ApprovedDate, ApprovedBy)
VALUES
    (@SellerId, 'Audi', 'A6', 2022, 'Comfortable premium sedan for city and highway rental.', 4500.00, 1, 1, 'Approved', 'Valid insurance and registration', GETDATE(), GETDATE(), @AdminId),
    (@SellerId, 'BMW', 'X5', 2021, 'Luxury SUV with automatic transmission and spacious seating.', 6500.00, 2, 1, 'Approved', 'Valid insurance and registration', GETDATE(), GETDATE(), @AdminId),
    (@SellerId, 'Mercedes', 'C-Class', 2023, 'Elegant sedan suitable for business and personal travel.', 5500.00, 1, 1, 'Approved', 'Valid insurance and registration', GETDATE(), GETDATE(), @AdminId);

INSERT INTO dbo.VehicleImages (VehicleId, ImageUrl)
SELECT v.VehicleId, i.ImageUrl
FROM dbo.Vehicles v
CROSS APPLY
(
    VALUES
        ('/css/Image/Audi.jpeg'),
        ('/css/Image/BMW.jpeg'),
        ('/css/Image/Mercedes.jpeg')
) i(ImageUrl);
GO
