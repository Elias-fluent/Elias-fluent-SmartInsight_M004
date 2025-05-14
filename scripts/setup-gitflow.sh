#!/bin/bash

# Script to set up GitFlow branching model

# Ensure we are in the project root
if [ ! -f "SmartInsight.sln" ]; then
    echo "Error: This script must be run from the project root"
    exit 1
fi

# Check if git is initialized
if [ ! -d ".git" ]; then
    echo "Error: Git repository is not initialized"
    exit 1
fi

# Create develop branch if it doesn't exist
if ! git rev-parse --verify develop > /dev/null 2>&1; then
    echo "Creating develop branch..."
    git checkout -b develop
    git push -u origin develop
else
    echo "Develop branch already exists"
fi

# Set up git hooks
if [ ! -d ".git/hooks" ]; then
    echo "Error: Git hooks directory not found"
    exit 1
fi

# Create pre-commit hook
echo "Setting up pre-commit hook..."
cat > .git/hooks/pre-commit << 'EOF'
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
EOF

chmod +x .git/hooks/pre-commit

# Create pre-push hook
echo "Setting up pre-push hook..."
cat > .git/hooks/pre-push << 'EOF'
#!/bin/bash

# Run tests
echo "Running tests..."
dotnet test

if [ $? -ne 0 ]; then
    echo "Error: Tests failed. Please fix them before pushing."
    exit 1
fi

exit 0
EOF

chmod +x .git/hooks/pre-push

echo "GitFlow setup complete!"
echo "Main branch: main"
echo "Development branch: develop"
echo "Feature branches should be created from 'develop' using: git checkout -b feature/your-feature-name develop"
echo "Bug fix branches should be created from 'develop' using: git checkout -b bugfix/your-bugfix-name develop"
echo "Release branches should be created from 'develop' using: git checkout -b release/version-number develop"
echo "Hotfix branches should be created from 'main' using: git checkout -b hotfix/your-hotfix-name main" 