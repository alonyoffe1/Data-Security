using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace NetworkingProject.Models
{
    public class BookRepository
    {
        private readonly string _connectionString;

        public BookRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<BookModel> GetAllBooks()
        {
            var books = new List<BookModel>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = "SELECT * FROM NetProj_Web_db.dbo.Books"; // Query to fetch all records
                    SqlCommand command = new SqlCommand(query, connection);

                    try
                    {
                        connection.Open();
                        System.Diagnostics.Debug.WriteLine("Database connection successful");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Connection error: {ex.Message}");
                    }
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        books.Add(new BookModel
                        {
                            Title = reader["Title"].ToString(),
                            Author = reader["Author"].ToString(),
                            Publisher = reader["Publisher"].ToString(),
                            Price = Convert.ToSingle(reader["Price"]),
                            DiscountPrice = reader["DiscountPrice"] != DBNull.Value
                                    ? Convert.ToSingle(reader["DiscountPrice"])
                                    : (float?)null, // Safely handle nullable DiscountPrice
                            BorrowPrice = Convert.ToSingle(reader["BorrowPrice"]),
                            PublishingYear = Convert.ToInt32(reader["PublishingYear"]),
                            Genre = reader["Genre"].ToString(),
                            AgeLim = Convert.ToInt32(reader["AgeLim"]),
                            Picture = reader["Picture"].ToString(),
                            Rating = reader["Rating"] != DBNull.Value
                            ? Convert.ToInt32(reader["Rating"])
                            : 0,
                            Review = reader["Review"]?.ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching all books: {ex.Message}");
            }

            return books;
        }

        public BookModel GetBookByDetails(string title, string author)
        {
            BookModel book = null;

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = "SELECT * FROM NetProj_Web_db.dbo.Books WHERE Title = @Title AND Author = @Author";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Title", title);
                    command.Parameters.AddWithValue("@Author", author);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        book = new BookModel
                        {
                            Title = reader["Title"].ToString(),
                            Author = reader["Author"].ToString(),
                            Publisher = reader["Publisher"].ToString(),
                            Price = Convert.ToSingle(reader["Price"]),
                            DiscountPrice = reader["DiscountPrice"] != DBNull.Value
                                    ? Convert.ToSingle(reader["DiscountPrice"])
                                    : (float?)null,
                            BorrowPrice = Convert.ToSingle(reader["BorrowPrice"]),
                            PublishingYear = Convert.ToInt32(reader["PublishingYear"]),
                            Genre = reader["Genre"].ToString(),
                            AgeLim = Convert.ToInt32(reader["AgeLim"]),
                            Rating = reader["Rating"] != DBNull.Value
                            ? Convert.ToInt32(reader["Rating"])
                            : 0,
                            Review = reader["Review"]?.ToString()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching book by details: {ex.Message}");
            }

            return book;
        }

        public bool SetDiscountByTitle(string title, decimal discountPrice, string discountPeriod)
        {
            DateTime? discountEndDate = null;

            if (!string.IsNullOrEmpty(discountPeriod))
            {
                if (!DateTime.TryParse(discountPeriod, out DateTime parsedDate))
                {
                    Console.WriteLine("Invalid discount period format.");
                    return false;
                }
                discountEndDate = parsedDate;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        UPDATE NetProj_Web_db.dbo.Books
                        SET DiscountPrice = @DiscountPrice, Expiration = @DiscountEndDate
                        WHERE Title = @Title";

                    SqlCommand command = new SqlCommand(query, connection);

                    if (discountPrice == -1000)
                    {
                        command.Parameters.AddWithValue("@DiscountPrice", DBNull.Value);
                        command.Parameters.AddWithValue("@DiscountEndDate", DBNull.Value);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@DiscountPrice", discountPrice);
                        command.Parameters.AddWithValue("@DiscountEndDate", discountEndDate.HasValue ? (object)discountEndDate.Value : DBNull.Value);
                    }

                    command.Parameters.AddWithValue("@Title", title);

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        string updateBorrowPriceQuery = @"
                            UPDATE NetProj_Web_db.dbo.Books
                            SET BorrowPrice = CASE
                                WHEN DiscountPrice IS NULL THEN 
                                    CASE WHEN (Price - 3.00) < 0 THEN 0 ELSE (Price - 3.00) END
                                ELSE 
                                    CASE WHEN (DiscountPrice - 3.00) < 0 THEN 0 ELSE (DiscountPrice - 3.00) END
                            END
                            WHERE Title = @Title";

                        SqlCommand borrowPriceCommand = new SqlCommand(updateBorrowPriceQuery, connection);
                        borrowPriceCommand.Parameters.AddWithValue("@Title", title);
                        borrowPriceCommand.ExecuteNonQuery();
                    }

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting discount by title: {ex.Message}");
                return false;
            }
        }

        public enum AddBookResult
        {
            Success,
            AlreadyExists,
            Failure
        }

        public AddBookResult AddNewBook(string title, string author, string publisher, decimal price, int year, string genre, int age, int rating, string review)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string checkQuery = "SELECT COUNT(*) FROM Books WHERE Title = @Title AND Author = @Author";
                    string insertQuery = @"
                        INSERT INTO Books (Title, Author, Publisher, Price, PublishingYear, Genre, AgeLim, BorrowCopies, Review, Rating)
                        VALUES (@Title, @Author, @Publisher, @Price, @Year, @Genre, @Age, 3, @Review, @Rating)";

                    connection.Open();

                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Title", title);
                        checkCommand.Parameters.AddWithValue("@Author", author);

                        int count = (int)checkCommand.ExecuteScalar();
                        if (count > 0)
                        {
                            return AddBookResult.AlreadyExists;
                        }
                    }

                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@Title", title);
                        insertCommand.Parameters.AddWithValue("@Author", author);
                        insertCommand.Parameters.AddWithValue("@Publisher", publisher);
                        insertCommand.Parameters.AddWithValue("@Price", price);
                        insertCommand.Parameters.AddWithValue("@Year", year);
                        insertCommand.Parameters.AddWithValue("@Genre", genre);
                        insertCommand.Parameters.AddWithValue("@Age", age);
                        insertCommand.Parameters.AddWithValue("@Review", review);

                        int rowsAffected = insertCommand.ExecuteNonQuery();
                        return rowsAffected > 0 ? AddBookResult.Success : AddBookResult.Failure;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding new book: {ex.Message}");
                return AddBookResult.Failure;
            }
        }

        public bool DeleteBookByTitle(string title)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = "DELETE FROM NetProj_Web_db.dbo.Books WHERE Title = @Title";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Title", title);

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting book: {ex.Message}");
                return false;
            }
        }

        public bool CheckIfBorrowableCopiesAvailable(string title, string author)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = "SELECT BorrowCopies FROM Books WHERE Title = @Title AND Author = @Author";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Title", title);
                    command.Parameters.AddWithValue("@Author", author);

                    connection.Open();
                    var result = command.ExecuteScalar();
                    return result != null && Convert.ToInt32(result) > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking borrow availability: {ex.Message}");
                return false;
            }
        }
    }
}
