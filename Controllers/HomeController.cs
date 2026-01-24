using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus_Service_Marketing.Data;
using Nexus_Service_Marketing.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace Nexus_Service_Marketing.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly ApplicationDbContext _context;


        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var plans = _context.Plans
        .Include(p => p.Equipment)
        .ToList();

            return View(plans);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Blog()
        {
            return View();
        }

        public IActionResult Blog_details()
        {
            return View();
        }

        public IActionResult Elements()
        {
            return View();
        }

        public IActionResult features()
        {
            return View();
        }
        public IActionResult main()
        {
            return View();
        }
        public IActionResult Package()
        {
            var plans = _context.Plans
        .Include(p => p.Equipment)
        .ToList();
            return View(plans);
        }








        public IActionResult Register()
        {
            return View();
        }


        //[HttpPost]
        //public IActionResult Register(User user)
        //{


        //    user.Role = "role";


        //    _context.Users.Add(user);
        //    _context.SaveChanges();
        //    return RedirectToAction("Login");
        //}
        [HttpPost]
        public IActionResult Register(User user)
        {
            if (_context.Users.Any(x => x.Email == user.Email))
            {
                ViewBag.Error = "Email already exists";
                return View(user);
            }

            // 3️⃣ Add Claims + Auto Login
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),   // Login username
                new Claim(ClaimTypes.Role, user.Role),       // Role (Customer / Employee / Admin)
                new Claim("Id", user.Id.ToString())  // UserId for filtering orders
             };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);


            if (ModelState.IsValid)
            {
                _context.Users.Add(user);
                _context.SaveChanges();
                return RedirectToAction("Login");
            }

            return View(user);
        }


        /*
        INSERT INTO Users VALUES('Admin','admin@gmail.com','123','Admin')*/


        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users
                .FirstOrDefault(x => x.Email == email && x.Password == password && x.IsActive);

            if (user == null)
            {
                ViewBag.Error = "Invalid Email or Password";
                return View();
            }

            // Add claims
            var claims = new List<Claim>
            {
              new Claim(ClaimTypes.Name, user.Name),
              new Claim(ClaimTypes.Role, user.Role),
              new Claim("Id", user.Id.ToString())
             };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);


            if (user != null)
            {
                HttpContext.Session.SetString("IsLogged", "true");
                HttpContext.Session.SetString("UserName", user.Name);
                HttpContext.Session.SetInt32("UserId", user.Id);

                HttpContext.Session.SetString("Role", user.Role);


                // ✅ User ka NAME session me save
                //HttpContext.Session.SetString("UserName", user.Name);


                if (user.Role == "admin")
                    return RedirectToAction("Dashboard", "Admin");

                if (user.Role == "Employee")
                    return RedirectToAction("Dashboard", "Employee");

                if (user.Role == "Accounts")
                    return RedirectToAction("Dashboard", "Accounts");

                if (user.Role == "Technical")
                    return RedirectToAction("Dashboard", "Technical");

                else
                    return RedirectToAction("Index", "Home");
            }


            ViewBag.Error = "Invalid Login";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login","Home");
        }









        // CREATE FORM
        public IActionResult CreateOrder()
        {
            if (HttpContext.Session.GetString("Role") != "User")
                return RedirectToAction("Login", "Home");
            return View();
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        public IActionResult CreateOrder(Customer model)
        {
            if (HttpContext.Session.GetString("Role") != "User")
                return RedirectToAction("Login", "Home");
            // 🔹 1. Logged-in UserId (CLAIM se)
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Home");
            }

            int userId = int.Parse(userIdClaim);

            // 🔹 2. Link customer with logged-in user
            model.UserId = userId;          // ✅ YAHAN LINK HOGA
            model.RegisteredBy = "Customer";
            model.CreatedDate = DateTime.Now;

            _context.Customers.Add(model);
            _context.SaveChanges();

            return RedirectToAction("OrderPlace");
        }



        
        [Authorize(Roles = "User")]
        public IActionResult MyOrders()
        {
            // 🔹 1. UserId claim safely read
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "Id");

            if (userIdClaim == null)
            {
                return Content("UserId claim missing. Logout and login again.");
            }

            int userId = int.Parse(userIdClaim.Value);

            // 🔹 2. Customer record
            var customer = _context.Customers
                .FirstOrDefault(c => c.UserId == userId);

            if (customer == null)
            {
                return Content("Customer record not found for this user.");
            }

            // 🔹 3. Orders
            var orders = _context.Orders
                .Where(o => o.CustomerId == customer.CustomerId)
                .ToList();

            return View(orders);
        }


        // 2️⃣ Place Order Form
        public IActionResult OrderPlace()
        {
            ViewBag.Plans = _context.Plans.ToList();
            ViewBag.Customers = _context.Customers.ToList();
            return View();
        }

        // 2️⃣ Place Order POST
        [HttpPost]
        public IActionResult OrderPlace(Order order)
        {

             // 🔹 1. UserId claim safely read
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "Id");

            if (userIdClaim == null)
            {
                return Content("UserId claim missing. Logout and login again.");
            }

            int userId = int.Parse(userIdClaim.Value);


            // 2️⃣ Get customer linked to this user
            var customer = _context.Customers.FirstOrDefault(c => c.UserId == userId);

            if (customer == null)
            {
                // Customer record missing
                ModelState.AddModelError("", "Customer record not found. Please register first.");
                ViewBag.Plans = _context.Plans.ToList();
                return View(order);
            }

            // 3️⃣ Assign CustomerId safely
            order.CustomerId = customer.CustomerId;

            // 4️⃣ Generate OrderCode & other details
            order.OrderCode = "O" + DateTime.Now.Ticks;
            order.Status = "Pending";
            order.OrderDate = DateTime.Now;

            // 5️⃣ Save order
            _context.Orders.Add(order);
            _context.SaveChanges(); // ✅ Now FK safe

            return RedirectToAction("MyOrders");
        }


        // 3️⃣ Order Details
        public IActionResult Details(int id)
        {
            var customerId = int.Parse(User.Claims.First(c => c.Type == "CustomerId").Value);
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == id && o.CustomerId == customerId);
            if (order == null) return NotFound();

            return View(order);
        }


        [Authorize(Roles = "User")]
        public IActionResult OrderStatus(string orderCode)
        {
            var order = _context.Orders
                .FirstOrDefault(o => o.OrderCode == orderCode);

            return View(order);
        }



        // Utility: Generate Order Code
        private string GenerateOrderCode(string type)
        {
            string prefix = "D"; // Dialup default
            if (type == "Broadband") prefix = "B";
            if (type == "Telephone") prefix = "T";

            var lastOrder = _context.Orders.OrderByDescending(o => o.OrderId).FirstOrDefault();
            int serial = lastOrder != null ? lastOrder.OrderId + 1 : 1;

            return prefix + serial.ToString("D10"); // e.g., D0000000001
        }



        [Authorize(Roles = "User")]
        public IActionResult MyBills()
        {
            //var userId = int.Parse(User.FindFirst("Id").Value);
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "Id");
            int userId = int.Parse(userIdClaim.Value);
            var customer = _context.Customers
                .FirstOrDefault(c => c.UserId == userId);

            var bills = _context.Bills
                .Include(b => b.Order)
                .Where(b => b.Order.CustomerId == customer.CustomerId)
                .ToList();

            return View(bills);
        }
        // 🔹 View bill details
        public IActionResult BillDetails(int id)
        {
            var bill = _context.Bills
                .Include(b => b.Order)
                .ThenInclude(o => o.Customer)
                .Include(b => b.Payments)
                .FirstOrDefault(b => b.BillId == id);

            if (bill == null) return NotFound();

            return View(bill);
        }

        [Authorize(Roles = "User")]
        public IActionResult Payments(int billId)
        {
            var payments = _context.Payments
                .Where(p => p.BillId == billId)
                .ToList();

            return View(payments);
        }










        [Authorize(Roles = "User")]
        //public IActionResult ConnectionStatus(string connectionId)
        //{
        //    var connection = _context.Connections
        //        .FirstOrDefault(c => c.ConnectionId == connection);

        //    return View(connection);
        //}













        [Authorize(Roles = "User")]
        public IActionResult Search()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Search(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                ViewBag.Error = "Please enter OrderCode or AccountId";
                return View();
            }

            var userId = int.Parse(User.Claims.First(c => c.Type == "Id").Value);

            // 🔹 Check if code matches OrderCode first
            var orderResult = _context.Orders
                .Include(o => o.Plan)
                .Where(o => o.Customer.UserId == userId && o.OrderCode == code)
                .Select(o => new
                {
                    Type = "Order",
                    o.OrderCode,
                    AccountId = o.Connection != null ? o.Connection.AccountId : null,
                    Status = o.Status,
                    Plan = o.Plan.PlanName,
                    o.OrderDate
                })
                .FirstOrDefault();

            if (orderResult != null)
            {
                return View("SearchResult", orderResult);
            }

            // 🔹 If not order, check for AccountId
            var accountResult = _context.Connections
                .Include(c => c.Order)
                    .ThenInclude(o => o.Plan)
                .Where(c => c.Order.Customer.UserId == userId && c.AccountId == code)
                .Select(c => new
                {
                    Type = "Account",
                    OrderCode = c.Order.OrderCode,
                    AccountId = c.AccountId,
                    Status = c.Order.Status,
                    Plan = c.Order.Plan.PlanName,
                    c.Order.OrderDate
                })
                .FirstOrDefault();

            if (accountResult != null)
            {
                return View("SearchResult", accountResult);
            }

            ViewBag.Error = "No matching OrderCode or AccountId found.";
            return View();
        }








        // GET: Contact Page
        // =========================
        public ActionResult Contact()
        {
            return View();
        }

        // =========================
        // POST: Submit Contact Form
        // =========================
        [Authorize(Roles = "User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(Contact model)
        {

            if (ModelState.IsValid)
            {
                // Database mein data add karna
                _context.Contacts.Add(model);
                await _context.SaveChangesAsync();

                // Success Message
                ViewBag.Success = "Your message has been received! Our team will get back to you soon.";

                // Form ko khali karne ke liye redirect ya fresh view
                return View();
            }

            // Agar validation fail ho jaye to wapis model bhej dena
            return View(model);
        }





        //public IActionResult Contact()
        //{
        //    return View();
        //}

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
