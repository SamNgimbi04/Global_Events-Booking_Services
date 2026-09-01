using Microsoft.Data.SqlClient;

namespace ITHCA0_GlobalEvents_Service
{
    public class Service1
    {
        private readonly string _connectionString;

        public Service1(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        // CUSTOMER OPERATIONS

        // Register a new customer
        public bool RegisterCustomer(string email, string password, string fullName,
            string idNumber, string address, string contactNumber,
            string securityQuestion, string securityAnswer)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Check if email exists
                    string checkQuery = "SELECT COUNT(*) FROM Customers WHERE Email = @Email";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Email", email);
                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0) return false;
                    }

                    // Insert customer
                    string insertQuery = @"INSERT INTO Customers 
                        (Email, Password, FullName, IDNumber, Address, ContactNumber, SecurityQuestion, SecurityAnswer)
                        VALUES (@Email, @Password, @FullName, @IDNumber, @Address, @ContactNumber, @SecurityQuestion, @SecurityAnswer)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Password", password);
                        cmd.Parameters.AddWithValue("@FullName", fullName);
                        cmd.Parameters.AddWithValue("@IDNumber", idNumber);
                        cmd.Parameters.AddWithValue("@Address", address);
                        cmd.Parameters.AddWithValue("@ContactNumber", contactNumber);
                        cmd.Parameters.AddWithValue("@SecurityQuestion", securityQuestion ?? "");
                        cmd.Parameters.AddWithValue("@SecurityAnswer", securityAnswer ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch { return false; }
        }

        // Update customer information
        public bool UpdateCustomer(int customerID, string fullName, string address, string contactNumber)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"UPDATE Customers 
                        SET FullName = @FullName, Address = @Address, ContactNumber = @ContactNumber
                        WHERE CustomerID = @CustomerID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FullName", fullName);
                        cmd.Parameters.AddWithValue("@Address", address);
                        cmd.Parameters.AddWithValue("@ContactNumber", contactNumber);
                        cmd.Parameters.AddWithValue("@CustomerID", customerID);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch { return false; }
        }

        // ACCOUNT OPERATIONS

        // Add new account
        public bool AddAccount(int customerID, string accountType, int initialTickets)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO Accounts (CustomerID, AccountType, TicketBalance, DateCreated)
                        VALUES (@CustomerID, @AccountType, @TicketBalance, @DateCreated)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CustomerID", customerID);
                        cmd.Parameters.AddWithValue("@AccountType", accountType);
                        cmd.Parameters.AddWithValue("@TicketBalance", initialTickets);
                        cmd.Parameters.AddWithValue("@DateCreated", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch { return false; }
        }

        // Top up ticket balance
        public bool TopUpAccount(int accountID, int ticketsToAdd)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Update balance
                    string updateQuery = "UPDATE Accounts SET TicketBalance = TicketBalance + @Tickets WHERE AccountID = @AccountID";
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Tickets", ticketsToAdd);
                        cmd.Parameters.AddWithValue("@AccountID", accountID);
                        cmd.ExecuteNonQuery();
                    }

                    // Log top-up transaction
                    string logQuery = @"INSERT INTO Transactions (AccountID, EventID, TicketsBooked, TotalAmount, BookingDate)
                        VALUES (@AccountID, NULL, @Tickets, 0, @Date)";
                    using (SqlCommand cmd = new SqlCommand(logQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccountID", accountID);
                        cmd.Parameters.AddWithValue("@Tickets", ticketsToAdd);
                        cmd.Parameters.AddWithValue("@Date", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch { return false; }
        }

        // BOOKING OPERATIONS
     
        // Book tickets for an event
        public string BookTickets(int accountID, int eventID, int quantity)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Get account balance
                    int balance = 0;
                    string balQuery = "SELECT TicketBalance FROM Accounts WHERE AccountID = @AccountID";
                    using (SqlCommand cmd = new SqlCommand(balQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccountID", accountID);
                        balance = (int)cmd.ExecuteScalar();
                    }

                    if (quantity > balance)
                        return "INSUFFICIENT_BALANCE";

                    // Get event details
                    int availableTickets = 0;
                    decimal ticketPrice = 0;
                    string eventQuery = "SELECT AvailableTickets, TicketPrice FROM Events WHERE EventID = @EventID";
                    using (SqlCommand cmd = new SqlCommand(eventQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@EventID", eventID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                availableTickets = (int)reader["AvailableTickets"];
                                ticketPrice = (decimal)reader["TicketPrice"];
                            }
                        }
                    }

                    if (quantity > availableTickets)
                        return "INSUFFICIENT_EVENT_TICKETS";

                    decimal totalAmount = quantity * ticketPrice;

                    // Deduct from account
                    string updateAccount = "UPDATE Accounts SET TicketBalance = TicketBalance - @Qty WHERE AccountID = @AccountID";
                    using (SqlCommand cmd = new SqlCommand(updateAccount, conn))
                    {
                        cmd.Parameters.AddWithValue("@Qty", quantity);
                        cmd.Parameters.AddWithValue("@AccountID", accountID);
                        cmd.ExecuteNonQuery();
                    }

                    // Deduct from event
                    string updateEvent = "UPDATE Events SET AvailableTickets = AvailableTickets - @Qty WHERE EventID = @EventID";
                    using (SqlCommand cmd = new SqlCommand(updateEvent, conn))
                    {
                        cmd.Parameters.AddWithValue("@Qty", quantity);
                        cmd.Parameters.AddWithValue("@EventID", eventID);
                        cmd.ExecuteNonQuery();
                    }

                    // Log transaction
                    string logQuery = @"INSERT INTO Transactions (AccountID, EventID, TicketsBooked, TotalAmount, BookingDate)
                        VALUES (@AccountID, @EventID, @Qty, @Total, @Date)";
                    using (SqlCommand cmd = new SqlCommand(logQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccountID", accountID);
                        cmd.Parameters.AddWithValue("@EventID", eventID);
                        cmd.Parameters.AddWithValue("@Qty", quantity);
                        cmd.Parameters.AddWithValue("@Total", totalAmount);
                        cmd.Parameters.AddWithValue("@Date", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }

                    // Write to log file
                    WriteLog($"BOOKING: AccountID={accountID}, EventID={eventID}, Qty={quantity}, Total=R{totalAmount}, Date={DateTime.Now}");
                }
                return "SUCCESS";
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.Message;
            }
        }

       
        // LOG FILE

        // Write booking log to file
        public void WriteLog(string message)
        {
            try
            {
                string logPath = "C:\\GlobalEventsLogs\\transactions.log";
                Directory.CreateDirectory("C:\\GlobalEventsLogs");
                using (StreamWriter sw = new StreamWriter(logPath, append: true))
                {
                    sw.WriteLine($"[{DateTime.Now}] {message}");
                }
            }
            catch { }
        }
    }
}

