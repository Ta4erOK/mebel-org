/* База данных ООО «МебельОрг» — 3 нормальная форма.
   Скрипт идемпотентен: пересоздаёт базу целиком.
   Применение: sqlcmd -S "DESKTOP-FINIK\SQLEXPRESS" -E -b -f 65001 -i db\schema.sql */

IF DB_ID(N'MebelOrg') IS NOT NULL
BEGIN
    ALTER DATABASE MebelOrg SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE MebelOrg;
END
GO

CREATE DATABASE MebelOrg;
GO

USE MebelOrg;
GO

CREATE TABLE Roles
(
    Id   INT IDENTITY (1, 1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    Name NVARCHAR(50)        NOT NULL CONSTRAINT UQ_Roles_Name UNIQUE
);
GO

CREATE TABLE Units
(
    Id   INT IDENTITY (1, 1) NOT NULL CONSTRAINT PK_Units PRIMARY KEY,
    Name NVARCHAR(20)        NOT NULL CONSTRAINT UQ_Units_Name UNIQUE
);
GO

CREATE TABLE Categories
(
    Id   INT IDENTITY (1, 1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    Name NVARCHAR(100)       NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE
);
GO

CREATE TABLE Manufacturers
(
    Id   INT IDENTITY (1, 1) NOT NULL CONSTRAINT PK_Manufacturers PRIMARY KEY,
    Name NVARCHAR(100)       NOT NULL CONSTRAINT UQ_Manufacturers_Name UNIQUE
);
GO

CREATE TABLE Suppliers
(
    Id   INT IDENTITY (1, 1) NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
    Name NVARCHAR(100)       NOT NULL CONSTRAINT UQ_Suppliers_Name UNIQUE
);
GO

CREATE TABLE Statuses
(
    Id   INT IDENTITY (1, 1) NOT NULL CONSTRAINT PK_Statuses PRIMARY KEY,
    Name NVARCHAR(50)        NOT NULL CONSTRAINT UQ_Statuses_Name UNIQUE
);
GO

CREATE TABLE PickupPoints
(
    Id      INT IDENTITY (1, 1) NOT NULL CONSTRAINT PK_PickupPoints PRIMARY KEY,
    Address NVARCHAR(200)       NOT NULL CONSTRAINT UQ_PickupPoints_Address UNIQUE
);
GO

CREATE TABLE Users
(
    Id       INT IDENTITY (1, 1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    RoleId   INT                 NOT NULL CONSTRAINT FK_Users_Roles REFERENCES Roles (Id),
    FullName NVARCHAR(150)       NOT NULL,
    Login    NVARCHAR(100)       NOT NULL CONSTRAINT UQ_Users_Login UNIQUE,
    Password NVARCHAR(100)       NOT NULL
);
GO

CREATE TABLE Products
(
    Id             INT IDENTITY (1, 1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    Article        NVARCHAR(50)        NOT NULL CONSTRAINT UQ_Products_Article UNIQUE,
    Name           NVARCHAR(300)       NOT NULL,
    Description    NVARCHAR(MAX)       NULL,
    CategoryId     INT                 NOT NULL CONSTRAINT FK_Products_Categories REFERENCES Categories (Id),
    ManufacturerId INT                 NOT NULL CONSTRAINT FK_Products_Manufacturers REFERENCES Manufacturers (Id),
    SupplierId     INT                 NOT NULL CONSTRAINT FK_Products_Suppliers REFERENCES Suppliers (Id),
    UnitId         INT                 NOT NULL CONSTRAINT FK_Products_Units REFERENCES Units (Id),
    Price          DECIMAL(10, 2)      NOT NULL CONSTRAINT CK_Products_Price CHECK (Price >= 0),
    Quantity       INT                 NOT NULL CONSTRAINT CK_Products_Quantity CHECK (Quantity >= 0),
    Discount       DECIMAL(5, 2)       NOT NULL CONSTRAINT DF_Products_Discount DEFAULT (0)
        CONSTRAINT CK_Products_Discount CHECK (Discount >= 0 AND Discount < 100),
    ImagePath      NVARCHAR(200)       NULL
);
GO

CREATE TABLE Orders
(
    Id            INT IDENTITY (1, 1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    Number        INT                 NOT NULL CONSTRAINT UQ_Orders_Number UNIQUE,
    UserId        INT                 NOT NULL CONSTRAINT FK_Orders_Users REFERENCES Users (Id),
    PickupPointId INT                 NOT NULL CONSTRAINT FK_Orders_PickupPoints REFERENCES PickupPoints (Id),
    StatusId      INT                 NOT NULL CONSTRAINT FK_Orders_Statuses REFERENCES Statuses (Id),
    OrderDate     DATE                NOT NULL,
    DeliveryDate  DATE                NULL,
    Code          INT                 NOT NULL
);
GO

CREATE TABLE OrderItems
(
    Id        INT IDENTITY (1, 1) NOT NULL CONSTRAINT PK_OrderItems PRIMARY KEY,
    OrderId   INT                 NOT NULL CONSTRAINT FK_OrderItems_Orders REFERENCES Orders (Id) ON DELETE CASCADE,
    ProductId INT                 NOT NULL CONSTRAINT FK_OrderItems_Products REFERENCES Products (Id),
    Quantity  INT                 NOT NULL CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT UQ_OrderItems_Order_Product UNIQUE (OrderId, ProductId)
);
GO

CREATE INDEX IX_Users_RoleId ON Users (RoleId);
CREATE INDEX IX_Products_CategoryId ON Products (CategoryId);
CREATE INDEX IX_Products_ManufacturerId ON Products (ManufacturerId);
CREATE INDEX IX_Products_SupplierId ON Products (SupplierId);
CREATE INDEX IX_Products_UnitId ON Products (UnitId);
CREATE INDEX IX_Orders_UserId ON Orders (UserId);
CREATE INDEX IX_Orders_PickupPointId ON Orders (PickupPointId);
CREATE INDEX IX_Orders_StatusId ON Orders (StatusId);
CREATE INDEX IX_OrderItems_OrderId ON OrderItems (OrderId);
CREATE INDEX IX_OrderItems_ProductId ON OrderItems (ProductId);
GO
