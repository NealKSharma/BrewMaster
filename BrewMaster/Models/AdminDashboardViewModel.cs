using System.Data;

namespace BrewMaster.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrdersCount { get; set; }
        public decimal TotalRevenue { get; set; }

        public DataTable LowStockProducts { get; set; } = new DataTable();
        public DataTable OutOfStockProducts { get; set; } = new DataTable();

        public DataTable RecentOrders { get; set; } = new DataTable();
        public DataTable RecentUsers { get; set; } = new DataTable();
    }
}
