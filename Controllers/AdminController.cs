using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus_Service_Marketing.Data;
using Nexus_Service_Marketing.Models;

namespace Nexus_Service_Marketing.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "admin")]
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("Login", "Home");
            // 🔹 Summary Cards
            ViewBag.TotalCustomers = _context.Customers.Count();
            ViewBag.TotalEmployees = _context.Users.Count(u => u.Role == "Employee");
            ViewBag.TotalOrders = _context.Orders.Count();
            ViewBag.TotalRevenue = _context.Payments.Sum(p => p.PaidAmount);

            // 🔹 NEW: Plans & Equipment
            ViewBag.TotalPlans = _context.Plans.Count();
            ViewBag.TotalEquipment = _context.Equipments.Count();


            // 🔹 Monthly Revenue
            ViewBag.Months = _context.Payments
                .GroupBy(p => p.PaymentDate.Month)
                .Select(g => g.Key)
                .ToList();

            ViewBag.MonthlyRevenue = _context.Payments
                .GroupBy(p => p.PaymentDate.Month)
                .Select(g => g.Sum(x => x.PaidAmount))
                .ToList();

            // 🔹 Employee Performance
            ViewBag.EmployeeNames = _context.Users
                .Where(u => u.Role == "Employee")
                .Select(u => u.Name)
                .ToList();

            ViewBag.EmployeeOrders = _context.Orders
                .GroupBy(o => o.Customer.RegisteredBy)
                .Select(g => g.Count())
                .ToList();

            return View();
        }




        public IActionResult Add_Register()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Add_Register(User user)
        {
            if (_context.Users.Any(x => x.Email == user.Email))
            {
                ViewBag.Error = "Email already exists";
                return View(user);
            }

            if (ModelState.IsValid)
            {
                _context.Users.Add(user);
                _context.SaveChanges();
                return RedirectToAction("Login");
            }

            return View(user);
        }


        // ================= USER LIST =================
        public IActionResult Users()
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("Login", "Home");

            return View(_context.Users.ToList());
        }

        // ================= EDIT USER =================
        public IActionResult EditUsers(int id)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("Login", "Home");

            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        public IActionResult EditUsers(User user)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("Login", "Home");

            var dbUser = _context.Users.Find(user.Id);
            if (dbUser == null) return NotFound();

            dbUser.Name = user.Name;
            dbUser.Email = user.Email;
            dbUser.Role = user.Role;
            dbUser.IsActive = user.IsActive;

            _context.SaveChanges();
            return RedirectToAction("Users");
        }

        // ================= DELETE USER =================
        public IActionResult DeleteUser(int id)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("Login", "Home");

            var user = _context.Users.Find(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }

            return RedirectToAction("Users");
        }

        // ================= ACTIVATE / DEACTIVATE =================
        public IActionResult ToggleStatus(int id)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("Login", "Home");

            var user = _context.Users.Find(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                _context.SaveChanges();
            }

            return RedirectToAction("Users");
        }



        // ================= EMPLOYEES =================

        public IActionResult Employees()
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("Login", "Home");

            var list = _context.Employees
                               .Join(_context.Users,
                                     e => e.Id,
                                     u => u.Id,
                                     (e, u) => new
                                     {
                                         e.EmployeeId,
                                         u.Name,
                                         u.Email,
                                         e.Designation,
                                         e.ContactNo
                                     }).ToList();

            return View(list);
        }

        // ---------- CREATE ----------
        public IActionResult AddEmployee()
        {
            ViewBag.Users = _context.Users
                .Where(x => x.Role == "Employee")
                .ToList();

            return View();
        }

        [HttpPost]
        public IActionResult AddEmployee(Employee emp)
        {
            _context.Employees.Add(emp);
            _context.SaveChanges();
            return RedirectToAction("Employees");
        }

        // ---------- EDIT ----------
        public IActionResult EditEmployee(int id)
        {
            var emp = _context.Employees.Find(id);
            return View(emp);
        }

        [HttpPost]
        public IActionResult EditEmployee(Employee emp)
        {
            _context.Employees.Update(emp);
            _context.SaveChanges();
            return RedirectToAction("Employees");
        }

        // ---------- DELETE ----------
        public IActionResult DeleteEmployee(int id)
        {
            var emp = _context.Employees.Find(id);
            _context.Employees.Remove(emp);
            _context.SaveChanges();
            return RedirectToAction("Employees");
        }



        public IActionResult RetailShops()
        {
            return View(_context.RetailShops.ToList());
        }

        public IActionResult AddRetailShop() => View();

        [HttpPost]
        public IActionResult AddRetailShop(RetailShop shop)
        {
            _context.RetailShops.Add(shop);
            _context.SaveChanges();
            return RedirectToAction("RetailShops");
        }
        // EDIT
        public IActionResult EditRetailShop(int id)
        {
            var shop = _context.RetailShops.Find(id);
            return View(shop);
        }

        [HttpPost]
        public IActionResult EditRetailShop(RetailShop shop)
        {
            _context.RetailShops.Update(shop);
            _context.SaveChanges();
            return RedirectToAction("RetailShops");
        }

        // DELETE
        public IActionResult DeleteRetailShop(int id)
        {
            var shop = _context.RetailShops.Find(id);
            _context.RetailShops.Remove(shop);
            _context.SaveChanges();
            return RedirectToAction("RetailShops");
        }



        // ================= VENDORS =================

        // LIST
        public IActionResult Vendors()
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("Login", "Home");

            return View(_context.Vendors.ToList());
        }

        // ADD
        public IActionResult AddVendor()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddVendor(Vendor vendor)
        {
            _context.Vendors.Add(vendor);
            _context.SaveChanges();
            return RedirectToAction("Vendors");
        }

        // EDIT
        public IActionResult EditVendor(int id)
        {
            var vendor = _context.Vendors.Find(id);
            return View(vendor);
        }

        [HttpPost]
        public IActionResult EditVendor(Vendor vendor)
        {
            _context.Vendors.Update(vendor);
            _context.SaveChanges();
            return RedirectToAction("Vendors");
        }

        // DELETE
        public IActionResult DeleteVendor(int id)
        {
            var vendor = _context.Vendors.Find(id);
            _context.Vendors.Remove(vendor);
            _context.SaveChanges();
            return RedirectToAction("Vendors");
        }






        // LIST
        public IActionResult Equipments()
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("Login", "Home");
            var data = _context.Equipments
                .Include(e => e.Vendor)
                .ToList();

            return View(data);
        }

        // CREATE
        public IActionResult AddEquipment()
        {
            ViewBag.Vendors = _context.Vendors.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult AddEquipment(Equipment model)
        {
            // 🔹 Available = Total at start
            model.AvailableQuantity = model.TotalQuantity;

            _context.Equipments.Add(model);
            _context.SaveChanges();

            return RedirectToAction(nameof(Equipments));
        }

        // EDIT (OPTIONAL)
        public IActionResult EditEquipment(int id)
        {
            var equipment = _context.Equipments.Find(id);
            ViewBag.Vendors = _context.Vendors.ToList();
            return View(equipment);
        }

        [HttpPost]
        public IActionResult EditEquipment(Equipment model)
        {
            _context.Equipments.Update(model);
            _context.SaveChanges();
            return RedirectToAction(nameof(Equipments));
        }







        // ================= PLANS =================

        // LIST
        public IActionResult Plans()
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("Login", "Home");

            var plans = _context.Plans
                    .Include(p => p.Equipment)
                    .ToList();

            return View(plans);
        }

        // ADD
        public IActionResult AddPlan()
        {
            ViewBag.Equipments = _context.Equipments.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult AddPlan(Plan plan)
        {
            _context.Plans.Add(plan);
            _context.SaveChanges();
            return RedirectToAction("Plans");
        }

        // EDIT
        public IActionResult EditPlan(int id)
        {
            ViewBag.Equipments = _context.Equipments.ToList();
            return View(_context.Plans.Find(id));
        }

        [HttpPost]
        public IActionResult EditPlan(Plan plan)
        {
            _context.Plans.Update(plan);
            _context.SaveChanges();
            return RedirectToAction("Plans");
        }

        // DELETE
        public IActionResult DeletePlan(int id)
        {
            var plan = _context.Plans.Find(id);
            _context.Plans.Remove(plan);
            _context.SaveChanges();
            return RedirectToAction("Plans");
        }



        // GET: Contact/Inbox
        public async Task<IActionResult> Messages()
        {
            var messages = await _context.Contacts.OrderByDescending(x => x.ContactId).ToListAsync();
            return View(messages);
        }

        // POST: Contact/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _context.Contacts.FindAsync(id);
            if (message != null)
            {
                _context.Contacts.Remove(message);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Messages));
        }


    }


}
