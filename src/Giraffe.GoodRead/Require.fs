namespace Giraffe.GoodRead

open Giraffe
open Microsoft.AspNetCore.Http
open System.Threading.Tasks

[<AutoOpen>]
module RequireImpl =

    let require<'t> = ReaderBuilder<HttpContext, 't>()

    let getService<'t> (context : HttpContext) : 't =
        if typeof<'t>.GUID = typeof<HttpContext>.GUID
        then context |> unbox<'t>
        else context.GetService<'t>()

    let service<'t>() = Reader (fun (httpContext: HttpContext) -> getService<'t>(httpContext))

type Require() =
    static member apply(inputReader : Reader<HttpContext, HttpHandler>) : HttpHandler =
        context (fun env -> Reader.run env inputReader)

    static member apply(inputReader : Reader<HttpContext, Task<HttpHandler>>) : HttpHandler =
        contextTask (fun env -> Reader.run env inputReader)

    static member apply(inputReader : Reader<HttpContext, Async<HttpHandler>>) : HttpHandler =
        contextAsync (fun env -> Reader.run env inputReader)

    static member apply<'t>(inputReader : Reader<HttpContext, Async<'t>>, mapOutput: 't -> HttpHandler) : HttpHandler =
        Require.apply(require {
            let! asyncValue = inputReader
            return async {
                let! unwrapped = asyncValue
                return mapOutput unwrapped
            }
        })

    static member apply<'t>(inputReader : Reader<HttpContext, Task<'t>>, mapOutput: 't -> HttpHandler) : HttpHandler =
        Require.apply(require {
            let! asyncValue = inputReader
            return async {
                let! unwrapped = Async.AwaitTask asyncValue
                return mapOutput unwrapped
            }
        })

    static member services<'t>(map: 't -> HttpHandler) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            return map first
        })

    static member services<'t, 'u>(map: 't -> 'u -> HttpHandler) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            let! second = service<'u>()
            return map first second
        })

    static member services<'t, 'u, 'w>(map: 't -> 'u -> 'w -> HttpHandler) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            let! second = service<'u>()
            let! third = service<'w>()
            return map first second third
        })

    static member services<'t, 'u, 'w, 'z>(map: 't -> 'u -> 'w -> 'z -> HttpHandler) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            let! second = service<'u>()
            let! third = service<'w>()
            let! forth = service<'z>()
            return map first second third forth
        })

    static member services<'t>(map: 't -> Async<HttpHandler>) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            return map first
        })

    static member services<'t>(map: 't -> Task<HttpHandler>) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            return map first
        })

    static member services<'t, 'u>(map: 't -> 'u -> Async<HttpHandler>) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            let! second = service<'u>()
            return map first second
        })

    static member services<'t, 'u>(map: 't -> 'u -> Task<HttpHandler>) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            let! second = service<'u>()
            return map first second
        })

    static member services<'t, 'u, 'w>(map: 't -> 'u -> 'w -> Task<HttpHandler>) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            let! second = service<'u>()
            let! third = service<'w>()
            return map first second third
        })

    static member services<'t, 'u, 'w>(map: 't -> 'u -> 'w -> Async<HttpHandler>) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            let! second = service<'u>()
            let! third = service<'w>()
            return map first second third
        })

    static member services<'t, 'u, 'w, 'z>(map: 't -> 'u -> 'w -> 'z -> Task<HttpHandler>) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            let! second = service<'u>()
            let! third = service<'w>()
            let! forth = service<'z>()
            return map first second third forth
        })

    static member services<'t, 'u, 'w, 'z>(map: 't -> 'u -> 'w -> 'z -> Async<HttpHandler>) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            let! second = service<'u>()
            let! third = service<'w>()
            let! forth = service<'z>()
            return map first second third forth
        })

    static member services<'t, 'u>(map: 't -> Async<'u>, toHandler: 'u -> HttpHandler) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            return async {
                let! result = map first
                return toHandler result
            }
        })

    static member services<'t, 'u>(map: 't -> Task<'u>, toHandler: 'u -> HttpHandler) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            return async {
                let! result = Async.AwaitTask (map first)
                return toHandler result
            }
        })

    static member services<'t, 'u, 'w>(map: 't -> 'u -> Async<'w>, toHandler: 'w -> HttpHandler) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            let! second = service<'u>()
            return async {
                let! result = map first second
                return toHandler result
            }
        })

    static member services<'t, 'u, 'w>(map: 't -> 'u -> Task<'w>, toHandler: 'w -> HttpHandler) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            let! second = service<'u>()
            return async {
                let! result = Async.AwaitTask (map first second)
                return toHandler result
            }
        })

    static member services<'t, 'u, 'w, 'z>(map: 't -> 'u -> 'w -> Async<'z>, toHandler: 'z -> HttpHandler) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            let! second = service<'u>()
            let! third = service<'w>()
            return async {
                let! result = map first second third
                return toHandler result
            }
        })

    static member services<'t, 'u, 'w, 'z>(map: 't -> 'u -> 'w -> Task<'z>, toHandler: 'z -> HttpHandler) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            let! second = service<'u>()
            let! third = service<'w>()
            return async {
                let! result = Async.AwaitTask (map first second third)
                return toHandler result
            }
        })

    static member services<'t, 'u, 'w, 'z, 'y>(map: 't -> 'u -> 'w -> 'z -> Async<'y>, toHandler: 'y -> HttpHandler) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            let! second = service<'u>()
            let! third = service<'w>()
            let! forth = service<'z>()
            return async {
                let! result = map first second third forth
                return toHandler result
            }
        })

    static member services<'t, 'u, 'w, 'z, 'y>(map: 't -> 'u -> 'w -> 'z -> Task<'y>, toHandler: 'y -> HttpHandler) : HttpHandler =
        Require.apply(require {
            let! first = service<'t>()
            let! second = service<'u>()
            let! third = service<'w>()
            let! forth = service<'z>()
            return async {
                let! result = Async.AwaitTask (map first second third forth)
                return toHandler result
            }
        })
