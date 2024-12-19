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
                string query = "SELECT * FROM ebooks.dbo.Books"; // Query to fetch all records
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
    }
}