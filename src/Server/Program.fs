module Program

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.FileProviders
open Giraffe
open System
open System.IO

/// Load environment variables from .env file
let private loadEnvFile () =
    // Try to find .env file - check current directory and parent directories
    let rec findEnvFile (dir: string) =
        let envPath = Path.Combine(dir, ".env")
        if File.Exists(envPath) then
            Some envPath
        else
            let parent = Directory.GetParent(dir)
            if isNull parent then None
            else findEnvFile parent.FullName

    match findEnvFile (Directory.GetCurrentDirectory()) with
    | Some envPath ->
        DotNetEnv.Env.Load(envPath) |> ignore
        printfn $"Loaded environment from: {envPath}"
    | None ->
        printfn "No .env file found (using system environment variables)"

[<EntryPoint>]
let main args =
    // Load .env file first (before any other initialization)
    loadEnvFile()

    let builder = WebApplication.CreateBuilder(args)

    // Add Giraffe
    builder.Services.AddGiraffe() |> ignore

    // Configure CORS for development
    builder.Services.AddCors(fun options ->
        options.AddPolicy("AllowAll", fun policy ->
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
            |> ignore
        )
    ) |> ignore

    let app = builder.Build()

    // Initialize data directory
    Persistence.ensureDataDir()

    // Run database migrations
    printfn $"Database path: {Persistence.getDatabasePath()}"
    Migrations.runMigrations Persistence.connectionString

    if app.Environment.IsDevelopment() then
        app.UseDeveloperExceptionPage() |> ignore

    app.UseCors("AllowAll") |> ignore
    app.UseRouting() |> ignore

    // Serve static files from dist/public
    let publicPath = Path.Combine(Directory.GetCurrentDirectory(), "dist", "public")
    if Directory.Exists(publicPath) then
        let fileProvider = new PhysicalFileProvider(publicPath)

        // Serve index.html for root path
        app.UseDefaultFiles(DefaultFilesOptions(
            FileProvider = fileProvider
        )) |> ignore

        // Serve static files
        app.UseStaticFiles(StaticFileOptions(
            FileProvider = fileProvider
        )) |> ignore

        printfn $"Serving static files from: {publicPath}"
    else
        printfn $"Static files directory not found: {publicPath}"

    // API routes
    app.UseGiraffe(Api.webApp())

    // SPA fallback: serve index.html for non-API routes
    if Directory.Exists(publicPath) then
        let indexPath = Path.Combine(publicPath, "index.html")
        if File.Exists(indexPath) then
            app.MapFallbackToFile("index.html", StaticFileOptions(
                FileProvider = new PhysicalFileProvider(publicPath)
            )) |> ignore

    printfn "Cinemarco server starting on http://localhost:5000"
    printfn "API ready at /api/ICinemarcoApi/*"

    app.Run("http://0.0.0.0:5000")
    0
