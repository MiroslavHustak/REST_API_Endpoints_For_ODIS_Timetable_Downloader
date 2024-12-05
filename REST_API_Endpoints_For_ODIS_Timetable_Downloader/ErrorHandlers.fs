namespace MyFsToolkit

open System

open Builders

[<RequireQualifiedAccess>]            
module Result = 

    let internal mapErr fOk (fErr : Lazy<'a>) =                          
        function
        | Ok value -> value |> fOk
        | Error _  -> fErr.Force()       
                   
    let internal toOption = 
        function   
        | Ok value -> Some value 
        | Error _  -> None  

    let internal fromOption = 
        function   
        | Some value -> Ok value
        | None       -> Error String.Empty  
        
    let internal sequence aListOfResults = //gets the first error - see the book Domain Modelling Made Functional

        let prepend firstR restR =
            match firstR, restR with
            | Ok first, Ok rest   -> Ok (first :: rest) | Error err1, Ok _ -> Error err1
            | Ok _, Error err2    -> Error err2
            | Error err1, Error _ -> Error err1

        let initialValue = Ok [] 
        List.foldBack prepend aListOfResults initialValue

    let internal sequence1 aListOfResults =       
        
        aListOfResults 
        |> List.choose (fun item -> item |> Result.toOption)
        |> List.length
        |> function   
            | 0 -> 
                 let err = 
                     aListOfResults 
                     |> List.map
                         (fun item ->
                                    match item with
                                    | Ok _      -> String.Empty
                                    | Error err -> err
                         )
                         |> List.tryHead //One exception or None is enough for the calculation to fail
                         |> function
                             | Some value -> value
                             | None       -> String.Empty
                 Error err
            | _ ->
                 let okList = 
                     aListOfResults 
                     |> List.map
                         (fun item -> 
                                    match item with
                                    | Ok value -> value
                                    | _        -> String.Empty 
                         )   
                 Ok okList 


[<RequireQualifiedAccess>]
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

    let internal ofNull (value : 'nullableValue) =
        match System.Object.ReferenceEquals(value, null) with //The "value" type can be even non-nullable, and ReferenceEquals will still work.
        | true  -> None
        | false -> Some value     
  
    let internal toResult err = 
        function   
        | Some value -> Ok value 
        | None       -> Error err              

    let internal ofStringOption str = 
        str
        |> Option.bind (fun item -> Option.filter (fun item -> not (item.Equals(String.Empty))) (Some (string item))) 
                             
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
                                
module Casting = 
    
    //normalne nepouzivat!!! zatim nutnost jen u deserializace xml - viz SAFE Stack app
    let internal castAs<'a> (o : obj) : 'a option =    //the :? operator in F# is used for type testing     srtp pri teto strukture nefunguje
        match Option.ofNull o with
        | Some (:? 'a as result) -> Some result
        | _                      -> None