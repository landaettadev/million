# JWT Configuration

## Overview
This document describes the JWT (JSON Web Token) configuration for the RealEstate API authentication system.

## Configuration Keys

### Required
- `JWT:KEY` - Secret key for signing JWT tokens (minimum 256 bits recommended)
- `JWT:ISSUER` - Token issuer identifier
- `JWT:AUDIENCE` - Token audience identifier  
- `JWT:EXPIRES_MIN` - Token expiration time in minutes

### Example Configuration
```json
{
  "JWT": {
    "KEY": "your-super-secure-secret-key-256-bits-minimum",
    "ISSUER": "millionluxury",
    "AUDIENCE": "millionluxury-admin",
    "EXPIRES_MIN": "60"
  }
}
```

## Environment-Specific Configurations

### Development
- **File**: `appsettings.Development.json`
- **Expiration**: 60 minutes
- **Validation**: Basic (issuer/audience validation disabled for development)

### Staging
- **File**: `appsettings.Staging.json`
- **Expiration**: 45 minutes
- **Validation**: Full (issuer/audience validation enabled)

### Production
- **File**: `appsettings.Production.json`
- **Expiration**: 30 minutes
- **Validation**: Full (issuer/audience validation enabled)

## Security Features

### Token Validation
- **Algorithm**: HMAC-SHA256 (HS256)
- **Issuer Validation**: Enabled in staging/production
- **Audience Validation**: Enabled in staging/production
- **Clock Skew**: 30 seconds tolerance

### Claims
- `sub` - User ID
- `email` - User email address
- `name` - User display name
- `role` - User role (Admin)

### Rate Limiting
- **Max Login Attempts**: 5 per IP
- **Lockout Duration**: 15 minutes
- **Tracking**: IP-based and user-based (when available)

## Implementation Details

### AuthService
- Generates JWT tokens with configured issuer/audience
- Validates refresh tokens with full issuer/audience validation
- Uses BCrypt for password hashing

### Program.cs
- Configures JWT Bearer authentication
- Applies issuer/audience validation based on configuration
- Integrates with ASP.NET Core authorization system

### Endpoints
- `/api/admin/auth/login` - Public endpoint for authentication
- `/api/admin/auth/refresh` - Protected endpoint for token refresh
- `/api/admin/auth/logout` - Protected endpoint for logout
- All admin endpoints require `Admin` role

## Security Best Practices

1. **Secret Key**: Use a strong, randomly generated secret key (minimum 256 bits)
2. **Environment Variables**: Override JWT settings via environment variables in production
3. **Token Expiration**: Keep expiration times short (30-60 minutes)
4. **HTTPS Only**: Always use HTTPS in production
5. **Rate Limiting**: Implement rate limiting to prevent brute force attacks
6. **Audit Logging**: Log authentication attempts and failures

## Environment Variables

You can override JWT settings using environment variables:

```bash
# Windows
set JWT__KEY=your-secret-key
set JWT__ISSUER=your-issuer
set JWT__AUDIENCE=your-audience
set JWT__EXPIRES_MIN=30

# Linux/macOS
export JWT__KEY=your-secret-key
export JWT__ISSUER=your-issuer
export JWT__AUDIENCE=your-audience
export JWT__EXPIRES_MIN=30
```

## Testing

The JWT configuration is tested in:
- `AuthServiceTests` - Unit tests for JWT generation and validation
- `AuthEndpointsIntegrationTests` - Integration tests for authentication endpoints
- `RateLimitingMiddlewareTests` - Tests for rate limiting functionality

## Troubleshooting

### Common Issues

1. **"JWT:KEY missing"** - Ensure JWT:KEY is configured in appsettings
2. **Token validation fails** - Check issuer/audience configuration matches
3. **Rate limiting too aggressive** - Adjust MaxLoginAttempts and LockoutDurationMinutes
4. **CORS issues** - Verify CORS configuration for admin panel origins
