using SqlPlanVisualizer.Core.Enums;
using SqlPlanVisualizer.Core.Models.Database;
using SqlPlanVisualizer.Infrastructure.PostgreSql;

var options = new DatabaseConnectionOptions
{
    Host = "localhost",
    Port = 5432,
    Database = "szakdoga_teszt",
    Username = "postgres",
    Schema = "szakdoga_lab",
    TlsMode = DatabaseTlsMode.Disable
};

Console.Write("Jelszó: ");

string password = string.Empty;

while (true)
{
    var key = Console.ReadKey(intercept: true);

    if (key.Key == ConsoleKey.Enter)
        break;

    if (key.Key == ConsoleKey.Backspace)
    {
        if (password.Length > 0)
        {
            password = password[..^1];
            Console.Write("\b \b");
        }
    }
    else if (!char.IsControl(key.KeyChar))
    {
        password += key.KeyChar;
        Console.Write("*");
    }
}

Console.WriteLine();

try
{
    var service = new PostgreSqlConnectionService();

    var info = await service.TestConnectionAsync(options, password);

    Console.WriteLine("Sikeres kapcsolódás!");
    Console.WriteLine($"Adatbázis: {info.DatabaseName}");
    Console.WriteLine($"Felhasználó: {info.Username}");
    Console.WriteLine($"PostgreSQL-verzió: {info.ServerVersion}");

}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
