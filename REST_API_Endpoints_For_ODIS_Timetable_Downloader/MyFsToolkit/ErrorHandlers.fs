namespace MyFsToolkit

open System

open Builders

//***********************************
            
module Result =    
            
    let internal sequence aListOfResults = //gets the first error - see the book Domain Modelling Made Functional
        let prepend firstR restR =
            match firstR, restR with
            | Ok first, Ok rest   -> Ok (first :: rest) | Error err1, Ok _ -> Error err1
            | Ok _, Error err2    -> Error err2
            | Error err1, Error _ -> Error err1

        let initialValue = Ok [] 
        List.foldBack prepend aListOfResults initialValue  

    let internal fromOption = 
        function   
        | Some value -> Ok value
        | None       -> Error String.Empty  

    let internal toOption = 
        function   
        | Ok value -> Some value 
        | Error _  -> None  

    let internal fromBool ok err =                               
        function   
        | true  -> Ok ok  
        | false -> Error err

    (*
    let defaultWith defaultFn res =
        match res with
        | Ok value  -> value
        | Error err -> defaultFn err 
        
    let defaultValue default res =
        match res with
        | Ok value -> value
        | Error _  -> default
        
    let map f res =
        match res with
        | Ok value  -> Ok (f value)
        | Error err -> Error err

    let mapError f res =
        match res with
        | Ok value  -> Ok value
        | Error err -> Error (f err)

    let bind f res =
        match res with
        | Ok value  -> f value
        | Error err -> Error err
    *)
  
module Option =

    let internal ofBool =                           
        function   
        | true  -> Some ()  
        | false -> None

    let internal toBool = 
        function   
        | Some _ -> true
        | None   -> false

    let internal fromBool value =                               
        function   
        | true  -> Some value  
        | false -> None
            
    let internal ofNull' (value : 'nullableValue) =
        match System.Object.ReferenceEquals(box value, null) with 
        | true  -> None
        | false -> Some value     

    let inline internal ofPtrOrNull (value : 'nullableValue) =  
        match System.Object.ReferenceEquals(box value, null) with 
        | true  ->
                None
        | false -> 
                match box value with
                | null 
                    -> None
                | :? IntPtr as ptr 
                    when ptr = IntPtr.Zero
                    -> None
                | _   
                    -> Some value          
    
    let inline internal ofNullEmpty (value : 'nullableValue) : string option = //NullOrEmpty
        pyramidOfDoom 
            {
                let!_ = (not <| System.Object.ReferenceEquals(box value, null)) |> fromBool value, None 
                let value = string value 
                let! _ = (not <| String.IsNullOrEmpty value) |> fromBool value, None //IsNullOrEmpty is not for nullable types

                return Some value
            }

    let inline internal ofNullEmptySpace (value : 'nullableValue) = //NullOrEmpty, NullOrWhiteSpace
        pyramidOfDoom //nelze option {}
            {
                let!_ = (not <| System.Object.ReferenceEquals(box value, null)) |> fromBool Some, None 
                let value = string value 
                let! _ = (not <| String.IsNullOrWhiteSpace(value)) |> fromBool Some, None
       
                return Some value
            }

    let internal toResult err = 
        function   
        | Some value -> Ok value 
        | None       -> Error err     

    (*
    //FsToolkit
    let internal toResult (error : 'error) (opt : 'value option) : Result<'value, 'error> =

        match opt with
        | Some value -> Result.Ok value
        | None       -> Result.Error error    
    *)
                                
module Casting = 
    
    //normalne nepouzivat!!! zatim nutnost jen u deserializace xml - viz SAFE Stack app
    let internal castAs<'a> (o : obj) : 'a option =    //the :? operator in F# is used for type testing     srtp pri teto strukture nefunguje
        match Option.ofNull' o with
        | Some (:? 'a as result) -> Some result
        | _                      -> None