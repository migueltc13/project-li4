using Microsoft.Data.SqlClient;

namespace BetterFinds.Utils;

public class Images(IConfiguration configuration)
{
    public string GetImages(int productId)
    {
        string images = "";

        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        string query = $"SELECT Images FROM Product WHERE ProductId = {productId}";
        using SqlConnection con = new(connectionString);
        con.Open();

        using SqlCommand command = new(query, con);
        using SqlDataReader reader = command.ExecuteReader();
        if (reader.Read())
        {
            images = (string)reader["Images"];
            con.Close();
            return images;
        }

        con.Close();
        return images;
    }

    public List<string> ParseImagesList(string images)
    {
        if (images == null || images == "")
            return [];

        return new List<string>(images.Split(','));
    }

    public List<string> GetImagesList(int productId)
    {
        string images = GetImages(productId);

        return ParseImagesList(images);
    }

    public bool IsValidImages(string Images, ref string errorMessage)
    {
        if (Images == null || Images == "")
            return true;

        // Check if images string is less than 2048 characters
        if (Images.Length > 2048)
        {
            errorMessage = "Images must be 2048 characters or less.";
            return false;
        }

        // Check if images are less than 10
        if (ParseImagesList(Images).Count > 10)
        {
            errorMessage = "Images must be 10 or less.";
            return false;
        }
        return true;
    }
}
