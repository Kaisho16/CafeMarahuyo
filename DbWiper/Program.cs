using System;
using Npgsql;

var connString = "Server=ep-wild-violet-ad4xn118-pooler.c-2.us-east-1.aws.neon.tech;Database=neondb;User Id=neondb_owner;Password=npg_7OByscUPiGM0;Ssl Mode=Require;";

using var conn = new NpgsqlConnection(connString);
conn.Open();

var cmd = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", conn);
using var reader = cmd.ExecuteReader();

Console.WriteLine("Tables in database:");
int count = 0;
while (reader.Read())
{
    Console.WriteLine("- " + reader.GetString(0));
    count++;
}
if (count == 0) Console.WriteLine("NO TABLES FOUND! DATABASE IS EMPTY.");
