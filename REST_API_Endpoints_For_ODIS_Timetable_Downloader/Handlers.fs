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
    let [<Literal>] private indentation = 2
    let [<Literal>] private maxFileSizeKb = 512L // Maximum file size in kilobytes 

    //*************** Helpers ****************

    let private sendResponse statusCode msg1 msg2 next (ctx : HttpContext) =
            
        let responseJson = encoderPutAndPost >> Encode.toString indentation <| { Message1 = msg1; Message2 = msg2 }
        ctx.Response.ContentType <- "application/json"
        ctx.Response.StatusCode <- statusCode
           
        text responseJson next ctx |> Async.AwaitTask   

        
    // ************** GET ******************* 

    let private getHandler<'a> path createResponse (encodeResponse : 'a -> JsonValue) : HttpHandler =  // GIRAFFE

        let getJsonStringAsync path =
            try
                pyramidOfDoom 
                    {
                        let filepath = Path.GetFullPath path |> Option.ofNullEmpty 
                        let! filepath = filepath, Error (sprintf "Chyba při čtení cesty k souboru: %s" path)

                        let fInfodat : FileInfo = FileInfo filepath
                        let! _ = fInfodat.Exists |> Option.ofBool, Error (sprintf "Soubor %s nenalezen" path) 
                 
                        let fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None) // nelze use
                        let! _ = fs |> Option.ofNull, Error (sprintf "Chyba při čtení dat ze souboru: %s" filepath)                        
                    
                        let reader = new StreamReader(fs) // use nelze pouzit v dusledku async pouzivani reader nize
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

        fun (next : HttpFunc) (ctx : HttpContext) ->
            async
                {      
                    try
                        match getJsonStringAsync path with
                        | Ok jsonStringAsync
                            ->
                            let! jsonString = jsonStringAsync
                   
                            let jsonString = 
                                jsonString 
                                |> Option.ofNullEmpty 
                                |> Option.defaultValue jsonEmpty 
                        
                            let responseJson = 
                                createResponse >> encodeResponse >> Encode.toString indentation <| (jsonString, "Success")
                                                                                                                
                            ctx.Response.ContentType <- "application/json"
                            ctx.Response.StatusCode <- 200
                                                  
                            return! text responseJson next ctx |> Async.AwaitTask // GIRAFFE

                        | Error err 
                            -> 
                            let responseJson =
                                createResponse >> encodeResponse >> Encode.toString indentation <| (jsonEmpty, err)                   

                            ctx.Response.ContentType <- "application/json"
                            ctx.Response.StatusCode <- 404

                            return! text responseJson next ctx |> Async.AwaitTask  // GIRAFFE  

                    with
                    | ex -> return! sendResponse 500 String.Empty (sprintf "Error: %s" ex.Message) next ctx  // GIRAFFE 
                }
            |> Async.StartImmediateAsTask

    let linksHandler path = 
        getHandler<ResponseGetLinks> path (fun (json, msg) -> { GetLinks = json; Message = msg }) encoderGetLinks
    
    let logEntriesHandler path = 
        getHandler<ResponseGetLogEntries> path (fun (json, msg) -> { GetLogEntries = json; Message = msg }) encoderGetLogEntries
        
        
    // ************** PUT ******************* 
   
    let internal putHandler path : HttpHandler =   //GIRAFFE
              
        let prepareJsonAsyncWrite (jsonString : string) path = // it only prepares an asynchronous operation that writes the json string
            
            try  
                pyramidOfDoom
                    {
                        let filepath = Path.GetFullPath path |> Option.ofNullEmpty 
                        let! filepath = filepath, Error (sprintf "%s%s" "Chyba při čtení cesty k souboru " path)

                        let fInfodat : FileInfo = FileInfo filepath
                        let! _ = fInfodat.Exists |> Option.ofBool, Error (sprintf "Soubor %s nenalezen" path) 
                                                      
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
                                do! writer.FlushAsync() |> Async.AwaitTask

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
                            return! sendResponse 200 "Successfully updated" String.Empty next ctx //GIRAFFE

                        | Error err
                            ->                              
                            return! sendResponse 404 String.Empty err next ctx 
                       
                    with
                    | ex -> return! sendResponse 500 String.Empty (sprintf "Error: %s" ex.Message) next ctx 

                }   
            |> Async.StartImmediateAsTask 


 // ************** POST *******************     
    
    let internal postHandler path : HttpHandler =

        let prepareJsonAsyncAppend (jsonString : string) path =
            try
                pyramidOfDoom
                    {
                        let filepath = Path.GetFullPath path |> Option.ofNullEmpty
                        let! filepath = filepath, Error (sprintf "Chyba při čtení cesty k souboru %s" path)
    
                        //pri append operation je soubor vytvoren, pokud neexistuje, proto nelze fInfodat.Exists a Error
    
                        let writer = new StreamWriter(filepath, true) // Append mode
                        let! _ = writer |> Option.ofNull, Error (sprintf "Chyba při vytváření writeru pro %s" path)
    
                        return Ok writer
                    }
                |> Result.map
                    (fun writer
                        ->
                        async
                            {
                                do! writer.WriteLineAsync jsonString |> Async.AwaitTask
                                do! writer.FlushAsync() |> Async.AwaitTask

                                return! writer.DisposeAsync().AsTask() |> Async.AwaitTask
                            }
                    )
            with
            | ex -> Error (string ex.Message)
    
        let checkFileSize path =
        
            try
                let fileInfo = FileInfo path
        
                let sizeKb = 
                    match fileInfo.Exists with
                    | true  -> fileInfo.Length / 1024L  //abychom dostali hodnotu v KB
                    | false -> 0L
                    
                match (<) sizeKb <| int64 maxFileSizeKb with
                | true  -> ()
                | false -> fileInfo.Delete()
        
                Ok sizeKb
        
            with
            | ex -> Error (sprintf "Chyba při kontrole velikosti souboru: %s" ex.Message)      
    
        fun (next : HttpFunc) (ctx : HttpContext) //GIRAFFE
            ->
            async
                {
                    try
                        use reader = new StreamReader(ctx.Request.Body)
                        let! body = reader.ReadToEndAsync() |> Async.AwaitTask
                       
                        match checkFileSize path with
                        | Ok _
                            ->                     
                            match prepareJsonAsyncAppend body path with
                            | Ok asyncWriter
                                ->
                                do! asyncWriter
                                return! sendResponse 201 "Záznam úspěšně přidán" String.Empty next ctx 

                            | Error err
                                ->
                                return! sendResponse 500 String.Empty err next ctx

                        | Error err 
                            ->
                            return! sendResponse 400 String.Empty err next ctx  

                    with
                    | ex -> return! sendResponse 500 String.Empty (sprintf "Chyba serveru: %s" ex.Message) next ctx 
                }
            |> Async.StartImmediateAsTask