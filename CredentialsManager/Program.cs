using Spectre.Console;

namespace CredentialsManager
{
    public class Program
    {
        private static string fileName = "credentials.bin";
        private static CredentialsStorage _storage = new(fileName);

        static void Main(string[] args)
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(4) ?? "4.3.2.0";
            AnsiConsole.MarkupLine($"[blue]Sql Server Index Management System - Credentials Manager v{version}[/]");

            // Parse --file parameter
            var commandArgs = new List<string>(args);

            var fileIndex = commandArgs.FindIndex(arg => arg == "--file");
            if (fileIndex >= 0 && fileIndex + 1 < commandArgs.Count)
            {
                fileName = commandArgs[fileIndex + 1];
                commandArgs.RemoveRange(fileIndex, 2);
                _storage = new CredentialsStorage(fileName);
            }

            if (commandArgs.Count == 0)
            {
                ShowHelp();
                return;
            }

            var command = commandArgs[0].ToLower();

            switch (command)
            {
                case "add":
                    HandleAdd(commandArgs.ToArray());
                    break;
                case "remove":
                    HandleRemove(commandArgs.ToArray());
                    break;
                case "list":
                    HandleList();
                    break;
                default:
                    AnsiConsole.MarkupLine($"[red]Unknown command: {command}[/]");
                    ShowHelp();
                    break;
            }
        }

        private static void HandleAdd(string[] args)
        {
            if (args.Length != 4)
            {
                AnsiConsole.MarkupLine("[red]Usage: add <server> <username> <password>[/]");
                return;
            }

            var server = args[1];
            var username = args[2];
            var password = args[3];

            var credentials = _storage.LoadCredentials();
            
            // Remove existing credential with same key
            var existingCount = credentials.RemoveAll(c => c.Server.ToLower() == server.ToLower());
            
            // Add new credential
            credentials.Add(new Credential { Server = server, Username = username, Password = password });
            
            _storage.SaveCredentials(credentials);
            
            if (existingCount > 0)
            {
                AnsiConsole.MarkupLine($"[yellow]Credential '{server}' updated successfully in file '{fileName}'.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]Credential '{server}' added successfully to file '{fileName}'.[/]");
            }
        }

        private static void HandleRemove(string[] args)
        {
            if (args.Length != 2)
            {
                AnsiConsole.MarkupLine("[red]Usage: remove <server>[/]");
                return;
            }

            var server = args[1];
            var credentials = _storage.LoadCredentials();
            var removed = credentials.RemoveAll(c => c.Server.ToLower() == server.ToLower());

            if (removed > 0)
            {
                _storage.SaveCredentials(credentials);
                AnsiConsole.MarkupLine($"[green]Credential '{server}' removed successfully from file '{fileName}'.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Credential with server '{server}' not found in file '{fileName}'.[/]");
            }
        }

        private static void HandleList()
        {
            var credentials = _storage.LoadCredentials();
            
            if (credentials.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No credentials stored.[/]");
                return;
            }

            var table = new Table();
            table.AddColumn("Server");
            table.AddColumn("Username");
            table.Border(TableBorder.Rounded);
            table.Title("[green]Stored Credentials[/]");

            foreach (var credential in credentials)
            {
                table.AddRow(credential.Server, credential.Username);
            }

            AnsiConsole.Write(table);
        }

        private static void ShowHelp()
        {
            var panel = new Panel(
                new Markup(
                    "[bold]Commands:[/]\n" +
                    "[green]add[/] <server> <username> <password>  - Add or update a credential\n" +
                    "[green]remove[/] <server>                     - Remove a credential by server\n" +
                    "[green]list[/]                             - List all credential servers\n\n" +
                    "[bold]Options:[/]\n" +
                    "[cyan]--file[/] <filename>                   - Specify credentials file (default: credentials.bin)"))
                .Header("Usage")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue);

            AnsiConsole.Write(panel);
        }
    }
}
