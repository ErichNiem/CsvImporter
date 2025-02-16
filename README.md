# CSV Importer

A .NET application for importing and processing CSV data with Entity Framework Core support.

## Project Structure

The solution consists of three main projects:

- **CsvImporterDomain**: Contains the core domain models and CSV processing logic
  - Models for CSV data mapping
  - Conversion services
  - CSV import services

- **CsvDataLayer**: Handles data persistence using Entity Framework Core
  - Entity definitions
  - Database context and migrations
  - Repositories and services for data access

- **CsvImporter**: The main console application
  - Entry point for the CSV import process

## Features

- CSV file parsing with custom mapping
- Decimal value conversion support
- Entity Framework Core for data persistence
- Repository pattern implementation
- Service layer abstraction

## Prerequisites

- .NET 8.0
- Entity Framework Core
- SQL Server (or your configured database)

## Getting Started

1. Clone the repository
2. Update the connection string in `App.config` files
3. Run Entity Framework migrations:
   ```
   dotnet ef database update
   ```
4. Place your CSV file in the appropriate directory
5. Run the application:
   ```
   dotnet run --project CsvImporter
   ```

## Project Dependencies

- Entity Framework Core
- CsvHelper (for CSV file processing)
- Azure Core

## Database Schema

The database schema is managed through Entity Framework Core migrations. Check the `Migrations` folder in the CsvDataLayer project for the current database structure.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

[Add your chosen license here]