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
   
    open Giraffe
    open FsToolkit.ErrorHandling
    open Microsoft.AspNetCore.Http      

    open ThothCoders
    open MyFsToolkit

    let [<Literal>] private jsonEmpty = """{ "list": [] }"""
    let [<Literal>] private indentation = 2
    let [<Literal>] private maxFileSizeKb = 10L // Maximum file size in kilobytes 

    //*************** Helpers ****************

    type private GetError =
        | GetInvalidPath of string
        | GetReadFailed of string

    type private PutError =
        | PutInvalidPath of string
        | PutWriteFailed of string

    type private PostError =
        | SizeExceeded of string
        | ServerError of string
        | PostWriteFailed of string    

    let private sendResponse statusCode msg1 msg2 next (ctx: HttpContext) =
            
        let responseJson =
            encoderPutAndPost >> Encode.toString indentation <| { Message1 = msg1; Message2 = msg2 }
        
        ctx.Response.ContentType <- "application/json"
        ctx.Response.StatusCode <- statusCode
           
        text responseJson next ctx |> Async.AwaitTask   

        
    // ************** GET ******************* 

    let private getHandler<'a> path createResponse (encodeResponse : 'a -> JsonValue) : HttpHandler = //like SELECT in SQL

        fun (next: HttpFunc) (ctx: HttpContext)
            ->
            async 
                {
                    try
                        let readJsonAsync () =

                            asyncResult
                                {
                                    let! fullPath =
                                        Path.GetFullPath path
                                        |> Option.ofNullEmpty
                                        |> Option.toResult (GetInvalidPath (sprintf "%s%s" "Chyba při čtení cesty k souboru " path))           
                                    
                                    try
                                        use fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                                        use reader = new StreamReader(fs)                                       
    
                                        let! content = reader.ReadToEndAsync() |> Async.AwaitTask |> Async.map Ok //See Excel -> DB for explanation

                                        return 
                                            content
                                            |> Option.ofNullEmpty
                                            |> Option.defaultValue jsonEmpty
                                    with
                                    | ex -> return! Error (GetReadFailed (sprintf "Chyba při čtení ze souboru: %s" (string ex.Message))) 
                                }
                            |> AsyncResult.catch (fun ex -> GetReadFailed <| string ex.Message)

                        match! readJsonAsync () with
                        | Ok jsonString
                            ->                                      
                            let jsonString = 
                                jsonString 
                                |> Option.ofNullEmpty 
                                |> Option.defaultValue jsonEmpty 
                                           
                            let responseJson = 
                                createResponse >> encodeResponse >> Encode.toString indentation <| (jsonString, "Success")
                                                                                                                                   
                            ctx.Response.ContentType <- "application/json"
                            ctx.Response.StatusCode <- 200
                                                                     
                            return! text responseJson next ctx |> Async.AwaitTask // GIRAFFE

                        | Error _ 
                            -> 
                            let responseJson =
                                createResponse >> encodeResponse >> Encode.toString indentation <| (jsonEmpty, "Failure")                   

                            ctx.Response.ContentType <- "application/json"
                            ctx.Response.StatusCode <- 404

                            return! text responseJson next ctx |> Async.AwaitTask  // GIRAFFE
    
                    with
                    | ex -> return! sendResponse 500 String.Empty (sprintf "Error: %s" ex.Message) next ctx
                }   
            |> Async.StartImmediateAsTask

    let linksHandler path = 
        getHandler<ResponseGetLinks> path (fun (json, msg) -> { GetLinks = json; Message = msg }) encoderGetLinks
    
    let logEntriesHandler path = 
        getHandler<ResponseGetLogEntries> path (fun (json, msg) -> { GetLogEntries = json; Message = msg }) encoderGetLogEntries
        
        
    // ************** PUT ******************* 

    let internal putHandler path : HttpHandler = //for links updating  //like INSERT INTO OR UPDATE in SQL
       
        fun (next: HttpFunc) (ctx: HttpContext) //GIRAFFE
            ->  
            async 
                {
                    try
                        use reader = new StreamReader(ctx.Request.Body)
                        let! body = reader.ReadToEndAsync() |> Async.AwaitTask 
    
                        let writeJsonAsync (jsonString: string) =

                            asyncResult 
                                {
                                    let! fullPath =
                                        Path.GetFullPath path
                                        |> Option.ofNullEmpty
                                        |> Option.toResult (PutInvalidPath (sprintf "%s%s" "Chyba při čtení cesty k souboru " path))                           
                                    
                                    try
                                        use writer = new StreamWriter(fullPath, append = false)
                                        do! writer.WriteAsync jsonString |> Async.AwaitTask |> Async.map Ok //See Excel -> DB for explanation
                                        do! writer.FlushAsync() |> Async.AwaitTask |> Async.map Ok //See Excel -> DB for explanation
    
                                        return ()
                                    with
                                    | ex -> return! Error (PutWriteFailed (sprintf "Chyba při zápisu do souboru: %s" (string ex.Message))) 
                                }
                            |> AsyncResult.catch (fun ex -> PutWriteFailed <| string ex.Message)

                        match! writeJsonAsync body with
                        | Ok ()
                            -> return! sendResponse 200 "Successfully updated" String.Empty next ctx

                        | Error (PutInvalidPath msg) 
                            -> return! sendResponse 400 String.Empty msg next ctx

                        | Error (PutWriteFailed msg) 
                            -> return! sendResponse 500 String.Empty msg next ctx
    
                    with
                    | ex -> return! sendResponse 500 String.Empty (sprintf "Error: %s" ex.Message) next ctx 
                }                 
            |> Async.StartImmediateAsTask 
     
// ************** POST *******************    
 
    let internal postHandler path : HttpHandler = //for log entries appending //like INSERT INTO in SQL
    
        let getCurrentSizeKb (fullPath: string) : Result<int64, PostError> =
            try
                let fi = FileInfo fullPath
                Ok (fi.Exists |> function true -> fi.Length / 1024L | false -> 0L)
            with
            | ex -> Error (ServerError (sprintf "Chyba při čtení velikosti souboru: %s" (string ex.Message)))
    
        let truncateFile (fullPath : string) : Async<Result<unit, PostError>> =

            async
                {
                    try
                        do! File.WriteAllBytesAsync(fullPath, Array.empty<byte>) |> Async.AwaitTask
                        return Ok ()
                    with
                    | ex -> return Error (ServerError (sprintf "Chyba při ořezávání souboru: %s" (string ex.Message)))
                }
    
        fun (next: HttpFunc) (ctx: HttpContext) 
            ->
            async 
                {
                    try
                        use reader = new StreamReader(ctx.Request.Body)
                        let! body = reader.ReadToEndAsync() |> Async.AwaitTask
    
                        // Accurate byte estimation
                        let estimatedNewBytes = 
                            (System.Text.Encoding.UTF8.GetByteCount body |> int64) + 100L  // margin for newline + formatting
    
                        let appendJsonAsync (jsonString: string) =

                            asyncResult 
                                {
                                    let! fullPath =
                                        Path.GetFullPath path
                                        |> Option.ofNullEmpty
                                        |> Option.toResult (ServerError (sprintf "Chyba při čtení cesty k souboru: %s" path))
    
                                    let! currentSizeKb = getCurrentSizeKb fullPath    
                                    let estimatedTotalKb = currentSizeKb + (estimatedNewBytes / 1024L)
    
                                    match estimatedTotalKb >= int64 maxFileSizeKb with
                                    | true  ->
                                            do! truncateFile fullPath                                          
                                    | false ->
                                            () 
    
                                    try
                                        use writer = new StreamWriter(fullPath, append = true)
                                        do! writer.WriteLineAsync jsonString |> Async.AwaitTask |> Async.map Ok //See Excel -> DB for explanation
                                        do! writer.FlushAsync() |> Async.AwaitTask |> Async.map Ok //See Excel -> DB for explanation
                                        return ()
                                    with
                                    | ex -> return! Error (PostWriteFailed (sprintf "Chyba při zápisu do souboru: %s" ex.Message))
                                }
                            |> AsyncResult.catch (fun ex -> PostWriteFailed <| string ex.Message)  
    
                        match! appendJsonAsync body with
                        | Ok () 
                            -> return! sendResponse 201 "Záznam úspěšně přidán" String.Empty next ctx
    
                        | Error (ServerError msg)
                            -> return! sendResponse 500 String.Empty msg next ctx
    
                        | Error (PostWriteFailed msg) 
                            -> return! sendResponse 500 String.Empty msg next ctx

                        | Error (SizeExceeded msg)
                            -> return! sendResponse 413 String.Empty msg next ctx // SizeExceeded no longer used, but kept for completeness

                    with
                    | ex -> return! sendResponse 500 String.Empty (sprintf "Chyba serveru: %s" ex.Message) next ctx
                }
            |> Async.StartImmediateAsTask    