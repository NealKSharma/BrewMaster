using BrewMaster.Data;
using BrewMaster.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace BrewMaster.Helpers
{
    public class DbHelper
    {
        private readonly IConfiguration _config;
        private readonly ErrorLogger _errorLogger;

        public DbHelper(IConfiguration config, ErrorLogger errorLogger)
        {
            _config = config;
            _errorLogger = errorLogger;
        }

        public bool Exists(string fieldName, string value)
        {
            try
            {
                using var con = new SqlConnection(_config.GetConnectionString("BrewMaster"));
                using var cmd = new SqlCommand("sp_ExistsUserMaster", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@FieldName", fieldName);
                cmd.Parameters.AddWithValue("@FieldValue", value);

                con.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return false;
            }
        }

        public void InsertUser(SignUpViewModel model, string hashedPassword)
        {
            try
            {
                using var con = new SqlConnection(_config.GetConnectionString("BrewMaster"));
                using var cmd = new SqlCommand("InsertUserSignupWithSecurity", con);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Email", model.Email);
                cmd.Parameters.AddWithValue("@UserName", model.Username);
                cmd.Parameters.AddWithValue("@UserPassword", hashedPassword);
                cmd.Parameters.AddWithValue("@SecurityQuestion", model.SecurityQuestion);
                cmd.Parameters.AddWithValue("@SecurityAnswer", model.SecurityAnswer);

                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
            }
        }

        // Login Helper Methods
        public class LoginResult
        {
            public bool IsValid { get; set; }
            public string UserRole { get; set; } = "";
            public int UserId { get; set; }
        }

        public LoginResult ValidateUserLogin(string username, string password)
        {
            var result = new LoginResult();

            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("BrewMaster")))
                {
                    string query = "SELECT UserPassword, UserRole, UserId FROM tblUserMaster WHERE UserName = @UserName";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UserName", username);
                        con.Open();

                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                string? storedHash = dr["UserPassword"]?.ToString();
                                string? userRole = dr["UserRole"]?.ToString();
                                var userId = dr["UserId"];

                                if (!string.IsNullOrEmpty(storedHash) && !string.IsNullOrEmpty(userRole) && userId != DBNull.Value && PasswordHasher.VerifyPassword(password, storedHash))
                                {
                                    result.IsValid = true;
                                    result.UserRole = userRole;
                                    result.UserId = Convert.ToInt32(dr["UserId"]);
                                }
                            }
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return result;
            }
        }

        public void LogUserLoginAudit(int userId, string username, string role, string? ipAddress)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("BrewMaster")))
                {
                    string query = @"INSERT INTO tblLoginAudit (UserId, UserName, UserRole, IPAddress)
                         VALUES (@UserId, @UserName, @UserRole, @IPAddress)";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@UserName", username);
                        cmd.Parameters.AddWithValue("@UserRole", role);
                        cmd.Parameters.AddWithValue("@IPAddress", ipAddress ?? "Unknown");

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
            }
        }

        public string GetSecurityQuestion(string username)
        {
            try
            {
                using var con = new SqlConnection(_config.GetConnectionString("BrewMaster"));
                string query = "SELECT SecurityQuestion FROM tblUserMaster WHERE UserName = @UserName";

                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserName", username);
                con.Open();

                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return "";
            }
        }

        public bool VerifySecurityAnswer(string username, string answer)
        {
            try
            {
                using var con = new SqlConnection(_config.GetConnectionString("BrewMaster"));
                string query = "SELECT SecurityAnswer FROM tblUserMaster WHERE UserName = @UserName";

                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserName", username);
                con.Open();

                var storedAnswer = cmd.ExecuteScalar()?.ToString()?.Trim().ToLower();
                return storedAnswer == answer.Trim().ToLower();
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return false;
            }
        }

        public void UpdatePassword(string username, string newHashedPassword)
        {
            try
            {
                using var con = new SqlConnection(_config.GetConnectionString("BrewMaster"));
                string query = "UPDATE tblUserMaster SET UserPassword = @Password WHERE UserName = @UserName";

                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Password", newHashedPassword);
                cmd.Parameters.AddWithValue("@UserName", username);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
            }
        }

        public bool AddProduct(ProductViewModel model)
        {
            var logPath = Path.Combine(Directory.GetCurrentDirectory(), $"addproduct_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            void WriteLog(string message)
            {
                try
                {
                    System.IO.File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} - {message}\n");
                }
                catch { }
            }

            WriteLog("=== DbHelper.AddProduct START ===");

            try
            {
                WriteLog($"Input validation:");
                WriteLog($"  ProductName: '{model.ProductName}'");
                WriteLog($"  ProductDescription: '{model.ProductDescription}'");
                WriteLog($"  Price: {model.Price}");
                WriteLog($"  Stock: {model.Stock}");
                WriteLog($"  ProductImage is null: {model.ProductImage == null}");
                WriteLog($"  ProductImage length: {model.ProductImage?.Length ?? 0}");

                var connectionString = _config.GetConnectionString("BrewMaster");
                WriteLog($"Connection string exists: {!string.IsNullOrEmpty(connectionString)}");

                using var con = new SqlConnection(connectionString);

                string query = @"INSERT INTO tblProducts 
                        (ProductName, ProductDescription, ProductImage, Price, Stock, CreatedDate)
                         VALUES (@Name, @Desc, @Image, @Price, @Stock, GETDATE())";

                WriteLog("SQL Query prepared");

                using var cmd = new SqlCommand(query, con);

                WriteLog("Adding parameters with explicit types...");
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = model.ProductName ?? "";
                WriteLog("  @Name added");

                cmd.Parameters.Add("@Desc", SqlDbType.NVarChar).Value = model.ProductDescription ?? "";
                WriteLog("  @Desc added");

                var imageParam = cmd.Parameters.Add("@Image", SqlDbType.VarBinary, -1);
                if (model.ProductImage != null && model.ProductImage.Length > 0)
                {
                    imageParam.Value = model.ProductImage;
                    WriteLog($"  @Image added with {model.ProductImage.Length} bytes");
                }
                else
                {
                    imageParam.Value = DBNull.Value;
                    WriteLog("  @Image added as DBNull");
                }

                cmd.Parameters.Add("@Price", SqlDbType.Decimal).Value = model.Price;
                WriteLog("  @Price added");

                cmd.Parameters.Add("@Stock", SqlDbType.Int).Value = model.Stock;
                WriteLog("  @Stock added");

                WriteLog("Opening database connection...");
                con.Open();
                WriteLog("Database connection opened successfully");

                WriteLog("Executing query...");
                int result = cmd.ExecuteNonQuery();
                WriteLog($"ExecuteNonQuery returned: {result}");

                bool success = result > 0;
                WriteLog($"=== DbHelper.AddProduct END - Success: {success} ===");

                return success;
            }
            catch (Exception ex)
            {
                WriteLog($"=== DbHelper.AddProduct EXCEPTION ===");
                WriteLog($"Exception Type: {ex.GetType().Name}");
                WriteLog($"Message: {ex.Message}");
                WriteLog($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    WriteLog($"Inner exception: {ex.InnerException.Message}");
                }

                try
                {
                    _errorLogger.LogError(ex);
                    WriteLog("Exception logged to database successfully");
                }
                catch (Exception logEx)
                {
                    WriteLog($"Failed to log exception: {logEx.Message}");
                }

                return false;
            }
        }

        public bool UpdateProducts(ProductViewModel model)
        {
            try
            {
                using var con = new SqlConnection(_config.GetConnectionString("BrewMaster"));
                con.Open();

                string selectQuery = "SELECT * FROM tblProducts WHERE ProductId = @Id";
                using var selectCmd = new SqlCommand(selectQuery, con);
                selectCmd.Parameters.AddWithValue("@Id", model.ProductId);

                using var reader = selectCmd.ExecuteReader();
                if (!reader.Read()) return false;

                string existingName = reader["ProductName"].ToString()!;
                string existingDesc = reader["ProductDescription"]?.ToString() ?? "";
                decimal existingPrice = Convert.ToDecimal(reader["Price"]);
                int existingStock = Convert.ToInt32(reader["Stock"]);
                byte[]? existingImage = reader["ProductImage"] != DBNull.Value ? (byte[])reader["ProductImage"] : null;

                reader.Close();
                con.Close();

                string updateQuery = @"UPDATE tblProducts SET 
                                ProductName = @Name,
                                ProductDescription = @Desc,
                                Price = @Price,
                                Stock = @Stock" +
                                        (model.ProductImage != null ? ", ProductImage = @Image" : "") +
                                        " WHERE ProductId = @Id";
                con.Open();
                using var updateCmd = new SqlCommand(updateQuery, con);
                updateCmd.Parameters.AddWithValue("@Id", model.ProductId);
                updateCmd.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(model.ProductName) ? existingName : model.ProductName);
                updateCmd.Parameters.AddWithValue("@Desc", string.IsNullOrWhiteSpace(model.ProductDescription) ? existingDesc : model.ProductDescription);
                updateCmd.Parameters.AddWithValue("@Price", model.Price <= 0 ? existingPrice : model.Price);
                updateCmd.Parameters.AddWithValue("@Stock", model.Stock < 0 ? existingStock : model.Stock);

                if (model.ProductImage != null)
                {
                    updateCmd.Parameters.AddWithValue("@Image", model.ProductImage);
                }

                return updateCmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return false;
            }
        }


        public bool DeleteProducts(ProductViewModel model)
        {
            try
            {
                using var con = new SqlConnection(_config.GetConnectionString("BrewMaster"));
                string query = "DELETE FROM tblProducts WHERE ProductId = @Id";

                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", model.ProductId);

                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return false;
            }
        }

        public DataTable SearchProductById(int id)
        {
            var dt = new DataTable();

            try
            {
                using var con = new SqlConnection(_config.GetConnectionString("BrewMaster"));
                string query = "SELECT * FROM tblProducts WHERE ProductId = @Id";

                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);
                using var adapter = new SqlDataAdapter(cmd);

                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
            }

            return dt;
        }

        public DataTable GetAllProducts()
        {
            var dt = new DataTable();
            try
            {
                using var con = new SqlConnection(_config.GetConnectionString("BrewMaster"));
                string query = "SELECT * FROM tblProducts ORDER BY CreatedDate DESC";
                using var cmd = new SqlCommand(query, con);
                using var adapter = new SqlDataAdapter(cmd);

                adapter.Fill(dt);
                return dt;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return dt;
            }
        }
    }
}
