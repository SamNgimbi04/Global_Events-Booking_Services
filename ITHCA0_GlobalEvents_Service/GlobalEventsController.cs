using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ITHCA0_GlobalEvents_Service
{
    [Route("api/[controller]")]
    [ApiController]
    public class GlobalEventsController : ControllerBase
    {
        private readonly Service1 _service;

        public GlobalEventsController(Service1 service)
        {
            _service = service;
        }

        // POST: api/globalevents/register
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest req)
        {
            bool result = _service.RegisterCustomer(
                req.Email, req.Password, req.FullName,
                req.IDNumber, req.Address, req.ContactNumber,
                req.SecurityQuestion, req.SecurityAnswer);

            return result ? Ok("SUCCESS") : BadRequest("Email already exists");
        }

        // POST: api/globalevents/addaccount
        [HttpPost("addaccount")]
        public IActionResult AddAccount([FromBody] AddAccountRequest req)
        {
            bool result = _service.AddAccount(req.CustomerID, req.AccountType, req.InitialTickets);
            return result ? Ok("SUCCESS") : BadRequest("Failed to add account");
        }

        // POST: api/globalevents/topup
        [HttpPost("topup")]
        public IActionResult TopUp([FromBody] TopUpRequest req)
        {
            bool result = _service.TopUpAccount(req.AccountID, req.TicketsToAdd);
            return result ? Ok("SUCCESS") : BadRequest("Top up failed");
        }

        // POST: api/globalevents/book
        [HttpPost("book")]
        public IActionResult BookTickets([FromBody] BookRequest req)
        {
            string result = _service.BookTickets(req.AccountID, req.EventID, req.Quantity);
            return result == "SUCCESS" ? Ok("SUCCESS") : BadRequest(result);
        }

        // PUT: api/globalevents/updatecustomer
        [HttpPut("updatecustomer")]
        public IActionResult UpdateCustomer([FromBody] UpdateCustomerRequest req)
        {
            bool result = _service.UpdateCustomer(req.CustomerID, req.FullName, req.Address, req.ContactNumber);
            return result ? Ok("SUCCESS") : BadRequest("Update failed");
        }
    }

    // REQUEST MODELS
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string IDNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string? SecurityQuestion { get; set; }
        public string? SecurityAnswer { get; set; }
    }

    public class AddAccountRequest
    {
        public int CustomerID { get; set; }
        public string AccountType { get; set; } = string.Empty;
        public int InitialTickets { get; set; }
    }

    public class TopUpRequest
    {
        public int AccountID { get; set; }
        public int TicketsToAdd { get; set; }
    }

    public class BookRequest
    {
        public int AccountID { get; set; }
        public int EventID { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateCustomerRequest
    {
        public int CustomerID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
    }
}
