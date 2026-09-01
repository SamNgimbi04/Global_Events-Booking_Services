using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;

namespace ITHCA0__Global_Events.Pages
{
    public class TopUpModel : PageModel
    {
        private readonly string _connectionString;

        public TopUpModel(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        [BindProperty(SupportsGet = true)]
        public int AccountId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Please enter number of tickets to add")]
        [Range(1, int.MaxValue, ErrorMessage = "Must add at least 1 ticket")]
        public int TicketsToAdd { get; set; }

        public int CurrentBalance { get; set; }
        public string AccountType { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public IActionResult OnGet()
        {
            // Check login session
            if (HttpContext.Session.GetString("CustomerID") == null)
                return RedirectToPage("/Login");

            LoadAccountInfo();
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

        public IActionResult OnPostTopUp()
        {
            // Check login session
            if (HttpContext.Session.GetString("CustomerID") == null)
                return RedirectToPage("/Login");

            if (!ModelState.IsValid)
            {
                LoadAccountInfo();
                return Page();
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Update account balance
                    string updateQuery = "UPDATE Accounts SET TicketBalance = TicketBalance + @TicketsToAdd WHERE AccountID = @AccountID";
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@TicketsToAdd", TicketsToAdd);
                        cmd.Parameters.AddWithValue("@AccountID", AccountId);
                        cmd.ExecuteNonQuery();
                    }

                    // Log top-up as a transaction
                    string insertTransaction = @"INSERT INTO Transactions 
                        (AccountID, EventID, TicketsBooked, TotalAmount, BookingDate)
                        VALUES (@AccountID, NULL, @TicketsToAdd, 0, @BookingDate)";
                    using (SqlCommand cmd = new SqlCommand(insertTransaction, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccountID", AccountId);
                        cmd.Parameters.AddWithValue("@TicketsToAdd", TicketsToAdd);
                        cmd.Parameters.AddWithValue("@BookingDate", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }

                    SuccessMessage = $"{TicketsToAdd} tickets added successfully!";
                }

                // Reload updated balance
                LoadAccountInfo();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error: " + ex.Message;
                LoadAccountInfo();
                return Page();
            }
        }
    }

}

