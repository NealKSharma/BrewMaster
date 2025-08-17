-------------------------------------------------------
-- 1. DATABASE CREATION (If it does not exists)
-------------------------------------------------------
IF DB_ID('BrewMaster') IS NULL
BEGIN
    CREATE DATABASE BrewMaster
END
GO

USE BrewMaster;
GO

-- UPDATE tblUserMaster set UserRole = 'Admin' Where UserId = 1
-- Use this to set a user as Admin

-------------------------------------------------------
-- 2. TABLE CREATION
-------------------------------------------------------

-- 2.1 Users Table
CREATE TABLE tblUserMaster (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    UserName NVARCHAR(100) UNIQUE NOT NULL,
    UserPassword NVARCHAR(255) NOT NULL,
    FirstName NVARCHAR(100),
    SurName NVARCHAR(100),
    UserRole NVARCHAR(20) DEFAULT 'User', -- 'User' or 'Admin'
    Mobile NVARCHAR(20),
    StreetAddress NVARCHAR(500),
    City NVARCHAR(100),
    UserState NVARCHAR(100),
    PostalCode NVARCHAR(20),
    Country NVARCHAR(100),
    SecurityQuestion NVARCHAR(250),
    SecurityAnswer NVARCHAR(250),
    EntryDate DATETIME DEFAULT GETDATE()
);
GO

-- 2.2 Products Table
CREATE TABLE tblProducts (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(255) NOT NULL,
    ProductDescription NVARCHAR(MAX),
    ProductImage VARBINARY(MAX),
    Price DECIMAL(10,2) NOT NULL,
    Stock INT DEFAULT 0,
    CreationDate DATETIME DEFAULT GETDATE()
);
GO

-- 2.3 Orders Table
CREATE TABLE tblOrders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    OrderTotal DECIMAL(10,2) NOT NULL,
    OrderStatus NVARCHAR(50) DEFAULT 'Pending',
    StreetAddress NVARCHAR(500),
    City NVARCHAR(100),
    UserState NVARCHAR(100),
    PostalCode NVARCHAR(20),
    Country NVARCHAR(100),
    Mobile NVARCHAR(20),
    OrderDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES tblUserMaster(UserId)
);
GO

-- 2.4 OrderItems Table
CREATE TABLE tblOrderItems (
    OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (OrderId) REFERENCES tblOrders(OrderId),
    FOREIGN KEY (ProductId) REFERENCES tblProducts(ProductId)
);
GO

-- 2.5 Cart Table
CREATE TABLE tblCart (
    CartId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    AddedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES tblUserMaster(UserId),
    FOREIGN KEY (ProductId) REFERENCES tblProducts(ProductId)
);
GO

-- 2.6 Login Audit Table
CREATE TABLE tblLoginAudit (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT,
    UserName NVARCHAR(100),
    UserRole NVARCHAR(50),
    IPAddress NVARCHAR(50),
    LoginTime DATETIME DEFAULT GETDATE()
);
GO

-- 2.7 Error Log Table
CREATE TABLE ErrorLog (
    ErrorId INT IDENTITY(1,1) PRIMARY KEY,
    ErrorMessage NVARCHAR(1000) NOT NULL,
    ErrorFileName NVARCHAR(200),
    MethodName NVARCHAR(100),
    LineNumber INT,
    StackTrace NVARCHAR(MAX),
    ErrorDate DATETIME DEFAULT GETDATE()
);
GO

-- 2.8 Users Log
CREATE TABLE tblUserMaster_Log (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    ActionType NVARCHAR(10),
    PerformedBy NVARCHAR(100),
    ActionDate DATETIME DEFAULT GETDATE(),
    UserId INT,
    UserName NVARCHAR(100),
    Email NVARCHAR(150),
    UserRole NVARCHAR(50)
);
GO

-- 2.9 Products Log
CREATE TABLE tblProducts_Log (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    ActionType NVARCHAR(10),
    PerformedBy NVARCHAR(100),
    ActionDate DATETIME DEFAULT GETDATE(),
    ProductId INT,
    ProductName NVARCHAR(100),
    Price DECIMAL(10, 2),
    Stock INT
);
GO

-------------------------------------------------------
-- 3. TRIGGERS
-------------------------------------------------------

-- 3.1 UserMaster Log Trigger
CREATE TRIGGER trg_tblUserMaster_Log
ON tblUserMaster
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @PerformedBy NVARCHAR(100);
    SET @PerformedBy = CAST(SESSION_CONTEXT(N'username') AS NVARCHAR(100));

    -- INSERT
    IF EXISTS (SELECT * FROM INSERTED) AND NOT EXISTS (SELECT * FROM DELETED)
        INSERT INTO tblUserMaster_Log(ActionType, PerformedBy, ActionDate, UserId, UserName, Email, UserRole)
        SELECT 'INSERT', @PerformedBy, GETDATE(), UserId, UserName, Email, UserRole FROM INSERTED;

    -- DELETE
    IF EXISTS (SELECT * FROM DELETED) AND NOT EXISTS (SELECT * FROM INSERTED)
        INSERT INTO tblUserMaster_Log(ActionType, PerformedBy, ActionDate, UserId, UserName, Email, UserRole)
        SELECT 'DELETE', @PerformedBy, GETDATE(), UserId, UserName, Email, UserRole FROM DELETED;

    -- UPDATE
    IF EXISTS (SELECT * FROM INSERTED) AND EXISTS (SELECT * FROM DELETED)
        INSERT INTO tblUserMaster_Log(ActionType, PerformedBy, ActionDate, UserId, UserName, Email, UserRole)
        SELECT 'UPDATE', @PerformedBy, GETDATE(), UserId, UserName, Email, UserRole FROM INSERTED;
END;
GO

-- 3.2 Products Log Trigger
CREATE TRIGGER trg_tblProducts_Log
ON tblProducts
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @PerformedBy NVARCHAR(100);
    SET @PerformedBy = CAST(SESSION_CONTEXT(N'username') AS NVARCHAR(100));

    -- INSERT
    IF EXISTS (SELECT * FROM INSERTED) AND NOT EXISTS (SELECT * FROM DELETED)
        INSERT INTO tblProducts_Log(ActionType, PerformedBy, ActionDate, ProductId, ProductName, Price, Stock)
        SELECT 'INSERT', @PerformedBy, GETDATE(), ProductId, ProductName, Price, Stock FROM INSERTED;

    -- DELETE
    IF EXISTS (SELECT * FROM DELETED) AND NOT EXISTS (SELECT * FROM INSERTED)
        INSERT INTO tblProducts_Log(ActionType, PerformedBy, ActionDate, ProductId, ProductName, Price, Stock)
        SELECT 'DELETE', @PerformedBy, GETDATE(), ProductId, ProductName, Price, Stock FROM DELETED;

    -- UPDATE
    IF EXISTS (SELECT * FROM INSERTED) AND EXISTS (SELECT * FROM DELETED)
        INSERT INTO tblProducts_Log(ActionType, PerformedBy, ActionDate, ProductId, ProductName, Price, Stock)
        SELECT 'UPDATE', @PerformedBy, GETDATE(), ProductId, ProductName, Price, Stock FROM INSERTED;
END;
GO

-------------------------------------------------------
-- 4. STORED PROCEDURES
-------------------------------------------------------

-- 4.1 Update User Details
CREATE PROCEDURE UpdateUserDetails
    @UserName nvarchar(50),
    @FirstName nvarchar(50),
    @SurName nvarchar(50),
    @StreetAddress nvarchar(250),
    @City nvarchar(50),
    @UserState nvarchar(50),
    @PostalCode nvarchar(10),
    @Country nvarchar(50),
    @Mobile nvarchar(50)
AS
BEGIN
    UPDATE tblUserMaster
    SET FirstName = @FirstName,
        SurName = @SurName,
        StreetAddress = @StreetAddress,
        City = @City,
        UserState = @UserState,
        PostalCode = @PostalCode,
        Country = @Country,
        Mobile = @Mobile
    WHERE UserName = @UserName;
END;
GO

-- 4.2 Insert User Signup With Security
CREATE PROCEDURE InsertUserSignupWithSecurity
    @Email NVARCHAR(255),
    @UserName NVARCHAR(100),
    @UserPassword NVARCHAR(255),
    @SecurityQuestion NVARCHAR(250),
    @SecurityAnswer NVARCHAR(250)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO tblUserMaster (Email, UserName, UserPassword, SecurityQuestion, SecurityAnswer, EntryDate)
    VALUES (@Email, @UserName, @UserPassword, @SecurityQuestion, LOWER(LTRIM(RTRIM(@SecurityAnswer))), GETDATE());
END;
GO

-- 4.3 Check if User Exists
CREATE PROCEDURE sp_ExistsUserMaster
    @FieldName NVARCHAR(128),
    @FieldValue NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    IF COL_LENGTH('tblUserMaster', @FieldName) IS NULL
    BEGIN
        RAISERROR('Invalid column name.', 16, 1);
        RETURN;
    END

    DECLARE @sql NVARCHAR(MAX) = 
        'SELECT COUNT(*) FROM tblUserMaster WHERE ' + QUOTENAME(@FieldName) + ' = @val';
    EXEC sp_executesql @sql, N'@val NVARCHAR(256)', @val = @FieldValue;
END;
GO

-------------------------------------------------------
-- 5. INSERT DUMMY DATA
-------------------------------------------------------

-- 5.1 Products
INSERT INTO tblProducts (ProductName, ProductDescription, Price, Stock, CreationDate)
VALUES
('Espresso Roast', 'Our signature espresso roast delivers an intense, bold, and rich flavor. Perfect for those who love their coffee strong. Made from carefully selected beans and roasted perfectly.', 99.00, 6, '2025-08-11 14:36:29.927'),
('Premium Arabica', 'Experience the smooth, rich taste of our premium Arabica beans. Grown in high-altitude regions, these beans offer a complex flavor profile with sweetness and aromatic qualities.', 119.00, 3, '2025-08-11 14:36:48.840'),
('Colombian Supreme', 'A classic from the mountains of Colombia. This supreme blend offers a well-balanced cup with rich flavor and aromatic finish that coffee lovers have enjoyed for generations.', 149.00, 7, '2025-08-11 14:37:12.230'),
('French Roast', 'Our darkest roast with an intense, smoky flavor. This French roast delivers a bold, robust cup with low acidity and a rich, full body that stands up to any addition.', 129.00, 5, '2025-08-11 14:39:12.230');
 -- Images will be Place Holder images (You can update these products to add image from the product management tab in the admin controller)
GO