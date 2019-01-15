namespace Giraffe.GoodRead

open Giraffe
open Microsoft.AspNetCore.Http
open System.Threading.Tasks
open Microsoft.Extensions.Options

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
