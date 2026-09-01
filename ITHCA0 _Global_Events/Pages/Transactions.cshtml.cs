using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace ITHCA0__Global_Events.Pages
{
    public class TransactionItem
    {
        public int TransactionID { get; set; }
        public int AccountID { get; set; }
        public string? EventName { get; set; }
        public DateTime BookingDate { get; set; }
        public int TicketsBooked { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class TransactionsModel : PageModel
    {
        private readonly string _connectionString;

        public TransactionsModel(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        [BindProperty(SupportsGet = true)]
        public int AccountId { get; set; }

        public string AccountType { get; set; } = string.Empty;
        public int CurrentBalance { get; set; }
        public List<TransactionItem> Transactions { get; set; } = new List<TransactionItem>();
        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            // Check login session
            if (HttpContext.Session.GetString("CustomerID") == null)
                return RedirectToPage("/Login");

            LoadAccountInfo();
            LoadTransactions();
            return Page();
        }

        private void LoadAccountInfo()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT AccountType, TicketBalance FROM Accounts WHERE AccountID = @AccountID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AccountID", AccountId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            AccountType = reader["AccountType"].ToString()!;
                            CurrentBalance = (int)reader["TicketBalance"];
                        }
                    }
                }
            }
        }

        private void LoadTransactions()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Join with Events to get event name
                    // EventID can be NULL for top-ups
                    string query = @"
                        SELECT 
                            t.TransactionID,
                            t.AccountID,
                            e.EventName,
                            t.BookingDate,
                            t.TicketsBooked,
                            t.TotalAmount
                        FROM Transactions t
                        LEFT JOIN Events e ON t.EventID = e.EventID
                        WHERE t.AccountID = @AccountID
                        ORDER BY t.BookingDate DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccountID", AccountId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Transactions.Add(new TransactionItem
                                {
                                    TransactionID = (int)reader["TransactionID"],
                                    AccountID = (int)reader["AccountID"],
                                    EventName = reader["EventName"] == DBNull.Value
                                                ? null
                                                : reader["EventName"].ToString(),
                                    BookingDate = (DateTime)reader["BookingDate"],
                                    TicketsBooked = (int)reader["TicketsBooked"],
                                    TotalAmount = (decimal)reader["TotalAmount"]
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading transactions: " + ex.Message;
            }
        }
    }

}
