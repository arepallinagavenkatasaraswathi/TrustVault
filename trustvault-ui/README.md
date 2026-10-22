# TrustVault — Secure Document Sharing with Audit Trail

## Overview
TrustVault is a secure document-sharing platform that helps users upload, access, share, and revoke access to confidential documents. It maintains audit logs to record important document activities.

## Features
- User registration and login with JWT authentication
- Secure document upload and download
- Document ownership and access control
- Share read access with other users
- Revoke previously granted access
- Audit logging for document activities
- Angular-based user interface
- ASP.NET Core REST API
- MySQL database integration

## Technology Stack
- **Frontend:** Angular
- **Backend:** ASP.NET Core Web API (C#)
- **Database:** MySQL
- **ORM:** Entity Framework Core
- **Authentication:** JWT
- **API Documentation:** Swagger / OpenAPI

## Prerequisites
Install the following before running the project:
- .NET SDK compatible with the backend project
- Node.js and npm
- Angular CLI
- MySQL Server

## Configuration
The backend uses a MySQL database named `TrustVaultDb`.

Configure the database connection using .NET User Secrets or environment variables. Do not commit database passwords, JWT signing keys, or other secrets to source control.

Initialize User Secrets from the backend project directory if needed:

```powershell
dotnet user-secrets init
```

Set the database connection string securely:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_DATABASE_CONNECTION_STRING"
```

Replace the placeholder with your own local database connection string. Do not include real credentials in this README.

## Run the Backend

Open a terminal:

```powershell
cd C:\TrustVault\TrustVault.Api
dotnet restore
dotnet run
```

Open Swagger in your browser:

http://localhost:5200/swagger

If the backend starts on a different port, use the URL shown in the terminal.

## Run the Frontend

Open a separate terminal:

```powershell
cd C:\TrustVault\trustvault-ui
npm install
ng serve
```

Open the frontend:

http://localhost:4200

Keep both backend and frontend terminals running while using the application.

## Main API Endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `/api/Auth/register` | Register a user |
| POST | `/api/Auth/login` | Authenticate a user |
| GET | `/api/Documents` | List accessible documents |
| POST | `/api/Documents` | Upload a document |
| GET | `/api/AuditLogs` | Retrieve audit logs |
| POST | `/api/Permissions/{documentId}/share` | Share document access |
| DELETE | `/api/Permissions/{documentId}/share/{userName}` | Revoke shared access |

Check Swagger for the exact request formats and available endpoints.

## Security
- Authenticate users before accessing protected APIs.
- Verify document permissions before allowing downloads.
- Allow document owners to grant and revoke access.
- Record important actions in audit logs.
- Keep secrets out of source control.
- Validate uploaded files and enforce appropriate size limits before production deployment.

## Testing Checklist
- [ ] Register and log in successfully.
- [ ] Upload and download a document as its owner.
- [ ] Share a document with another user.
- [ ] Verify the authorized user can download it.
- [ ] Revoke access and verify the user can no longer download it.
- [ ] Verify relevant actions appear in audit logs.
- [ ] Confirm the application starts from a fresh terminal.

## Future Improvements
- File content and extension validation
- Malware scanning for uploaded files
- Stronger production secret management
- Automated unit and integration tests
- Deployment with HTTPS and production database configuration

## Author
Developed as part of the Snovasys Fresher Hiring Challenge 2026.
