namespace LinksScrapedByCanopyApi

open System
open System.IO
open System.Data

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting

open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection

open Saturn
open Giraffe

open Helpers
open RestApiThothJson.Handlers

// REST API created with -> SATURN and GIRAFFE
// Data format           -> JSON
// Client library        -> FsHttp 
// (De)Serialization     -> Thoth.Json(.Net)

//*.fsproj !!!! See below.

module Program =

    [<EntryPoint>]
    let main args =

        let apiKey = "test747646s5d4fvasfd645654asgasga654a6g13a2fg465a4fg4a3"  

        let pathCanopy = "CanopyResults/canopy_results.json"
        let pathJsonLinks = "CanopyResults/jsonLinks_results.json"

        let validateApiKey (next : HttpFunc) (ctx : HttpContext) =  //GIRAFFE
                     
            match ctx.Request.Headers.TryGetValue("X-API-KEY") with
            | true, key 
                when string key = apiKey 
                    -> 
                    next ctx  
            | _     ->
                    ctx.Response.StatusCode <- 401
                    ctx.Response.WriteAsync("Unauthorized: Invalid API Key") |> ignore
                    System.Threading.Tasks.Task.FromResult<HttpContext option>(None) // API key is missing or invalid

        let apiRouter = //SATURN //http://kodis.somee.com

            router
                { 
                    pipe_through validateApiKey //...for every request
                    get "/" (getHandler pathCanopy)  //anebo /user atd.
                    put "/" (putHandler pathCanopy) //anebo /user atd.      
                    get "/jsonLinks" (getHandler pathJsonLinks)  //anebo /user atd.
                    put "/jsonLinks" (putHandler pathJsonLinks) //anebo /user atd.     
                }

        let app =  //SATURN

            application
                {
                    use_router apiRouter
                    url "http://kodis.somee.com/api"
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
*)
