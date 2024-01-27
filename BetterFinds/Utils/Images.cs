using Microsoft.Data.SqlClient;

namespace BetterFinds.Utils
{
    /// <summary>
    /// Provides utility functions for handling **images-related** operations.
    /// </summary>
    public class Images
    {
        /// <summary>
        /// The IConfiguration instance.
        /// </summary>
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Images"/> class.
        /// </summary>
        /// <param name="configuration">The IConfiguration instance.</param>
        public Images(IConfiguration configuration) =>
            this.configuration = configuration;

        /// <summary>
        /// Returns the images string of the product with the specified ProductId.
        /// </summary>
        /// <param name="productId">The ProductId of the product.</param>
        /// <returns>The images string of the product with the specified ProductId.</returns>
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

        /// <summary>
        /// Returns a list of images from the specified images string.
        /// </summary>
        /// <remarks>
        /// Splits the images string by comma and returns the resulting list.
        /// <para/>
        /// Used in <see cref="Pages.AuctionModel"/> to display the images of the product.
        /// </remarks>
        /// <param name="images">The images string.</param>
        /// <returns>A list of images from the specified images string.</returns>
        public List<string> ParseImagesList(string images)
        {
            if (images == null || images == "")
                return [];

            return new List<string>(images.Split(','));
        }

        /// <summary>
        /// Returns a list of images from the product with the specified ProductId.
        /// </summary>
        /// <remarks>
        /// Currently not being used.
        /// <para/>
        /// Obtains the images list using <see cref="GetImages(int)"/> and parses it using
        /// <see cref="ParseImagesList(string)"/>.
        /// </remarks>
        /// <param name="productId">The ProductId of the product.</param>
        /// <returns>A list of images from the product with the specified ProductId.</returns>
        public List<string> GetImagesList(int productId)
        {
            string images = GetImages(productId);

            return ParseImagesList(images);
        }

        /// <summary>
        /// Validates the specified images string.
        /// </summary>
        /// <remarks>
        /// Both used in <see cref="Pages.CreateModel"/> and <see cref="Pages.EditModel"/>.
        /// <para/>
        /// Checks if the images string is 2048 characters or less and if the images are 10 or less.
        /// </remarks>
        /// <param name="Images">Images string to validate.</param>
        /// <param name="errorMessage">Error message to return if validation fails.</param>
        /// <returns>True if the images string is valid, false otherwise.</returns>
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
