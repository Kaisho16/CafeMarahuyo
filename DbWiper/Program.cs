using System;
using Npgsql;

var connString = "Server=ep-wild-violet-ad4xn118-pooler.c-2.us-east-1.aws.neon.tech;Database=neondb;User Id=neondb_owner;Password=npg_7OByscUPiGM0;Ssl Mode=Require;";

using var conn = new NpgsqlConnection(connString);
conn.Open();

// Drop all tables in public schema
var dropCmd = new NpgsqlCommand(@"
    DO $$ DECLARE
        r RECORD;
    BEGIN
        FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = current_schema()) LOOP
            EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
        END LOOP;
    END $$;
", conn);

dropCmd.ExecuteNonQuery();

Console.WriteLine("All tables dropped successfully.");
