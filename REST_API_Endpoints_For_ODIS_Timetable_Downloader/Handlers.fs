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
    

    let [<Literal>] private jsonEmpty = """{ "list": [] }"""

    let [<Literal>] private identation = 2

        
    // ************** GET ******************* 
           
    let internal getHandler path : HttpHandler =  //GIRAFFE       
        
        let getJsonStringAsync path =

            try
                pyramidOfDoom 
                    {
                        let filepath = Path.GetFullPath path |> Option.ofNullEmpty 
                        let! filepath = filepath, Error (sprintf "Chyba při čtení cesty k souboru: %s" path)

                        let fInfodat : FileInfo = FileInfo filepath
                        let! _ = fInfodat.Exists |> Option.ofBool, Error (sprintf "Soubor %s nenalezen" path) 
                     
                        let fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None) //nelze use
                        let! _ = fs |> Option.ofNull, Error (sprintf "Chyba při čtení dat ze souboru: %s" filepath)                        
                        
                        let reader = new StreamReader(fs) //use nelze pouzit v dusledku async pouzivani reader nize
                        let! _ = reader |> Option.ofNull, Error (sprintf "Chyba při čtení dat ze souboru: %s" filepath) 
                    
                        return Ok (reader, fs)
                    }

                |> Result.map 
                    (fun (reader, fs) 
                        -> 
                        async
                            { 
                                let! json = reader.ReadToEndAsync() |> Async.AwaitTask
                                
                                reader.Dispose()
                                do! fs.DisposeAsync().AsTask() |> Async.AwaitTask

                                return json 
                            }
                    ) 
                
            with
            | ex -> Error (string ex.Message)

        fun (next : HttpFunc) (ctx : HttpContext)
            -> 
            async
                {      
                    try
                        let response param = 
                            {
                                GetLinks = fst param 
                                Message = snd param 
                            }
                                           
                        match getJsonStringAsync path with
                        | Ok jsonStringAsync
                            -> 
                            let! jsonString = jsonStringAsync
                       
                            let jsonString = 
                                jsonString 
                                |> Option.ofNull 
                                |> Option.defaultValue jsonEmpty 
                            
                            let responseJson = response >> encoderGet >> Encode.toString identation <| (jsonString, "Success")
                                                                                     
                            ctx.Response.ContentType <- "application/json"
                                                      
                            return! text responseJson next ctx |> Async.AwaitTask //GIRAFFE

                        | Error err    
                            -> 
                            let responseJson = response >> encoderGet >> Encode.toString identation <| (jsonEmpty, err) 

                            ctx.Response.ContentType <- "application/json"

                            return! text responseJson next ctx |> Async.AwaitTask  //GIRAFFE  
                    with
                    | ex
                        ->
                        ctx.Response.StatusCode <- 400
                        return! text (sprintf "Error: %s" ex.Message) next ctx |> Async.AwaitTask  //GIRAFFE  
                }
            |> Async.StartImmediateAsTask 
   
                
    // ************** PUT ******************* 
   
    let internal putHandler path : HttpHandler =   //GIRAFFE
              
        let prepareJsonAsyncWrite (jsonString : string) path = // it only prepares an asynchronous operation that writes the json string
            
            try  
                pyramidOfDoom
                    {
                        let filepath = Path.GetFullPath path |> Option.ofNullEmpty 
                        let! filepath = filepath, Error (sprintf "%s%s" "Chyba při čtení cesty k souboru " path)
                                                      
                        let writer = new StreamWriter(filepath, false)                
                        let! _ = writer |> Option.ofNull, Error (sprintf "%s%s" "Chyba při serializaci do " path)
                                                                          
                        return Ok writer
                    }         
                        
                |> Result.map 
                    (fun writer 
                        -> 
                        async
                            {
                                do! writer.WriteAsync jsonString |> Async.AwaitTask
                                return! writer.DisposeAsync().AsTask() |> Async.AwaitTask 
                            }
                    ) 
                       
            with
            | ex -> Error (string ex.Message)
                       
        fun (next : HttpFunc) (ctx : HttpContext)   //GIRAFFE
            ->
            async
                {
                    try   
                        use reader = new StreamReader(ctx.Request.Body)
                        let! body = reader.ReadToEndAsync() |> Async.AwaitTask 
                       
                        match prepareJsonAsyncWrite body path with
                        | Ok asyncWriter     
                            ->
                            do! asyncWriter

                            let responseJson = encoderPut >> Encode.toString identation <| { Message1 = "Successfully updated"; Message2 = String.Empty }

                            ctx.Response.ContentType <- "application/json" 
    
                            return! text responseJson next ctx |> Async.AwaitTask  //GIRAFFE

                        | Error err
                            ->   
                            let responseJson = encoderPut >> Encode.toString identation <| { Message1 = String.Empty; Message2 = err }

                            ctx.Response.ContentType <- "application/json"
                            ctx.Response.StatusCode <- 404

                            return! text responseJson next ctx |> Async.AwaitTask //GIRAFFE
                       
                    with
                    | ex 
                        -> 
                        ctx.Response.StatusCode <- 400
                        return! text (sprintf "Error: %s" ex.Message) next ctx  |> Async.AwaitTask  //GIRAFFE             
                }   
            |> Async.StartImmediateAsTask 