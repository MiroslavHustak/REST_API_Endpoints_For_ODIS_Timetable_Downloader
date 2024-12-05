namespace RestApiThothJson

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

type ResponseGet = 
    {
        GetLinks : string
        Message : string
    } 

type ResponsePut = 
    {
        Message1 : string
        Message2 : string
    }

module ThothCoders =   

    //**************** GET ********************

    let internal encoderGet (result : ResponseGet) = 

        Encode.object
            [
                "GetLinks", Encode.string result.GetLinks  
                "Message", Encode.string result.Message    
            ]
    
    //**************** PUT ********************

    let internal encoderPut (result : ResponsePut) = 
        
        Encode.object
            [
                "Message1", Encode.string result.Message1 
                "Message2", Encode.string result.Message2
            ]