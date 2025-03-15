# Book-Management-System
An online book management system that allows admins to manage books, users.

Users can borrow books, add reviews.

For detailed documentation, refer to documentation folder

# Setup
1. Install MySql
2. Install .NET 8.
3. Clone the repository
4. Create the database: mysql -u root -p -e "CREATE DATABASE IF NOT EXISTS BMS;"
5. Import the schema using the given schema file: mysql -u root -p BMS < schema_only.sql
