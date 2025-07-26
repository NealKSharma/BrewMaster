using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace BrewMaster.Data
{
    public class ErrorLogger
    {
        private readonly IConfiguration _config;

        public ErrorLogger(IConfiguration config)
        {
            _config = config;
        }

        public void LogError(Exception ex)
        {
            try
            {
                string errorFileName = "";
                string methodName = "";
                int lineNumber = -1;

                var stackTrace = new StackTrace(ex, true);
                var frame = stackTrace.GetFrames()?.FirstOrDefault(f => !string.IsNullOrEmpty(f?.GetFileName()));

                if (frame != null)
                {
                    errorFileName = Path.GetFileName(frame.GetFileName()) ?? "";
                    methodName = frame.GetMethod()?.Name ?? "";
                    lineNumber = frame.GetFileLineNumber();
                }

                using (var con = new SqlConnection(_config.GetConnectionString("BrewMaster")))
                {
                    string query = @"INSERT INTO ErrorLog (ErrorMessage, ErrorFileName, MethodName, LineNumber, StackTrace) 
                                     VALUES (@ErrorMessage, @ErrorFileName, @MethodName, @LineNumber, @StackTrace)";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ErrorMessage", ex.Message);
                        cmd.Parameters.AddWithValue("@ErrorFileName", errorFileName);
                        cmd.Parameters.AddWithValue("@MethodName", methodName);
                        cmd.Parameters.AddWithValue("@LineNumber", lineNumber);
                        cmd.Parameters.AddWithValue("@StackTrace", ex.StackTrace ?? "");

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
            
            }
        }
    }
}
