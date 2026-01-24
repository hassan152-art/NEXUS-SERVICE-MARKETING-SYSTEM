using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus_Service_Marketing.Data;
using Nexus_Service_Marketing.Models;

namespace Nexus_Service_Marketing.Controllers
{
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountsController(ApplicationDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "Accounts")]
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("Role") != "Accounts")
                return RedirectToAction("Login", "Home");

            // Stats Cards Data
            ViewBag.TotalRevenue = _context.Bills.Where(b => b.IsPaid).Sum(b => (decimal?)b.Amount) ?? 0;
            ViewBag.PendingBills = _context.Bills.Count(b => !b.IsPaid);
            ViewBag.ActiveClients = _context.Customers.Count();
            ViewBag.OperationalCost = _context.Bills.Sum(b => (decimal?)b.TaxAmount) ?? 0;

            // Revenue Growth Data (Jan - Jun 2026)
            var currentYear = 2026;
            var monthlyData = _context.Bills
                .Where(b => b.IsPaid && b.BillDate.Year == currentYear)
                .GroupBy(b => b.BillDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(b => b.Amount) })
                .ToList();

            decimal[] chartValues = new decimal[6];
            for (int i = 1; i <= 6; i++)
            {
                chartValues[i - 1] = monthlyData.FirstOrDefault(d => d.Month == i)?.Total ?? 0;
            }

            ViewBag.ChartData = string.Join(",", chartValues);
            return View();
        }

        // =====================================================
        // 🔹 2. SETTINGS: Profile Management (Using 'Name' from User Model)
        // =====================================================
        [Authorize(Roles = "Accounts")]
        public IActionResult Settings()
        {
            if (HttpContext.Session.GetString("Role") != "Accounts")
                return RedirectToAction("Login", "Home");

            ViewBag.AccountName = HttpContext.Session.GetString("UserName") ?? "Bilal";
            ViewBag.Role = "Finance Specialist";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Accounts")]
        public IActionResult UpdateProfile(string accountName)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var user = _context.Users.Find(userId);

            if (user != null && !string.IsNullOrEmpty(accountName))
            {
                // ✅ CORRECTED: Using 'Name' property based on your User Model
                user.Name = accountName;
                _context.SaveChanges();

                // Update session so the name changes in the layout/sidebar immediately
                HttpContext.Session.SetString("UserName", accountName);
                TempData["Message"] = "Profile Updated Successfully!";
            }
            return RedirectToAction("Settings");
        }


        [Authorize(Roles = "Accounts")]
        public IActionResult Orders()
        {
            var orders = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Plan)
                .Where(o => o.Status != null &&
                            o.Status.Trim().ToLower() == "approved" &&
                            !_context.Bills.Any(b => b.OrderId == o.OrderId))
                .ToList();

            return View(orders);
        }

        // =====================================================
        // 🔹 2. CREATE BILL (GET)
        // OrderId URL se aayegi
        // =====================================================
        [Authorize(Roles = "Accounts")]
        public IActionResult CreateBill(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Plan)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            decimal planAmount = order.Plan.Charges;   // 🔁 Plan table ka price
            decimal discount = 0;                    // yahan future me bulk discount logic
            decimal taxableAmount = planAmount - discount;

            decimal taxPercent = 12.24m;
            decimal taxAmount = (taxableAmount * taxPercent) / 100;
            decimal totalAmount = taxableAmount + taxAmount;

            var bill = new Bill
            {
                OrderId = order.OrderId,
                PlanAmount = planAmount,
                DiscountAmount = discount,
                TaxPercentage = taxPercent,
                TaxAmount = taxAmount,
                Amount = totalAmount,
                BillDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(15),
                IsPaid = false,
                GeneratedBy = User.Identity.Name
            };

            return View(bill);
        }

        // =====================================================
        // 🔹 3. CREATE BILL (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Accounts")]
        public IActionResult CreateBill(Bill model)
        {
            // Server-side calculation (backup)
            var taxable = model.PlanAmount - model.DiscountAmount;
            model.TaxAmount = (taxable * model.TaxPercentage) / 100;
            model.Amount = taxable + model.TaxAmount;

            // 🔹 Make sure GeneratedBy is assigned
            if (string.IsNullOrEmpty(model.GeneratedBy))
                model.GeneratedBy = User.Identity.Name;

            _context.Bills.Add(model);
            _context.SaveChanges();

            return RedirectToAction("BillDetails", new { id = model.BillId });
        }

        // =====================================================
        // 🔹 4. BILL DETAILS / INVOICE VIEW
        // =====================================================
        [Authorize(Roles = "Accounts")]
        public IActionResult BillDetails(int id)
        {
            var bill = _context.Bills
                .Include(b => b.Order)
                    .ThenInclude(o => o.Customer)
                .Include(b => b.Payments)
                .FirstOrDefault(b => b.BillId == id);

            if (bill == null)
                return NotFound();

            return View(bill);
        }


        [Authorize(Roles = "Accounts")]
        public IActionResult Invoice(int id)
        {
            var bill = _context.Bills
                .Include(b => b.Order)
                    .ThenInclude(o => o.Customer)
                .Include(b => b.Order.Plan)
                .Include(b => b.Payments)
                .FirstOrDefault(b => b.BillId == id);

            if (bill == null)
                return NotFound();

            return View(bill);
        }




        // =====================================================
        // 🔹 5. ALL BILLS LIST
        // =====================================================
        [Authorize(Roles = "Accounts")]
        public IActionResult BillView()
        {
            var bills = _context.Bills
                .Include(b => b.Order)
                    .ThenInclude(o => o.Customer)
                .OrderByDescending(b => b.BillDate)
                .ToList();

            return View(bills);
        }


        // 🔹 Edit bill (optional: only for amount / correction)
        [Authorize(Roles = "Accounts")]
        public IActionResult EditBill(int id)
        {
            var bill = _context.Bills.Find(id);
            if (bill == null) return NotFound();
            return View(bill);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Accounts")]
        public IActionResult EditBill(Bill model)
        {
            if (ModelState.IsValid)
            {
                _context.Bills.Update(model);
                _context.SaveChanges();
                return RedirectToAction("Details", new { id = model.BillId });
            }
            return View(model);
        }

        // =====================================================
        // 🔹 6. DELETE BILL
        // =====================================================
        [Authorize(Roles = "Accounts")]
        public IActionResult DeleteBill(int id)
        {
            var bill = _context.Bills
                .Include(b => b.Order)
                .FirstOrDefault(b => b.BillId == id);

            if (bill == null)
                return NotFound();

            return View(bill);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Accounts")]
        public IActionResult DeleteConfirmed(int id)
        {
            var bill = _context.Bills.Find(id);
            if (bill == null)
                return NotFound();

            _context.Bills.Remove(bill);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

















        // 🔹 1. Payment List (Accounts / Employee)
        public IActionResult AllPayments()
        {
            var payments = _context.Payments
                .Include(p => p.Bill)
                    .ThenInclude(b => b.Order)
                        .ThenInclude(o => o.Customer)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();

            return View(payments);
        }

        // 🔹 2. Payments of Specific Bill
        public IActionResult BillPayment(int billId)
        {
            var payments = _context.Payments
                .Where(p => p.BillId == billId)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();

            ViewBag.BillId = billId;
            return View(payments);
        }

        // 🔹 3. Create Payment (GET)
        public IActionResult PayPayement(int billId)
        {
            var bill = _context.Bills
                .Include(b => b.Payments)
                .FirstOrDefault(b => b.BillId == billId);


            ViewBag.BillAmount = bill.Amount;
            ViewBag.TotalPaid = bill.Payments.Sum(p => p.PaidAmount);
            ViewBag.Remaining = bill.Amount - ViewBag.TotalPaid;

            return View(new Payment
            {
                BillId = billId,
                PaymentDate = DateTime.Now
            });
        }

        // 🔹 4. Create Payment (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PayPayement(Payment model)
        {
            model.TransactionNo = "TXN-" + DateTime.Now.Ticks;
            model.PaymentDate = DateTime.Now;
            model.ReceivedBy = User.Identity.Name;
            model.Status = "Received";

            _context.Payments.Add(model);

            // 🔁 Bill Paid Status Update
            var bill = _context.Bills
                .Include(b => b.Payments)
                .FirstOrDefault(b => b.BillId == model.BillId);

            if (bill != null)
            {
                var totalPaid = bill.Payments.Sum(p => p.PaidAmount) + model.PaidAmount;

                bill.IsPaid = totalPaid >= bill.Amount;
            }

            _context.SaveChanges();
            return RedirectToAction("BillPayment", new { billId = model.BillId });
        }

        // 🔹 5. Verify Payment (Accounts Only)
        [Authorize(Roles = "Accounts")]
        public IActionResult Verify(int id)
        {
            var payment = _context.Payments.Find(id);
            if (payment == null) return NotFound();

            payment.Status = "Verified";
            _context.SaveChanges();

            return RedirectToAction("AllPayments");
        }

        // 🔹 6. Delete Payment (Optional)
        [Authorize(Roles = "Accounts")]
        public IActionResult DeletePayment(int id)
        {
            var payment = _context.Payments
                .Include(p => p.Bill)
                .FirstOrDefault(p => p.PaymentId == id);

            if (payment == null) return NotFound();

            return View(payment);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Accounts")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmedPayment(int id)
        {
            var payment = _context.Payments
                .Include(p => p.Bill)
                .ThenInclude(b => b.Payments)
                .FirstOrDefault(p => p.PaymentId == id);

            if (payment == null) return NotFound();

            _context.Payments.Remove(payment);

            // 🔁 Recalculate Bill Status
            var bill = payment.Bill;
            var totalPaid = bill.Payments
                .Where(p => p.PaymentId != id)
                .Sum(p => p.PaidAmount);

            bill.IsPaid = totalPaid >= bill.Amount;

            _context.SaveChanges();
            return RedirectToAction("AllPayments");
        }

    }
}
