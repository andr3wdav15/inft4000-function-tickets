using System;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace TicketHub.TicketsFunction
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Function1))]
        public async Task Run([QueueTrigger("tickethub", Connection = "AzureWebJobsStorage")] QueueMessage message)
        {
            _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");

            string messageJson = message.MessageText;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            TicketPurchase? ticketPurchase = JsonSerializer.Deserialize<TicketPurchase>(messageJson, options);

            if (ticketPurchase == null)
            {
                _logger.LogError("Failed to deserialize message");
                return;
            }

            _logger.LogInformation($"TicketPurchase: {ticketPurchase}");


            string? connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("SQL connection string is not set in the environment variables.");
            }

            string query = @"
                INSERT INTO Tickets (ConcertId, Email, Name, Phone, Quantity, CreditCard, Expiration, SecurityCode, Address, City, Province, PostalCode, Country)
                VALUES (@ConcertId, @Email, @Name, @Phone, @Quantity, @CreditCard, @Expiration, @SecurityCode, @Address, @City, @Province, @PostalCode, @Country)";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ConcertId", ticketPurchase.ConcertId);
                    cmd.Parameters.AddWithValue("@Email", ticketPurchase.Email);
                    cmd.Parameters.AddWithValue("@Name", ticketPurchase.Name);
                    cmd.Parameters.AddWithValue("@Phone", ticketPurchase.Phone);
                    cmd.Parameters.AddWithValue("@Quantity", ticketPurchase.Quantity);
                    cmd.Parameters.AddWithValue("@CreditCard", ticketPurchase.CreditCard); // Tokenized value should be used here
                    cmd.Parameters.AddWithValue("@Expiration", ticketPurchase.Expiration);
                    cmd.Parameters.AddWithValue("@SecurityCode", ticketPurchase.SecurityCode); // Tokenized value should be used here
                    cmd.Parameters.AddWithValue("@Address", ticketPurchase.Address);
                    cmd.Parameters.AddWithValue("@City", ticketPurchase.City);
                    cmd.Parameters.AddWithValue("@Province", ticketPurchase.Province);
                    cmd.Parameters.AddWithValue("@PostalCode", ticketPurchase.PostalCode);
                    cmd.Parameters.AddWithValue("@Country", ticketPurchase.Country);

                    await cmd.ExecuteNonQueryAsync();
                }

            }
        }
    }
}
