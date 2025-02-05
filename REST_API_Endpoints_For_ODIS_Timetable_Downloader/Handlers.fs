namespace RestApiThothJson

//Compiler directives
#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

// REST API created with -> SATURN and GIRAFFE
// Data format           -> JSON
// Client library        -> FsHttp 
// (De)Serialization     -> Thoth.Json(.Net)

module Handlers =

    open System
    open System.IO
    open System.Data
    
    open Saturn
    open Giraffe
    open Microsoft.AspNetCore.Http    

    open Helpers
    open ThothCoders

    open MyFsToolkit
    open MyFsToolkit.Builders    
    
    // ************** GET ******************* 
           
    let internal getHandler path : HttpHandler =  //GIRAFFE       
        
        let getJsonString path =

            try
                pyramidOfDoom
                    {
                        let filepath = Path.GetFullPath path |> Option.ofNullEmpty 
                        let! filepath = filepath, Error (sprintf "%s%s" "Chyba při čtení cesty k souboru " path)

                        let fInfodat : FileInfo = FileInfo filepath
                        let! _ =  fInfodat.Exists |> Option.ofBool, Error (sprintf "Soubor %s nenalezen" path) 
                 
                        use fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None) 
                        let! _ = fs |> Option.ofNull, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " filepath)                        
                    
                        use reader = new StreamReader(fs) //For large files, StreamReader may offer better performance and memory efficiency
                        let! _ = reader |> Option.ofNull, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " filepath) 
                
                        let jsonString = reader.ReadToEnd()
                        let! jsonString = jsonString |> Option.ofNullEmpty, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " filepath)                      
                                  
                        return Ok jsonString //Thoth output is of Result type 
                    }
            with
            | ex -> Error (string ex.Message)

        fun (next : HttpFunc) (ctx : HttpContext) 
            -> 
            async
                {       
                    let response param = 
                        {
                            GetLinks = fst param 
                            Message = snd param 
                        }

                    let response = 
                        match getJsonString path with
                        | Ok jsonString -> response (jsonString, "Success") 
                        | Error err     -> response (String.Empty, err)  

                    let responseJson = Encode.toString 2 (encoderGet response) 
                    ctx.Response.ContentType <- "application/json"

                    return! text responseJson next ctx |> Async.AwaitTask  //GIRAFFE
                }
                |> Async.StartImmediateAsTask 
   
                
    // ************** PUT ******************* 
   
    let internal putHandler path : HttpHandler =   //GIRAFFE
              
        let saveJsonString (jsonString : string) path =
            
                try
                    pyramidOfDoom
                        {
                            let filepath = Path.GetFullPath path |> Option.ofNullEmpty 
                            let! filepath = filepath, Error (sprintf "%s%s" "Chyba při čtení cesty k souboru " path)
                                    
                            use writer = new StreamWriter(filepath, false)                
                            let! _ = writer |> Option.ofNull, Error (sprintf "%s%s" "Chyba při serializaci do " path)

                            writer.Write jsonString

                            return Ok ()
                        }
                with
                | ex -> Error (string ex.Message)

        fun (next : HttpFunc) (ctx : HttpContext)   //GIRAFFE
            ->
            async
                {
                    try   
                        let reader = new StreamReader(ctx.Request.Body)
                        let! body = reader.ReadToEndAsync() |> Async.AwaitTask 

                        try
                            match saveJsonString body path with
                            | Ok _     
                                ->
                                let responseJson = Encode.toString 2 (encoderPut { Message1 = "Successfully updated"; Message2 = String.Empty })
                                ctx.Response.ContentType <- "application/json" 
    
                                return! text responseJson next ctx  |> Async.AwaitTask  //GIRAFFE
                            | Error err
                                ->   
                                let responseJson = Encode.toString 2 (encoderPut { Message1 = String.Empty; Message2 = err })
                                ctx.Response.ContentType <- "application/json"
                                ctx.Response.StatusCode <- 404

                                return! text responseJson next ctx |> Async.AwaitTask //GIRAFFE
                        finally
                            reader.Dispose() 
                    with
                    | ex 
                        -> 
                        ctx.Response.StatusCode <- 400
                        return! text (sprintf "Error: %s" ex.Message) next ctx  |> Async.AwaitTask  //GIRAFFE             
                }   
            |> Async.StartImmediateAsTask 