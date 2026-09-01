using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace ITHCA0__Global_Events.Pages
{
    //Model to hold account data
    public class AccountModel
    {
        public int AccountID { get; set; }
        public string AccountType { get; set; } = string.Empty;
        public int TicketBalance { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class DashboardModel : PageModel
    {
        private readonly string _connectionString;

        public DashboardModel(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        public string FullName { get; set; } = string.Empty;
        public List<AccountModel> Accounts { get; set; } = new List<AccountModel>();

        [BindProperty]
        public string NewAccountType { get; set; } = string.Empty;

        [BindProperty]
        public int InitialTickets { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public IActionResult OnGet() 
        {
            //Check login session
            if (HttpContext.Session.GetString("CustomerID") == null)
                return RedirectToPage("/Login");

            FullName = HttpContext.Session.GetString("FullName") ?? "Customer";
            LoadAccounts();
            return Page();
        }

        private void LoadAccounts()
        {
            int customerID = int.Parse(HttpContext.Session.GetString("CustomerID")!);
            
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT AccountID, AccountType, TicketBalance, DateCreated FROM Accounts WHERE CustomerID = @CustomerID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CustomerID", customerID);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Accounts.Add(new AccountModel
                            {
                                AccountID = (int)reader["AccountID"],
                                AccountType = reader["AccountType"].ToString()!,
                                TicketBalance = (int)reader["TicketBalance"],
                                DateCreated = (DateTime)reader["DateCreated"]
                            });
                        }
                    }
                }
            }
        }

        //Create new account
        public IActionResult OnPostCreateAccount()
        {
            if (HttpContext.Session.GetString("CustomerID") == null)
                return RedirectToPage("/Login");

            if (InitialTickets < 1)
            {
                ErrorMessage = "Initial tickets must be at least 1.";
                FullName = HttpContext.Session.GetString("FullName") ?? "Customer";
                LoadAccounts();
                return Page();
            }

            try
            {
                int customerID = int.Parse(HttpContext.Session.GetString("CustomerID")!);

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO Accounts (CustomerID, AccountType, TicketBalance, DateCreated)
                                    VALUES (@CustomerID, @AccountType, @TicketBalance, @DateCreated)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CustomerID", customerID);
                        cmd.Parameters.AddWithValue("@AccountType", NewAccountType);
                        cmd.Parameters.AddWithValue("@TicketBalance", InitialTickets);
                        cmd.Parameters.AddWithValue("@DateCreated", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }

                }
                SuccessMessage = "Account created successfully!";         
            }

            catch (Exception ex)
            {
                ErrorMessage = "Error: " + ex.Message;
            }

            FullName = HttpContext.Session.GetString("FullName") ?? "Customer";
            LoadAccounts();
            return Page();
        }

        //Logout 
        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login");
        }
    }
}


