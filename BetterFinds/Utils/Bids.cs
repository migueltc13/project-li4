﻿using Microsoft.Data.SqlClient;

namespace BetterFinds.Utils;

public class Bids(IConfiguration configuration)
{
    private static Dictionary<int, List<int>> BiddersGroup { get; set; } = [];
    public async Task CreateBidderGroupAsync()
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        string query = "SELECT ClientId, AuctionId FROM Bid";

        using (SqlConnection con = new(connectionString))
        {
            con.Open();
            using (SqlCommand cmd = new(query, con))
            {
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int clientId = reader.GetInt32(reader.GetOrdinal("ClientId"));
                    int auctionId = reader.GetInt32(reader.GetOrdinal("AuctionId"));

                    // Console.WriteLine($"Processing clientId: {clientId}, auctionId: {auctionId}");

                    // Check if the auctionId already exists in the dictionary
                    if (BiddersGroup.TryGetValue(auctionId, out List<int>? auctionBidders))
                    {
                        // Check if the clientId is not already in the list for this auction
                        if (!auctionBidders.Contains(clientId))
                        {
                            // Add the clientId to the existing auction
                            auctionBidders.Add(clientId);
                            // Console.WriteLine($"Added clientId: {clientId} to auctionId: {auctionId}");
                        }
                        else
                        {
                            // Console.WriteLine($"Skipped clientId: {clientId} for auctionId: {auctionId} (already exists)");
                        }
                    }
                    else
                    {
                        // Create a new list for the auction and add the clientId
                        auctionBidders = [clientId];
                        BiddersGroup.Add(auctionId, auctionBidders);
                        // Console.WriteLine($"Created new list for auctionId: {auctionId} with clientId: {clientId}");
                    }
                }
                reader.Close();
            }
            con.Close();
        }

        // Print the biddersGroup dictionary
        PrintBiddersGroup();

        await Task.CompletedTask;
    }

    public async Task AddBidderToBidderGroupAsync(int clientId, int auctionId)
    {
        // Check if the auctionId already exists in the dictionary
        if (BiddersGroup.TryGetValue(auctionId, out List<int>? value))
        {
            // Check if the clientId is not already in the list for this auction
            if (value.Contains(clientId))
            {
                // Console.WriteLine($"Skipped clientId: {clientId} for auctionId: {auctionId} (already exists)");
                return;
            }

            // Add the clientId to the existing auction
            value.Add(clientId);
        }
        else
        {
            // Create a new list for the auction and add the clientId
            List<int> auctionBidders = [clientId];

            // Add the auction to the dictionary
            BiddersGroup.Add(auctionId, auctionBidders);
        }

        // Print the biddersGroup dictionary
        PrintBiddersGroup();

        await Task.CompletedTask;
    }

    public List<int> GetBiddersFromAuction(int auctionId)
    {
        if (BiddersGroup.TryGetValue(auctionId, out List<int>? value))
        {
            return value;
        }

        // Handle the case where the auctionId is not present in the dictionary or dictionary is null
        return [];
    }

    // Get all client bids organized by auction
    public List<Dictionary<int, List<Dictionary<string, object>>>> GetClientBids(int clientId)
    {
        List<Dictionary<int, List<Dictionary<string, object>>>> clientBids = [];

        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        using (SqlConnection con = new(connectionString))
        {
            con.Open();

            // Query to retrieve client bids organized by auction
            string query = "SELECT b.AuctionId, b.Value, b.Time, a.StartTime as AuctionStartTime, a.EndTime as AuctionEndTime, a.ProductId, a.MinimumBid, a.IsCompleted, p.Name as ProductName " +
                           "FROM Bid b " +
                           "JOIN Auction a ON b.AuctionId = a.AuctionId " +
                           "JOIN Product p ON a.ProductId = p.ProductId " +
                           "WHERE b.ClientId = @ClientId";
            using (SqlCommand cmd = new(query, con))
            {
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int auctionId = reader.GetInt32(reader.GetOrdinal("AuctionId"));
                    DateTime bidTime = reader.GetDateTime(reader.GetOrdinal("Time"));
                    decimal bidValue = reader.GetDecimal(reader.GetOrdinal("Value"));
                    DateTime auctionStartTime = reader.GetDateTime(reader.GetOrdinal("AuctionStartTime"));
                    DateTime auctionEndTime = reader.GetDateTime(reader.GetOrdinal("AuctionEndTime"));
                    int productId = reader.GetInt32(reader.GetOrdinal("ProductId"));
                    decimal minimumBid = reader.GetDecimal(reader.GetOrdinal("MinimumBid"));
                    bool isCompleted = reader.GetBoolean(reader.GetOrdinal("IsCompleted"));
                    string productName = reader.GetString(reader.GetOrdinal("ProductName"));

                    Dictionary<string, object> bidInfo = new()
                    {
                        { "BidTime", bidTime },
                        { "BidValue", bidValue },
                        { "ProductName", productName }
                    };

                    // Check if the auctionId already exists in the list
                    var auctionBids = clientBids.FirstOrDefault(b => b.ContainsKey(auctionId));

                    if (auctionBids != null)
                    {
                        // Add the bid information to the existing auction
                        auctionBids[auctionId].Add(bidInfo);
                    }
                    else
                    {
                        // Create a new dictionary for the auction and add the bid information
                        Dictionary<int, List<Dictionary<string, object>>> auctionDictionary = new()
                        {
                            {
                                auctionId, new List<Dictionary<string, object>>
                                {
                                    bidInfo
                                }
                            }
                        };

                        // Add the auction to the list
                        clientBids.Add(auctionDictionary);
                    }
                }
                reader.Close();
            }
            con.Close();
        }

        return clientBids;
    }

    public void PrintBiddersGroup()
    {
        Console.WriteLine("biddersGroup contents:");
        foreach (var entry in BiddersGroup)
        {
            Console.WriteLine($"AuctionId: {entry.Key}, Bidders: {string.Join(", ", entry.Value)}");
        }
    }
}
