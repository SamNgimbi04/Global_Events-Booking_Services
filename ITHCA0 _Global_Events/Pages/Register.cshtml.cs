//Author: [Your Name]
//Date: [Date]
//Purpose: Handles customer registration for Global Events system

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;
using System.Runtime.CompilerServices;

namespace ITHCA0__Global_Events.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly string _connectionString;

        public RegisterModel(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        [BindProperty]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email {  get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "ID number is required")]
        public string IDNumber { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Contact number is required")]
        public string ContactNumber { get; set; } = string.Empty;

        [BindProperty]
        public string? SecurityQuestion { get; set; }

        [BindProperty]
        public string? SecurityAnswer { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
            //Redirect if already logged in
            if (HttpContext.Session.GetString("CustomerID") != null)
                Response.Redirect("/Dashboard");
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
                return Page();
            
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    //Check if email already exists
                    string checkQuery = "SELECT COUNT (*) FROM Customers WHERE Email = @Email";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Email", Email);
                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            ErrorMessage = "Email already exists. Please use a different email.";
                            return Page();
                        }
                    }

                    //Insert new customer
                    string insertQuery = @"INSERT INTO Customers
                           (Email, Password, FullName, IDNumber, Address, ContactNumber, SecurityQuestion, SecurityAnswer)
                            VALUES (@Email, @Password, @FullName, @IDNumber, @Address, @ContactNumber, @SecurityQuestion, @SecurityAnswer)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", Email);
                        cmd.Parameters.AddWithValue("@Password", Password);
                        cmd.Parameters.AddWithValue("@FullName", FullName);
                        cmd.Parameters.AddWithValue("@IDNumber", IDNumber);
                        cmd.Parameters.AddWithValue("@Address", Address);
                        cmd.Parameters.AddWithValue("@ContactNumber", ContactNumber);
                        cmd.Parameters.AddWithValue("@SecurityQuestion", SecurityQuestion ?? "");
                        cmd.Parameters.AddWithValue("@SecurityAnswer", SecurityAnswer ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }

                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
               ErrorMessage = "An error occurred: " + ex.Message;
                return Page();
            }
        }
    }
}
