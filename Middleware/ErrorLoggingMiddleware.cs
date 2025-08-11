using BrewMaster.Data;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace BrewMaster.Middleware
{
    public class ErrorLoggingMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context, ErrorLogger errorLogger)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                errorLogger.LogError(ex);
                throw;
            }
        }
    }
}
