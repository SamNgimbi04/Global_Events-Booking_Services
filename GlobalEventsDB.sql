--CREATE DATABASE GlobalEventsDB;

-- Purpose: Database for Global Events ticket booking system


-- =====================
-- CUSTOMERS TABLE
-- =====================
CREATE TABLE Customers (
    CustomerID      INT IDENTITY(1,1) PRIMARY KEY,
    FullName        NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(100)   NOT NULL UNIQUE,
    Password        NVARCHAR(100)   NOT NULL,
    IDNumber        NVARCHAR(20)    NOT NULL,
    Address         NVARCHAR(200)   NOT NULL,
    ContactNumber   NVARCHAR(20)    NOT NULL,
    SecurityQuestion NVARCHAR(200)  NULL,
    SecurityAnswer  NVARCHAR(100)   NULL
);
GO

-- =====================
-- ACCOUNTS TABLE
-- =====================
CREATE TABLE Accounts (
    AccountID       INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID      INT             NOT NULL,
    AccountType     NVARCHAR(50)    NOT NULL,
    TicketBalance   INT             NOT NULL DEFAULT 0,
    DateCreated     DATETIME        NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID)
);
GO

-- =====================
-- EVENTS TABLE
-- =====================
CREATE TABLE Events (
    EventID             INT IDENTITY(1,1) PRIMARY KEY,
    EventName           NVARCHAR(100)   NOT NULL,
    EventDate           DATETIME        NOT NULL,
    AvailableTickets    INT             NOT NULL,
    TicketPrice         DECIMAL(10,2)   NOT NULL
);
GO

-- =====================
-- TRANSACTIONS TABLE
-- =====================
CREATE TABLE Transactions (
    TransactionID   INT IDENTITY(1,1) PRIMARY KEY,
    AccountID       INT             NOT NULL,
    EventID         INT             NULL,       -- NULL for top-ups
    TicketsBooked   INT             NOT NULL,
    TotalAmount     DECIMAL(10,2)   NOT NULL,
    BookingDate     DATETIME        NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (AccountID) REFERENCES Accounts(AccountID),
    FOREIGN KEY (EventID)   REFERENCES Events(EventID)
);
GO

-- =====================
-- SAMPLE DATA
-- =====================

-- Sample Customers
INSERT INTO Customers (FullName, Email, Password, IDNumber, Address, ContactNumber, SecurityQuestion, SecurityAnswer)
VALUES 
('John Smith',  'john@email.com',  'password123', '9001015012083', '12 Main St, Cape Town',    '0821234567', 'What is your pet name?', 'Buddy'),
('Jane Doe',    'jane@email.com',  'password123', '9505025012083', '45 Long St, Johannesburg', '0839876543', 'What is your mothers name?', 'Mary');
GO

-- Sample Accounts
INSERT INTO Accounts (CustomerID, AccountType, TicketBalance, DateCreated)
VALUES
(1, 'Standard', 10, GETDATE()),
(1, 'Premium',  20, GETDATE()),
(2, 'VIP',      15, GETDATE());
GO

-- Sample Events
INSERT INTO Events (EventName, EventDate, AvailableTickets, TicketPrice)
VALUES
('Cape Town Jazz Festival',     '2026-08-15', 100, 250.00),
('Joburg Comedy Night',         '2026-09-01', 50,  150.00),
('Durban Food & Wine Festival', '2026-09-20', 75,  180.00),
('Pretoria Art Exhibition',     '2026-10-05', 30,  80.00),
('Bloemfontein Music Fest',     '2026-10-18', 60,  120.00);
GO

-- Sample Transactions
INSERT INTO Transactions (AccountID, EventID, TicketsBooked, TotalAmount, BookingDate)
VALUES
(1, 1, 2, 500.00,  GETDATE()),
(1, 2, 1, 150.00,  GETDATE()),
(2, NULL, 5, 0.00, GETDATE()); -- Top up
GO