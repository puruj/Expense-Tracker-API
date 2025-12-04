# Expense Tracker API

Implementation of the [roadmap.sh Expense Tracker API project](https://roadmap.sh/projects/expense-tracker-api) built with ASP.NET Core 8, EF Core, and JWT auth.

## Features
- User signup and login with hashed passwords
- JWT issuance and validation for authenticated requests
- Expense CRUD scoped to the authenticated user
- Filters: past week, past month, last 3 months, and custom start/end dates
- Categories: Groceries, Leisure, Electronics, Utilities, Clothing, Health, Others
- Swagger UI with bearer auth support

## Running locally
1) `dotnet restore`
2) Update `appsettings.json` connection string if needed, then apply migrations: `dotnet ef database update`
3) Run the API: `dotnet run --project ExpenseTracker.API/ExpenseTracker.API`
4) Open Swagger at the launched URL. Register, log in, and use the “Authorize” button with `Bearer <token>` for protected endpoints.

## Tests
Integration tests cover auth, JWT enforcement, expense CRUD, and filters using an in-memory database:
`dotnet test`
