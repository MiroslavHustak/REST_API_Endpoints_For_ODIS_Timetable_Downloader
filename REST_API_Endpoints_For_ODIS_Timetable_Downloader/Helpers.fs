namespace Helpers

open System

open MyFsToolkit.Builders

[<RequireQualifiedAccess>]
module Option =      

    let internal ofNull (value : 'nullableValue) =

        match System.Object.ReferenceEquals(value, null) with 
        | true  -> None
        | false -> Some value        
                             
    let internal ofNullEmpty (value : 'nullableValue) = //NullOrEmpty

        pyramidOfHell
            {
                let!_ = not <| System.Object.ReferenceEquals(value, null), None 
                let value = string value 
                let! _ = not <| String.IsNullOrEmpty(value), None 

                return Some value
            }

    let internal ofNullEmptySpace (value : 'nullableValue) = //NullOrEmpty, NullOrWhiteSpace
        
        pyramidOfHell
            {
                let!_ = not <| System.Object.ReferenceEquals(value, null), None 
                let value = string value 
                let! _ = not <| (String.IsNullOrEmpty(value) || String.IsNullOrWhiteSpace(value)), None
        
                return Some value
            }