namespace LinksScrapedByCanopyApi

open System
open System.IO

open Microsoft.AspNetCore.Http

open Saturn
open Giraffe

open RestApiThothJson.Handlers

open MyFsToolkit
open MyFsToolkit.InteractiveHelpers

// REST API created with -> SATURN and GIRAFFE
// Data format           -> JSON
// Client library        -> FsHttp 
// (De)Serialization     -> Thoth.Json(.Net)

//*.fsproj !!!! See below.

module Program =

    [<EntryPoint>]
    let main args =

        //let apiKey = "test747646s5d4fvasfd645654asgasga654a6g13a2fg465a4fg4a3"  //WEB API

        let apiKey = 
            System.Environment.GetEnvironmentVariable "API_KEY"
            |> Option.ofNullEmptySpace
            |> Option.defaultValue "fallback-dev-key"
        
        //let pathCanopy = "CanopyResults/canopy_results.json"  //WEB API
        //let pathJsonLinks = "jsonLinks/jsonLinks_results.json"  //WEB API
        //let pathLogEntries = "logging/logEntries.json"  //WEB API

        // Kubernetes
        let basePath =
            Environment.GetEnvironmentVariable "APP_DATA_PATH"
            |> Option.ofNullEmptySpace
            |> Option.defaultValue AppContext.BaseDirectory  

        let serviceRoot = basePath // root for this service's data // if the path logic is to be changed, use Path.Combine like Path.Combine(basePath, "canopy")       

        let pathCanopy = Path.Combine(serviceRoot, "CanopyResults", "canopy_results.json") 
        let pathJsonLinks = Path.Combine(serviceRoot, "jsonLinks", "jsonLinks_results.json") 
        let pathLogEntries = Path.Combine(serviceRoot, "logging", "logEntries.json") 
  
        let validateApiKey (next : HttpFunc) (ctx : HttpContext) =  //GIRAFFE

            task
                {
                    match ctx.Request.Headers.TryGetValue "X-API-KEY" with
                    | true, key 
                        when string key = apiKey
                            ->
                            return! next ctx
                    | _ 
                            ->
                            ctx.Response.StatusCode <- 401
                            ctx.Response.ContentType <- "application/json"
                            return! ctx.WriteJsonAsync({| message = "Unauthorized: Invalid API Key" |})
                } 
     
        let apiRouter = //SATURN //http://kodis.somee.com
                                  
            router
                { 
                    pipe_through validateApiKey //...for every request
                    get "/" (linksHandler pathCanopy)  //anebo /user atd. //to jsou jen endpoints / = api/, atd. 
                    put "/" (putHandler pathCanopy) //anebo /user atd.      
                    get "/jsonLinks" (linksHandler pathJsonLinks)  //anebo /user atd.  
                    put "/jsonLinks" (putHandler pathJsonLinks) //anebo /user atd.   tady /jsonLinks prijdou data, ktere se ulozi do jsonLinks/jsonLinks_results.json
                    get "/logging" (logEntriesHandler pathLogEntries)  
                    post "/logging" (postHandler pathLogEntries) 
                }

        let app =  //SATURN

            let url_path =   
                match isInKubernetes || isInContainer with
                | true  -> @"http://0.0.0.0:80"  //For Docker networking
                | false -> @"http://kodis.somee.com/api" 

            application
                {
                    use_router apiRouter
                    url url_path
                    memory_cache
                    use_static "static"
                    use_gzip
                }

        run app //SATURN

        //Adding a try block around run app is optional for better diagnostics during development or to customize startup error handling (e.g., logging, exit codes).
        
        0

(*
<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<AspNetCoreHostingModel>InProcess</AspNetCoreHostingModel>
		<!-- Hosting model for ASP.NET Core -->
		<OutputType>Exe</OutputType>
		<!-- Specify that it's an executable -->
	</PropertyGroup>

	<ItemGroup>
		<Compile Include="CEBuilders.fs" />
		<Compile Include="ErrorHandlers.fs" />
		<Compile Include="Helpers.fs" />
		<Compile Include="ThothCoders.fs" />
		<Compile Include="UsingThothJson.fs" />
		<Compile Include="Program.fs" />
	</ItemGroup>

	<ItemGroup>
		<PackageReference Include="Giraffe" Version="7.0.2" />
		<PackageReference Include="Saturn" Version="0.17.0" />
		<PackageReference Include="Thoth.Json.Net" Version="12.0.0" />
	</ItemGroup>

</Project>

Server Role: The server validates JSON data to ensure it’s structurally correct (e.g., { "list": [...] } for links, { "list": [{ "ErrorMessage": "..." }, ...] } for log entries) and returns it as a string in the response (e.g., ResponseGetJsonLinks or ResponseGetLogEntries). The server does not map the JSON to a domain model or enforce business rules beyond basic JSON structure validation.

Client Role: The client receives the JSON string (e.g., GetLinks or GetLogEntries), deserializes it into a DTO (e.g., a string list or LogEntry list), applies a transformation layer to validate business rules, and maps it to a client-side domain model.

*)
