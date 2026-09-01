using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace ITHCA0__Global_Events.Pages
{
    public class EventModel
    {
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public int AvailableTickets { get; set; }
        public decimal TicketPrice { get; set; }
    }

    public class ReceiptModel
    {
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public DateTime BookingDate { get; set; }
        public int TicketsBooked { get; set; }
        public decimal PricePerTicket { get; set; }
        public decimal TotalAmount { get; set; }
        public int RemainingTickets { get; set; }
    }

    public class BookTicketModel : PageModel
    {
        private readonly string _connectionString;

        public BookTicketModel(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        public List<EventModel> Events { get; set; } = new List<EventModel>();
        public ReceiptModel? Receipt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public int AccountId { get; set; }

        public IActionResult OnGet()
        {
            //Check login session
            if (HttpContext.Session.GetString("CustomerID") == null)
                return RedirectToPage("/Login");

            LoadEvents();
            return Page();
        }

        private void LoadEvents()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT EventID, EventName, EventDate, AvailableTickets, TicketPrice FROM Events WHERE AvailableTickets > 0";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Events.Add(new EventModel
                        {
                            EventID = (int)reader["EventID"],
                            EventName = reader["EventName"].ToString()!,
                            EventDate = (DateTime)reader["EventDate"],
                            AvailableTickets = (int)reader["AvailableTickets"],
                            TicketPrice = (decimal)reader["TicketPrice"]
                        });
                    }
                }
            }
        }

        public IActionResult OnPostBook(int selectedEventID, int accountId, int quantity)
        {
            if (HttpContext.Session.GetString("CustomerID") == null)
                return RedirectToPage("/Login");

            AccountId = accountId;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    //Get account ticket balance
                    int ticketBalance = 0;
                    string balanceQuery = "SELECT TicketBalance FROM Accounts WHERE AccountID = @AccountID";

                    using (SqlCommand cmd = new SqlCommand(balanceQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccountID", accountId);
                        ticketBalance = (int)cmd.ExecuteScalar();
                    }

                    //Get event details
                    string eventName = "";
                    DateTime eventDate = DateTime.Now;
                    decimal ticketPrice = 0;
                    int availableTickets = 0;

                    string eventQuery = "SELECT EventName, EventDate, TicketPrice, AvailableTickets FROM Events WHERE EventID = @EventID";
                    using (SqlCommand cmd = new SqlCommand(eventQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@EventID", selectedEventID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                eventName = reader["EventName"].ToString()!;
                                eventDate = (DateTime)reader["Eventdate"];
                                ticketPrice = (decimal)reader["TicketPrice"];
                                availableTickets = (int)reader["AvailableTickets"];
                            }
                        }
                    }

                    //Validate enough tickets in account
                    if (quantity > ticketBalance)
                    {
                        ErrorMessage = "Not enough tickets in your acount. Please top up.";
                        LoadEvents();
                        return Page();
                    }


                    //Validate enough tickets available for event
                    if (quantity > availableTickets)
                    {
                        ErrorMessage = "Not enough tickets available for this event.";
                        LoadEvents();
                        return Page();
                    }

                    decimal totalAmount = quantity * ticketPrice;

                    //Deduct from account balance
                    string updateAccount = "UPDATE Accounts SET TicketBalance = TicketBalance - @Quantity WHERE AccountID = @AccountID";
                    using (SqlCommand cmd = new SqlCommand(updateAccount, conn))
                    {
                        cmd.Parameters.AddWithValue("@Quantity", quantity);
                        cmd.Parameters.AddWithValue("@AccountID", accountId);
                        cmd.ExecuteNonQuery();
                    }

                    //Deduct from event available tickets
                    string updateEvent = "UPDATE Events SET AvailableTickets = AvailableTickets - @Quantity WHERE EventID = @EventID";
                    using (SqlCommand cmd = new SqlCommand(updateEvent, conn))
                    {
                        cmd.Parameters.AddWithValue("@Quantity", quantity);
                        cmd.Parameters.AddWithValue("@EventID", selectedEventID);
                        cmd.ExecuteNonQuery();
                    }

                    //Log transaction
                    string insertTransaction = @"INSERT INTO Transactions
                                                (AccountID, EventID, TicketsBooked, TotalAmount, BookingDate)
                                                VALUES(@AccountID, @EventID, @TicketsBooked, @TotalAmount, @BookingDate)";
                    using (SqlCommand cmd = new SqlCommand(insertTransaction, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccountID", accountId);
                        cmd.Parameters.AddWithValue("@EventID", selectedEventID);
                        cmd.Parameters.AddWithValue("@TicketsBooked", quantity);
                        cmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                        cmd.Parameters.AddWithValue("@BookingDate", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }

                    //Get updated balance for receipt
                    int remainingTickets = 0;
                    string remainingQuery = "SELECT TicketBalance FROM Accounts WHERE AccountID = @AccountID";
                    using (SqlCommand cmd = new SqlCommand(remainingQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccountID", accountId);
                        remainingTickets = (int)cmd.ExecuteScalar();
                    }

                    //Build receipt
                    Receipt = new ReceiptModel
                    {
                        EventName = eventName,
                        EventDate = eventDate,
                        BookingDate = DateTime.Now,
                        TicketsBooked = quantity,
                        PricePerTicket = ticketPrice,
                        TotalAmount = totalAmount,
                        RemainingTickets = remainingTickets,
                    };

                    SuccessMessage = "Booking successful!";
                }

            }

            catch (Exception ex)
            {
                ErrorMessage = "Error: " + ex.Message;
            }

            LoadEvents();
            return Page();
        }
    }
}


