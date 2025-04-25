namespace RestApiThothJson

open System

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

type ResponseGetLinks = 
    {
        GetLinks : string
        Message : string
    } 

type ResponseGetLogEntries = 
    {
        GetLogEntries : string
        Message : string
    } 

type ResponsePutAndPost = 
    {
        Message1 : string
        Message2 : string
    }

//zatim nepouzivano
type LogEntry =
    {       
        ErrorMessage : string
        Timestamp : string
    }

module ThothCoders =   

    let internal encoderGetLinks (result : ResponseGetLinks) = 

        Encode.object
            [
                "GetLinks", Encode.string result.GetLinks  
                "Message", Encode.string result.Message    
            ]   

    let internal encoderGetLogEntries (result : ResponseGetLogEntries) = 
        
        Encode.object
            [
                "GetLogEntries", Encode.string result.GetLogEntries  
                "Message", Encode.string result.Message    
            ]   
                   
    let internal encoderPutAndPost (result : ResponsePutAndPost) = //quli jednotnosti, ale pri post messages zatim nijak akutne nepotrebuji
        
        Encode.object
            [
                "Message1", Encode.string result.Message1 
                "Message2", Encode.string result.Message2
            ]  
    
    //zatim nepouzivano
    let internal logEntryEncoder (result : LogEntry) = 

       Encode.object
            [               
                "ErrorMessage", Encode.string result.ErrorMessage
                "Timestamp", Encode.string result.Timestamp 
            ] 

    //zatim nepouzivano
    let internal logEntryDecoder : Decoder<LogEntry> =

        Decode.object
            (fun get
                ->
                {
                    ErrorMessage = get.Required.Field "ErrorMessage" Decode.string 
                    Timestamp = get.Required.Field "Timestamp" Decode.string 
                }
            )

    //zatim nepouzivano
    let internal responsePutDecoder : Decoder<ResponseGetLinks> =
        
        Decode.object
            (fun get
                ->
                {
                    GetLinks = get.Required.Field "GetLinks" Decode.string
                    Message = get.Required.Field "Message" Decode.string
                }
            )

   
  
    //{ "list": [{ "ErrorMessage": "error1" }, { "ErrorMessage": "error2" }, ...] }