# PowerShell script to set up GitFlow branching model

# Ensure we are in the project root
if (-not (Test-Path "SmartInsight.sln")) {
    Write-Error "Error: This script must be run from the project root"
    exit 1
}

# Check if git is initialized
if (-not (Test-Path ".git")) {
    Write-Error "Error: Git repository is not initialized"
    exit 1
}

# Create develop branch if it doesn't exist
$developExists = $false
try {
    $null = git rev-parse --verify develop
    $developExists = $true
} catch {
    $developExists = $false
}

if (-not $developExists) {
    Write-Host "Creating develop branch..."
    git checkout -b develop
    git push -u origin develop
} else {
    Write-Host "Develop branch already exists"
}

# Set up git hooks
if (-not (Test-Path ".git/hooks")) {
    Write-Error "Error: Git hooks directory not found"
    exit 1
}

# Create pre-commit hook
Write-Host "Setting up pre-commit hook..."
$preCommitContent = @"
#!/bin/bash

# Run dotnet format
echo "Running dotnet format..."
dotnet format --verify-no-changes

if [ $? -ne 0 ]; then
    echo "Error: Code formatting issues found. Please run 'dotnet format' to fix them."
    exit 1
fi

# Run ESLint for UI code if it exists
if [ -d "src/SmartInsight.UI" ]; then
    echo "Running ESLint..."
    cd src/SmartInsight.UI
    if [ -f "package.json" ]; then
        npm run lint
        if [ $? -ne 0 ]; then
            echo "Error: ESLint issues found. Please fix them before committing."
            exit 1
        fi
    fi
    cd ../..
fi

exit 0
"@

Set-Content -Path ".git/hooks/pre-commit" -Value $preCommitContent
# Make the hook executable in Git for Windows/WSL environments
git update-index --chmod=+x .git/hooks/pre-commit

# Create pre-push hook
Write-Host "Setting up pre-push hook..."
$prePushContent = @"
#!/bin/bash

# Run tests
echo "Running tests..."
dotnet test

if [ $? -ne 0 ]; then
    echo "Error: Tests failed. Please fix them before pushing."
    exit 1
fi

exit 0
"@

Set-Content -Path ".git/hooks/pre-push" -Value $prePushContent
# Make the hook executable in Git for Windows/WSL environments
git update-index --chmod=+x .git/hooks/pre-push

Write-Host "GitFlow setup complete!"
Write-Host "Main branch: main"
Write-Host "Development branch: develop"
Write-Host "Feature branches should be created from 'develop' using: git checkout -b feature/your-feature-name develop"
Write-Host "Bug fix branches should be created from 'develop' using: git checkout -b bugfix/your-bugfix-name develop"
Write-Host "Release branches should be created from 'develop' using: git checkout -b release/version-number develop"
Write-Host "Hotfix branches should be created from 'main' using: git checkout -b hotfix/your-hotfix-name main" 