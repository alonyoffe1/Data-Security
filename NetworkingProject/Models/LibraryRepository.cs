using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace NetworkingProject.Models
{
    public class LibraryRepository
    {
        private readonly string _connectionString;

        public LibraryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<LibraryModel> GetAllBooks()
        {
            var books = new List<LibraryModel>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Library";
                SqlCommand command = new SqlCommand(query, connection);

                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        var book = new LibraryModel
                        {
                            Title = reader["Title"].ToString(),
                            Author = reader["Author"].ToString(),
                            Publisher = reader["Publisher"].ToString(),
                            Genre = reader["Genre"].ToString(),
                            AgeLim = Convert.ToInt32(reader["AgeLim"]),
                            Borrowed = Convert.ToBoolean(reader["Borrowed"])
                        };
                        books.Add(book);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in GetAllBooks: {ex.Message}");
                }
            }

            return books;
        }

        public LibraryModel GetBookByDetails(string title, string author)
        {
            LibraryModel book = null;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Library WHERE Title = @Title AND Author = @Author";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Author", author);

                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        book = new LibraryModel
                        {
                            Title = reader["Title"].ToString(),
                            Author = reader["Author"].ToString(),
                            Publisher = reader["Publisher"].ToString(),
                            Genre = reader["Genre"].ToString(),
                            AgeLim = Convert.ToInt32(reader["AgeLim"]),
                            Borrowed = Convert.ToBoolean(reader["Borrowed"])
                        };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in GetBookByDetails: {ex.Message}");
                }
            }

            return book;
        }

        public bool MarkAsBorrowed(string title, string author)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "UPDATE Library SET Borrowed = 1 WHERE Title = @Title AND Author = @Author";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Author", author);

                try
                {
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in MarkAsBorrowed: {ex.Message}");
                    return false;
                }
            }
        }

        public bool MarkAsReturned(string title, string author)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "UPDATE Library SET Borrowed = 0 WHERE Title = @Title AND Author = @Author";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Author", author);

                try
                {
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in MarkAsReturned: {ex.Message}");
                    return false;
                }
            }
        }

        public bool AddNewBook(string title, string author, string publisher, string genre, int ageLim)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                    INSERT INTO Library (Title, Author, Publisher, Genre, AgeLim, Borrowed)
                    VALUES (@Title, @Author, @Publisher, @Genre, @AgeLim, 0)";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Author", author);
                command.Parameters.AddWithValue("@Publisher", publisher);
                command.Parameters.AddWithValue("@Genre", genre);
                command.Parameters.AddWithValue("@AgeLim", ageLim);

                try
                {
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in AddNewBook: {ex.Message}");
                    return false;
                }
            }
        }

        public bool DeleteBook(string title, string author)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM Library WHERE Title = @Title AND Author = @Author";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Author", author);

                try
                {
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in DeleteBook: {ex.Message}");
                    return false;
                }
            }
        }
    }
}
