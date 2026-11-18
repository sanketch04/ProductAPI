# ProductAPI

A modern, enterprise-grade **ASP.NET Core 8.0** REST API for comprehensive product management with advanced features including authentication, reporting, QR code generation, and email notifications.

## 📋 Table of Contents

- [Features](#features)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Installation & Setup](#installation--setup)
- [Configuration](#configuration)
- [API Endpoints](#api-endpoints)
- [Authentication](#authentication)
- [Database](#database)
- [Logging & Monitoring](#logging--monitoring)
- [Rate Limiting](#rate-limiting)
- [Contributing](#contributing)
- [License](#license)

## ✨ Features

### Core Product Management
- **CRUD Operations**: Create, read, update, and delete products
- **Pagination Support**: Retrieve products with customizable page size and number
- **Product Images**: Upload and manage product images
- **QR Code Generation**: Automatic QR code creation for products

### Authentication & Security
- **JWT-based Authentication**: Secure access with JSON Web Tokens
- **User Registration & Login**: Create new user accounts and authenticate
- **Refresh Token Mechanism**: Extended session management with token refresh capability
- **Token Revocation**: Logout functionality with refresh token revocation
- **Role-based Authorization**: Support for role-based access control

### Reporting & Export
- **PDF Reports**: Generate downloadable PDF reports of products using QuestPDF
- **Excel Export**: Create Excel spreadsheets with product data using ClosedXML
- **Range Processing**: Support for partial downloads and streaming

### Communication
- **Email Notifications**: Send product-related emails using MailKit
- **Rate Limiting**: Email sending restricted to 5 requests per minute

### Logging & Monitoring
- **Serilog Integration**: Comprehensive structured logging
- **Elasticsearch Integration**: Centralized log aggregation and analysis
- **Request Logging**: HTTP request/response tracking with contextual information
- **Performance Monitoring**: Machine name, thread ID, and execution time tracking

## 🛠️ Technology Stack

### Framework & Core
- **.NET 8.0**: Latest LTS framework with enhanced performance
- **ASP.NET Core 8.0**: Web API framework

### Database
- **SQL Server**: Primary data store with Entity Framework Core
- **Entity Framework Core 8.0.19**: ORM for database operations

### Authentication & Security
- **JWT Bearer Authentication**: Secure token-based authentication
- **BCrypt.Net-Next 4.2.0**: Password hashing and verification
- **System.IdentityModel.Tokens.Jwt 8.23.0**: JWT token generation and validation

### Reporting & Document Generation
- **QuestPDF 2026.9.1**: PDF generation (Community License)
- **ClosedXML 0.105.1**: Excel file creation
- **QRCoder 1.8.0**: QR code generation

### Communication
- **MailKit 4.18.1**: SMTP email delivery

### Logging & Monitoring
- **Serilog.AspNetCore 10.0.0**: Core logging framework
- **Serilog.Sinks.Elasticsearch 10.0.0**: Elasticsearch sink
- **Serilog.Settings.Configuration 10.0.1**: Configuration support
- **Serilog.Enrichers.Environment 3.0.1**: Environment enrichment
- **Serilog.Enrichers.Thread 4.0.0**: Thread enrichment

### API Documentation
- **Swashbuckle.AspNetCore 6.6.2**: Swagger/OpenAPI integration with JWT support

### Rate Limiting
- **Built-in RateLimiting**: .NET 8 native rate limiting middleware

## 📁 Project Structure
