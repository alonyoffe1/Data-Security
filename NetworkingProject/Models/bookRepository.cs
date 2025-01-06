using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.Mvc;

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

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM NetProj_Web_db.dbo.Books"; // Query to fetch all records
                SqlCommand command = new SqlCommand(query, connection);

                try
                {
                    connection.Open();
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
                            AgeLim = Convert.ToInt32(reader["AgeLim"])
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return books;
        }

        public BookModel GetBookByDetails(string title, string author)
        {
            BookModel book = null;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM NetProj_Web_db.dbo.Books WHERE Title = @Title AND Author = @Author";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Author", author);
                try
                {
                    
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        Console.WriteLine("Book found, mapping data...");
                        book = new BookModel
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
                        };
                    }
                    else
                    {
                        // Log if no record is found
                        Console.WriteLine("No book found with the provided details.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return book;
        }

        public bool SetDiscountByTitle(string title, decimal discountPrice, string discountPeriod)
        {
            DateTime? discountEndDate = null;

            // If discountPeriod is provided, convert it to a DateTime
            if (!string.IsNullOrEmpty(discountPeriod))
            {
                DateTime parsedDate;
                if (DateTime.TryParse(discountPeriod, out parsedDate))
                {
                    discountEndDate = parsedDate;
                }
                else
                {
                    return false; // Invalid discount period format
                }
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            UPDATE NetProj_Web_db.dbo.Books
            SET DiscountPrice = @DiscountPrice, Expiration = @DiscountEndDate
            WHERE Title = @Title";

                SqlCommand command = new SqlCommand(query, connection);
                // If discountPrice is -1000, set it and the Expiration  date to DBNull.Value
                if (discountPrice == -1000)
                {
                    command.Parameters.AddWithValue("@DiscountPrice", DBNull.Value);  // Use DBNull.Value for null
                    command.Parameters.AddWithValue("@DiscountEndDate", DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@DiscountPrice", discountPrice);
                    command.Parameters.AddWithValue("@DiscountEndDate", discountEndDate.HasValue ? (object)discountEndDate.Value : DBNull.Value);
                }
                command.Parameters.AddWithValue("@Title", title);

                try
                {
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        // Update BorrowPrice
                        string updateBorrowPriceQuery = @"
                    UPDATE NetProj_Web_db.dbo.Books
                    SET 
                        BorrowPrice = CASE
                            WHEN DiscountPrice IS NULL THEN 
                                CASE 
                                    WHEN (Price - 3.00) < 0 THEN 0
                                    ELSE (Price - 3.00)
                                END
                            ELSE 
                                CASE 
                                    WHEN (DiscountPrice - 3.00) < 0 THEN 0
                                    ELSE (DiscountPrice - 3.00)
                                END
                        END
                    WHERE Title = @Title";

                        SqlCommand borrowPriceCommand = new SqlCommand(updateBorrowPriceQuery, connection);
                        borrowPriceCommand.Parameters.AddWithValue("@Title", title);
                        borrowPriceCommand.ExecuteNonQuery();
                    }

                    return rowsAffected > 0; // Return true if at least one row was updated
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating discount: {ex.Message}");
                    return false;
                }
            }
        }

        public enum AddBookResult
        {
            Success,
            AlreadyExists,
            Failure
        }
        public AddBookResult AddNewBook(string title, string author, string publisher, decimal price, int year, string genre, int age)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string checkQuery = "SELECT COUNT(*) FROM Books WHERE Title = @Title AND Author = @Author";
                string insertQuery = @"INSERT INTO Books (Title, Author, Publisher, Price, PublishingYear, Genre, AgeLim, BorrowCopies)
                               VALUES (@Title, @Author, @Publisher, @Price, @Year, @Genre, @Age, 3)";

                try
                {
                    connection.Open();

                    // Check if the book already exists
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

                    // Add the new book
                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@Title", title);
                        insertCommand.Parameters.AddWithValue("@Author", author);
                        insertCommand.Parameters.AddWithValue("@Publisher", publisher);
                        insertCommand.Parameters.AddWithValue("@Price", price);
                        insertCommand.Parameters.AddWithValue("@Year", year);
                        insertCommand.Parameters.AddWithValue("@Genre", genre);
                        insertCommand.Parameters.AddWithValue("@Age", age);

                        int rowsAffected = insertCommand.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return AddBookResult.Success;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            return AddBookResult.Failure;
        }

        public bool DeleteBookByTitle(string title)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM NetProj_Web_db.dbo.Books WHERE Title = @Title";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Title", title);

                try
                {
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0; // Return true if at least one row was deleted
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting book: {ex.Message}");
                    return false;
                }
            }
        }
        public bool CheckIfBorrowableCopiesAvailable(string title, string author)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            SELECT BorrowCopies 
            FROM Books 
            WHERE Title = @Title AND Author = @Author";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Author", author);

                try
                {
                    connection.Open();
                    var result = command.ExecuteScalar();
                    if (result != null && Convert.ToInt32(result) > 0)
                    {
                        return true; // If borrowable copies are greater than 0, return true
                    }
                    return false; // No borrowable copies available
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking borrow availability: {ex.Message}");
                    return false; // If error occurs, assume no availability
                }
            }
        }
    }
}