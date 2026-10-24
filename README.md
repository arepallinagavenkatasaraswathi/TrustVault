# TrustVault - Secure Document Sharing

## Project Overview
TrustVault is a secure document-sharing platform that helps users share confidential documents with controlled access and audit logging.

## Live Demo
- **Live Website:** https://trustvault-saraswathi.vercel.app
- **Backend API / Swagger:** https://trustvault-api-x0dh.onrender.com/swagger
- **GitHub Repository:** https://github.com/arepallinagavenkatasaraswathi/TrustVault

## Features
- User registration and login
- JWT-based authentication
- Secure document upload and download
- Share documents with specific users
- Revoke document access
- Audit logs for document activities
- Angular frontend and ASP.NET Core backend
- Private Supabase Storage for uploaded documents

## Technology Stack
- Frontend: Angular, TypeScript, HTML, CSS
- Backend: ASP.NET Core Web API
- Database: MySQL
- ORM: Entity Framework Core
- Authentication: JWT
- File Storage: Supabase Storage

## Security Highlights
- Server-side authorization for document access
- Document sharing with specific users
- Access revocation
- Audit logging for document activities
- Private file storage
- Secrets excluded from source control

## Prerequisites
- .NET SDK
- Node.js and npm
- Angular CLI
- MySQL Server

## Backend Setup
Open PowerShell:

```powershell
cd C:\TrustVault\TrustVault.Api
dotnet restore
dotnet run
```

Configure the database connection and JWT key using .NET User Secrets before running the backend.

## Frontend Setup
Open another terminal:

```powershell
cd C:\TrustVault\trustvault-ui
npm install
ng serve
```

Open http://localhost:4200 in your browser.

## API Endpoints
- POST `/api/Auth/register` - Register a user
- POST `/api/Auth/login` - Log in
- GET `/api/Documents` - List accessible documents
- POST `/api/Documents` - Upload a document
- GET `/api/AuditLogs` - View audit logs
- POST `/api/Permissions/{documentId}/share` - Share a document
- DELETE `/api/Permissions/{documentId}/share/{userName}` - Revoke access

## Security Notes
- Do not commit passwords, JWT keys, or database connection secrets.
- Use .NET User Secrets for local development.
- Only authorized users should be able to download shared documents.
- Keep generated build files and dependencies out of source control.

## Author
TrustVault - Snovasys Fresher Hiring Challenge 2026
