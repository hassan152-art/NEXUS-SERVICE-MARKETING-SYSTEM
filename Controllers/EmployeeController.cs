using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus_Service_Marketing.Data;
using Nexus_Service_Marketing.Models;
using System.Linq;

public class EmployeeController : Controller
{
    private readonly ApplicationDbContext _context;

    public EmployeeController(ApplicationDbContext context)
    {
        _context = context;
    }








    [Authorize(Roles = "Employee")]
    public IActionResult Dashboard()
    {
        if (HttpContext.Session.GetString("Role") != "Employee")
            return RedirectToAction("Login", "Home");
        var employeeName = User.Identity.Name;

        ViewBag.TodayOrders = _context.Orders
            .Count(o => o.Customer.RegisteredBy == employeeName
                     && o.OrderDate.Date == DateTime.Today);

        ViewBag.TotalOrders = _context.Orders
            .Count(o => o.Customer.RegisteredBy == employeeName);

        ViewBag.PendingOrders = _context.Orders
            .Count(o => o.Customer.RegisteredBy == employeeName
                     && o.Status == "Pending");

        ViewBag.TotalPayments = _context.Payments
            .Where(p => p.ReceivedBy == employeeName)
            .Sum(p => p.PaidAmount);

        // Chart Data
        ViewBag.Months = Enumerable.Range(1, 12).ToList();

        ViewBag.MonthlyOrders = _context.Orders
            .Where(o => o.Customer.RegisteredBy == employeeName)
            .GroupBy(o => o.OrderDate.Month)
            .Select(g => g.Count())
            .ToList();

        return View();
    }


    // LIST
    [Authorize(Roles = "Employee")]
    public IActionResult CustomerList()
    {
        var customers = _context.Customers.ToList();
        return View(customers);
    }

    // CREATE FORM
    [Authorize(Roles = "Employee")]
    public IActionResult CreateCustomer()
    {
        return View();
    }

    // CREATE POST
    [HttpPost]
    [Authorize(Roles = "Employee")]
    public IActionResult CreateCustomer(Customer model)
    {
            model.UserId = null; // VERY IMPORTANT
            model.RegisteredBy = "Employee";
            model.CreatedDate = DateTime.Now;

            _context.Customers.Add(model);
            _context.SaveChanges();

            return RedirectToAction("CustomerList");
    }

    // DETAILS
    [Authorize(Roles = "Employee")]
    public IActionResult Details(int id)
    {
        var customer = _context.Customers.FirstOrDefault(c => c.CustomerId == id);
        if (customer == null)
            return NotFound();

        return View(customer);
    }



    // 1️⃣ Order List (All customers)
    [Authorize(Roles = "Employee")]
    public IActionResult OrderList()
    {
        var orders = _context.Orders
    .Include(o => o.Customer)
    .Include(o => o.Plan)
    .OrderByDescending(o => o.OrderDate)
    .ToList();
        return View(orders);
    }

    // 2️⃣ Place Order Form for a Customer
    [Authorize(Roles = "Employee")]
    public IActionResult PLaceOrder()
    {
        ViewBag.Plans = _context.Plans.ToList();
        ViewBag.Customers = _context.Customers.ToList();
        return View();
    }

    // 2️⃣ Place Order POST
    [HttpPost]
    [Authorize(Roles = "Employee")]
    public IActionResult PlaceOrder(Order model)
    {

            model.Status = "Pending";
            model.OrderDate = DateTime.Now;
            model.OrderCode = GenerateOrderCode(model.ConnectionType);

            _context.Orders.Add(model);
            _context.SaveChanges();
        ViewBag.Plans = _context.Plans.ToList();
        ViewBag.Customers = _context.Customers.ToList();

        return RedirectToAction("OrderList");
        


    }

    // Utility: Generate Order Code
    [Authorize(Roles = "Employee")]
    private string GenerateOrderCode(string type)
    {
        string prefix = "D"; // Dialup default
        if (type == "Broadband") prefix = "B";
        if (type == "Telephone") prefix = "T";

        var lastOrder = _context.Orders.OrderByDescending(o => o.OrderId).FirstOrDefault();
        int serial = lastOrder != null ? lastOrder.OrderId + 1 : 1;

        return prefix + serial.ToString("D10"); // e.g., D0000000001
    }




    [Authorize(Roles = "Employee")]
    public IActionResult BillView()
    {
        var bills = _context.Bills
            .Include(b => b.Order)
            .ThenInclude(o => o.Customer)
            .ToList();
        return View(bills);
    }

    // 🔹 View bill details
    [Authorize(Roles = "Employee")]
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







    // 🔹 1. Payment List (Accounts / Employee)
    [Authorize(Roles = "Employee")]
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

    // 🔹 6. Delete Payment (Optional)
    [Authorize(Roles = "Employee")]
    public IActionResult DeletePayment(int id)
    {
        var payment = _context.Payments
            .Include(p => p.Bill)
            .FirstOrDefault(p => p.PaymentId == id);

        if (payment == null) return NotFound();

        return View(payment);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Employee")]
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







    // 🔹 3. Create Payment (GET)
    [Authorize(Roles = "Employee")]
    public IActionResult ReceivedPayement(int billId)
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
    [Authorize(Roles = "Employee")]
    public IActionResult ReceivedPayement(Payment model)
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

    // 🔹 2. Payments of Specific Bill
    [Authorize(Roles = "Employee")]
    public IActionResult BillPayment(int billId)
    {
        var payments = _context.Payments
            .Where(p => p.BillId == billId)
            .OrderByDescending(p => p.PaymentDate)
            .ToList();

        ViewBag.BillId = billId;
        return View(payments);
    }


    [Authorize(Roles = "Employee")]
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






    // 🔹 LIST – Issued & Returned equipments
    [Authorize(Roles = "Employee")]
    public IActionResult IssueEnquipment()
    {
        var issues = _context.EquipmentIssues
            .Include(e => e.Customer)
            .Include(e => e.Equipment)
            .Include(e => e.Employee)
            .OrderByDescending(e => e.IssueDate)
            .ToList();

        return View(issues);
    }

    // 🔹 ISSUE – GET
    [Authorize(Roles = "Employee")]
    public IActionResult AddIssueEquipment()
    {
        ViewBag.Customers = _context.Customers.ToList();

        ViewBag.Equipments = _context.Equipments
            .Where(e => e.AvailableQuantity > 0)
            .ToList();

        return View();
    }

    // 🔹 ISSUE – POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddIssueEquipment(EquipmentIssue model)
    {
        var equipment = _context.Equipments
            .FirstOrDefault(e => e.EquipmentId == model.EquipmentId);

        if (equipment == null || equipment.AvailableQuantity <= 0)
        {
            ModelState.AddModelError("", "Equipment not available in stock");
            return View(model);
        }

        // 🔻 STOCK DECREASE
        equipment.AvailableQuantity -= 1;

        model.IssueDate = DateTime.Now;
        model.Status = "Issued";

        // 🔹 Logged-in Employee
        model.EmployeeId = int.Parse(
            User.Claims.First(c => c.Type == "Id").Value
        );

        _context.EquipmentIssues.Add(model);
        _context.SaveChanges();

        return RedirectToAction(nameof(IssueEnquipment));
    }

    // 🔹 RETURN – GET
    public IActionResult ReturnIssueEnquipment(int id)
    {
        var issue = _context.EquipmentIssues
            .Include(e => e.Equipment)
            .FirstOrDefault(e => e.IssueId == id);

        if (issue == null || issue.Status == "Returned")
            return NotFound();

        return View(issue);
    }

    // 🔹 RETURN – POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ReturnConfirmed(int IssueId)
    {
        var issue = _context.EquipmentIssues
            .Include(e => e.Equipment)
            .FirstOrDefault(e => e.IssueId == IssueId);

        if (issue == null)
            return NotFound();

        // 🔺 STOCK INCREASE
        issue.Equipment.AvailableQuantity += 1;

        issue.Status = "Returned";
        issue.IssueDate = DateTime.Now;

        _context.SaveChanges();

        return RedirectToAction(nameof(IssueEnquipment));
    }
}











