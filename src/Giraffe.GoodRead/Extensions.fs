namespace Giraffe.GoodRead

open System
open Giraffe
open Microsoft.AspNetCore.Http
open System.Threading.Tasks
open System.Globalization

[<AutoOpen>]
module Extensions =

    let internal context (contextMap : HttpContext -> HttpHandler) : HttpHandler =
      fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
           let createdHandler = contextMap ctx
           return! createdHandler next ctx
        }

    let internal contextTask (contextMap : HttpContext -> Task<HttpHandler>) : HttpHandler =
      fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
           let! createdHandler = contextMap ctx
           return! createdHandler next ctx
        }

    let internal contextAsync (contextMap : HttpContext -> Async<HttpHandler>) : HttpHandler =
      fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
           let! createdHandler = contextMap ctx
           return! createdHandler next ctx
        }

    let plainText (input: string) : HttpHandler =
        setHttpHeader "Content-Type" "text/plain"
        >=> setBodyFromString input

module Async =
    let Try value =
        async {
            let! catched = Async.Catch value
            match catched with
            | Choice1Of2 value -> return Ok value
            | Choice2Of2 err -> return Error err
        }
