using Models.User;
using Models.LoginCredentials;
using MySqlConnector;
using Utilities.Utility;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity.Data;

namespace Service.UserService;

// wrapper class for user filter criteria
public class UserFilterCriteria
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
    public DateOnly MinDateOfBirth { get; set; }
    public DateOnly MaxDateOfBirth { get; set; }
}

// wrapper class for password security
public class PasswordHasher
{
    private const int saltSize = 16;
    private const int hashSize = 16;
    // this is the number of times we apply the hash
    private const int iterations = 10000;

    public static string HashPassword(string password)
    {
        // generate a random salt
        byte[] salt = RandomNumberGenerator.GetBytes(saltSize);

        // Create the Rfc2898DeriveBytes and get the hash value
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
        {
            byte[] hash = pbkdf2.GetBytes(hashSize);

            // combine the salt and password
            byte[] hashBytes = new byte[saltSize + hashSize];
            // the hashBytes array would have salt copied to first 16 bytes
            Array.Copy(salt, 0, hashBytes, 0, saltSize);
            // the hashBytes array would have hash copied to remaining 16 bytes starting from 16th idx
            Array.Copy(hash, 0, hashBytes, saltSize, hashSize);

            // convert to base64
            return Convert.ToBase64String(hashBytes);
        }
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        // derive the salt from the storedHash
        byte[] hashBytes = Convert.FromBase64String(storedHash);
        byte[] salt = new byte[saltSize];
        // copy the first 16 bytes from hashBytes to salt (bytes array)
        Array.Copy(hashBytes, 0, salt, 0, saltSize);

        // Compute the hash
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
        {
            byte[] hash = pbkdf2.GetBytes(hashSize);

            // compare the computed hash with stored hash
            for (int i = 0; i < hashSize; i++)
            {
                if (hashBytes[i + saltSize] != hash[i])
                    return false;
            }
        }

        return true;
    }
}

public interface IUserService
{
    public Task<User> GetUserByUsername(string username);
    public Task<List<User>> GetAllUsers();
    public Task<User> GetUserById(string UserId);
    public Task<List<User>> GetUsersByFilter(UserFilterCriteria filterCriteria);
    public Task AddUser(User user);
    public Task ModifyUser(User user);
    public Task<bool> AuthenticateUser(LoginCredentials loginCredentials);
    public Task<string> GetUserRoleFromUsername(string username);
    public Task DeleteUser(string UserId);
}

public class UserService : IUserService
{
    private readonly MySqlConnection _connection;

    public UserService(MySqlConnection connection)
    {
        _connection = connection;
    }

    // helper method
    public async Task<User> GetUserByUsername(string username)
    {
        await _connection.OpenAsync();

        string query = "SELECT * FROM User WHERE UserName = @username";

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("@username", username);
            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    return new User
                    {
                        UserId = reader.GetString("UserId"),
                        UserName = reader.GetString("UserName"),
                        FirstName = reader.GetString("FirstName"),
                        LastName = reader.GetString("LastName"),
                        Email = reader.GetString("Email"),
                        Password = reader.GetString("Password"),
                        DateOfBirth = reader.GetDateOnly("DateOfBirth"),
                        Role = reader.GetString("Role"),
                        Status = reader.GetString("Status"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        LastModifiedAt = reader.GetDateTime("LastModifiedAt"),
                        LastLogin = reader.GetDateTime("LastLogin")
                    };
                }
            }
        }
        return null;
    }

    // get all users in the system - admin only operation
    public async Task<List<User>> GetAllUsers()
    {
        await _connection.OpenAsync();

        List<User> Users = new List<User>();

        string query = "SELECT * FROM User";

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    User user = new()
                    {
                        UserId = reader.GetString("UserId"),
                        UserName = reader.GetString("UserName"),
                        FirstName = reader.GetString("FirstName"),
                        LastName = reader.GetString("LastName"),
                        Email = reader.GetString("Email"),
                        Password = reader.GetString("Password"),
                        DateOfBirth = reader.GetDateOnly("DateOfBirth"),
                        Role = reader.GetString("Role"),
                        Status = reader.GetString("Status"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        LastModifiedAt = reader.GetDateTime("LastModifiedAt"),
                        LastLogin = reader.GetDateTime("LastLogin")
                    };

                    Users.Add(user);
                }
            }
        }

        return Users;
    }

    // get a specific user by user Id
    public async Task<User> GetUserById(string UserId)
    {
        await _connection.OpenAsync();

        string query = "SELECT * FROM User WHERE UserId = @UserId";

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("@UserId", UserId);
            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    User user = new()
                    {
                        UserId = reader.GetString("UserId"),
                        UserName = reader.GetString("UserName"),
                        FirstName = reader.GetString("FirstName"),
                        LastName = reader.GetString("LastName"),
                        Email = reader.GetString("Email"),
                        Password = reader.GetString("Password"),
                        DateOfBirth = reader.GetDateOnly("DateOfBirth"),
                        Role = reader.GetString("Role"),
                        Status = reader.GetString("Status"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        LastModifiedAt = reader.GetDateTime("LastModifiedAt"),
                        LastLogin = reader.GetDateTime("LastLogin")
                    };

                    return user;
                }
            }
        }

        return null;
    }

    // retrieve all users based on user filter criteria
    public async Task<List<User>> GetUsersByFilter(UserFilterCriteria filterCriteria)
    {
        await _connection.OpenAsync();

        List<User> users = new List<User>();

        List<String> WhereClauses = new List<String>();
        WhereClauses.Add(!string.IsNullOrEmpty(filterCriteria.FirstName) ? " FirstName = @firstname" : "");
        WhereClauses.Add(!string.IsNullOrEmpty(filterCriteria.LastName) ? " LastName = @lastname" : "");
        WhereClauses.Add(!string.IsNullOrEmpty(filterCriteria.Role) && Utility.IsValidRole(filterCriteria.Role) ? " Role = @role" : "");
        WhereClauses.Add(!string.IsNullOrEmpty(filterCriteria.Status) && Utility.IsValidStatus(filterCriteria.Status) ? " Status = @status" : "");
        WhereClauses.Add(Utility.IsValidDate(filterCriteria.MinDateOfBirth) ? " DateOfBirth >= @mindob" : "");
        WhereClauses.Add(Utility.IsValidDate(filterCriteria.MaxDateOfBirth) ? " DateOfBirth <= @maxdob" : "");

        // remove all empty where clauses
        WhereClauses.RemoveAll(x => x == "");

        string query = "SELECT * FROM User WHERE " + string.Join(" AND ", WhereClauses);

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("@firstname", filterCriteria.FirstName);
            cmd.Parameters.AddWithValue("@lastname", filterCriteria.LastName);
            cmd.Parameters.AddWithValue("@role", filterCriteria.Role);
            cmd.Parameters.AddWithValue("@status", filterCriteria.Status);
            cmd.Parameters.AddWithValue("@mindob", filterCriteria.MinDateOfBirth);
            cmd.Parameters.AddWithValue("@maxdob", filterCriteria.MaxDateOfBirth);

            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    User user = new User()
                    {
                        UserId = reader.GetString("UserId"),
                        UserName = reader.GetString("UserName"),
                        FirstName = reader.GetString("FirstName"),
                        LastName = reader.GetString("LastName"),
                        Email = reader.GetString("Email"),
                        Password = reader.GetString("Password"),
                        DateOfBirth = reader.GetDateOnly("DateOfBirth"),
                        Role = reader.GetString("Role"),
                        Status = reader.GetString("Status"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        LastModifiedAt = reader.GetDateTime("LastModifiedAt"),
                        LastLogin = reader.GetDateTime("LastLogin")
                    };

                    users.Add(user);
                }
            }
        }

        return users;
    }

    // this can be considered as signing up or registering a user
    public async Task AddUser(User user)
    {
        await _connection.OpenAsync();

        if (string.IsNullOrEmpty(user.UserId) || string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.FirstName) || string.IsNullOrEmpty(user.LastName))
        {
            throw new Exception("Fields - User Id, UserName, FirstName, LastName cannot be empty or null");
        }

        // validate email address
        user.Email = !Utility.IsValidEmail(user.Email) || string.IsNullOrEmpty(user.Email)
        ? throw new Exception("Email is not valid") : user.Email;

        // validate password
        user.Password = !Utility.IsValidPassword(user.Password) || string.IsNullOrEmpty(user.Password)
        ? throw new Exception("Password is not valid - cannot be empty and must contain at least 8 characters, a digit, a lowercase character, an uppercase character and a special character") : user.Password;

        // salt and hash the password
        user.Password = PasswordHasher.HashPassword(user.Password);

        // validate role
        user.Role = !Utility.IsValidRole(user.Role.ToLower()) || string.IsNullOrEmpty(user.Role)
        ? throw new Exception("Role is not valid") : user.Role;

        // validate status
        user.Status = !Utility.IsValidStatus(user.Status.ToLower()) || string.IsNullOrEmpty(user.Status)
        ? throw new Exception("Status is not valid") : user.Status;

        // set the createdAt, LastModifiedAt, and LastLogin properties
        user.CreatedAt = DateTime.Today;
        user.LastModifiedAt = DateTime.Today;
        user.LastLogin = DateTime.Today;

        string query = "INSERT INTO User(UserId, UserName, FirstName, LastName, Email, Password, DateOfBirth, Role, ";
        query += "Status, CreatedAt, LastModifiedAt, LastLogin) ";
        query += "VALUES(@userId, @userName, @firstName, @lastName, @email, @password, @dateOfBirth, @role, ";
        query += "@status, @createdAt, @lastModifiedAt, @lastLogin)";

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("@userId", user.UserId);
            cmd.Parameters.AddWithValue("@userName", user.UserName);
            cmd.Parameters.AddWithValue("@firstName", user.FirstName);
            cmd.Parameters.AddWithValue("@lastName", user.LastName);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@password", user.Password);
            cmd.Parameters.AddWithValue("@dateOfBirth", user.DateOfBirth);
            cmd.Parameters.AddWithValue("@role", user.Role);
            cmd.Parameters.AddWithValue("@status", user.Status);
            cmd.Parameters.AddWithValue("@createdAt", user.CreatedAt);
            cmd.Parameters.AddWithValue("@lastModifiedAt", user.LastModifiedAt);
            cmd.Parameters.AddWithValue("@lastLogin", user.LastLogin);

            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task ModifyUser(User user)
    {
        await _connection.OpenAsync();

        List<String> SetClauses = new List<String>();
        SetClauses.Add(string.IsNullOrEmpty(user.UserName) ? "" : "UserName = @username");
        SetClauses.Add(string.IsNullOrEmpty(user.FirstName) ? "" : "FirstName = @firstname");
        SetClauses.Add(string.IsNullOrEmpty(user.LastName) ? "" : "LastName = @lastname");
        SetClauses.Add(string.IsNullOrEmpty(user.Email) || !Utility.IsValidEmail(user.Email) ? "" : "Email = @email");
        SetClauses.Add(string.IsNullOrEmpty(user.Password) || !Utility.IsValidPassword(user.Password) ? "" : "Password = @password");
        SetClauses.Add(string.IsNullOrEmpty(user.Role) || !Utility.IsValidRole(user.Role) ? "" : "Role = @role");
        SetClauses.Add(string.IsNullOrEmpty(user.Status) || !Utility.IsValidStatus(user.Status) ? "" : "Status = @status");
        SetClauses.Add(Utility.IsValidDateTime(user.CreatedAt) ? "" : "CreatedDate = @createdDate");
        SetClauses.Add(Utility.IsValidDateTime(user.LastModifiedAt) ? "" : "LastModifiedAt = @lastmodifiedat");
        SetClauses.Add(Utility.IsValidDateTime(user.LastLogin) ? "" : "LastModifiedAt = @lastmodifiedat");

        // remove empty values
        SetClauses.RemoveAll(x => x == "");

        // salt and hash the password
        if (!string.IsNullOrEmpty(user.Password) && Utility.IsValidPassword(user.Password))
            user.Password = PasswordHasher.HashPassword(user.Password);

        string userIdFilter = string.IsNullOrEmpty(user.UserId)
        ? throw new Exception("User Id is required.") : " WHERE UserId = @userid";

        string query = "UPDATE User SET " + string.Join(", ", SetClauses) + userIdFilter;

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("@userid", user.UserId);
            cmd.Parameters.AddWithValue("@username", user.UserName);
            cmd.Parameters.AddWithValue("@firstname", user.FirstName);
            cmd.Parameters.AddWithValue("@lastname", user.LastName);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@password", user.Password);
            cmd.Parameters.AddWithValue("@DateOfBirth", user.DateOfBirth);
            cmd.Parameters.AddWithValue("@Role", user.Role);
            cmd.Parameters.AddWithValue("@Status", user.Status);
            cmd.Parameters.AddWithValue("@createdat", user.CreatedAt);
            cmd.Parameters.AddWithValue("@lastmodifiedat", user.LastModifiedAt);
            cmd.Parameters.AddWithValue("@lastlogin", user.LastLogin);

            int rowsAffected = await cmd.ExecuteNonQueryAsync();

            if (rowsAffected == 0) throw new Exception("No user exists with User Id: " + user.UserId);
        }
    }

    // this method authenticates the first layer which is the correct username and the hashed password
    public async Task<bool> AuthenticateUser(LoginCredentials loginCredentials)
    {
        await _connection.OpenAsync();

        string query = "SELECT UserName, Password FROM User WHERE UserName = @username";

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("@username", loginCredentials.username);

            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                // we won't hit the below if no user exists
                if (await reader.ReadAsync())
                {
                    string hashedPassword = reader.GetString("Password");
                    // verify password
                    if (string.IsNullOrEmpty(loginCredentials.password))
                    {
                        throw new Exception("Password cannot be null or empty");
                    }
                    if (PasswordHasher.VerifyPassword(loginCredentials.password, hashedPassword))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // helper method
    public async Task<string> GetUserRoleFromUsername(string username)
    {
        // await _connection.OpenAsync();

        string query = "SELECT Role FROM User WHERE UserName = @username";

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("@username", username);
            using (MySqlDataReader reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    string role = reader.GetString("Role");
                    return role;
                }
            }
        }
        throw new Exception("Username is incorrect");
    }

    // admin only operation
    public async Task DeleteUser(string UserId)
    {
        await _connection.OpenAsync();
        string query = "DELETE FROM User WHERE UserId = @UserId";

        using (MySqlCommand cmd = new MySqlCommand(query, _connection))
        {
            cmd.Parameters.AddWithValue("@UserId", UserId);

            int rowsAffected = await cmd.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
                throw new Exception("No user found with User Id: " + UserId);
        }
    }
}
