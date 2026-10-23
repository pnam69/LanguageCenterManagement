LanguageCenterManagement



An ASP.NET Core MVC application for language center management and AI-assisted learning.



Technologies



\- C# and ASP.NET Core MVC (.NET 10)

\- Entity Framework Core 10

\- Microsoft SQL Server

\- ASP.NET Core Identity

\- Bootstrap, HTML, CSS, and JavaScript

\- Ollama with `qwen2.5:3b`



Features



\- User authentication and role-based access for Admin, Teacher, and Student

\- Exam creation, scheduling, and grading

\- Reading, Listening, Speaking, and Writing assessments

\- AI-generated reading passages and questions

\- AI-assisted writing assessment

\- Student performance analysis and learning recommendations

\- Exam monitoring for tab switching and loss of browser focus

\- AI usage quota management and tracking

\- Learning materials and student results



Requirements



Install the following on Windows 10 or Windows 11:



1\. \[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

2\. \[SQL Server Developer or Express](https://www.microsoft.com/sql-server/sql-server-downloads)

3\. \[SQL Server Management Studio](https://learn.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms)

4\. \[Ollama](https://ollama.com/download)

5\. \[Visual Studio](https://visualstudio.microsoft.com/) with the ASP.NET and web development workload, or another suitable C# editor.



Install Git if you plan to clone the repository.



Installation



1\. Get the source code



Clone the repository:



powershell

git clone <YOUR\_REPOSITORY\_URL>

cd LanguageCenterManagement





Replace the repository URL with the actual URL.



Alternatively, download and extract the project files.



Open a terminal in the directory containing `LanguageCenterManagement.csproj`.

2\. Configure the database



Install SQL Server and make sure the database service is running.



Open `appsettings.json` and configure the connection string:



json

"ConnectionStrings": {

&#x20; "DefaultConnection": "Server=localhost;Database=LanguageCenterDB;Trusted\_Connection=True;TrustServerCertificate=True;"

}





Adjust the server name if your SQL Server instance uses a different name. Preserve the other settings in the existing configuration file.



3\. Restore dependencies



Run:



powershell

dotnet restore





4\. Set up the database



Check that the project contains an Entity Framework Core `Migrations` folder.



If the migrations already exist, install the matching EF Core command-line tool if necessary:



powershell

dotnet tool install --global dotnet-ef --version 10.0.11





If the tool is already installed, update it instead:



powershell

dotnet tool update --global dotnet-ef --version 10.0.11





Apply the migrations:



powershell

dotnet ef database update





This creates or updates the database schema.



If no migrations are included, check the project's database setup before creating a new migration.



5\. Install the AI model



Install Ollama, then run:



powershell

ollama pull qwen2.5:3b





Verify that the model is installed:



powershell

ollama list





The application uses the local Ollama endpoint:



text

http://localhost:11434/





Check that Ollama is running by opening:



http://localhost:11434/api/tags



The endpoint should return a JSON response listing the available models.



6\. Build and run



Build the project:



powershell

dotnet build





Start the application:



powershell

dotnet run





Open the URL displayed in the terminal.



To run the application with automatic restart during development, use:



powershell

dotnet watch run





Initial Account Setup



The application uses ASP.NET Core Identity for authentication and role management.



The expected roles are Admin, Teacher, and Student. Check `Data/IdentitySeeder.cs` for the initial roles and account setup.



Do not assume default login credentials unless they are explicitly defined in the project.



Configuration



Before running the application, check:



\- `appsettings.json` for the database connection string.

\- `Program.cs` for application startup and service configuration.

\- `Data/IdentitySeeder.cs` for role and account initialization.

\- The AI service classes for the Ollama endpoint and model name.



Keep passwords, API keys, and other secrets out of source control. Local development secrets must be configured separately on each computer.



Troubleshooting



\*\*Database connection errors\*\*



Check that SQL Server is running and that the connection string references the correct instance.



\*\*Missing .NET SDK\*\*



Run `dotnet --list-sdks` and install the .NET 10 SDK if it is missing.



\*\*Migration errors\*\*



Verify that the migration files exist and that the connection string is correct before applying migrations.



\*\*AI features not working\*\*



Check that Ollama is running, `qwen2.5:3b` is installed, and the configured endpoint is accessible.



\*\*Build errors\*\*



Run the following commands:



powershell

dotnet clean

dotnet restore

dotnet build





Resolve any remaining compiler errors before running the application.



Verification



After installation, confirm that:



\- The application builds and starts successfully.

\- The login page loads.

\- Database operations work.

\- User roles and permissions are correct.

\- Exams can be created, taken, and submitted.

\- Results are saved and displayed.

\- AI features can generate responses through Ollama.

\- Exam monitoring and AI usage tracking work correctly.



License



