namespace Helpers

open System

[<Struct>]
type internal PyramidOfDoom = PyramidOfDoom with    
    member _.Bind((optionExpr, err), nextFunc) =
        match optionExpr with
        | Some value -> nextFunc value 
        | _          -> err  
    member _.Return x : 'a = x   
    member _.ReturnFrom x : 'a = x 
    member _.TryFinally(body, compensation) =
        try body()
        finally compensation()
    member _.Zero () = ()
    member _.Using(resource, binder) =
        use r = resource
        binder r

[<Struct>]
type internal MyBuilder = MyBuilder with    
     member _.Bind(condition, nextFunc) =
         match fst condition with
         | false -> snd condition
         | true  -> nextFunc()  
     member _.Return x = x
     member _.Using x = x


[<RequireQualifiedAccess>]
module Option =       

    let internal pyramidOfHell = MyBuilder

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

