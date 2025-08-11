using BrewMaster.Data;
using BrewMaster.Models;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NuGet.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace BrewMaster.Utilities
{
    public class DbHelper(IConfiguration config, ErrorLogger errorLogger)
    {
        private readonly IConfiguration _config = config;
        private readonly ErrorLogger _errorLogger = errorLogger;

        private string GetCon()
        {
            return _config.GetConnectionString("BrewMaster")
                ?? throw new InvalidOperationException("Connection string 'BrewMaster' is missing.");
        }

        public bool Exists(string fieldName, string value)
        {
            try
            {
                using var con = new SqlConnection(GetCon());
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
                using var con = new SqlConnection(GetCon());
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
                using (var con = new SqlConnection(GetCon()))
                {
                    string query = "SELECT UserPassword, UserRole, UserId FROM tblUserMaster WHERE UserName = @UserName";

                    using var cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@UserName", username);
                    con.Open();

                    using var dr = cmd.ExecuteReader();
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
                using var con = new SqlConnection(GetCon());
                string query = @"INSERT INTO tblLoginAudit (UserId, UserName, UserRole, IPAddress)
                         VALUES (@UserId, @UserName, @UserRole, @IPAddress)";

                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@UserName", username);
                cmd.Parameters.AddWithValue("@UserRole", role);
                cmd.Parameters.AddWithValue("@IPAddress", ipAddress ?? "Unknown");

                con.Open();
                cmd.ExecuteNonQuery();
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
                using var con = new SqlConnection(GetCon());
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
                using var con = new SqlConnection(GetCon());
                string query = "SELECT SecurityAnswer FROM tblUserMaster WHERE UserName = @UserName";

                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserName", username);
                con.Open();

                var result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                {
                    return false;
                }

                string storedAnswer = result.ToString().Trim().ToLower();
                return storedAnswer.Equals(answer.Trim(), StringComparison.CurrentCultureIgnoreCase);
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
                using var con = new SqlConnection(GetCon());
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

        // PRODUCT MANAGEMENT
        public bool AddProduct(string currentUser, ProductViewModel model)
        {
            try
            {
                using var con = new SqlConnection(GetCon());

                string query = @"INSERT INTO tblProducts 
            (ProductName, ProductDescription, ProductImage, Price, Stock, CreationDate)
            VALUES (@Name, @Desc, @Image, @Price, @Stock, GETDATE())";

                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Name", model.ProductName);
                cmd.Parameters.AddWithValue("@Desc", model.ProductDescription ?? "");

                SqlParameter imgParam = new("@Image", SqlDbType.VarBinary);
                if (model.ProductImage != null && model.ProductImage.Length > 0)
                {
                    imgParam.Value = model.ProductImage;
                }
                else
                {
                    imgParam.Value = DBNull.Value;
                }
                cmd.Parameters.Add(imgParam);

                cmd.Parameters.AddWithValue("@Price", model.Price);
                cmd.Parameters.AddWithValue("@Stock", model.Stock);

                con.Open();

                SetSessionUser(con, currentUser);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                File.AppendAllText("log.txt", $"\n[AddProduct Crash] {DateTime.Now}: {ex}");
                return false;
            }
        }

        public bool UpdateProducts(string currentUser, ProductViewModel model)
        {
            try
            {
                using var con = new SqlConnection(GetCon());
                con.Open();

                SetSessionUser(con, currentUser);

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

                string updateQuery = @"UPDATE tblProducts SET 
                        ProductName = @Name,
                        ProductDescription = @Desc,
                        Price = @Price,
                        Stock = @Stock" +
                                        (model.ProductImage != null ? ", ProductImage = @Image" : "") +
                                        " WHERE ProductId = @Id";

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

        public bool DeleteProducts(string currentUser, ProductViewModel model)
        {
            try
            {
                using var con = new SqlConnection(GetCon());
                string query = "DELETE FROM tblProducts WHERE ProductId = @Id";

                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", model.ProductId);

                con.Open();

                SetSessionUser(con, currentUser);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return false;
            }
        }

        public DataTable GetAllProducts()
        {
            var dt = new DataTable();
            try
            {
                using var con = new SqlConnection(GetCon());
                string query = "SELECT * FROM tblProducts ORDER BY CreationDate ASC";
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

        private void SetSessionUser(SqlConnection con, string username)
        {
            using var cmd = new SqlCommand(
                "EXEC sys.sp_set_session_context @key=N'username', @value=@user;", con);
            cmd.Parameters.AddWithValue("@user", username);
            cmd.ExecuteNonQuery();
        }

        // LOG MANAGEMENT METHODS
        public DataTable GetLogs(string logType, string logId)
        {
            try
            {
                string query = string.IsNullOrEmpty(logId)
                    ? GetBindQuery(logType)
                    : GetSearchQuery(logType);

                if (string.IsNullOrEmpty(query))
                    return new DataTable();

                using SqlConnection con = new(GetCon());
                using SqlCommand cmd = new(query, con);

                if (!string.IsNullOrEmpty(logId) && int.TryParse(logId, out int id))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                }

                using SqlDataAdapter adapter = new(cmd);
                DataTable dt = new();
                adapter.Fill(dt);
                return dt;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return new DataTable();
            }
        }

        public void DeleteLogById(string logType, int logId)
        {
            try
            {
                string table = GetTableName(logType);
                string idColumn = GetIdColumn(logType);

                if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(idColumn))
                    return;

                string query = $"DELETE FROM {table} WHERE {idColumn} = @ID";

                using SqlConnection con = new(GetCon());
                using SqlCommand cmd = new(query, con);
                cmd.Parameters.AddWithValue("@ID", logId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
            }
        }

        public void DeleteAllLogs(string logType)
        {
            try
            {
                string table = GetTableName(logType);

                if (string.IsNullOrEmpty(table))
                    return;

                string query = $"DELETE FROM {table}";

                using SqlConnection con = new(GetCon());
                using SqlCommand cmd = new(query, con);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
            }
        }

        private static string GetTableName(string logType)
        {
            return logType switch
            {
                "ErrorLog" => "ErrorLog",
                "LoginAudit" => "tblLoginAudit",
                "ProductLog" => "tblProducts_Log",
                "UserLog" => "tblUserMaster_Log",
                _ => ""
            };
        }

        private static string GetIdColumn(string logType)
        {
            return logType == "ErrorLog" ? "ErrorId" : "LogId";
        }

        private static string GetBindQuery(string logType)
        {
            return logType switch
            {
                "ErrorLog" =>
                    @"SELECT ErrorId AS [ID], ErrorMessage, ErrorFileName, MethodName, LineNumber, ErrorDate 
                  FROM ErrorLog ORDER BY ErrorId",

                "LoginAudit" =>
                    @"SELECT LogId AS [ID], UserId, UserName, UserRole, IPAddress, LoginTime 
                  FROM tblLoginAudit ORDER BY LogId",

                "ProductLog" =>
                    @"SELECT LogId AS [ID], ActionType, PerformedBy, ActionDate, ProductId, ProductName, Price, Stock 
                  FROM tblProducts_Log ORDER BY LogId",

                "UserLog" =>
                    @"SELECT LogId AS [ID], ActionType, PerformedBy, ActionDate, UserId, UserName, Email, UserRole 
                  FROM tblUserMaster_Log ORDER BY LogId",

                _ => ""
            };
        }

        private static string GetSearchQuery(string logType)
        {
            return logType switch
            {
                "ErrorLog" =>
                    @"SELECT ErrorId AS [ID], ErrorMessage, ErrorFileName, MethodName, LineNumber, ErrorDate 
                  FROM ErrorLog WHERE ErrorId = @ID",

                "LoginAudit" =>
                    @"SELECT LogId AS [ID], UserId, UserName, UserRole, IPAddress, LoginTime 
                  FROM tblLoginAudit WHERE LogId = @ID",

                "ProductLog" =>
                    @"SELECT LogId AS [ID], ActionType, PerformedBy, ActionDate, ProductId, ProductName, Price, Stock 
                  FROM tblProducts_Log WHERE LogId = @ID",

                "UserLog" =>
                    @"SELECT LogId AS [ID], ActionType, PerformedBy, ActionDate, UserId, UserName, Email, UserRole 
                  FROM tblUserMaster_Log WHERE LogId = @ID",

                _ => ""
            };
        }

        // USER HELPER METHODS
        public DataTable GetUsers(string searchUsername)
        {
            using var connection = new SqlConnection(GetCon());
            connection.Open();

            string query = @"SELECT UserId, UserName, 
                               CONCAT(ISNULL(FirstName, ''), ' ', ISNULL(SurName, '')) AS FullName,
                               Email, UserRole, EntryDate 
                               FROM tblUserMaster";

            if (!string.IsNullOrEmpty(searchUsername))
            {
                query += " WHERE UserName = @UserName";
            }

            query += " ORDER BY UserId";

            using var command = new SqlCommand(query, connection);
            if (!string.IsNullOrEmpty(searchUsername))
            {
                command.Parameters.AddWithValue("@UserName", searchUsername.Trim());
            }

            var adapter = new SqlDataAdapter(command);
            var dataTable = new DataTable();
            adapter.Fill(dataTable);
            return dataTable;
        }

        public void AddUser(string email, string username, string password, string firstName, string lastName, string userRole, string currentUser)
        {
            using var connection = new SqlConnection(GetCon());
            connection.Open();

            SetSessionUser(connection, currentUser);

            string query = @"INSERT INTO tblUserMaster (Email, UserName, UserPassword, FirstName, SurName, UserRole, EntryDate) 
                               VALUES (@Email, @UserName, @UserPassword, @FirstName, @SurName, @UserRole, GETDATE())";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", email?.Trim() ?? "");
            command.Parameters.AddWithValue("@UserName", username?.Trim() ?? "");
            command.Parameters.AddWithValue("@UserPassword", HashPassword(password ?? ""));
            command.Parameters.AddWithValue("@FirstName", firstName?.Trim() ?? "");
            command.Parameters.AddWithValue("@SurName", lastName?.Trim() ?? "");
            command.Parameters.AddWithValue("@UserRole", userRole ?? "User");

            command.ExecuteNonQuery();
        }

        public bool UpdateUser(string username, string email, string firstName, string lastName, string userRole, string password, string currentUser)
        {
            using var connection = new SqlConnection(GetCon());
            connection.Open();

            SetSessionUser(connection, currentUser);

            string query = @"UPDATE tblUserMaster 
                               SET Email = @Email, 
                                   FirstName = @FirstName, 
                                   SurName = @SurName, 
                                   UserRole = @UserRole";

            // Only update password if provided
            if (!string.IsNullOrEmpty(password))
            {
                query += ", UserPassword = @UserPassword";
            }

            query += " WHERE UserName = @UserName";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserName", username.Trim());
            command.Parameters.AddWithValue("@Email", email?.Trim() ?? "");
            command.Parameters.AddWithValue("@FirstName", firstName?.Trim() ?? "");
            command.Parameters.AddWithValue("@SurName", lastName?.Trim() ?? "");
            command.Parameters.AddWithValue("@UserRole", userRole ?? "User");

            if (!string.IsNullOrEmpty(password))
            {
                command.Parameters.AddWithValue("@UserPassword", HashPassword(password));
            }

            int rowsAffected = command.ExecuteNonQuery();
            return rowsAffected > 0;
        }

        public bool DeleteUser(string username, string currentUser)
        {
            using var connection = new SqlConnection(GetCon());
            connection.Open();

            SetSessionUser(connection, currentUser);

            string query = "DELETE FROM tblUserMaster WHERE UserName = @UserName";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserName", username.Trim());

            int rowsAffected = command.ExecuteNonQuery();
            return rowsAffected > 0;
        }

        private static string HashPassword(string password)
        {
            return PasswordHasher.HashPassword(password);
        }

        // ACCOUNT STUFF
        public UserAccountViewModel GetUserDetails(string username)
        {
            using var con = new SqlConnection(GetCon());
            using var cmd = new SqlCommand("SELECT * FROM tblUserMaster WHERE UserName = @UserName", con);
            cmd.Parameters.AddWithValue("@UserName", username);
            var user = new UserAccountViewModel();
            con.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                user.UserName = username;
                user.FirstName = reader["FirstName"]?.ToString();
                user.SurName = reader["SurName"]?.ToString();
                user.Mobile = reader["Mobile"]?.ToString();
                user.StreetAddress = reader["StreetAddress"]?.ToString();
                user.City = reader["City"]?.ToString();
                user.UserState = reader["UserState"]?.ToString();
                user.PostalCode = reader["PostalCode"]?.ToString();
                user.Country = reader["Country"]?.ToString();
            }
            return user;
        }

        public bool UpdateSingleField(string username, string fieldName, string fieldValue)
        {
            using var con = new SqlConnection(GetCon());
            string query = $"UPDATE tblUserMaster SET {fieldName} = @value WHERE UserName = @UserName";
            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@value", string.IsNullOrWhiteSpace(fieldValue) ? (object)DBNull.Value : fieldValue);
            cmd.Parameters.AddWithValue("@UserName", username);
            con.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        // HOME AND CART STUFF

        public List<ProductViewModel> GetAvailableProducts()
        {
            var products = new List<ProductViewModel>();
            try
            {
                using var con = new SqlConnection(GetCon());
                string query = @"SELECT ProductId, ProductName, ProductDescription, ProductImage, Price, Stock, CreationDate
                                FROM tblProducts 
                                WHERE Stock > 0 
                                ORDER BY ProductName";
                using var cmd = new SqlCommand(query, con);
                con.Open();
                using var dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    products.Add(new ProductViewModel
                    {
                        ProductId = Convert.ToInt32(dr["ProductId"]),
                        ProductName = dr["ProductName"].ToString() ?? "",
                        ProductDescription = dr["ProductDescription"]?.ToString() ?? "",
                        Price = Convert.ToDecimal(dr["Price"]),
                        Stock = Convert.ToInt32(dr["Stock"]),
                        CreatedDate = Convert.ToDateTime(dr["CreationDate"]),
                        ProductImage = dr["ProductImage"] != DBNull.Value ? (byte[])dr["ProductImage"] : null
                    });
                }
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
            }
            return products;
        }

        public byte[]? GetProductImage(int productId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(GetCon()))
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT ProductImage FROM tblProducts WHERE ProductId = @ProductId", con))
                    {
                        cmd.Parameters.AddWithValue("@ProductId", productId);
                        con.Open();
                        var result = cmd.ExecuteScalar();
                        return result != DBNull.Value ? (byte[])result : null;
                    }
                }
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return null;
            }
        }

        public (bool success, string message) AddToCart(int userId, int productId)
        {
            try
            {
                using var con = new SqlConnection(GetCon());
                con.Open();

                string productQuery = "SELECT ProductName, Stock FROM tblProducts WHERE ProductId = @ProductId";
                using var productCmd = new SqlCommand(productQuery, con);
                productCmd.Parameters.AddWithValue("@ProductId", productId);

                using var reader = productCmd.ExecuteReader();
                if (!reader.Read())
                {
                    return (false, "Product not found.");
                }

                string productName = reader["ProductName"].ToString()!;
                int stock = Convert.ToInt32(reader["Stock"]);
                reader.Close();

                if (stock <= 0)
                {
                    return (false, $"{productName} is out of stock.");
                }

                string checkCartQuery = "SELECT CartId, Quantity FROM tblCart WHERE UserId = @UserId AND ProductId = @ProductId";
                using var checkCmd = new SqlCommand(checkCartQuery, con);
                checkCmd.Parameters.AddWithValue("@UserId", userId);
                checkCmd.Parameters.AddWithValue("@ProductId", productId);

                using var cartReader = checkCmd.ExecuteReader();
                if (cartReader.Read())
                {
                    int cartId = Convert.ToInt32(cartReader["CartId"]);
                    int currentQty = Convert.ToInt32(cartReader["Quantity"]);
                    cartReader.Close();

                    if (currentQty >= stock)
                    {
                        return (false, $"Only {stock} in stock. You already have {currentQty} in cart.");
                    }

                    int newQty = currentQty + 1;
                    string updateQuery = "UPDATE tblCart SET Quantity = @Quantity WHERE CartId = @CartId";
                    using var updateCmd = new SqlCommand(updateQuery, con);
                    updateCmd.Parameters.AddWithValue("@Quantity", newQty);
                    updateCmd.Parameters.AddWithValue("@CartId", cartId);
                    updateCmd.ExecuteNonQuery();

                    return (true, $"Increased quantity of {productName} in your cart.");
                }
                else
                {
                    cartReader.Close();

                    string insertQuery = @"INSERT INTO tblCart (UserId, ProductId, Quantity, AddedDate)
                                   VALUES (@UserId, @ProductId, 1, GETDATE())";
                    using var insertCmd = new SqlCommand(insertQuery, con);
                    insertCmd.Parameters.AddWithValue("@UserId", userId);
                    insertCmd.Parameters.AddWithValue("@ProductId", productId);
                    insertCmd.ExecuteNonQuery();

                    return (true, $"{productName} added to cart!");
                }
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return (false, "Error adding product to cart. Try again later.");
            }
        }

        public CartViewModel GetCartItems(int userId)
        {
            var model = new CartViewModel();

            try
            {
                using var con = new SqlConnection(GetCon());
                con.Open();

                string query = @"SELECT c.CartId, c.ProductId, c.Quantity, p.ProductName, p.Price, p.Stock
                                 FROM tblCart c
                                 JOIN tblProducts p ON c.ProductId = p.ProductId
                                 WHERE c.UserId = @UserId
                                 ORDER BY c.AddedDate DESC";

                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserId", userId);
                using var dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    model.Items.Add(new CartItem
                    {
                        CartId = (int)dr["CartId"],
                        ProductId = (int)dr["ProductId"],
                        ProductName = dr["ProductName"].ToString() ?? "",
                        Price = (decimal)dr["Price"],
                        Quantity = (int)dr["Quantity"],
                        Stock = (int)dr["Stock"]
                    });
                }
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
            }

            return model;
        }

        public bool UpdateCartQuantity(int cartId, int quantity)
        {
            try
            {
                using var con = new SqlConnection(GetCon());
                con.Open();

                string query = "UPDATE tblCart SET Quantity = @Quantity WHERE CartId = @CartId";
                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Quantity", quantity);
                cmd.Parameters.AddWithValue("@CartId", cartId);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return false;
            }
        }

        public bool RemoveCartItem(int cartId)
        {
            try
            {
                using var con = new SqlConnection(GetCon());
                con.Open();

                string query = "DELETE FROM tblCart WHERE CartId = @CartId";
                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@CartId", cartId);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return false;
            }
        }

        public bool ClearCart(int userId)
        {
            try
            {
                using var con = new SqlConnection(GetCon());
                con.Open();

                string query = "DELETE FROM tblCart WHERE UserId = @UserId";
                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserId", userId);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return false;
            }
        }

        public int GetCartItemCount(int userId)
        {
            try
            {
                using var con = new SqlConnection(GetCon());
                con.Open();

                string query = "SELECT COALESCE(SUM(Quantity), 0) FROM tblCart WHERE UserId = @UserId";
                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserId", userId);
                var result = cmd.ExecuteScalar();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return 0;
            }
        }

        public string GetProductName(int productId)
        {
            try
            {
                using var con = new SqlConnection(GetCon());
                con.Open();

                string query = "SELECT ProductName FROM tblProducts WHERE ProductId = @ProductId";
                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ProductId", productId);
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "Unknown Product";
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return "Unknown Product";
            }
        }

        // ADMIN DASHBOARD

        public int GetTotalUsersCount()
        {
            try
            {
                using var con = new SqlConnection(GetCon());
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM tblUserMaster", con);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return 0;
            }
        }

        public int GetTotalProductsCount()
        {
            try
            {
                using var con = new SqlConnection(GetCon());
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM tblProducts", con);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return 0;
            }
        }

        public int GetTotalOrdersCount()
        {
            try
            {
                using var con = new SqlConnection(GetCon());
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM tblOrders", con);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return 0;
            }
        }

        public int GetPendingOrdersCount()
        {
            try
            {
                using var con = new SqlConnection(GetCon());
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM tblOrders WHERE OrderStatus = 'Pending'", con);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return 0;
            }
        }

        public decimal GetTotalRevenue()
        {
            try
            {
                using var con = new SqlConnection(GetCon());
                using var cmd = new SqlCommand(@"SELECT ISNULL(SUM(OrderTotal), 0) 
                                         FROM tblOrders 
                                         WHERE OrderStatus IN ('Confirmed', 'Shipped', 'Delivered')", con);
                con.Open();
                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
                return 0;
            }
        }

        public DataTable GetLowStockProducts(int threshold)
        {
            var dt = new DataTable();
            try
            {
                using var con = new SqlConnection(GetCon());
                using var cmd = new SqlCommand(@"SELECT ProductName, Stock, Price 
                                         FROM tblProducts 
                                         WHERE Stock > 0 AND Stock <= @Threshold", con);
                cmd.Parameters.AddWithValue("@Threshold", threshold);
                using var adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
            }
            return dt;
        }

        public DataTable GetOutOfStockProducts()
        {
            var dt = new DataTable();
            try
            {
                using var con = new SqlConnection(GetCon());
                using var cmd = new SqlCommand(@"SELECT ProductName, Price 
                                         FROM tblProducts 
                                         WHERE Stock = 0", con);
                using var adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
            }
            return dt;
        }

        public DataTable GetRecentOrders(int count)
        {
            var dt = new DataTable();
            try
            {
                using var con = new SqlConnection(GetCon());
                using var cmd = new SqlCommand(@"SELECT TOP (@Count) o.OrderId, u.UserName, o.OrderTotal, o.OrderStatus 
                                         FROM tblOrders o
                                         INNER JOIN tblUserMaster u ON o.UserId = u.UserId
                                         ORDER BY o.OrderDate DESC", con);
                cmd.Parameters.AddWithValue("@Count", count);
                using var adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
            }
            return dt;
        }

        public DataTable GetRecentUsers(int count)
        {
            var dt = new DataTable();
            try
            {
                using var con = new SqlConnection(GetCon());
                using var cmd = new SqlCommand(@"SELECT TOP (@Count) UserName, Email, EntryDate 
                                         FROM tblUserMaster 
                                         ORDER BY EntryDate DESC", con);
                cmd.Parameters.AddWithValue("@Count", count);
                using var adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                _errorLogger.LogError(ex);
            }
            return dt;
        }
    }
}