# Cinemarco - Development Commands

## Development

```bash
# Start both backend and frontend together (recommended)
npm run dev

# Or start separately:
# Backend (Terminal 1)
cd src/Server && dotnet watch run

# Frontend (Terminal 2)  
npm run dev:client
```

## Testing

```bash
# Run all tests
dotnet test

# Run tests with specific project
dotnet test src/Tests/Tests.fsproj
```

## Building

```bash
# Build frontend
npm run build

# Build backend
dotnet build

# Build for production
npm run build && dotnet publish src/Server -c Release
```

## Docker

```bash
# Build image
docker build -t cinemarco .

# Run locally
docker run -p 5000:5000 -v $(pwd)/data:/app/data cinemarco

# Deploy stack
docker-compose up -d

# View logs
docker logs cinemarco
```

## Git (Windows)

```bash
git status
git add .
git commit -m "message"
git push
git log --oneline -10
```

## Windows Utilities

```powershell
# List directory
dir
Get-ChildItem

# Find files
Get-ChildItem -Recurse -Filter "*.fs"

# Search in files
Select-String -Path "src/**/*.fs" -Pattern "pattern"
```
