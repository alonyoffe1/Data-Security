using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Configuration;

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
            Console.WriteLine($"New book object created. GetBookByDetails - Title: {title}, Author: {author}");
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
                            PublishingYear = Convert.ToInt32(reader["PublishingYear"]),
                            Genre = reader["Genre"].ToString(),
                            AgeLim = Convert.ToInt32(reader["AgeLim"])
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
    }
}