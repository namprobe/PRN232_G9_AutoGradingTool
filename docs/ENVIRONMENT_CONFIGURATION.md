# Environment Configuration Guide

Hướng dẫn cấu hình environment variables cho các môi trường khác nhau: Local Development, CI/CD, và AWS (Terraform).

## 📋 Priority Order (Cao → Thấp)

1. **Environment Variables** (từ OS/CI/CD/AWS) - **Cao nhất**
2. `.env.{environment}.local` (local overrides)
3. `.env.{environment}` (environment-specific)
4. `.env.local` (local overrides)
5. `.env.docker` (Docker-specific)
6. `.env` (base configuration) - **Thấp nhất**

**Lưu ý:** Environment variables từ CI/CD và AWS **luôn có priority cao nhất**, đảm bảo production config không bị override bởi .env files.

---

## 🏠 Local Development

### Setup

1. **Copy `.env.example` thành `.env`**
   ```bash
   cp .env.example .env
   ```

2. **Chỉnh sửa `.env` với giá trị local của bạn**
   ```env
   POSTGRES_USER=postgres
   POSTGRES_PASSWORD=your_local_password
   ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=PRN232_G9_AutoGradingTool_db;...
   ```

3. **Chạy ứng dụng**
   ```bash
   dotnet run
   ```

### Local Overrides

- `.env.local` - Override cho tất cả environments (không commit vào Git)
- `.env.Development.local` - Override cho Development environment

---

## 🚀 CI/CD (GitHub Actions, GitLab CI, etc.)

### GitHub Actions Example

```yaml
name: Deploy to AWS

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Set up .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Build
        env:
          # Environment variables override .env files
          ConnectionStrings__DefaultConnection: ${{ secrets.DB_CONNECTION_STRING }}
          Jwt__Key: ${{ secrets.JWT_SECRET_KEY }}
          ASPNETCORE_ENVIRONMENT: Production
        run: dotnet build
      
      - name: Deploy
        env:
          ConnectionStrings__DefaultConnection: ${{ secrets.DB_CONNECTION_STRING }}
          Jwt__Key: ${{ secrets.JWT_SECRET_KEY }}
        run: dotnet publish
```

### GitLab CI Example

```yaml
deploy:
  script:
    - dotnet build
    - dotnet publish
  variables:
    ConnectionStrings__DefaultConnection: $CI_DB_CONNECTION_STRING
    Jwt__Key: $CI_JWT_SECRET_KEY
    ASPNETCORE_ENVIRONMENT: Production
```

**Lưu ý:** 
- Sử dụng **GitHub Secrets** hoặc **GitLab CI/CD Variables** để lưu sensitive data
- Environment variables được set trong CI/CD sẽ **tự động override** .env files

---

## ☁️ AWS Deployment (Terraform)

### ECS Task Definition

```hcl
resource "aws_ecs_task_definition" "PRN232_G9_AutoGradingTool_api" {
  family                   = "PRN232_G9_AutoGradingTool-api"
  requires_compatibilities = ["FARGATE"]
  network_mode            = "awsvpc"
  cpu                     = "512"
  memory                  = "1024"

  container_definitions = jsonencode([{
    name  = "PRN232_G9_AutoGradingTool-api"
    image = "your-ecr-repo/PRN232_G9_AutoGradingTool-api:latest"
    
    environment = [
      {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      },
      {
        name  = "Localization__SupportedCultures"
        value = "en,vi"
      },
      {
        name  = "Localization__DefaultCulture"
        value = "en"
      }
    ]
    
    secrets = [
      {
        name      = "ConnectionStrings__DefaultConnection"
        valueFrom = aws_secretsmanager_secret.db_connection.arn
      },
      {
        name      = "Jwt__Key"
        valueFrom = aws_secretsmanager_secret.jwt_key.arn
      },
      {
        name      = "POSTGRES_PASSWORD"
        valueFrom = aws_secretsmanager_secret.postgres_password.arn
      }
    ]
  }])
}
```

### AWS Secrets Manager Integration

```hcl
# Create secrets in AWS Secrets Manager
resource "aws_secretsmanager_secret" "db_connection" {
  name = "PRN232_G9_AutoGradingTool/db/connection-string"
}

resource "aws_secretsmanager_secret_version" "db_connection" {
  secret_id = aws_secretsmanager_secret.db_connection.id
  secret_string = jsonencode({
    connection_string = "Host=${aws_rds_cluster.main.endpoint};Port=5432;Database=PRN232_G9_AutoGradingTool_db;Username=${var.db_username};Password=${var.db_password};"
  })
}

resource "aws_secretsmanager_secret" "jwt_key" {
  name = "PRN232_G9_AutoGradingTool/jwt/secret-key"
}

resource "aws_secretsmanager_secret_version" "jwt_key" {
  secret_id = aws_secretsmanager_secret.jwt_key.id
  secret_string = var.jwt_secret_key
}
```

### ECS Service với Secrets

```hcl
resource "aws_ecs_service" "PRN232_G9_AutoGradingTool_api" {
  name            = "PRN232_G9_AutoGradingTool-api"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.PRN232_G9_AutoGradingTool_api.arn
  desired_count   = 2
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = aws_subnet.private[*].id
    security_groups  = [aws_security_group.ecs.id]
    assign_public_ip = false
  }

  # IAM role để ECS task có thể đọc Secrets Manager
  depends_on = [aws_iam_role_policy.ecs_secrets]
}
```

### IAM Role cho Secrets Manager

```hcl
resource "aws_iam_role" "ecs_task" {
  name = "PRN232_G9_AutoGradingTool-ecs-task-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "ecs-tasks.amazonaws.com"
      }
    }]
  })
}

resource "aws_iam_role_policy" "ecs_secrets" {
  name = "PRN232_G9_AutoGradingTool-ecs-secrets-policy"
  role = aws_iam_role.ecs_task.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = [
        "secretsmanager:GetSecretValue",
        "secretsmanager:DescribeSecret"
      ]
      Resource = [
        aws_secretsmanager_secret.db_connection.arn,
        aws_secretsmanager_secret.jwt_key.arn,
        aws_secretsmanager_secret.postgres_password.arn
      ]
    }]
  })
}
```

---

## 🐳 Docker & Docker Compose

### Docker Compose với Environment Variables

```yaml
services:
  PRN232_G9_AutoGradingTool-api:
    build:
      context: .
      dockerfile: ./src/PRN232_G9_AutoGradingTool.API/Dockerfile
    environment:
      # Override .env file values
      - ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Development}
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION_STRING}
    env_file:
      - .env  # Base config
      - .env.docker  # Docker-specific overrides
```

### Docker Run với Environment Variables

```bash
docker run -d \
  -e ConnectionStrings__DefaultConnection="Host=db;Port=5432;Database=PRN232_G9_AutoGradingTool_db;..." \
  -e Jwt__Key="your-secret-key" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  PRN232_G9_AutoGradingTool-api:latest
```

---

## 🔐 Security Best Practices

### ✅ DO

1. **Sử dụng Secrets Manager** cho production secrets
2. **Không commit** `.env` files vào Git
3. **Sử dụng environment variables** trong CI/CD
4. **Rotate secrets** định kỳ
5. **Sử dụng IAM roles** thay vì hard-code credentials

### ❌ DON'T

1. **Không hard-code** secrets trong code
2. **Không commit** `.env` files với real values
3. **Không log** sensitive environment variables
4. **Không share** `.env` files qua email/chat

---

## 📝 Environment Variable Naming

### Format

- **Flat keys:** `POSTGRES_USER`, `API_PORT`
- **Hierarchical keys:** `ConnectionStrings__DefaultConnection`, `Jwt__Key`
  - Double underscore `__` được convert thành `:` trong IConfiguration
  - Example: `ConnectionStrings__DefaultConnection` → `ConnectionStrings:DefaultConnection`

### Examples

```env
# Flat keys
POSTGRES_USER=admin
API_PORT=5000

# Hierarchical keys (double underscore)
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;...
Jwt__Key=your-secret-key
Localization__SupportedCultures=en,vi
Hangfire__Queues=notification-system,default
```

---

## 🔍 Debugging

### Check Environment Variables

```bash
# Windows PowerShell
Get-ChildItem Env: | Where-Object { $_.Name -like "*ConnectionStrings*" }

# Linux/Mac
env | grep ConnectionStrings
```

### Log Configuration on Startup

Application sẽ log configuration values khi startup:
- Localization configuration
- Hangfire queues configuration
- Database connection status

Check logs để verify environment variables được load đúng.

---

## 📚 References

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [AWS ECS Task Definitions](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task_definition_parameters.html)
- [AWS Secrets Manager](https://docs.aws.amazon.com/secretsmanager/)
- [Terraform AWS Provider](https://registry.terraform.io/providers/hashicorp/aws/latest/docs)

---

**Made with ❤️ by the Log.AI-CAMS Team**
