namespace MyFsToolkit
       
module Builders =

    [<Struct>]
    type internal MyBuilder4 = MyBuilder4 with  
        member _.Recover(m, nextFunc) = //neni monada, nesplnuje vsechny 3 monadicke zakony (left/right identity, associativity)   
            match m with
            | (Ok v, _)           
                -> nextFunc v 
            | (Error err, handler) 
                -> async { return () }, handler err

        member inline this.Bind(m, f) = this.Recover(m, f) //an alias to prevent confusion
        
        member _.Zero () = ()       
        member _.Return x = x
        member _.ReturnFrom x = x     
           
    let internal pyramidOfAsyncInferno = MyBuilder4 

    //**************************************************************************************

    [<Struct>]
    type internal MyBuilder3 = MyBuilder3 with   
        member _.Recover(m, nextFunc) = //neni monada, nesplnuje vsechny 3 monadicke zakony   
            match m with
            | (Ok v, _)           
                -> nextFunc v 
            | (Error err, handler) 
                -> handler err

        member inline this.Bind(m, f) = this.Recover(m, f) //an alias to prevent confusion
        
        member _.Zero () = ()       
        member _.Return x = x
        member _.ReturnFrom x = x     
        
    let internal pyramidOfInferno = MyBuilder3             
     
    //**************************************************************************************

    [<Struct>]
    type internal MyBuilder = MyBuilder with    
        member _.Bind(m : bool * (unit -> 'a), nextFunc : unit -> 'a) : 'a =
              match m with
              | (false, handleFalse)
                  -> handleFalse()
              | (true, _)
                  -> nextFunc()    
          member _.Return x : 'a = x   
          member _.ReturnFrom x : 'a = x 
          member _.Using(x : 'a, _body: 'a -> 'b) : 'b = _body x    
          member _.Delay(f : unit -> 'a) = f()
          member _.Zero() = ()   

    let internal pyramidOfHell = MyBuilder

    //**************************************************************************************

    [<Struct>]
    type internal Builder2 = Builder2 with    
        member _.Bind((m, recovery), nextFunc) =
            match m with
            | Some v -> nextFunc v
            | None   -> recovery    
        member _.Return x : 'a = x   
        member _.ReturnFrom x : 'a = x
        member _.TryFinally(body, compensation) =
            try body()
            finally compensation()
        member _.Zero () = ()
        member _.Using(resource, binder) =
            use r = resource
            binder r
    
    let internal pyramidOfDoom = Builder2