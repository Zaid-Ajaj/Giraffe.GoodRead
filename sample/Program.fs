open System
open System.Threading.Tasks
open Giraffe
open Giraffe.GoodRead
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

type Maybe<'a> = 
  | Nothing
  | Just of 'a

type Rec = { First: string; Job: Maybe<string> }

let webApp : HttpHandler = 
  choose [ GET >=> route "/index" >=> text "Index"
           POST >=> route "/echo" >=> text "Echo" ]

type Startup() =
    member __.ConfigureServices (services : IServiceCollection) =
        // Register default Giraffe dependencies
        services.AddGiraffe() |> ignore

    member __.Configure (app : IApplicationBuilder)
                        (env : IHostingEnvironment)
                        (loggerFactory : ILoggerFactory) =
        // Add Giraffe to the ASP.NET Core pipeline
        app.UseGiraffe webApp

[<EntryPoint>]
let main _ =
    0
