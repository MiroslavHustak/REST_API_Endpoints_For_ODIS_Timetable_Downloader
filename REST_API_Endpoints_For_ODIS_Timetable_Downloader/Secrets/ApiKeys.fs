module ApiKeys

open System
open System.IO
open FsToolkit.ErrorHandling

open Thoth.Json.Net

type Secrets =
    {
        ApiKey : string
    }

module Secrets =

    let private decoder : Decoder<Secrets> =
        Decode.object
            (fun get
                ->
                {
                    ApiKey = get.Required.Field "ApiKey" Decode.string
                }
            )

    let internal loadApiKey (path : string) : Result<Secrets, string> =
        try
            let fullPath = Path.Combine(AppContext.BaseDirectory, path) //AppContext.BaseDirectory always points to where your compiled app lives, regardless of what the process working directory happens to be
            let json = System.IO.File.ReadAllText fullPath
            Decode.fromString decoder json
        with
        | ex -> Error (sprintf "Failed to read secrets file: %s" ex.Message)

    (*
    Do <ItemGroup>
    pridej
    <Content Update="Secrets\secrets.json">
    	<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>    
    *)