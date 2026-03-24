namespace MyFsToolkit

module InteractiveHelpers =

    let internal isInKubernetes = 
        System.Environment.GetEnvironmentVariable "KUBERNETES_SERVICE_HOST"
        |> Option.ofNullEmptySpace
        |> Option.isSome

    let internal isInContainer = 
        System.Environment.GetEnvironmentVariable "DOTNET_RUNNING_IN_CONTAINER"
        |> Option.ofNullEmptySpace
        |> Option.isSome

