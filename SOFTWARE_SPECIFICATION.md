# Software Specification - Farewell My Beloved

## 1. Introduction

### 1.1 Purpose
This document provides a comprehensive technical specification for the Farewell My Beloved web application. The application serves as a digital memorial platform where users can create dedicated pages for deceased individuals and allow visitors to leave farewell messages, with comprehensive admin moderation and content management capabilities.

### 1.2 Scope
The current implementation includes:
- Creation of memorial pages with customizable content
- Dynamic routing for individual memorial pages
- Message posting with optional author identification
- File upload support for images using S3 storage
- Responsive web interface
- Admin dashboard with analytics and monitoring
- Content moderation and reporting system
- GitHub OAuth authentication
- Complete audit logging of admin actions

### 1.3 Definitions
- **Memorial Page**: A dedicated web page for remembering a deceased person
- **Slug**: URL-friendly identifier for memorial pages (e.g., "donald-trump")
- **Message**: A farewell message left by a visitor
- **Author**: Optional identifier for message posters (name/email)
- **Content Report**: User-submitted report for inappropriate content
- **Moderator Log**: Administrative action record for content moderation
- **S3 Storage**: AWS S3-compatible file storage service (Filebase)
- **GitHub OAuth**: Authentication using GitHub's OAuth 2.0 system

## 2. System Architecture

### 2.1 High-Level Architecture
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Presentation  │    │    Business     │    │      Data       │
│     Layer       │◄──►│     Logic       │◄──►│     Layer       │
│                 │    │     Layer       │    │                 │
│ - Controllers   │    │ - Services      │    │ - Entity Models │
│ - Views         │    │ - Validation    │    │ - DbContext     │
│ - ViewModels    │    │ - Business Rules│    │ - Repositories  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### 2.2 Technology Stack
- **Backend Framework**: ASP.NET Core 9.0 MVC
- **Database**: Entity Framework Core with SQL Server
- **Frontend**: Bootstrap 5, jQuery 3.7.0
- **Image Hosting**: AWS S3-compatible storage (Filebase)
- **Authentication**: GitHub OAuth with cookie-based sessions
- **Cloud Services**: Amazon S3 SDK for file management
- **Runtime**: .NET 9.0

### 2.3 Design Patterns
- **MVC Pattern**: Separation of concerns between Model, View, and Controller
- **Repository Pattern**: Data access abstraction
- **Service Layer Pattern**: Business logic encapsulation
- **Dependency Injection**: For loose coupling and testability

## 3. Database Design

### 3.1 Entity-Relationship Diagram
```mermaid
erDiagram
    FAREWELL_PERSON {
        int Id PK
        string Name "Required, max 200 chars"
        string Slug "Required, unique, URL-friendly"
        string Description "Required, max 5000 chars"
        string PortraitUrl "Optional, max 500 chars"
        string BackgroundUrl "Optional, max 500 chars"
        string Email "Optional, max 255 chars"
        DateTime CreatedAt "Required, default GetUtcDate()"
        DateTime UpdatedAt "Required, default GetUtcDate()"
        bool IsPublic "Required, default true"
    }
    
    FAREWELL_MESSAGE {
        int Id PK
        int FarewellPersonId "Required, FK to FAREWELL_PERSON"
        string AuthorName "Optional, max 100 chars"
        string AuthorEmail "Optional, max 255 chars"
        string Message "Required, max 2000 chars"
        DateTime CreatedAt "Required, default GetUtcDate()"
        bool IsPublic "Required, default true"
    }
    
    CONTENT_REPORT {
        guid Id PK
        string Email "Required, max 255 chars"
        int? FarewellPersonId "Optional, FK to FAREWELL_PERSON"
        int? FarewellMessageId "Optional, FK to FAREWELL_MESSAGE"
        string Reason "Required, max 100 chars"
        string? Explanation "Optional, max 1000 chars"
        DateTime CreatedAt "Required, default GetUtcDate()"
        DateTime? ResolvedAt "Optional"
    }
    
    MODERATOR_LOG {
        int Id PK
        string ModeratorName "Required, max 100 chars"
        string TargetType "Required, max 50 chars"
        int TargetId "Required"
        string Action "Required, max 50 chars"
        string Reason "Optional, max 200 chars"
        string Details "Required, max 1000 chars"
        guid? ContentReportId "Optional, FK to CONTENT_REPORT"
        DateTime CreatedAt "Required, default GetUtcDate()"
    }
    
    FAREWELL_PERSON ||--o{ FAREWELL_MESSAGE : "has"
    FAREWELL_PERSON ||--o{ CONTENT_REPORT : "reported"
    FAREWELL_MESSAGE ||--o{ CONTENT_REPORT : "reported"
    CONTENT_REPORT ||--o{ MODERATOR_LOG : "handled_by"
    FAREWELL_PERSON ||--o{ MODERATOR_LOG : "modified"
    FAREWELL_MESSAGE ||--o{ MODERATOR_LOG : "modified"
```

### 3.2 Database Schema Details

#### FAREWELL_PERSON Table
| Column | Data Type | Constraints | Description |
|--------|-----------|-------------|-------------|
| Id | int | Primary Key, Identity | Unique identifier |
| Name | nvarchar(200) | Required, MaxLength(200) | Person's full name |
| Slug | nvarchar(200) | Required, MaxLength(200), Unique | URL-friendly identifier |
| Description | nvarchar(5000) | Required, MaxLength(5000) | Biographical information |
| PortraitUrl | nvarchar(500) | Optional, MaxLength(500) | S3 URL to portrait image |
| BackgroundUrl | nvarchar(500) | Optional, MaxLength(500) | S3 URL to background image |
| Email | nvarchar(255) | Optional, MaxLength(255) | Contact email (optional) |
| CreatedAt | datetime2 | Required, Default(GetUtcDate()) | Creation timestamp |
| UpdatedAt | datetime2 | Required, Default(GetUtcDate()) | Last update timestamp |
| IsPublic | bit | Required, Default(1) | Page visibility status |

#### FAREWELL_MESSAGE Table
| Column | Data Type | Constraints | Description |
|--------|-----------|-------------|-------------|
| Id | int | Primary Key, Identity | Unique identifier |
| FarewellPersonId | int | Required, Foreign Key | Reference to memorial page |
| AuthorName | nvarchar(100) | Optional, MaxLength(100) | Message author's name |
| AuthorEmail | nvarchar(255) | Optional, MaxLength(255) | Message author's email |
| Message | nvarchar(2000) | Required, MaxLength(2000) | Message content |
| CreatedAt | datetime2 | Required, Default(GetUtcDate()) | Creation timestamp |
| IsPublic | bit | Required, Default(1) | Message visibility status |

#### CONTENT_REPORT Table
| Column | Data Type | Constraints | Description |
|--------|-----------|-------------|-------------|
| Id | uniqueidentifier | Primary Key, Default(NewId()) | Unique identifier |
| Email | nvarchar(255) | Required, MaxLength(255) | Reporter's email |
| FarewellPersonId | int | Optional, Foreign Key | Reference to reported person |
| FarewellMessageId | int | Optional, Foreign Key | Reference to reported message |
| Reason | nvarchar(100) | Required, MaxLength(100) | Report reason (Spam, Abuse, etc.) |
| Explanation | nvarchar(1000) | Optional, MaxLength(1000) | Detailed explanation |
| CreatedAt | datetime2 | Required, Default(GetUtcDate()) | Report creation timestamp |
| ResolvedAt | datetime2 | Optional | Resolution timestamp |

#### MODERATOR_LOG Table
| Column | Data Type | Constraints | Description |
|--------|-----------|-------------|-------------|
| Id | int | Primary Key, Identity | Unique identifier |
| ModeratorName | nvarchar(100) | Required | Admin user's display name |
| TargetType | nvarchar(50) | Required | Entity type modified |
| TargetId | int | Required | ID of modified entity |
| Action | nvarchar(50) | Required | Action performed (edit, delete, etc.) |
| Reason | nvarchar(200) | Optional | Action reason code |
| Details | nvarchar(1000) | Required | Detailed action description |
| ContentReportId | uniqueidentifier | Optional, Foreign Key | Related content report |
| CreatedAt | datetime2 | Required, Default(GetUtcDate()) | Action timestamp |

### 3.3 Database Indexes
```sql
-- Primary keys are automatically indexed

-- Performance indexes
CREATE INDEX IX_FarewellPerson_Slug ON FAREWELL_PERSON(Slug);
CREATE INDEX IX_FarewellMessage_FarewellPersonId ON FAREWELL_MESSAGE(FarewellPersonId);
CREATE INDEX IX_FarewellMessage_CreatedAt ON FAREWELL_MESSAGE(CreatedAt DESC);
CREATE INDEX IX_ContentReport_CreatedAt ON CONTENT_REPORT(CreatedAt DESC);
CREATE INDEX IX_ContentReport_FarewellPersonId ON CONTENT_REPORT(FarewellPersonId);
CREATE INDEX IX_ContentReport_FarewellMessageId ON CONTENT_REPORT(FarewellMessageId);
CREATE INDEX IX_ModeratorLog_CreatedAt ON MODERATOR_LOG(CreatedAt DESC);
CREATE INDEX IX_ModeratorLog_TargetType ON MODERATOR_LOG(TargetType, TargetId);
```

## 4. API Specification

### 4.1 Controllers and Actions

#### HomeController
| Action | HTTP Method | Route | Description |
|--------|-------------|-------|-------------|
| Index | GET | / | Displays the home page |
| Privacy | GET | /Privacy | Displays the privacy policy page |
| Error | GET | /Home/Error | Displays the error page |
| Search | GET | /Home/Search | Performs a search for memorial pages |
| Slug | GET | /{slug} | Displays a specific memorial page by its slug |

#### FarewellPersonController
| Action | HTTP Method | Route | Description |
|--------|-------------|-------|-------------|
| Create | GET | /FarewellPerson/Create | Displays the form to create a new memorial page |
| Create | POST | /FarewellPerson/Create | Handles the submission for creating a new memorial page |

#### FarewellMessageController
| Action | HTTP Method | Route | Description |
|--------|-------------|-------|-------------|
| Create | GET | /FarewellMessage/Create | Displays the form to create a new farewell message |
| Create | POST | /FarewellMessage/Create | Handles the submission for creating a new farewell message |

#### AdminController
| Action | HTTP Method | Route | Description |
|--------|-------------|-------|-------------|
| Index | GET | /Admin | Displays the admin dashboard with analytics |
| FarewellPeople | GET | /Admin/FarewellPeople | Manages memorial pages with pagination |
| FarewellMessages | GET | /Admin/FarewellMessages | Manages farewell messages with pagination |
| ContentReports | GET | /Admin/ContentReports | Manages content reports with pagination |
| AdminLogs | GET | /Admin/AdminLogs | Displays admin action logs with pagination |
| Login | GET | /Admin/login | Initiates GitHub OAuth login process |
| Logout | GET | /Admin/logout | Logs out the current admin user |
| EditFarewellPerson | GET | /Admin/FarewellPeople/Edit/{id} | Displays the form to edit a memorial page |
| EditFarewellPerson | POST | /Admin/FarewellPeople/Edit/{id} | Handles submission for editing a memorial page |
| EditFarewellMessage | GET | /Admin/FarewellMessages/Edit/{id} | Displays the form to edit a farewell message |
| EditFarewellMessage | POST | /Admin/FarewellMessages/Edit/{id} | Handles submission for editing a farewell message |
| DeleteFarewellMessage | GET | /Admin/FarewellMessages/Delete/{id} | Displays confirmation for deleting a message |
| DeleteFarewellMessage | POST | /Admin/FarewellMessages/Delete/{id} | Handles deletion of a farewell message |
| DeleteFarewellPerson | GET | /Admin/FarewellPeople/Delete/{id} | Displays confirmation for deleting a memorial page |
| DeleteFarewellPerson | POST | /Admin/FarewellPeople/Delete/{id} | Handles deletion of a memorial page |

#### ReportController
| Action | HTTP Method | Route | Description |
|--------|-------------|-------|-------------|
| Index | GET | /report | Displays the content report submission form |
| Index | POST | /report | Handles the submission of a new content report |
| Success | GET | /report/success | Displays the report submission success page |

## 5. File Upload Specification

### 5.1 Upload Requirements
- **Portrait Images**: Max 5MB, formats: JPG, PNG, GIF
- **Background Images**: Max 10MB, formats: JPG, PNG
- **Storage Location**: AWS S3-compatible storage (Filebase)
- **File Naming**: `{type}/{guid}-{original_filename}` (type = portrait/background)
- **Access Control**: Public read access, signed URLs for preview

### 5.2 Upload Validation
```csharp
public class FileUploadValidator
{
    public static bool IsValidImage(IFormFile file, out string errorMessage)
    {
        errorMessage = string.Empty;
        
        if (file == null || file.Length == 0)
        {
            errorMessage = "No file selected";
            return false;
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(fileExtension))
        {
            errorMessage = "Only JPG, PNG, and GIF files are allowed";
            return false;
        }

        if (file.Length > 5 * 1024 * 1024) // 5MB
        {
            errorMessage = "File size cannot exceed 5MB";
            return false;
        }

        return true;
    }
}
```

## 6. Routing Configuration

### 6.1 Default Routes
```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Dynamic route for memorial pages
app.MapControllerRoute(
    name: "slug",
    pattern: "{slug:minlength(1)}",
    defaults: new { controller = "Home", action = "Slug" });
```

### 6.2 Route Priority
1. Static routes (Privacy, Admin, etc.)
2. Memorial page dynamic route (`/{slug}`)
3. Default MVC route

## 7. Security Considerations

### 7.1 Input Validation
- Server-side validation for all user inputs
- Client-side validation using jQuery validation
- HTML encoding to prevent XSS attacks
- SQL injection prevention through Entity Framework
- Model validation with Data Annotations

### 7.2 File Upload Security
- File type validation for S3 uploads
- File size limits (5MB for portraits, 10MB for backgrounds)
- Secure file naming with GUIDs
- S3 access controls with signed URLs
- Path traversal prevention

### 7.3 Authentication & Authorization
- GitHub OAuth 2.0 authentication
- Cookie-based session management
- Role-based access control with email whitelisting
- Admin-only route protection
- Automatic logout for unauthorized users

### 7.4 Data Protection
- S3 secure storage with proper access controls
- Input sanitization and validation
- Rate limiting for API endpoints
- Request size limits
- CORS configuration
- Audit logging for all admin actions

## 8. Performance Requirements

### 8.1 Response Time Targets
- Page load: < 2 seconds
- Database query: < 500ms
- File upload: < 10 seconds (for 5MB files)
- Message submission: < 1 second

### 8.2 Scalability Requirements
- Support 1000+ concurrent users
- Handle 10,000+ memorial pages
- Process 100,000+ messages
- Efficient database indexing

### 8.3 Caching Strategy
- Static file caching
- Database query caching (future enhancement)
- View model caching (future enhancement)

## 9. Error Handling

### 9.1 Error Types
- **Validation Errors**: Form field validation
- **File Upload Errors**: Invalid files, size limits
- **Database Errors**: Connection issues, constraint violations
- **Routing Errors**: 404 for non-existent pages
- **General Errors**: 500 for unexpected issues

### 9.2 Error Response Format
```csharp
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
    public string? RequestId { get; set; }
}
```

## 10. Testing Requirements

### 10.1 Unit Testing
- Controller action testing
- Service layer testing
- Model validation testing
- File upload validation testing

### 10.2 Integration Testing
- Database operations testing
- File upload functionality testing
- API endpoint testing
- Routing testing

### 10.3 End-to-End Testing
- User journey testing
- Browser compatibility testing
- Responsive design testing
- Performance testing

## 11. Deployment Requirements

### 11.1 Environment Setup
- Development: Local IIS Express with SQL Server LocalDB
- Staging: Cloud-based staging environment with production database
- Production: Cloud-based production environment with S3 storage

### 11.2 Configuration Management
- Environment-specific settings in `appsettings.json` and `appsettings.Production.json`
- Database connection strings
- S3 storage configuration (bucket, credentials, endpoint)
- GitHub OAuth configuration (client ID, client secret)
- Admin email whitelisting
- Content report reason strings
- Logging configuration

### 11.3 Monitoring and Logging
- Application logging using built .NET logging
- Error tracking with custom error pages
- Performance monitoring for database queries
- User activity logging (anonymous analytics)
- Admin action audit logging
- S3 API usage tracking
- Content report resolution tracking

## 12. Future Enhancements

### 12.1 Phase 1 (Current - Implemented)
- Core memorial page functionality
- Message system with optional author info
- S3 file upload support
- Responsive design
- GitHub OAuth authentication
- Admin dashboard with analytics
- Content reporting system
- Complete audit logging
- Role-based access control

### 12.2 Phase 2 (Planned)
- Email notifications for new messages and reports
- Advanced search and filtering capabilities
- Enhanced analytics dashboard
- Social media sharing integration
- Theme customization options
- Rate limiting and DDoS protection

### 12.3 Phase 3 (Future)
- Mobile application development
- Multi-language support
- API development for third-party integration
- Enhanced caching for performance
- SEO optimization
- Multiple S3 provider support

## 13. Compliance and Legal

### 13.1 Data Privacy
- GDPR compliance considerations
- Data retention policies
- User consent for data collection
- Anonymous data handling

### 13.2 Accessibility
- WCAG 2.1 AA compliance
- Screen reader compatibility
- Keyboard navigation support
- Color contrast requirements

---

*This specification document will be updated as the project evolves and new requirements are identified.*