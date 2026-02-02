#!/bin/bash

echo "🧹 Starting cleanup of build artifacts and unnecessary files..."
echo ""

# Function to safely remove directory if it exists
remove_dir() {
    if [ -d "$1" ]; then
        echo "  Removing: $1"
        rm -rf "$1"
    fi
}

# Function to safely remove files matching pattern
remove_files() {
    find . -name "$1" -type f -not -path "./.git/*" -delete 2>/dev/null
    if [ $? -eq 0 ]; then
        echo "  Removed files matching: $1"
    fi
}

# Remove build outputs
echo "📦 Removing build outputs..."
remove_dir "backend/bin"
remove_dir "backend/obj"
remove_dir "backend.Tests/bin"
remove_dir "backend.Tests/obj"
remove_dir "frontend/dist"
remove_dir "frontend/node_modules"

# Remove IDE files
echo "💻 Removing IDE files..."
remove_dir ".vs"
remove_dir ".idea"
remove_dir ".vscode"
remove_dir ".history"

# Remove log files
echo "📝 Removing log files..."
remove_files "*.log"
remove_files "*.suo"
remove_files "*.user"
remove_files "*.userosscache"
remove_files "*.sln.docstates"

# Remove temporary files
echo "🗑️  Removing temporary files..."
remove_files "*.tmp"
remove_files "*.cache"
remove_files "*.pidb"
remove_files "*.svclog"

# Remove OS-specific files
echo "🖥️  Removing OS-specific files..."
remove_files ".DS_Store"
remove_files "Thumbs.db"

echo ""
echo "✅ Cleanup complete!"
echo ""
echo "📋 Essential files preserved:"
echo "  ✅ All source code (.cs, .tsx, .ts files)"
echo "  ✅ All documentation (.md files)"
echo "  ✅ Configuration files (appsettings.json, docker-compose.yml)"
echo "  ✅ Project files (.csproj, .sln, package.json)"
echo "  ✅ Migration files"
echo "  ✅ .gitignore"
echo ""
