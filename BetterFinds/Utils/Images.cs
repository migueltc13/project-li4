using Microsoft.Data.SqlClient;
namespace BetterFinds.Utils
{
    public class Images
    {
        private readonly IConfiguration _configuration;
        public Images(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetImages(int productId)
        {
            string query = $"SELECT Images FROM Product WHERE ProductId = {productId}";

            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return (string)reader["Images"];
                        }
                    }
                }
            }

            return "";
        }

        public List<string> ParseImagesList(string images)
        {
            if (images == null || images == "")
                return new List<string>();

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
}
