using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus_Service_Marketing.Data;
using Nexus_Service_Marketing.Models;

namespace Nexus_Service_Marketing.Controllers
{
    [Authorize(Roles = "Technical")]
    public class TechnicalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TechnicalController(ApplicationDbContext context)
        {
            _context = context;
        }

        //[Authorize(Roles = "Technical")]
        //public IActionResult Dashboard()
        //{
        //    if (HttpContext.Session.GetString("Role") != "Technical")
        //        return RedirectToAction("Login", "Home");
        //    ViewBag.PendingFeasibility = _context.Orders
        //        .Count(o => o.Status == "Pending");

        //    ViewBag.ApprovedOrders = _context.Orders
        //        .Count(o => o.Status == "Approved");

        //    ViewBag.ActiveConnections = _context.Connections
        //        .Count(c => c.Status == "Active");

        //    ViewBag.InactiveConnections = _context.Connections
        //        .Count(c => c.Status != "Active");

        //    return View();
        //}


        [Authorize(Roles = "Technical")]
        public IActionResult Dashboard()
        {
            // 🔹 Orders status counts
            var ordersData = _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            // 🔹 Connections status counts
            var connectionsData = _context.Connections
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            // 🔹 Cards values
            ViewBag.PendingFeasibility = _context.Orders.Count(o => o.Status == "Pending");
            ViewBag.ApprovedOrders = _context.Orders.Count(o => o.Status == "Approved");
            ViewBag.ActiveConnections = _context.Connections.Count(c => c.Status == "Active");
            ViewBag.InactiveConnections = _context.Connections.Count(c => c.Status != "Active");

            // 🔹 Pass data to view for charts
            ViewBag.OrdersStatus = ordersData;
            ViewBag.ConnectionsStatus = connectionsData;

            return View();
        }






        [Authorize(Roles = "Technical")]
        public IActionResult Orders()
        {
            var orders = _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.Status != "Connection Active")
                .ToList();

            return View(orders);
        }


        // 🔹 GET: Feasibility Check
        public IActionResult Feasibility(int orderId)
        {
            var order = _context.Orders.Find(orderId);
            if (order == null) return NotFound();

            var model = new FeasibilityCheck
            {
                OrderId = orderId,
                CheckedDate = DateTime.Now
            };

            return View(model);
        }

        // 🔹 POST: Feasibility Check
        [HttpPost]
        public IActionResult Feasibility(FeasibilityCheck model)
        {
            model.CheckedDate = DateTime.Now;
            model.CheckedBy = User.Identity.Name;

            _context.Feasibilities.Add(model);

            // 🔹 Approve / Reject order
            var order = _context.Orders.Find(model.OrderId);
            order.Status = model.IsFeasible ? "Approved" : "Rejected";
            _context.Orders.Update(order);
            _context.SaveChanges();
            return RedirectToAction("Orders");
        }



        [Authorize(Roles = "Technical")]
        public IActionResult Connections()
        {
            var connections = _context.Connections
                .Include(c => c.Order)
                .ToList();

            return View(connections);
        }



        // 🔹 Activate Connection
        [Authorize(Roles = "Technical")]
        public IActionResult ActivateConnection(int orderId)
        {
            var order = _context.Orders.Find(orderId);
            if (order == null || order.Status != "Approved")
                return NotFound();

            var connection = new Connection
            {
                OrderId = orderId,
                AccountId = "ACC-" + DateTime.Now.Ticks,  // auto generate
                Status = "Active",
                ActivatedDate = DateTime.Now,
                Remarks = "Connection activated successfully"
            };


            _context.Connections.Add(connection);

            order.Status = "Connection Active";
            _context.SaveChanges();

            return RedirectToAction("Connections");
        }


        public IActionResult ChangeConnectionStatus(int id, string status)
        {
            var connection = _context.Connections.Find(id);
            if (connection == null) return NotFound();

            connection.Status = status;
            connection.DeactivatedDate = DateTime.Now;

            _context.SaveChanges();
            return RedirectToAction("Connections");
        }



        public IActionResult Index()
        {
            return View();
        }
    }
}
