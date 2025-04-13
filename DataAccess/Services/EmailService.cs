using DataAccess.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public class EmailService : IEmailService
    {
        public async Task SendEmailAsync(string to, string subject, string message)
        {
            await Task.Delay(100); // Simulate network latency

            Console.WriteLine($"Email sent to {to}, Subject: {subject}, Message: {message}");
        }
    }
}
