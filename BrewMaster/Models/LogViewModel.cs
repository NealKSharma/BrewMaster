using System.ComponentModel.DataAnnotations;
using System.Data;

namespace BrewMaster.Models
{
    public class LogViewModel
    {
        public string SelectedLogType { get; set; } = "ErrorLog";

        [RegularExpression("^\\d*$", ErrorMessage = "Only numeric IDs are allowed.")]
        public string? SearchLogId { get; set; }
        public DataTable LogsTable { get; set; } = new DataTable();

        public LogViewModel() { }
    }
}
