using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;

namespace ITHCA0__Global_Events.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly string _connectionString;

        public ForgotPasswordModel(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string? SecurityAnswer { get; set; }

        [BindProperty]
        public string? NewPassword { get; set; }

        public string? SecurityQuestion {  get; set; }
        public string? ErrorMessage {  get; set; }
        public string? SuccessMessage {  get; set; }
        public bool QuestionVisible { get; set; } = false;

        public void OnGet() { }
        
        //Step 1: Find account by email
        public IActionResult OnPostFindEmail()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = "SELECT SecurityQuestion FROM Customers WHERE Email = @Email";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", Email);
                        var result = cmd.ExecuteScalar();

                   
                        if (result != null)
                        {
                            SecurityQuestion = result.ToString();
                            QuestionVisible = true;        
                        }
                        else
                        {
                            ErrorMessage = "No account found with that email.";
                        }
                        
                    }
                }
            }

            catch (Exception ex)
            {
                ErrorMessage = "An error occurred: " + ex.Message;
            }

            return Page();
        }

        //Step 2: Verify answer and reset password
        public IActionResult OnPostResetEmail()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    //Verify security answer
                    string checkQuery = "SELECT COUNT (*) FROM Customers WHERE Email = @Email AND SecurityAnswer = @Answer";

                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Email", Email);
                        checkCmd.Parameters.AddWithValue("@Answer", SecurityAnswer ?? "");
                        int count = (int)checkCmd.ExecuteScalar();


                        if (count == 0)
                        {
                            ErrorMessage = "Incorrect answer. Please try again.";
                            QuestionVisible = true;

                            //Reload security question
                            string qQuery = "SELECT SecurityQuestion FROM  Customers WHERE Email = @Email";
                            using (SqlCommand qCmd = new SqlCommand(qQuery, conn))
                            {
                                qCmd.Parameters.AddWithValue("@Email", Email);
                                SecurityQuestion = qCmd.ExecuteScalar()?.ToString();
                            }
                            return Page();
                        }
                       
                    }

                    //Update password
                    string updateQuery = "UPDATE Customers SET Password = @NewPassword WHERE Email = @Email";
                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@NewPassword", NewPassword ?? "");
                        updateCmd.Parameters.AddWithValue("@Email", Email);
                        updateCmd.ExecuteNonQuery();
                    }

                    SuccessMessage = "Password reset successfully!";
                    return RedirectToPage("/Login");
                }
            }

            catch (Exception ex)
            {
                ErrorMessage = "An error occurred: " + ex.Message;
                return Page();
            }
          
        }
    }
}


