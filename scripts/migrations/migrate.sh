#!/bin/bash

# PRN232_G9_AutoGradingTool Migration Tool for Linux/macOS
# Usage: ./scripts/migrations/migrate.sh <action> [name] [context] [env-file]
# Example: ./scripts/migrations/migrate.sh add "AddUserTable" main .env.docker

set -e  # Exit on error

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
NC='\033[0m' # No Color

# Parameters
ACTION=$1
NAME=$2
CONTEXT=${3:-both}
ENV_FILE=$4

# Validate action
if [[ ! "$ACTION" =~ ^(add|update|remove|list)$ ]]; then
    echo -e "${RED}Error: Invalid action '$ACTION'${NC}"
    echo "Valid actions: add, update, remove, list"
    echo "Usage: ./scripts/migrations/migrate.sh <action> [name] [context] [env-file]"
    echo "Example: ./scripts/migrations/migrate.sh add 'AddUserTable' main"
    exit 1
fi

# Validate name for add action
if [[ "$ACTION" == "add" && -z "$NAME" ]]; then
    echo -e "${RED}Error: Migration name is required for 'add' action${NC}"
    echo "Usage: ./scripts/migrations/migrate.sh add <name> [context] [env-file]"
    exit 1
fi

echo -e "${MAGENTA}=== PRN232_G9_AutoGradingTool Migration Tool ===${NC}"
echo ""

# Get root directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCRIPTS_DIR="$(dirname "$SCRIPT_DIR")"
ROOT_DIR="$(dirname "$SCRIPTS_DIR")"

# Function to load environment file
load_env_file() {
    local file_path=$1
    
    if [[ -f "$file_path" ]]; then
        echo -e "${YELLOW}Loading: $(basename "$file_path")${NC}"
        
        while IFS='=' read -r key value; do
            # Skip comments and empty lines
            [[ "$key" =~ ^#.*$ ]] && continue
            [[ -z "$key" ]] && continue
            
            # Remove quotes from value
            value=$(echo "$value" | sed -e 's/^"//' -e 's/"$//' -e "s/^'//" -e "s/'$//")
            
            # Export variable
            export "$key=$value"
        done < "$file_path"
        
        return 0
    fi
    
    return 1
}

# Load environment files
echo -e "${CYAN}Loading environment variables...${NC}"
LOADED=false

if [[ -n "$ENV_FILE" ]]; then
    # Use user-specified env file
    CUSTOM_ENV_PATH="$ROOT_DIR/$ENV_FILE"
    if load_env_file "$CUSTOM_ENV_PATH"; then
        LOADED=true
    else
        echo -e "${YELLOW}[WARN] Specified env file not found: $ENV_FILE${NC}"
    fi
else
    # Load in priority order (later files override earlier ones)
    ENV_FILES=(
        ".env"
        ".env.local"
        ".env.${ASPNETCORE_ENVIRONMENT:-Development}"
        ".env.docker"
    )
    
    for env_file in "${ENV_FILES[@]}"; do
        env_path="$ROOT_DIR/$env_file"
        if load_env_file "$env_path"; then
            LOADED=true
        fi
    done
fi

if $LOADED; then
    echo -e "${GREEN}[OK] Environment variables loaded${NC}"
else
    echo -e "${YELLOW}[WARN] No environment files found${NC}"
fi
echo ""

# Verify connection strings
if [[ -n "$ConnectionStrings__DefaultConnection" ]]; then
    echo -e "${GREEN}[OK] DefaultConnection: ${ConnectionStrings__DefaultConnection:0:50}...${NC}"
else
    echo -e "${YELLOW}[WARN] DefaultConnection not found in environment${NC}"
fi

if [[ -n "$ConnectionStrings__OuterDbConnection" ]]; then
    echo -e "${GREEN}[OK] OuterDbConnection: ${ConnectionStrings__OuterDbConnection:0:50}...${NC}"
else
    echo -e "${YELLOW}[WARN] OuterDbConnection not found in environment${NC}"
fi
echo ""

# Infrastructure project path
INFRA_PROJECT="src/PRN232_G9_AutoGradingTool.Infrastructure"

# Change to root directory
cd "$ROOT_DIR"

# Function to run migration
run_migration() {
    local context_name=$1
    local output_dir=$2
    
    echo -e "${CYAN}>> Processing $context_name...${NC}"
    
    local exit_code=0
    
    case "$ACTION" in
        add)
            dotnet ef migrations add "$NAME" \
                --context "$context_name" \
                --output-dir "$output_dir" \
                --project "$INFRA_PROJECT" || exit_code=$?
            ;;
        update)
            dotnet ef database update \
                --context "$context_name" \
                --project "$INFRA_PROJECT" || exit_code=$?
            ;;
        remove)
            dotnet ef migrations remove \
                --context "$context_name" \
                --project "$INFRA_PROJECT" \
                --force || exit_code=$?
            ;;
        list)
            dotnet ef migrations list \
                --context "$context_name" \
                --project "$INFRA_PROJECT" || exit_code=$?
            ;;
    esac
    
    if [[ $exit_code -eq 0 ]]; then
        echo -e "  ${GREEN}[OK] Success${NC}"
        echo ""
        return 0
    else
        echo -e "  ${RED}[FAIL] Failed with exit code $exit_code${NC}"
        echo ""
        return 1
    fi
}

# Track success
SUCCESS=true

# Execute based on context
if [[ "$CONTEXT" == "both" || "$CONTEXT" == "main" ]]; then
    run_migration "PRN232_G9_AutoGradingToolDbContext" "Migrations/PRN232_G9_AutoGradingTool" || SUCCESS=false
fi

if [[ "$CONTEXT" == "both" || "$CONTEXT" == "outer" ]]; then
    run_migration "PRN232_G9_AutoGradingToolOuterDbContext" "Migrations/PRN232_G9_AutoGradingToolOuter" || SUCCESS=false
fi

# Final status
if $SUCCESS; then
    echo -e "${GREEN}=== All operations completed successfully ===${NC}"
    exit 0
else
    echo -e "${RED}=== Some operations failed ===${NC}"
    exit 1
fi
