ALTER PROCEDURE usp_RegisterUser
    @Full_name VARCHAR(50),
    @Password VARCHAR(100),
    @Address VARCHAR(100),
    @Birthdate DATETIME,
    @Contact_No VARCHAR(12),
    @Email VARCHAR(50),
    @RoleName VARCHAR(30) = 'Customer'
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @NewUserID INT = -1;
    DECLARE @ErrorCode INT = 0;
    DECLARE @ErrorMessage NVARCHAR(4000);
    
    BEGIN TRY
        -- Check if email exists (outside transaction)
        IF EXISTS (SELECT 1 FROM UserMaster WHERE Email = @Email)
        BEGIN
            RETURN -1; -- Email exists
        END
        
        -- Get RoleId (outside transaction)
        DECLARE @RoleId INT;
        SELECT @RoleId = RoleId FROM UserRole WHERE RoleName = @RoleName;
        
        IF @RoleId IS NULL
        BEGIN
            RETURN -2; -- Invalid role
        END
        
        BEGIN TRANSACTION;
        
        -- Insert user
        INSERT INTO UserMaster (
            Full_name, Password, Address, Birthdate, 
            Contact_No, Email, RoleId, CreatedDate, IsActive
        )
        VALUES (
            @Full_name, @Password, @Address, @Birthdate,
            @Contact_No, @Email, @RoleId, GETDATE(), 1
        );
        
        SET @NewUserID = SCOPE_IDENTITY();
        
        COMMIT TRANSACTION;
        RETURN @NewUserID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        SET @ErrorCode = ERROR_NUMBER();
        SET @ErrorMessage = ERROR_MESSAGE();
        
        -- Log error if you have an error logging table
        -- INSERT INTO ErrorLog(...) VALUES (@ErrorCode, @ErrorMessage, ...)
        
        RETURN -3; -- Error occurred
    END CATCH
END